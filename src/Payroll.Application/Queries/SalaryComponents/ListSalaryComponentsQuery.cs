using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.SalaryComponents;

public sealed record ListSalaryComponentsQuery(ComponentCategory? Category = null)
    : IRequest<List<SalaryComponentSummaryDto>>;

public sealed class ListSalaryComponentsHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext)
    : IRequestHandler<ListSalaryComponentsQuery, List<SalaryComponentSummaryDto>>
{
    public async Task<List<SalaryComponentSummaryDto>> Handle(
        ListSalaryComponentsQuery req, CancellationToken ct)
    {
        List<Domain.Entities.SalaryComponent> components =
            await repo.ListByTenantAsync(tenantContext.TenantId, req.Category, ct);

        return components.Select(c => new SalaryComponentSummaryDto(
            c.Id, c.Name, c.NameInPayslip, c.Code, c.Category,
            c.IsActive, c.IsSystemComponent, c.IsAssociatedWithEmployee, c.IsOneTime,
            c.FormulaType, c.FixedAmount, c.Percentage,
            c.DeductionFrequency, c.ReimbursementAmount, c.BenefitPercentage))
            .ToList();
    }
}
