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
        DateOnly? latestEffective = await db.ProfessionalTaxSlabs
            .Where(s => s.StateCode == stateCode && s.EffectiveDate <= asOf && s.IsActive)
            .MaxAsync(s => (DateOnly?)s.EffectiveDate, ct);

        if (latestEffective is null)
            return [];

        return await db.ProfessionalTaxSlabs
            .Where(s => s.StateCode == stateCode && s.EffectiveDate == latestEffective && s.IsActive)
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

    public async Task<IReadOnlyList<LwfStateConfig>> GetAllLwfConfigsForStatesAsync(
        IEnumerable<string> stateCodes, CancellationToken ct = default)
    {
        List<string> codes = stateCodes.ToList();
        return await db.LwfStateConfigs
            .Where(l => codes.Contains(l.StateCode))
            .ToListAsync(ct);
    }

    // Returns the config regardless of IsActive — needed for toggle operations
    public Task<LwfStateConfig?> GetLwfConfigAsync(string stateCode, CancellationToken ct = default) =>
        db.LwfStateConfigs.FirstOrDefaultAsync(l => l.StateCode == stateCode, ct);

    public void UpdateLwfConfig(LwfStateConfig config) =>
        db.LwfStateConfigs.Update(config);

    public async Task<IReadOnlyList<PtStateRegistration>> GetPtRegistrationsAsync(CancellationToken ct = default) =>
        await db.PtStateRegistrations.ToListAsync(ct);

    public Task<PtStateRegistration?> GetPtRegistrationAsync(string stateCode, CancellationToken ct = default) =>
        db.PtStateRegistrations.FirstOrDefaultAsync(r => r.StateCode == stateCode, ct);

    public async Task AddPtRegistrationAsync(PtStateRegistration registration, CancellationToken ct = default) =>
        await db.PtStateRegistrations.AddAsync(registration, ct);

    public void UpdatePtRegistration(PtStateRegistration registration) =>
        db.PtStateRegistrations.Update(registration);

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
