using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Middleware;

namespace Payroll.Infrastructure.Persistence;

internal sealed class RedisTenantResolver(
    IDistributedCache cache,
    PlatformDbContext platformDb) : ITenantResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public async Task<TenantInfo?> ResolveAsync(string slug, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"tenant:slug:{slug}";
        string? cached = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached is not null)
            return JsonSerializer.Deserialize<TenantInfo>(cached);

        Domain.Entities.Tenant? tenant = await platformDb.Tenants
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

        if (tenant is null) return null;

        TenantInfo info = new(tenant.Id, tenant.Schema, tenant.Slug, tenant.IsActive);
        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(info),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return info;
    }
}
