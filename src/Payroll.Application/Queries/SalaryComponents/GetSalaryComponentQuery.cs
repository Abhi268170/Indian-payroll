using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.SalaryComponents;

public sealed record GetSalaryComponentQuery(Guid Id) : IRequest<SalaryComponentDetailDto?>;

public sealed class GetSalaryComponentHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext)
    : IRequestHandler<GetSalaryComponentQuery, SalaryComponentDetailDto?>
{
    public async Task<SalaryComponentDetailDto?> Handle(
        GetSalaryComponentQuery req, CancellationToken ct)
    {
        SalaryComponent? c = await repo.GetByIdAsync(req.Id, ct);
        if (c is null || c.TenantId != tenantContext.TenantId) return null;

        return new SalaryComponentDetailDto(
            c.Id, c.Name, c.NameInPayslip, c.Code, c.Category,
            c.IsActive, c.IsSystemComponent, c.IsAssociatedWithEmployee,
            c.EarningType, c.PayType, c.FormulaType,
            c.FixedAmount, c.Percentage, c.IsTaxable,
            c.ConsiderForEpf, c.EpfInclusionRule, c.ConsiderForEsi,
            c.CalculateOnProRata, c.ShowInPayslip,
            c.DeductionFrequency,
            c.ReimbursementType, c.ReimbursementAmount, c.UnclaimedHandling,
            c.BenefitType, c.BenefitPercentage, c.IsApplicableToAllEmployees, c.IsNpsGovernmentSector,
            c.ForCorrectionOfComponentId,
            c.ForCorrectionOfComponent?.Name);
    }
}
