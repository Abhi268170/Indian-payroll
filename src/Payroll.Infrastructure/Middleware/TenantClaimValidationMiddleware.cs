using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Middleware;

// Must be registered AFTER UseAuthentication (so HttpContext.User is populated)
// and BEFORE UseAuthorization (so a 403 fires before role policy evaluation).
//
// Four-case predicate (IsResolved × claim-present):
//   IsResolved=false + no claim   → pass  (SuperAdmin on base domain)
//   IsResolved=false + claim      → 403   (tenant user calling base domain)
//   IsResolved=true  + match      → pass
//   IsResolved=true  + mismatch   → 403
public sealed class TenantClaimValidationMiddleware(
    RequestDelegate next,
    ILogger<TenantClaimValidationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        string? tenantIdClaim = context.User.FindFirstValue("tenant_id");

        if (!tenantContext.IsResolved && tenantIdClaim is null)
        {
            // SuperAdmin on base domain — legitimate
            await next(context);
            return;
        }

        if (!tenantContext.IsResolved && tenantIdClaim is not null)
        {
            logger.LogWarning(
                "TENANT_MISMATCH: tenant user (claim={TenantId}) called base domain",
                tenantIdClaim);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // IsResolved == true beyond this point
        if (tenantIdClaim is not null
            && Guid.TryParse(tenantIdClaim, out Guid claimedId)
            && claimedId == tenantContext.TenantId)
        {
            await next(context);
            return;
        }

        logger.LogWarning(
            "TENANT_MISMATCH: claim={ClaimedId} context={ContextId}",
            tenantIdClaim ?? "<absent>",
            tenantContext.TenantId);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }
}
