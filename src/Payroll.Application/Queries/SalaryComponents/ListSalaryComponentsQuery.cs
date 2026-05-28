using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.SalaryComponents;

public sealed record ListSalaryComponentsQuery(
    ComponentCategory? Category = null,
    PaginationParams? Pagination = null)
    : IRequest<PagedResult<SalaryComponentSummaryDto>>;

public sealed class ListSalaryComponentsHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext)
    : IRequestHandler<ListSalaryComponentsQuery, PagedResult<SalaryComponentSummaryDto>>
{
    public async Task<PagedResult<SalaryComponentSummaryDto>> Handle(
        ListSalaryComponentsQuery req, CancellationToken ct)
    {
        var pagination = req.Pagination ?? new PaginationParams();
        List<Domain.Entities.SalaryComponent> all =
            await repo.ListByTenantAsync(tenantContext.TenantId, req.Category, ct);

        var pageRows = all
            .OrderBy(c => c.Name)
            .Skip(pagination.SkipCount)
            .Take(pagination.TakeCount)
            .Select(c => new SalaryComponentSummaryDto(
                c.Id, c.Name, c.NameInPayslip, c.Code, c.Category,
                c.IsActive, c.IsSystemComponent, c.IsAssociatedWithEmployee, c.IsOneTime,
                c.FormulaType, c.FixedAmount, c.Percentage,
                c.DeductionFrequency, c.ReimbursementAmount, c.BenefitPercentage, c.EarningType, c.ConsiderForEpf == true))
            .ToList();

        return new PagedResult<SalaryComponentSummaryDto>(
            pageRows, all.Count, pagination.NormalizedPage, pagination.NormalizedSize);
    }
}
