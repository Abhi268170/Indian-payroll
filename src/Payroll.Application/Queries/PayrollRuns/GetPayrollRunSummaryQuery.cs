using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPayrollRunSummaryQuery(Guid RunId) : IRequest<PayrollRunSummaryDto>;

internal sealed class GetPayrollRunSummaryHandler(IPayrollRunRepository repo)
    : IRequestHandler<GetPayrollRunSummaryQuery, PayrollRunSummaryDto>
{
    public async Task<PayrollRunSummaryDto> Handle(GetPayrollRunSummaryQuery req, CancellationToken ct)
    {
        var run = await repo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

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
            PaidAt: run.PaidAt);
    }
}
