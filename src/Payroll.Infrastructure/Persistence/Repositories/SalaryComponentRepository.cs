using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class SalaryComponentRepository(PayrollDbContext db) : ISalaryComponentRepository
{
    public Task<SalaryComponent?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.SalaryComponents
          .Include(c => c.ForCorrectionOfComponent)
          .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<SalaryComponent>> GetByIdsAsync(
        IReadOnlyList<Guid> ids, CancellationToken ct = default) =>
        db.SalaryComponents
          .Where(c => ids.Contains(c.Id))
          .ToListAsync(ct);

    public Task<List<SalaryComponent>> ListByTenantAsync(
        Guid tenantId, ComponentCategory? category = null, CancellationToken ct = default) =>
        db.SalaryComponents
          .Where(c => c.TenantId == tenantId && (category == null || c.Category == category))
          .OrderBy(c => c.Name)
          .ToListAsync(ct);

    public Task<List<SalaryComponent>> ListActiveEarningsAsync(
        Guid tenantId, CancellationToken ct = default) =>
        db.SalaryComponents
          .Where(c => c.TenantId == tenantId
                   && c.Category == ComponentCategory.Earning
                   && c.IsActive
                   && !c.IsSystemComponent)
          .OrderBy(c => c.Name)
          .ToListAsync(ct);

    public Task<List<SalaryComponent>> ListActiveBenefitsAsync(
        Guid tenantId, CancellationToken ct = default) =>
        db.SalaryComponents
          .Where(c => c.TenantId == tenantId
                   && c.Category == ComponentCategory.Benefit
                   && c.IsActive)
          .OrderBy(c => c.Name)
          .ToListAsync(ct);

    public async Task AddAsync(SalaryComponent component, CancellationToken ct = default) =>
        await db.SalaryComponents.AddAsync(component, ct);

    public Task<bool> ExistsCodeAsync(
        Guid tenantId, string code, Guid? excludeId = null, CancellationToken ct = default) =>
        db.SalaryComponents
          .AnyAsync(c => c.TenantId == tenantId
                      && c.Code == code
                      && (excludeId == null || c.Id != excludeId), ct);

    public Task<bool> IsReferencedByTemplateAsync(Guid componentId, CancellationToken ct = default) =>
        db.SalaryStructureComponents
          .AnyAsync(sc => sc.ComponentId == componentId, ct);

    public Task<bool> HasActiveBenefitTypeAsync(
        Guid tenantId, BenefitType benefitType, Guid? excludeId = null, CancellationToken ct = default) =>
        db.SalaryComponents
          .AnyAsync(c => c.TenantId == tenantId
                      && c.Category == ComponentCategory.Benefit
                      && c.BenefitType == benefitType
                      && c.IsActive
                      && (excludeId == null || c.Id != excludeId), ct);
}
