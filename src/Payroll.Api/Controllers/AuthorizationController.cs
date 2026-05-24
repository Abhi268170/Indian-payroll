using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Payroll.Domain.Entities;
using Payroll.Infrastructure.Persistence;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Payroll.Api.Controllers;

// AllowAnonymous required: this IS the authentication endpoint — no bearer token exists yet.
[AllowAnonymous]
[ApiController]
public sealed class AuthorizationController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    PlatformDbContext platformDb) : ControllerBase
{
    [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        OpenIddictRequest request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict server request not found.");

        if (!request.IsPasswordGrantType())
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        ApplicationUser? user = await userManager.FindByNameAsync(request.Username ?? string.Empty);
        if (user is null)
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        SignInResult signInResult = await signInManager.CheckPasswordSignInAsync(
            user, request.Password ?? string.Empty, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // SL-002: block token issuance for users whose tenant is suspended.
        Tenant? tenant = null;
        if (user.TenantId.HasValue)
        {
            tenant = await platformDb.Tenants.FindAsync([user.TenantId.Value], cancellationToken);
            if (tenant is null || !tenant.IsActive)
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        ClaimsIdentity identity = new(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        identity.SetClaim(OpenIddictConstants.Claims.Email, user.Email ?? string.Empty);
        identity.SetClaim(OpenIddictConstants.Claims.Name, user.UserName ?? string.Empty);

        IList<string> roles = await userManager.GetRolesAsync(user);
        foreach (string role in roles)
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));

        if (user.TenantId.HasValue && tenant is not null)
        {
            identity.SetClaim("tenant_id", user.TenantId.Value.ToString());
            identity.SetClaim("tenant_schema", tenant.Schema);
            identity.SetClaim("tenant_slug", tenant.Slug);
        }

        ClaimsPrincipal principal = new(identity);
        principal.SetScopes(request.GetScopes());

        foreach (Claim claim in principal.Claims)
            claim.SetDestinations(GetDestinations(claim));

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static IEnumerable<string> GetDestinations(Claim claim) =>
        claim.Type switch
        {
            OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Email =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Subject =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Role =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            _ => [OpenIddictConstants.Destinations.AccessToken],
        };
}
