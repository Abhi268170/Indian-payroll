using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPayrollRunSummaryQuery(Guid RunId) : IRequest<PayrollRunSummaryDto>;

internal sealed class GetPayrollRunSummaryHandler(
    IPayrollRunRepository repo,
    IEmployeeExitRepository exitRepo)
    : IRequestHandler<GetPayrollRunSummaryQuery, PayrollRunSummaryDto>
{
    public async Task<PayrollRunSummaryDto> Handle(GetPayrollRunSummaryQuery req, CancellationToken ct)
    {
        var run = await repo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        // WI-20: surface exit metadata for a single FinalSettlement run so the
        // FnF detail screen doesn't need a second call. Bulk runs cover multiple
        // exits, so run-level metadata would be ambiguous — left null there.
        DateOnly? lwd = null;
        string? exitReason = null, settlementMode = null;
        DateOnly? settlementDate = null;
        if (run.Type == PayrollRunType.FinalSettlement)
        {
            var exits = await exitRepo.GetByFnfRunIdsAsync(new[] { run.Id }, ct);
            if (exits.Count == 1)
            {
                EmployeeExit e = exits[0];
                lwd = e.LastWorkingDay;
                exitReason = e.Reason.ToString();
                settlementMode = e.SettlementMode.ToString();
                settlementDate = e.SettlementDate;
            }
        }

        return new PayrollRunSummaryDto(
            Id: run.Id,
            Year: run.PayPeriod.Year,
            Month: run.PayPeriod.Month,
            PeriodLabel: run.PayPeriod.ToString(),
            Status: run.Status.ToString(),
            Type: run.Type.ToString(),
            PayDay: run.PayDay,
            PayrollCost: run.PayrollCost,
            TotalNetPay: run.TotalNetPay,
            TotalEmployerPf: run.TotalEmployerPf,
            TotalEmployerEsi: run.TotalEmployerEsi,
            TotalTds: run.TotalTds,
            TotalPt: run.TotalPt,
            EmployeeCount: run.EmployeeCount,
            CreatedAt: run.CreatedAt,
            ApprovedAt: run.ApprovedAt,
            PaidAt: run.PaidAt,
            LastWorkingDay: lwd,
            ExitReason: exitReason,
            SettlementMode: settlementMode,
            SettlementDate: settlementDate);
    }
}
