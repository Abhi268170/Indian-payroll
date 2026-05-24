using Microsoft.Extensions.Caching.Distributed;
using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Services;

internal sealed class TenantCacheService(IDistributedCache cache) : ITenantCacheService
{
    public Task EvictAsync(string tenantSlug, CancellationToken ct = default) =>
        cache.RemoveAsync($"tenant:slug:{tenantSlug}", ct);
}
