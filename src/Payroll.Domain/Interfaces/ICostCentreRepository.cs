using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface ICostCentreRepository
{
    Task<CostCentre?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CostCentre>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(CostCentre costCentre, CancellationToken cancellationToken = default);
}
