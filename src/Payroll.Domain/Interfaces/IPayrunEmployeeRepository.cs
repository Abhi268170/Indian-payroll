using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Interfaces;

public interface IPayrunEmployeeRepository
{
    Task<IReadOnlyList<PayrunEmployee>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default);
    Task<PayrunEmployee?> GetByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PayrunEmployee> employees, CancellationToken ct = default);
    Task AddAsync(PayrunEmployee employee, CancellationToken ct = default);
    void Update(PayrunEmployee employee);
    Task<IReadOnlyList<PayrunEmployee>> GetByRunIdWithStatusAsync(Guid payrollRunId, PayrunEmployeeStatus status, CancellationToken ct = default);
}
