using FluentValidation;
using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine;
using Payroll.Engine.Inputs;

namespace Payroll.Application.Commands.PayrollRuns;

public record SetLopCommand(Guid RunId, Guid EmployeeId, int LopDays, Guid ActorId) : IRequest;

public sealed class SetLopCommandValidator : AbstractValidator<SetLopCommand>
{
    public SetLopCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LopDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class SetLopCommandHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayScheduleRepository payScheduleRepo,
    IPayrollRecomputeService recomputeService,
    IPayrollCostCalculator costCalculator,
    IUnitOfWork uow)
    : IRequestHandler<SetLopCommand>
{
    public async Task Handle(SetLopCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        if (payrunEmp.Status == PayrunEmployeeStatus.Skipped)
            throw new InvalidOperationException("Cannot set LOP for a skipped employee.");

        var paySchedule = await payScheduleRepo.GetAsync(ct)
            ?? throw new DomainException("Pay Schedule not configured.");
        EngineSalaryCalculationMethod engineCalcMethod = paySchedule.SalaryCalculationMethod == SalaryCalculationMethod.ActualDays
            ? EngineSalaryCalculationMethod.ActualDays
            : EngineSalaryCalculationMethod.FixedDays;
        int salaryDivisor = PayScheduleHelpers.GetDivisor(
            engineCalcMethod, paySchedule.FixedWorkingDaysPerMonth,
            run.PayPeriod.Year, run.PayPeriod.Month);

        // Guard — LOP must not exceed the salary divisor to prevent negative net pay
        if (req.LopDays >= salaryDivisor)
            throw new InvalidOperationException($"LOP days ({req.LopDays}) must be less than the payable days for this month ({salaryDivisor}).");

        payrunEmp.SetLop(req.LopDays, req.ActorId);

        RecomputeResult recompute = await recomputeService.RecomputeEmployeeAsync(req.RunId, req.EmployeeId, ct);
        var result = recompute.Engine;

        payrunEmp.UpdateComputedAmounts(
            grossPay: result.Gross.GrossWage,
            netPay: recompute.NetPayWithReimbursement,
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
