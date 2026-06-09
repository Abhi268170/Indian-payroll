using FluentValidation;
using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record UpdateFnfRunCommand(
    Guid RunId,
    Guid EmployeeId,
    decimal LopDays,
    decimal Bonus,
    decimal Commission,
    decimal LeaveEncashment,
    decimal Gratuity,
    bool HasNoticePay,
    string? NoticePayDirection, // "Payable" or "Receivable"
    decimal NoticePayAmount,
    string? PayslipNotes,
    IReadOnlyList<FnfAdhocDeductionDto> Deductions,
    Guid ActorId) : IRequest;

public record FnfAdhocDeductionDto(string Name, decimal Amount);

public sealed class UpdateFnfRunCommandValidator : AbstractValidator<UpdateFnfRunCommand>
{
    public UpdateFnfRunCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.LopDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Bonus).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Commission).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.LeaveEncashment).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Gratuity).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.NoticePayAmount)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.HasNoticePay);
        RuleFor(x => x.NoticePayDirection)
            .Must(v => v == "Payable" || v == "Receivable")
            .When(x => x.HasNoticePay)
            .WithMessage("NoticePayDirection must be 'Payable' or 'Receivable'.");
        RuleForEach(x => x.Deductions).ChildRules(d =>
        {
            d.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            d.RuleFor(x => x.Amount).GreaterThan(0m);
        });
        RuleFor(x => x.PayslipNotes).MaximumLength(2000);
    }
}

