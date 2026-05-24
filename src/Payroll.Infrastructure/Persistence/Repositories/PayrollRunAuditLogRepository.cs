using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class PayrollRunAuditLogRepository(PayrollDbContext db) : IPayrollRunAuditLogRepository
{
    public async Task AddAsync(PayrollRunAuditLog entry, CancellationToken ct = default) =>
        await db.PayrollRunAuditLogs.AddAsync(entry, ct);

    public Task<IReadOnlyList<PayrollRunAuditLog>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default) =>
        db.PayrollRunAuditLogs
            .Where(e => e.PayrollRunId == payrollRunId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<PayrollRunAuditLog>>(t => t.Result, ct);
}
