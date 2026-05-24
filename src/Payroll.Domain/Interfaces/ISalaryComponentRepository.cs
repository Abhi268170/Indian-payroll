using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Interfaces;

public interface ISalaryComponentRepository
{
    Task<SalaryComponent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SalaryComponent>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);
    Task<List<SalaryComponent>> ListByTenantAsync(Guid tenantId, ComponentCategory? category = null, CancellationToken ct = default);
    Task<List<SalaryComponent>> ListActiveEarningsAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<SalaryComponent>> ListActiveBenefitsAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(SalaryComponent component, CancellationToken ct = default);
    Task<bool> ExistsCodeAsync(Guid tenantId, string code, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> IsReferencedByTemplateAsync(Guid componentId, CancellationToken ct = default);
    Task<bool> HasActiveBenefitTypeAsync(Guid tenantId, BenefitType benefitType, Guid? excludeId = null, CancellationToken ct = default);
}