public sealed class UpdateFnfRunHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmpRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IPayrollFnfOrchestrator orchestrator,
    ITdsWorksheetRepository tdsWorksheetRepo,
    IEmployeeRepository employeeRepo,
    IEmployeeExitRepository exitRepo,
    IStatutoryConfigRepository statutoryRepo,
    IPayrollCostCalculator costCalculator,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<UpdateFnfRunCommand>
{
    private static readonly HashSet<string> FnfCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FNF_BONUS", "FNF_COMMISSION",
        "FNF_LEAVE_ENCASHMENT_EXEMPT", "FNF_LEAVE_ENCASHMENT_TAXABLE",
        "FNF_GRATUITY_EXEMPT", "FNF_GRATUITY_TAXABLE",
        "FNF_NOTICE_PAY_PAYABLE", "FNF_NOTICE_PAY_RECEIVABLE",
        "FNF_ADHOC_DEDUCTION"
    };

    public async Task Handle(UpdateFnfRunCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("FnF run can only be edited while in Draft status.");
        if (run.Type != PayrollRunType.FinalSettlement && run.Type != PayrollRunType.BulkFinalSettlement)
            throw new InvalidOperationException("UpdateFnfRunCommand only applies to FnF runs.");

        var payrunEmp = await payrunEmpRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException("Employee not in this FnF run.");

        payrunEmp.SetLop(req.LopDays, req.ActorId);

        // WI-19: exemption limits come from tenant statutory config (not hardcoded),
        // so budget amendments can be applied without a deployment.
        StatutoryOrgConfig orgConfig = await statutoryRepo.GetByTenantAsync(ct)
            ?? throw new DomainException("Statutory configuration not found.");
        decimal gratuityExemptionLimit = orgConfig.GratuityExemptionLimit;
        decimal leaveEncashmentExemptionLimit = orgConfig.LeaveEncashmentExemptionLimit;

        // Replace all FnF-prefixed breakdowns. Recurring (non-FNF) breakdowns
        // are left alone — Phase 4 initiation will populate those.
        var existing = await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        foreach (var b in existing.Where(x => FnfCodes.Contains(x.ComponentCode)))
            breakdownRepo.Remove(b);

        // Gratuity eligibility (WI-15): only payable after 5 years (4y 240d).
        bool gratuityEligible = true;
        if (req.Gratuity > 0)
        {
            Employee employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
                ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");
            EmployeeExit exit = await exitRepo.GetActiveByEmployeeAsync(req.EmployeeId, ct)
                ?? throw new DomainException($"No active exit for employee {req.EmployeeId}.");
            gratuityEligible = employee.IsGratuityEligibleAt(exit.LastWorkingDay);
        }

        var toAdd = BuildFnfRows(
            req.RunId, req.EmployeeId, tenantContext.TenantId,
            req.Bonus, req.Commission, req.LeaveEncashment, req.Gratuity, gratuityEligible,
            req.HasNoticePay, req.NoticePayDirection, req.NoticePayAmount, req.Deductions,
            gratuityExemptionLimit, leaveEncashmentExemptionLimit);

        await breakdownRepo.AddRangeAsync(toAdd, ct);
        await uow.SaveChangesAsync(ct);

        // Engine recompute via orchestrator.
        FnfEngineResult fnf = await orchestrator.ComputeAsync(req.RunId, req.EmployeeId, ct);

        PayrollFnfOrchestrator.ApplyToPayrunEmployee(payrunEmp, fnf, req.ActorId);
        payrunEmpRepo.Update(payrunEmp);

        // Upsert TDS worksheet so draft state is auditable (WI-04).
        await tdsWorksheetRepo.DeleteByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        await tdsWorksheetRepo.AddAsync(
            PayrollFnfOrchestrator.BuildWorksheet(run, payrunEmp, fnf, req.ActorId), ct);

        // Refresh run totals.
        var allRows = await payrunEmpRepo.GetByRunIdAsync(req.RunId, ct);
        var active = allRows.Where(e => e.Status == PayrunEmployeeStatus.Active).ToList();
        var snapshot = costCalculator.Calculate(active);
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

    // Shared builder for the FnF one-time rows (bonus, commission, leave-encashment
    // split, gratuity split, notice pay, ad-hoc deductions). Used by UpdateFnf (which
    // persists the rows) and by the FnF preview (WI-22, in-memory only).
    public static List<PayrunComponentBreakdown> BuildFnfRows(
        Guid runId, Guid employeeId, Guid tenantId,
        decimal bonus, decimal commission, decimal leaveEncashment, decimal gratuity,
        bool gratuityEligible,
        bool hasNoticePay, string? noticePayDirection, decimal noticePayAmount,
        IReadOnlyList<FnfAdhocDeductionDto> deductions,
        decimal gratuityExemptionLimit, decimal leaveEncashmentExemptionLimit)
    {
        var rows = new List<PayrunComponentBreakdown>();

        if (bonus > 0) rows.Add(MakeFnfRow(runId, employeeId, tenantId, "FNF_BONUS", "Bonus", bonus, true));
        if (commission > 0) rows.Add(MakeFnfRow(runId, employeeId, tenantId, "FNF_COMMISSION", "Commission", commission, true));

        if (leaveEncashment > 0)
        {
            // Section 10(10AA): private-sector employees exempt up to the limit.
            decimal leExempt = Math.Min(leaveEncashment, leaveEncashmentExemptionLimit);
            decimal leTaxable = leaveEncashment - leExempt;
            if (leExempt > 0) rows.Add(MakeFnfRow(runId, employeeId, tenantId, "FNF_LEAVE_ENCASHMENT_EXEMPT", "Leave Encashment (Exempt)", leExempt, false));
            if (leTaxable > 0) rows.Add(MakeFnfRow(runId, employeeId, tenantId, "FNF_LEAVE_ENCASHMENT_TAXABLE", "Leave Encashment (Taxable)", leTaxable, true));
        }

        if (gratuity > 0)
        {
            if (!gratuityEligible)
                throw new DomainException(
                    "Employee has not completed 5 years of continuous service (4y 240d). "
                    + "Gratuity is not payable under the Payment of Gratuity Act, 1972.");

            decimal exempt = Math.Min(gratuity, gratuityExemptionLimit);
            decimal taxable = gratuity - exempt;
            if (exempt > 0) rows.Add(MakeFnfRow(runId, employeeId, tenantId, "FNF_GRATUITY_EXEMPT", "Gratuity (Exempt)", exempt, false));
            if (taxable > 0) rows.Add(MakeFnfRow(runId, employeeId, tenantId, "FNF_GRATUITY_TAXABLE", "Gratuity (Taxable)", taxable, true));
        }

        if (hasNoticePay && noticePayAmount > 0)
        {
            if (noticePayDirection == "Payable")
                rows.Add(MakeFnfRow(runId, employeeId, tenantId, "FNF_NOTICE_PAY_PAYABLE", "Notice Pay (Company pays)", noticePayAmount, true));
            else
                rows.Add(MakeFnfRow(runId, employeeId, tenantId, "FNF_NOTICE_PAY_RECEIVABLE", "Notice Pay (Recovered)", -noticePayAmount, false));
        }

        foreach (var d in deductions)
            rows.Add(MakeFnfRow(runId, employeeId, tenantId, "FNF_ADHOC_DEDUCTION", d.Name, -d.Amount, false));

        return rows;
    }

    private static PayrunComponentBreakdown MakeFnfRow(
        Guid runId, Guid employeeId, Guid tenantId, string code, string name, decimal amount, bool isTaxable) =>
        PayrunComponentBreakdown.Create(
            payrollRunId: runId,
            employeeId: employeeId,
            tenantId: tenantId,
            salaryComponentId: null,
            componentCode: code,
            componentName: name,
            fullAmount: amount,
            proratedAmount: amount,
            isOneTimeEarning: true,
            isTaxable: isTaxable,
            considerForEpf: false,
            considerForEsi: false,
            calculateOnProRata: false,
            epfInclusionRule: EpfInclusionRule.Always,
            showInPayslip: true);
}
