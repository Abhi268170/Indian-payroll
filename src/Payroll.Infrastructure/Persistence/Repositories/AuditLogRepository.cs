using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(PayrollDbContext db) : IAuditLogRepository
{
    public Task AddAsync(AuditLog entry, CancellationToken ct = default) =>
        db.AuditLogs.AddAsync(entry, ct).AsTask();

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default) =>
        await db.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(ct);
}
