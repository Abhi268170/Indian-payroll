using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Payroll.Domain.Constants;
using Payroll.Infrastructure.Persistence;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Payroll.Infrastructure.Services;

// Seeds roles, SuperAdmin user, and OpenIddict client application at startup.
// All operations are idempotent: check-before-create on every resource.
// Runs after the inline PlatformDbContext migration in Program.cs (guaranteed table existence).
public sealed class SeedDataService(
    IServiceScopeFactory scopeFactory,
    ILogger<SeedDataService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        string superAdminEmail = Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL")
            ?? throw new InvalidOperationException(
                "SUPERADMIN_EMAIL environment variable is required but not set.");

        string superAdminPassword = Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD")
            ?? throw new InvalidOperationException(
                "SUPERADMIN_PASSWORD environment variable is required but not set.");

        string clientSecret = Environment.GetEnvironmentVariable("OPENIDDICT_CLIENT_SECRET")
            ?? throw new InvalidOperationException(
                "OPENIDDICT_CLIENT_SECRET environment variable is required but not set.");

        RoleManager<ApplicationRole> roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<ApplicationRole>>();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        IOpenIddictApplicationManager appManager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictApplicationManager>();
        IOpenIddictScopeManager scopeManager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictScopeManager>();

        await SeedRolesAsync(roleManager, cancellationToken);
        await SeedSuperAdminAsync(userManager, superAdminEmail, superAdminPassword, cancellationToken);
        await SeedOpenIddictClientAsync(appManager, clientSecret, cancellationToken);
        await SeedOpenIddictScopesAsync(scopeManager, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedRolesAsync(
        RoleManager<ApplicationRole> roleManager,
        CancellationToken cancellationToken)
    {
        foreach (string roleName in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            IdentityResult result = await roleManager.CreateAsync(new ApplicationRole(roleName));
            if (!result.Succeeded)
                logger.LogError("Failed to create role {Role}: {Errors}", roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            else
                logger.LogInformation("Created role {Role}", roleName);
        }
    }

    private async Task SeedSuperAdminAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        ApplicationUser? existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            logger.LogDebug("SuperAdmin user already exists: {Email}", email);
            return;
        }

        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsSuperAdmin = true,
            TenantId = null,
        };

        IdentityResult createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(
                $"Failed to create SuperAdmin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

        IdentityResult roleResult = await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException(
                $"Failed to assign SuperAdmin role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");

        logger.LogInformation("Created SuperAdmin user: {Email}", email);
    }

    private async Task SeedOpenIddictClientAsync(
        IOpenIddictApplicationManager appManager,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        object? existing = await appManager.FindByClientIdAsync("payroll-api", cancellationToken);
        if (existing is not null)
        {
            logger.LogDebug("OpenIddict client 'payroll-api' already exists");
            return;
        }

        await appManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "payroll-api",
            ClientSecret = clientSecret,
            DisplayName = "Indian Payroll API",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Revocation,
                Permissions.GrantTypes.Password,
                Permissions.GrantTypes.RefreshToken,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                $"{Permissions.Prefixes.Scope}payroll.api",
                $"{Permissions.Prefixes.Scope}{Scopes.OfflineAccess}",
            },
        }, cancellationToken);

        logger.LogInformation("Created OpenIddict client 'payroll-api'");
    }

    private async Task SeedOpenIddictScopesAsync(
        IOpenIddictScopeManager scopeManager,
        CancellationToken cancellationToken)
    {
        string[][] scopes = [["payroll.api", "Payroll API"], ["roles", "User Roles"]];
        foreach (string[] s in scopes)
        {
            if (await scopeManager.FindByNameAsync(s[0], cancellationToken) is not null)
                continue;
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = s[0],
                DisplayName = s[1],
            }, cancellationToken);
            logger.LogInformation("Created OpenIddict scope '{Scope}'", s[0]);
        }
    }
}
