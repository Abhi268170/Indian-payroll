using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPayrollHistoryQuery(int Page = 1, int PageSize = 25) : IRequest<PagedResult<PayrollHistoryItemDto>>;

public sealed class GetPayrollHistoryHandler(IPayrollRunRepository runRepo)
    : IRequestHandler<GetPayrollHistoryQuery, PagedResult<PayrollHistoryItemDto>>
{
    public async Task<PagedResult<PayrollHistoryItemDto>> Handle(GetPayrollHistoryQuery req, CancellationToken ct)
    {
        var pagination = new PaginationParams(req.Page, req.PageSize);
        int total = await runRepo.GetHistoryCountAsync(ct);
        var runs = await runRepo.GetHistoryAsync(pagination.SkipCount, pagination.TakeCount, ct);

        var items = runs.Select(r => new PayrollHistoryItemDto(
            Id: r.Id,
            Year: r.PayPeriod.Year,
            Month: r.PayPeriod.Month,
            PeriodLabel: new DateTime(r.PayPeriod.Year, r.PayPeriod.Month, 1).ToString("MMMM yyyy"),
            TotalNetPay: r.TotalNetPay,
            EmployeeCount: r.EmployeeCount,
            PaidAt: r.PaidAt))
            .ToList();

        return new PagedResult<PayrollHistoryItemDto>(
            items, total, pagination.NormalizedPage, pagination.NormalizedSize);
    }
}
