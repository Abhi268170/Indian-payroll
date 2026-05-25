using FluentValidation;
using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record AddOneTimeDeductionCommand(
    Guid RunId,
    Guid EmployeeId,
    Guid ComponentId,
    decimal Amount,
    Guid ActorId) : IRequest<Guid>;

public sealed class AddOneTimeDeductionCommandValidator : AbstractValidator<AddOneTimeDeductionCommand>
{
    public AddOneTimeDeductionCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ComponentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m).LessThanOrEqualTo(10_000_000m);
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class AddOneTimeDeductionHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    ISalaryComponentRepository componentRepo,
    IPayrollRecomputeService recomputeService,
    IPayrollCostCalculator costCalculator,
    IUnitOfWork uow)
    : IRequestHandler<AddOneTimeDeductionCommand, Guid>
{
    public async Task<Guid> Handle(AddOneTimeDeductionCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        if (payrunEmp.Status == PayrunEmployeeStatus.Skipped)
            throw new InvalidOperationException("Cannot add deductions for a skipped employee.");

        var component = await componentRepo.GetByIdAsync(req.ComponentId, ct)
            ?? throw new NotFoundException($"Salary component {req.ComponentId} not found.");

        if (component.TenantId != run.TenantId)
            throw new InvalidOperationException("Salary component does not belong to this tenant.");
        if (!component.IsActive)
            throw new InvalidOperationException($"Salary component '{component.Code}' is inactive.");
        if (!component.IsOneTime)
            throw new InvalidOperationException($"Salary component '{component.Code}' is not a one-time component.");
        if (component.Category != ComponentCategory.Deduction)
            throw new InvalidOperationException($"Salary component '{component.Code}' is not a Deduction. Use the earnings endpoint for earnings.");

        // Deductions reduce net pay only. They never enter gross or statutory
        // base. The recompute service classifies rows by the linked component's
        // Category and subtracts deductions from NetPay post-engine — so we
        // store a positive amount here and let the service handle the sign.
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
            isTaxable: false,
            considerForEpf: false,
            considerForEsi: false,
            calculateOnProRata: false,
            epfInclusionRule: EpfInclusionRule.Always,
            showInPayslip: true);

        await breakdownRepo.AddAsync(breakdown, ct);
        await uow.SaveChangesAsync(ct);

        RecomputeResult recompute = await recomputeService.RecomputeEmployeeAsync(req.RunId, req.EmployeeId, ct);
        var result = recompute.Engine;

        payrunEmp.UpdateComputedAmounts(
            grossPay: result.Gross.GrossWage,
            taxableGrossPay: result.Gross.TaxableGrossWage,
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

        return breakdown.Id;
    }
}
