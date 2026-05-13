using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

internal sealed class TenantRepository(PlatformDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Tenants.FindAsync([id], cancellationToken).AsTask();

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        db.Tenants.Add(tenant);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        db.Tenants.Remove(tenant);
        return Task.CompletedTask;
    }
}
