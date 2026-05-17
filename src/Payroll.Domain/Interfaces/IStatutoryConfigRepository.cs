using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IStatutoryConfigRepository
{
    Task<StatutoryOrgConfig?> GetByTenantAsync(CancellationToken ct = default);
    Task AddAsync(StatutoryOrgConfig config, CancellationToken ct = default);
    void Update(StatutoryOrgConfig config);
    Task<IReadOnlyList<ProfessionalTaxSlab>> GetPtSlabsAsync(string stateCode, DateOnly asOf, CancellationToken ct = default);
    Task AddPtSlabsAsync(IEnumerable<ProfessionalTaxSlab> slabs, CancellationToken ct = default);
    Task<IReadOnlyList<LwfStateConfig>> GetLwfConfigsAsync(IEnumerable<string> stateCodes, CancellationToken ct = default);
    Task<LwfStateConfig?> GetLwfConfigAsync(string stateCode, CancellationToken ct = default);
    Task<IncomeTaxConfig?> GetIncomeTaxConfigAsync(string fiscalYear, string regime, CancellationToken ct = default);
    Task<IReadOnlyList<IncomeTaxSlab>> GetIncomeTaxSlabsAsync(string fiscalYear, string regime, CancellationToken ct = default);
    Task<IReadOnlyList<IncomeTaxSurchargeSlab>> GetSurchargeSlabsAsync(string fiscalYear, string regime, CancellationToken ct = default);
}
