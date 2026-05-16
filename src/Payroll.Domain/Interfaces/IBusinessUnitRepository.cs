using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IBusinessUnitRepository
{
    Task<IReadOnlyList<BusinessUnit>> ListAsync(CancellationToken ct = default);
    Task<BusinessUnit?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(BusinessUnit businessUnit, CancellationToken ct = default);
    void Update(BusinessUnit businessUnit);
    void Remove(BusinessUnit businessUnit);
}
