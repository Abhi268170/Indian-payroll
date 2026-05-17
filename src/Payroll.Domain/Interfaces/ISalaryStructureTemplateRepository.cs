using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface ISalaryStructureTemplateRepository
{
    Task<SalaryStructureTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SalaryStructureTemplate?> GetByIdWithComponentsAsync(Guid id, CancellationToken ct = default);
    Task<List<SalaryStructureTemplate>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(SalaryStructureTemplate template, CancellationToken ct = default);
}
