using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Payroll.Domain.Constants;
using Payroll.Infrastructure.Persistence;
using Payroll.Api.Tests.Infrastructure;
using Xunit;

namespace Payroll.Api.Tests.Auth;

[Collection("Integration")]
public sealed class SeedTests
{
    private readonly PostgresFixture _postgres;
    private readonly RedisFixture _redis;
    private readonly PayrollWebApplicationFactory _factory;

    public SeedTests(PostgresFixture postgres, RedisFixture redis)
    {
        _postgres = postgres;
        _redis = redis;
        _factory = new PayrollWebApplicationFactory(postgres, redis);
    }

    [Fact]
    public async Task AllSixRolesExist_AfterStartup()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        RoleManager<ApplicationRole> roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (string role in Roles.All)
        {
            bool exists = await roleManager.RoleExistsAsync(role);
            exists.Should().BeTrue($"role '{role}' should be seeded at startup");
        }
    }

    [Fact]
    public async Task SuperAdminUser_ExistsWithCorrectProperties()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        ApplicationUser? user = await userManager.FindByEmailAsync("superadmin@test.local");
        user.Should().NotBeNull();
        user!.IsSuperAdmin.Should().BeTrue();
        user.TenantId.Should().BeNull();

        IList<string> roles = await userManager.GetRolesAsync(user);
        roles.Should().Contain(Roles.SuperAdmin);
    }

    [Fact]
    public async Task OpenIddictClient_IsRegistered()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IOpenIddictApplicationManager appManager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictApplicationManager>();

        object? app = await appManager.FindByClientIdAsync("payroll-api");
        app.Should().NotBeNull("OpenIddict client 'payroll-api' should be seeded at startup");
    }

    [Fact]
    public void SeedIsIdempotent_NoDuplicatesOnSecondRun()
    {
        // Starting the factory a second time should not throw or create duplicates
        using PayrollWebApplicationFactory factory2 = new(_postgres, _redis);

        using IServiceScope scope = factory2.Services.CreateScope();
        RoleManager<ApplicationRole> roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<ApplicationRole>>();

        int count = roleManager.Roles.Count();
        count.Should().Be(Roles.All.Length, "no duplicate roles should be created");
    }

    [Fact]
    public async Task MissingSuperAdminEmail_ThrowsOnStartup()
    {
        // Arrange: create factory without the env var
        string? original = Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL");
        try
        {
            Environment.SetEnvironmentVariable("SUPERADMIN_EMAIL", null);
            // The factory startup should throw InvalidOperationException
            Func<Task> act = async () =>
            {
                using PayrollWebApplicationFactory factory = new(
                    _factory.Services.GetRequiredService<PostgresFixture>(),
                    _factory.Services.GetRequiredService<RedisFixture>());
                _ = factory.Server; // triggers host startup
                await Task.CompletedTask;
            };
            await act.Should().ThrowAsync<Exception>();
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUPERADMIN_EMAIL", original);
        }
    }
}
