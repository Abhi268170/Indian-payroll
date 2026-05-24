using FluentValidation;
using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record RemoveOneTimeEarningCommand(Guid RunId, Guid EmployeeId, Guid BreakdownId, Guid ActorId) : IRequest;

public sealed class RemoveOneTimeEarningCommandValidator : AbstractValidator<RemoveOneTimeEarningCommand>
{
    public RemoveOneTimeEarningCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BreakdownId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class RemoveOneTimeEarningHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IPayrollRecomputeService recomputeService,
    IPayrollCostCalculator costCalculator,
    IUnitOfWork uow)
    : IRequestHandler<RemoveOneTimeEarningCommand>
{
    public async Task Handle(RemoveOneTimeEarningCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        var allBreakdowns = await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        var target = allBreakdowns.FirstOrDefault(b => b.Id == req.BreakdownId)
            ?? throw new NotFoundException($"Breakdown row {req.BreakdownId} not found.");

        if (!target.IsOneTimeEarning)
            throw new InvalidOperationException("Only one-time earnings can be removed.");

        breakdownRepo.Remove(target);
        await uow.SaveChangesAsync(ct);

        RecomputeResult recompute = await recomputeService.RecomputeEmployeeAsync(req.RunId, req.EmployeeId, ct);
        var result = recompute.Engine;

        payrunEmp.UpdateComputedAmounts(
            grossPay: result.Gross.GrossWage,
            netPay: recompute.NetPayWithAdjustments,
            taxesAmount: result.TDS.MonthlyTDS + result.PT.Amount,
            benefitsAmount: result.PF.EPFEmployerContribution + result.ESI.EmployerContribution,
            reimbursementsAmount: recompute.ReimbursementsAmount,
            employeePf: result.PF.EmployeeContribution,
            employerPf: result.PF.EPFEmployerContribution,
            employeeEsi: result.ESI.EmployeeContribution,
            employerEsi: result.ESI.EmployerContribution,
            ptAmount: result.PT.Amount,
            tdsAmount: result.TDS.MonthlyTDS,
            lwfEmployeeAmount: result.LWF.EmployeeAmount,
            lwfEmployerAmount: result.LWF.EmployerAmount,
            gratuityAmount: result.Gratuity.MonthlyAccrual,
            epsAmount: result.PF.EPSEmployerContribution,
            monthlyCTC: payrunEmp.MonthlyCTC,
            actorId: req.ActorId);
        payrunEmployeeRepo.Update(payrunEmp);

        var allEmployees = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);
        var activeEmployees = allEmployees.Where(e => e.Status == PayrunEmployeeStatus.Active).ToList();
        var snapshot = costCalculator.Calculate(activeEmployees);
        run.UpdateFinancialSummary(
            payrollCost: snapshot.PayrollCost,
            totalNetPay: snapshot.TotalNet,
            totalEmployerPf: snapshot.TotalEmployerPf,
            totalEmployerEsi: snapshot.TotalEmployerEsi,
            totalTds: snapshot.TotalTds,
            totalPt: snapshot.TotalPt,
            employeeCount: snapshot.EmployeeCount,
            actorId: req.ActorId);
        runRepo.Update(run);

        await uow.SaveChangesAsync(ct);
    }
}
