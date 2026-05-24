using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Branch branch, CancellationToken cancellationToken = default);
}
