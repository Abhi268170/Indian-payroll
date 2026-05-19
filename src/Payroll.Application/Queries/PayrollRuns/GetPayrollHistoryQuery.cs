using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPayrollHistoryQuery(int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<PayrollHistoryItemDto>>;

public sealed class GetPayrollHistoryHandler(IPayrollRunRepository runRepo)
    : IRequestHandler<GetPayrollHistoryQuery, IReadOnlyList<PayrollHistoryItemDto>>
{
    public async Task<IReadOnlyList<PayrollHistoryItemDto>> Handle(GetPayrollHistoryQuery req, CancellationToken ct)
    {
        int skip = (req.Page - 1) * req.PageSize;
        var runs = await runRepo.GetHistoryAsync(skip, req.PageSize, ct);

        return runs.Select(r => new PayrollHistoryItemDto(
            Id: r.Id,
            Year: r.PayPeriod.Year,
            Month: r.PayPeriod.Month,
            PeriodLabel: new DateTime(r.PayPeriod.Year, r.PayPeriod.Month, 1).ToString("MMMM yyyy"),
            TotalNetPay: r.TotalNetPay,
            EmployeeCount: r.EmployeeCount,
            PaidAt: r.PaidAt))
            .ToList();
    }
}
