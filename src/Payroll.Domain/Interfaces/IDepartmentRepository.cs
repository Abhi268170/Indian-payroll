using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> ListAsync(CancellationToken ct = default);
    Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Department department, CancellationToken ct = default);
    void Update(Department department);
    void Remove(Department department);
}
