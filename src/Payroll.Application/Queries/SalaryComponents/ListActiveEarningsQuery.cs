using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.SalaryComponents;

public sealed record ListActiveEarningsQuery : IRequest<List<SalaryComponentSummaryDto>>;

public sealed class ListActiveEarningsHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext)
    : IRequestHandler<ListActiveEarningsQuery, List<SalaryComponentSummaryDto>>
{
    public async Task<List<SalaryComponentSummaryDto>> Handle(
        ListActiveEarningsQuery req, CancellationToken ct)
    {
        List<Domain.Entities.SalaryComponent> earnings =
            await repo.ListActiveEarningsAsync(tenantContext.TenantId, ct);

        return earnings.Select(c => new SalaryComponentSummaryDto(
            c.Id, c.Name, c.NameInPayslip, c.Code, c.Category,
            c.IsActive, c.IsSystemComponent, c.IsAssociatedWithEmployee, c.IsOneTime,
            c.FormulaType, c.FixedAmount, c.Percentage,
            c.DeductionFrequency, c.ReimbursementAmount, c.BenefitPercentage))
            .ToList();
    }
}
