using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface ICostCentreRepository
{
    Task<IReadOnlyList<CostCentre>> ListAsync(CancellationToken ct = default);
    Task<CostCentre?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(CostCentre costCentre, CancellationToken ct = default);
    void Update(CostCentre costCentre);
    void Remove(CostCentre costCentre);
}
