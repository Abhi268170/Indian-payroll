using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.SalaryComponents;

public sealed record ListActiveBenefitsQuery : IRequest<List<SalaryComponentSummaryDto>>;

public sealed class ListActiveBenefitsHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext)
    : IRequestHandler<ListActiveBenefitsQuery, List<SalaryComponentSummaryDto>>
{
    public async Task<List<SalaryComponentSummaryDto>> Handle(
        ListActiveBenefitsQuery req, CancellationToken ct)
    {
        List<Domain.Entities.SalaryComponent> benefits =
            await repo.ListActiveBenefitsAsync(tenantContext.TenantId, ct);

        return benefits.Select(c => new SalaryComponentSummaryDto(
            c.Id, c.Name, c.NameInPayslip, c.Code, c.Category,
            c.IsActive, c.IsSystemComponent, c.IsAssociatedWithEmployee, c.IsOneTime,
            c.FormulaType, c.FixedAmount, c.Percentage,
            c.DeductionFrequency, c.ReimbursementAmount, c.BenefitPercentage))
            .ToList();
    }
}
