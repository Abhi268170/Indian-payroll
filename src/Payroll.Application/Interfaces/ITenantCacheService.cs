namespace Payroll.Application.Interfaces;

public interface ITenantCacheService
{
    Task EvictAsync(string tenantSlug, CancellationToken ct = default);
}
