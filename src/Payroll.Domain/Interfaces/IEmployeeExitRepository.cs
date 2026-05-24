using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IEmployeeExitRepository
{
    Task<EmployeeExit?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EmployeeExit?> GetActiveByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task AddAsync(EmployeeExit exit, CancellationToken ct = default);
    void Update(EmployeeExit exit);
    void Remove(EmployeeExit exit);
}
