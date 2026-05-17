using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<Employee>> ListAsync(CancellationToken ct = default);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Employee?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken ct = default);
    Task<string> NextEmployeeCodeAsync(CancellationToken ct = default);
    Task AddAsync(Employee employee, CancellationToken ct = default);
    void Update(Employee employee);
}
