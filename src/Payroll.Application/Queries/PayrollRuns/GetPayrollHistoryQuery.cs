using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPayrollHistoryQuery(
    int Page = 1,
    int PageSize = 25,
    PayrollRunType? Type = null) : IRequest<PagedResult<PayrollHistoryItemDto>>;

public sealed class GetPayrollHistoryHandler(IPayrollRunRepository runRepo)
    : IRequestHandler<GetPayrollHistoryQuery, PagedResult<PayrollHistoryItemDto>>
{
    public async Task<PagedResult<PayrollHistoryItemDto>> Handle(GetPayrollHistoryQuery req, CancellationToken ct)
    {
        var pagination = new PaginationParams(req.Page, req.PageSize);
        int total = await runRepo.GetHistoryCountAsync(req.Type, ct);
        var runs = await runRepo.GetHistoryAsync(pagination.SkipCount, pagination.TakeCount, req.Type, ct);

        var items = runs.Select(r => new PayrollHistoryItemDto(
            Id: r.Id,
            Year: r.PayPeriod.Year,
            Month: r.PayPeriod.Month,
            PeriodLabel: new DateTime(r.PayPeriod.Year, r.PayPeriod.Month, 1).ToString("MMMM yyyy"),
            Type: r.Type.ToString(),
            TotalNetPay: r.TotalNetPay,
            EmployeeCount: r.EmployeeCount,
            PaymentDate: r.PaymentDate,
            PaidAt: r.PaidAt))
            .ToList();

        return new PagedResult<PayrollHistoryItemDto>(
            items, total, pagination.NormalizedPage, pagination.NormalizedSize);
    }
}
