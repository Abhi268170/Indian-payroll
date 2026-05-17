using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class StatutoryConfigRepository(PayrollDbContext db, ITenantContext tenant)
    : IStatutoryConfigRepository
{
    public Task<StatutoryOrgConfig?> GetByTenantAsync(CancellationToken ct = default) =>
        db.StatutoryOrgConfigs.FirstOrDefaultAsync(s => s.TenantId == tenant.TenantId, ct);

    public async Task AddAsync(StatutoryOrgConfig config, CancellationToken ct = default) =>
        await db.StatutoryOrgConfigs.AddAsync(config, ct);

    public void Update(StatutoryOrgConfig config) =>
        db.StatutoryOrgConfigs.Update(config);

    public async Task AddPtSlabsAsync(IEnumerable<ProfessionalTaxSlab> slabs, CancellationToken ct = default) =>
        await db.ProfessionalTaxSlabs.AddRangeAsync(slabs, ct);

    public async Task<IReadOnlyList<ProfessionalTaxSlab>> GetPtSlabsAsync(
        string stateCode, DateOnly asOf, CancellationToken ct = default)
    {
        DateOnly effectiveDate = await db.ProfessionalTaxSlabs
            .Where(s => s.StateCode == stateCode && s.EffectiveDate <= asOf && s.IsActive)
            .MaxAsync(s => (DateOnly?)s.EffectiveDate, ct) ?? asOf;

        return await db.ProfessionalTaxSlabs
            .Where(s => s.StateCode == stateCode && s.EffectiveDate == effectiveDate && s.IsActive)
            .OrderBy(s => s.MinGross)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LwfStateConfig>> GetLwfConfigsAsync(
        IEnumerable<string> stateCodes, CancellationToken ct = default)
    {
        List<string> codes = stateCodes.ToList();
        return await db.LwfStateConfigs
            .Where(l => codes.Contains(l.StateCode) && l.IsActive)
            .ToListAsync(ct);
    }

    public Task<LwfStateConfig?> GetLwfConfigAsync(string stateCode, CancellationToken ct = default) =>
        db.LwfStateConfigs.FirstOrDefaultAsync(l => l.StateCode == stateCode && l.IsActive, ct);

    public Task<IncomeTaxConfig?> GetIncomeTaxConfigAsync(
        string fiscalYear, string regime, CancellationToken ct = default) =>
        db.IncomeTaxConfigs.FirstOrDefaultAsync(c => c.FiscalYear == fiscalYear && c.Regime == regime, ct);

    public async Task<IReadOnlyList<IncomeTaxSlab>> GetIncomeTaxSlabsAsync(
        string fiscalYear, string regime, CancellationToken ct = default) =>
        await db.IncomeTaxSlabs
            .Where(s => s.FiscalYear == fiscalYear && s.Regime == regime)
            .OrderBy(s => s.BracketMin)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<IncomeTaxSurchargeSlab>> GetSurchargeSlabsAsync(
        string fiscalYear, string regime, CancellationToken ct = default) =>
        await db.IncomeTaxSurchargeSlabs
            .Where(s => s.FiscalYear == fiscalYear && s.Regime == regime)
            .OrderBy(s => s.IncomeFrom)
            .ToListAsync(ct);
}
