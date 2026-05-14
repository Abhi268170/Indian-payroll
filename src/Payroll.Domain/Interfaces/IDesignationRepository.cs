using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IDesignationRepository
{
    Task<Designation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Designation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Designation designation, CancellationToken cancellationToken = default);
}
