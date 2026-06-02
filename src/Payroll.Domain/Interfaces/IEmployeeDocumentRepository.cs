using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IEmployeeDocumentRepository
{
    Task AddAsync(EmployeeDocument document, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeDocument>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<EmployeeDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
