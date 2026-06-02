using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetFnfSummaryQuery(Guid RunId, Guid EmployeeId) : IRequest<FnfSummaryDto>;

public sealed record FnfLineDto(string Code, string Name, decimal Amount, bool IsTaxable);

public sealed record FnfStatutoryDto(decimal Epf, decimal Esi, decimal Pt, decimal Tds, decimal Lwf);

public sealed record FnfSummaryDto(
    Guid RunId,
    Guid EmployeeId,
    string RunType,
    DateOnly? LastWorkingDay,
    string? ExitReason,
    DateOnly? SettlementDate,
    int WorkedDays,
    IReadOnlyList<FnfLineDto> Earnings,
    IReadOnlyList<FnfLineDto> Deductions,
    FnfStatutoryDto StatutoryDeductions,
    decimal GrossPay,
    decimal Reimbursements,
    decimal NetSettlement);

internal sealed class GetFnfSummaryHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmpRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeExitRepository exitRepo)
    : IRequestHandler<GetFnfSummaryQuery, FnfSummaryDto>
{
    public async Task<FnfSummaryDto> Handle(GetFnfSummaryQuery req, CancellationToken ct)
    {
        PayrollRun run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");
        if (run.Type != PayrollRunType.FinalSettlement && run.Type != PayrollRunType.BulkFinalSettlement)
            throw new DomainException("Run is not a final settlement run.");

        PayrunEmployee pe = await payrunEmpRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException("Employee not in this settlement run.");

        IReadOnlyList<PayrunComponentBreakdown> breakdowns =
            await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        EmployeeExit? exit = await exitRepo.GetActiveByEmployeeAsync(req.EmployeeId, ct);

        // Earnings = positive non-benefit, non-reimbursement rows.
        // Deductions = negative rows (notice-pay recovered, ad-hoc deductions).
        var earnings = new List<FnfLineDto>();
        var deductions = new List<FnfLineDto>();
        foreach (PayrunComponentBreakdown b in breakdowns)
        {
            if (b.IsBenefit) continue; // employer-borne; shown separately on payslip
            if (IsReimbursement(b)) continue; // surfaced via Reimbursements total
            if (b.FullAmount >= 0)
                earnings.Add(new FnfLineDto(b.ComponentCode, b.ComponentName, b.FullAmount, b.IsTaxable));
            else
                deductions.Add(new FnfLineDto(b.ComponentCode, b.ComponentName, Math.Abs(b.FullAmount), b.IsTaxable));
        }

        decimal reimbursements = breakdowns.Where(IsReimbursement).Sum(b => b.FullAmount);

        return new FnfSummaryDto(
            RunId: run.Id,
            EmployeeId: req.EmployeeId,
            RunType: run.Type.ToString(),
            LastWorkingDay: exit?.LastWorkingDay,
            ExitReason: exit?.Reason.ToString(),
            SettlementDate: exit?.SettlementDate ?? run.PayDay,
            WorkedDays: pe.BaseDays - pe.LopDays,
            Earnings: earnings,
            Deductions: deductions,
            StatutoryDeductions: new FnfStatutoryDto(
                Epf: pe.EmployeePf, Esi: pe.EmployeeEsi, Pt: pe.PtAmount,
                Tds: pe.TdsAmount, Lwf: pe.LwfEmployeeAmount),
            GrossPay: pe.GrossPay,
            Reimbursements: reimbursements,
            NetSettlement: pe.NetPay);
    }

    private static bool IsReimbursement(PayrunComponentBreakdown b) =>
        string.Equals(b.ComponentCode, "REIMBURSEMENT", StringComparison.OrdinalIgnoreCase);
}
