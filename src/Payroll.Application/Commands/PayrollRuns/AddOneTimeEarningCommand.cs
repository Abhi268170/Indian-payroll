using FluentValidation;
using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record AddOneTimeEarningCommand(
    Guid RunId,
    Guid EmployeeId,
    Guid ComponentId,
    decimal Amount,
    Guid ActorId) : IRequest<Guid>;

public sealed class AddOneTimeEarningCommandValidator : AbstractValidator<AddOneTimeEarningCommand>
{
    public AddOneTimeEarningCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ComponentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m).LessThanOrEqualTo(10_000_000m);
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class AddOneTimeEarningHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    ISalaryComponentRepository componentRepo,
    IPayrollRecomputeService recomputeService,
    IPayrollCostCalculator costCalculator,
    IUnitOfWork uow)
    : IRequestHandler<AddOneTimeEarningCommand, Guid>
{
    public async Task<Guid> Handle(AddOneTimeEarningCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        if (payrunEmp.Status == PayrunEmployeeStatus.Skipped)
            throw new InvalidOperationException("Cannot add earnings for a skipped employee.");

        var component = await componentRepo.GetByIdAsync(req.ComponentId, ct)
            ?? throw new NotFoundException($"Salary component {req.ComponentId} not found.");

        if (component.TenantId != run.TenantId)
            throw new InvalidOperationException("Salary component does not belong to this tenant.");
        if (!component.IsActive)
            throw new InvalidOperationException($"Salary component '{component.Code}' is inactive.");
        if (!component.IsOneTime)
            throw new InvalidOperationException($"Salary component '{component.Code}' is not a one-time component.");
        if (component.Category != ComponentCategory.Earning)
            throw new InvalidOperationException($"Salary component '{component.Code}' is not an Earning. Use the deduction endpoint for deductions.");

        // Freeze the component's statutory flags onto the breakdown so engine
        // recompute stays deterministic even if the component is later edited.
        var breakdown = PayrunComponentBreakdown.Create(
            payrollRunId: run.Id,
            employeeId: req.EmployeeId,
            tenantId: run.TenantId,
            salaryComponentId: req.ComponentId,
            componentCode: component.Code,
            componentName: component.NameInPayslip,
            fullAmount: req.Amount,
            proratedAmount: req.Amount,
            isOneTimeEarning: true,
            isTaxable: component.IsTaxable ?? true,
            considerForEpf: component.ConsiderForEpf ?? false,
            considerForEsi: component.ConsiderForEsi ?? false,
            calculateOnProRata: false,
            epfInclusionRule: component.EpfInclusionRule ?? EpfInclusionRule.Always,
            showInPayslip: component.ShowInPayslip ?? true);

        await breakdownRepo.AddAsync(breakdown, ct);
        await uow.SaveChangesAsync(ct);

        await ApplyRecomputeAsync(req.RunId, req.EmployeeId, run, payrunEmp, req.ActorId, ct);

        return breakdown.Id;
    }

    private async Task ApplyRecomputeAsync(
        Guid runId,
        Guid employeeId,
        PayrollRun run,
        PayrunEmployee payrunEmp,
        Guid actorId,
        CancellationToken ct)
    {
        RecomputeResult recompute = await recomputeService.RecomputeEmployeeAsync(runId, employeeId, ct);
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
            actorId: actorId);
        payrunEmployeeRepo.Update(payrunEmp);

        var allEmployees = await payrunEmployeeRepo.GetByRunIdAsync(runId, ct);
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
            actorId: actorId);
        runRepo.Update(run);

        await uow.SaveChangesAsync(ct);
    }
}
