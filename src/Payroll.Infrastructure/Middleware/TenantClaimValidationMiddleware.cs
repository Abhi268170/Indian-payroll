using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Middleware;

// Must be registered AFTER UseAuthentication (so HttpContext.User is populated)
// and BEFORE UseAuthorization (so a 403 fires before role policy evaluation).
//
// Five-case predicate (IsResolved × claim-present):
//   IsResolved=false + no claim          → pass  (SuperAdmin on base domain, or anon)
//   IsResolved=false + claim + resolves  → pass  (tenant user, no subdomain — dev or direct API)
//   IsResolved=false + claim + not found → 403
//   IsResolved=true  + claim match       → pass
//   IsResolved=true  + claim mismatch    → 403
public sealed class TenantClaimValidationMiddleware(
    RequestDelegate next,
    ILogger<TenantClaimValidationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantResolver resolver)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        string? tenantIdClaim = context.User.FindFirstValue("tenant_id");
        string? tenantSlugClaim = context.User.FindFirstValue("tenant_slug");

        if (!tenantContext.IsResolved && tenantIdClaim is null)
        {
            if (!context.User.IsInRole("SuperAdmin"))
            {
                logger.LogWarning("TENANT_BYPASS_ATTEMPT: authenticated user with no tenant_id claim and not SuperAdmin");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await next(context);
            return;
        }

        if (!tenantContext.IsResolved && tenantSlugClaim is not null)
        {
            // No subdomain (local dev / direct API call) — resolve from JWT slug claim.
            // Security is maintained: TenantId from DB must match tenant_id claim below.
            TenantInfo? tenant = await resolver.ResolveAsync(tenantSlugClaim, context.RequestAborted);
            if (tenant is null || !tenant.IsActive)
            {
                logger.LogWarning("TENANT_RESOLVE_FAILED: slug from claim={Slug}", tenantSlugClaim);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            tenantContext.SetTenant(tenant);
        }

        // IsResolved == true beyond this point (either from subdomain or claim above)
        if (tenantIdClaim is not null
            && Guid.TryParse(tenantIdClaim, out Guid claimedId)
            && tenantContext.IsResolved
            && claimedId == tenantContext.TenantId)
        {
            await next(context);
            return;
        }

        logger.LogWarning(
            "TENANT_MISMATCH: claim={ClaimedId} context={ContextId}",
            tenantIdClaim ?? "<absent>",
            tenantContext.IsResolved ? tenantContext.TenantId.ToString() : "<unresolved>");
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }
}
