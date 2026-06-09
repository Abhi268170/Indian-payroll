using MediatR;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

// WI-22: compute an FnF settlement for hypothetical inputs WITHOUT persisting,
// so HR can model the tax impact of different gratuity / leave-encashment / notice
// pay amounts before committing.
public record GetFnfPreviewQuery(
    Guid RunId,
    Guid EmployeeId,
    decimal LopDays,
    decimal Bonus,
    decimal Commission,
    decimal LeaveEncashment,
    decimal Gratuity,
    bool HasNoticePay,
    string? NoticePayDirection,
    decimal NoticePayAmount,
    IReadOnlyList<FnfAdhocDeductionDto> Deductions) : IRequest<FnfPreviewDto>;

public sealed record FnfPreviewDto(
    decimal GrossPay,
    decimal TaxableGrossPay,
    decimal NetPay,
    decimal Tds,
    decimal EmployeePf,
    decimal EmployeeEsi,
    decimal Pt,
    decimal Lwf,
    decimal Reimbursements);

internal sealed class GetFnfPreviewHandler(
    IPayrollRunRepository runRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeRepository employeeRepo,
    IEmployeeExitRepository exitRepo,
    IStatutoryConfigRepository statutoryRepo,
    ITenantContext tenantContext,
    IPayrollFnfOrchestrator orchestrator)
    : IRequestHandler<GetFnfPreviewQuery, FnfPreviewDto>
{
    private static readonly HashSet<string> FnfCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FNF_BONUS", "FNF_COMMISSION",
        "FNF_LEAVE_ENCASHMENT_EXEMPT", "FNF_LEAVE_ENCASHMENT_TAXABLE",
        "FNF_GRATUITY_EXEMPT", "FNF_GRATUITY_TAXABLE",
        "FNF_NOTICE_PAY_PAYABLE", "FNF_NOTICE_PAY_RECEIVABLE",
        "FNF_ADHOC_DEDUCTION"
    };

    public async Task<FnfPreviewDto> Handle(GetFnfPreviewQuery req, CancellationToken ct)
    {
        PayrollRun run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");
        if (run.Type != PayrollRunType.FinalSettlement && run.Type != PayrollRunType.BulkFinalSettlement)
            throw new DomainException("Run is not a final settlement run.");

        StatutoryOrgConfig orgConfig = await statutoryRepo.GetByTenantAsync(ct)
            ?? throw new DomainException("Statutory configuration not found.");

        bool gratuityEligible = true;
        if (req.Gratuity > 0)
        {
            Employee employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
                ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");
            EmployeeExit exit = await exitRepo.GetActiveByEmployeeAsync(req.EmployeeId, ct)
                ?? throw new DomainException($"No active exit for employee {req.EmployeeId}.");
            gratuityEligible = employee.IsGratuityEligibleAt(exit.LastWorkingDay);
        }

        // Persisted recurring + benefit + reimbursement rows, minus any saved FnF
        // one-time rows (those are replaced by the hypothetical preview inputs).
        var persisted = await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        var combined = persisted.Where(b => !FnfCodes.Contains(b.ComponentCode)).ToList();

        combined.AddRange(UpdateFnfRunHandler.BuildFnfRows(
            req.RunId, req.EmployeeId, tenantContext.TenantId,
            req.Bonus, req.Commission, req.LeaveEncashment, req.Gratuity, gratuityEligible,
            req.HasNoticePay, req.NoticePayDirection, req.NoticePayAmount, req.Deductions,
            orgConfig.GratuityExemptionLimit, orgConfig.LeaveEncashmentExemptionLimit));

        FnfEngineResult fnf = await orchestrator.ComputeAsync(req.RunId, req.EmployeeId, combined, ct);
        PayrollResultView v = PayrollResultView.From(fnf);

        return new FnfPreviewDto(
            GrossPay: v.Gross,
            TaxableGrossPay: v.TaxableGross,
            NetPay: fnf.NetPayWithAdjustments,
            Tds: v.Tds,
            EmployeePf: v.EmployeePf,
            EmployeeEsi: v.EmployeeEsi,
            Pt: v.Pt,
            Lwf: v.Lwf,
            Reimbursements: fnf.ReimbursementsAmount);
    }

    private readonly record struct PayrollResultView(
        decimal Gross, decimal TaxableGross, decimal Tds,
        decimal EmployeePf, decimal EmployeeEsi, decimal Pt, decimal Lwf)
    {
        public static PayrollResultView From(FnfEngineResult fnf)
        {
            var r = fnf.Engine;
            return new PayrollResultView(
                r.Gross.GrossWage, r.Gross.TaxableGrossWage, r.TDS.MonthlyTDS,
                r.PF.EmployeeContribution, r.ESI.EmployeeContribution, r.PT.Amount, r.LWF.EmployeeAmount);
        }
    }
}
