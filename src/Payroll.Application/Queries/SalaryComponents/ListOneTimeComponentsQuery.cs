using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.SalaryComponents;

// Returns active components flagged IsOneTime=true, filtered by category.
// Drives the "Add Earning" / "Add Deduction" dropdowns inside a payroll run.
public sealed record ListOneTimeComponentsQuery(ComponentCategory Category)
    : IRequest<List<SalaryComponentSummaryDto>>;

public sealed class ListOneTimeComponentsHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext)
    : IRequestHandler<ListOneTimeComponentsQuery, List<SalaryComponentSummaryDto>>
{
    public async Task<List<SalaryComponentSummaryDto>> Handle(
        ListOneTimeComponentsQuery req, CancellationToken ct)
    {
        List<Domain.Entities.SalaryComponent> components =
            await repo.ListByTenantAsync(tenantContext.TenantId, req.Category, ct);

        return components
            .Where(c => c.IsActive && c.IsOneTime)
            .Select(c => new SalaryComponentSummaryDto(
                c.Id, c.Name, c.NameInPayslip, c.Code, c.Category,
                c.IsActive, c.IsSystemComponent, c.IsAssociatedWithEmployee, c.IsOneTime,
                c.FormulaType, c.FixedAmount, c.Percentage,
                c.DeductionFrequency, c.ReimbursementAmount, c.BenefitPercentage, c.EarningType, c.ConsiderForEpf == true))
            .ToList();
    }
}
