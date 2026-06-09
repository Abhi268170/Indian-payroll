using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Middleware;

// Must be registered BEFORE UseAuthentication in Program.cs.
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger,
    IConfiguration configuration)
{
    // Public base domain the API is served under (the host the browser/web proxy
    // sends). When set, "<slug>.<baseDomain>" → slug, and the bare base domain →
    // platform (no tenant). When unset, falls back to the 2-label-base heuristic.
    private readonly string _baseDomain =
        (configuration["Tenancy:BaseDomain"] ?? string.Empty).Trim().TrimEnd('.').ToLowerInvariant();

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Infrastructure endpoints are not tenant-scoped — k8s probes hit them by
        // pod IP, which would otherwise be parsed as a (nonexistent) tenant slug.
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        string host = context.Request.Host.Host;
        string slug = ExtractSlug(host);

        if (string.IsNullOrEmpty(slug))
        {
            await next(context);
            return;
        }

        // ITenantResolver is resolved per-request via DI
        ITenantResolver resolver = context.RequestServices
            .GetRequiredService<ITenantResolver>();

        TenantInfo? tenant = await resolver.ResolveAsync(slug, context.RequestAborted);

        if (tenant is null)
        {
            logger.LogWarning("Tenant not found for slug: {Slug}", slug);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!tenant.IsActive)
        {
            logger.LogWarning("Tenant suspended: {Slug}", slug);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        tenantContext.SetTenant(tenant);
        await next(context);
    }

    private string ExtractSlug(string host)
    {
        host = host.Trim().ToLowerInvariant();

        // Base-domain-aware (e.g. base "plhb-fe-dev.mypits.org"):
        //   acme.plhb-fe-dev.mypits.org → "acme";  plhb-fe-dev.mypits.org → "" (platform)
        // Unknown hosts (e.g. a pod IP) → "" so they're treated as platform, not a bogus slug.
        if (!string.IsNullOrEmpty(_baseDomain))
        {
            if (host == _baseDomain)
                return string.Empty;

            string suffix = "." + _baseDomain;
            if (!host.EndsWith(suffix, StringComparison.Ordinal))
                return string.Empty;

            string prefix = host[..^suffix.Length];
            int dot = prefix.IndexOf('.');
            return dot >= 0 ? prefix[..dot] : prefix;
        }

        // Fallback (local/compose): acme-corp.payroll.example.com → "acme-corp"
        string[] parts = host.Split('.');
        return parts.Length >= 3 ? parts[0] : string.Empty;
    }
}

public interface ITenantResolver
{
    Task<TenantInfo?> ResolveAsync(string slug, CancellationToken cancellationToken = default);
}
