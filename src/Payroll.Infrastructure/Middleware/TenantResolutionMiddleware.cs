using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Middleware;

// Must be registered BEFORE UseAuthentication in Program.cs.
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
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

    private static string ExtractSlug(string host)
    {
        // Expected: acme-corp.payroll.example.com → slug = "acme-corp"
        string[] parts = host.Split('.');
        return parts.Length >= 3 ? parts[0] : string.Empty;
    }
}

public interface ITenantResolver
{
    Task<TenantInfo?> ResolveAsync(string slug, CancellationToken cancellationToken = default);
}
