using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IDesignationRepository
{
    Task<IReadOnlyList<Designation>> ListAsync(CancellationToken ct = default);
    Task<Designation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Designation designation, CancellationToken ct = default);
    void Update(Designation designation);
    void Remove(Designation designation);
}
