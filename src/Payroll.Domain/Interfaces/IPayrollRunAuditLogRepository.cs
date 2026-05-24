using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IPayrollRunAuditLogRepository
{
    Task AddAsync(PayrollRunAuditLog entry, CancellationToken ct = default);
    Task<IReadOnlyList<PayrollRunAuditLog>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default);
}
