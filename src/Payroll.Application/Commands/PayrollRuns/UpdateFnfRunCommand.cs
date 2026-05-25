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
    int LopDays,
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
    IPayrollCostCalculator costCalculator,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<UpdateFnfRunCommand>
{
    // ₹20L lifetime exemption per Section 10(10). Prior received is assumed 0
    // for v1 since EmployeeFyOpening does not yet carry the field.
    private const decimal GratuityExemptionLimit = 2_000_000m;

    private static readonly HashSet<string> FnfCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FNF_BONUS", "FNF_COMMISSION", "FNF_LEAVE_ENCASHMENT",
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

        // Replace all FnF-prefixed breakdowns. Recurring (non-FNF) breakdowns
        // are left alone — Phase 4 initiation will populate those.
        var existing = await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        foreach (var b in existing.Where(x => FnfCodes.Contains(x.ComponentCode)))
            breakdownRepo.Remove(b);

        // Persist new FnF rows.
        var toAdd = new List<PayrunComponentBreakdown>();
        if (req.Bonus > 0) toAdd.Add(MakeFnf(req, "FNF_BONUS", "Bonus", req.Bonus, isTaxable: true));
        if (req.Commission > 0) toAdd.Add(MakeFnf(req, "FNF_COMMISSION", "Commission", req.Commission, isTaxable: true));
        if (req.LeaveEncashment > 0) toAdd.Add(MakeFnf(req, "FNF_LEAVE_ENCASHMENT", "Leave Encashment", req.LeaveEncashment, isTaxable: true));

        if (req.Gratuity > 0)
        {
            // Section 10(10) exempt up to ₹20L lifetime. Prior received = 0 for v1.
            decimal exempt = Math.Min(req.Gratuity, GratuityExemptionLimit);
            decimal taxable = req.Gratuity - exempt;
            if (exempt > 0) toAdd.Add(MakeFnf(req, "FNF_GRATUITY_EXEMPT", "Gratuity (Exempt)", exempt, isTaxable: false));
            if (taxable > 0) toAdd.Add(MakeFnf(req, "FNF_GRATUITY_TAXABLE", "Gratuity (Taxable)", taxable, isTaxable: true));
        }

        if (req.HasNoticePay && req.NoticePayAmount > 0)
        {
            if (req.NoticePayDirection == "Payable")
                toAdd.Add(MakeFnf(req, "FNF_NOTICE_PAY_PAYABLE", "Notice Pay (Company pays)", req.NoticePayAmount, isTaxable: true));
            else
                toAdd.Add(MakeFnf(req, "FNF_NOTICE_PAY_RECEIVABLE", "Notice Pay (Recovered)", -req.NoticePayAmount, isTaxable: false));
        }

        foreach (var d in req.Deductions)
            toAdd.Add(MakeFnf(req, "FNF_ADHOC_DEDUCTION", d.Name, -d.Amount, isTaxable: false));

        await breakdownRepo.AddRangeAsync(toAdd, ct);
        await uow.SaveChangesAsync(ct);

        // Engine recompute via orchestrator.
        FnfEngineResult fnf = await orchestrator.ComputeAsync(req.RunId, req.EmployeeId, ct);
        var result = fnf.Engine;

        payrunEmp.UpdateComputedAmounts(
            grossPay: result.Gross.GrossWage,
            taxableGrossPay: result.Gross.TaxableGrossWage,
            netPay: fnf.NetPayWithAdjustments,
            taxesAmount: result.TDS.MonthlyTDS + result.PT.Amount,
            benefitsAmount: result.PF.EPFEmployerContribution + result.ESI.EmployerContribution,
            reimbursementsAmount: fnf.ReimbursementsAmount,
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

        payrunEmpRepo.Update(payrunEmp);

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

    private PayrunComponentBreakdown MakeFnf(
        UpdateFnfRunCommand req, string code, string name, decimal amount, bool isTaxable) =>
        PayrunComponentBreakdown.Create(
            payrollRunId: req.RunId,
            employeeId: req.EmployeeId,
            tenantId: tenantContext.TenantId,
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
