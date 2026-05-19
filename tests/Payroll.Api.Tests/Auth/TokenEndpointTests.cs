using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Payroll.Domain.Constants;
using Payroll.Infrastructure.Persistence;
using Payroll.Api.Tests.Infrastructure;
using Xunit;

namespace Payroll.Api.Tests.Auth;

[Collection("Integration")]
public sealed class TokenEndpointTests
{
    private readonly PostgresFixture _postgres;
    private readonly PayrollWebApplicationFactory _factory;

    public TokenEndpointTests(PostgresFixture postgres, RedisFixture redis)
    {
        _postgres = postgres;
        _factory = new PayrollWebApplicationFactory(postgres, redis);
    }

    private static FormUrlEncodedContent PasswordForm(string username, string password) =>
        new([
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("client_id", "payroll-api"),
            new KeyValuePair<string, string>("client_secret", "test-client-secret"),
            new KeyValuePair<string, string>("scope", "openid profile email offline_access payroll.api"),
        ]);

    [Fact]
    public async Task PasswordFlow_SuperAdmin_Returns200WithAccessToken()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/connect/token",
            PasswordForm("superadmin@test.local", "SuperAdmin@Test1234!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("access_token");
        body!["access_token"].ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PasswordFlow_SuperAdmin_TokenHasNoTenantClaims()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/connect/token",
            PasswordForm("superadmin@test.local", "SuperAdmin@Test1234!"));

        response.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        string accessToken = body!["access_token"].ToString()!;

        JsonWebTokenHandler handler = new();
        JsonWebToken jwt = handler.ReadJsonWebToken(accessToken);
        jwt.Claims.Any(c => c.Type == "tenant_id").Should().BeFalse("SuperAdmin has no tenant");
    }

    [Fact]
    public async Task PasswordFlow_InvalidPassword_ReturnsErrorResponse()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/connect/token",
            PasswordForm("superadmin@test.local", "wrong-password"));

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task PasswordFlow_UnknownUser_ReturnsErrorResponse()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/connect/token",
            PasswordForm("nobody@example.com", "irrelevant"));

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task PasswordFlow_TenantUser_ReturnsTokenWithTenantIdClaim()
    {
        HttpClient client = _factory.CreateClient();

        // Get SuperAdmin token to create a tenant
        HttpResponseMessage tokenResp = await client.PostAsync(
            "/connect/token",
            PasswordForm("superadmin@test.local", "SuperAdmin@Test1234!"));
        tokenResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? tokenBody = await tokenResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        string adminToken = tokenBody!["access_token"].ToString()!;

        client.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);
        HttpResponseMessage createResp = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = "Token Test Corp",
            Slug = "token-test-corp",
            AdminEmail = "admin@token-test-corp.test",
        });
        createResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? tenantBody = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Guid tenantId = Guid.Parse(tenantBody!["id"].ToString()!);

        // Create a user for this tenant
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser tenantUser = new()
        {
            UserName = "orgadmin@token-test.local",
            Email = "orgadmin@token-test.local",
            EmailConfirmed = true,
            TenantId = tenantId,
        };
        IdentityResult createResult = await userManager.CreateAsync(tenantUser, "OrgAdmin@Test1234!");
        createResult.Succeeded.Should().BeTrue();
        await userManager.AddToRoleAsync(tenantUser, Roles.OrgAdmin);

        // Get token for tenant user
        client.DefaultRequestHeaders.Authorization = null;
        HttpResponseMessage tenantTokenResp = await client.PostAsync(
            "/connect/token",
            PasswordForm("orgadmin@token-test.local", "OrgAdmin@Test1234!"));

        tenantTokenResp.StatusCode.Should().Be(HttpStatusCode.OK);
        Dictionary<string, object>? tenantTokenBody = await tenantTokenResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        string tenantAccessToken = tenantTokenBody!["access_token"].ToString()!;

        JsonWebTokenHandler handler = new();
        JsonWebToken jwt = handler.ReadJsonWebToken(tenantAccessToken);
        string? claimedTenantId = jwt.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
        claimedTenantId.Should().Be(tenantId.ToString());
        jwt.Claims.Any(c => c.Type == "tenant_schema").Should().BeTrue();
    }

    [Fact]
    public async Task PasswordFlow_SuspendedTenant_ReturnsErrorResponse()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage tokenResp = await client.PostAsync(
            "/connect/token",
            PasswordForm("superadmin@test.local", "SuperAdmin@Test1234!"));
        tokenResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? adminTokenBody = await tokenResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        string adminToken = adminTokenBody!["access_token"].ToString()!;

        client.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);
        HttpResponseMessage createResp = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = "Suspended Corp",
            Slug = "suspended-corp",
            AdminEmail = "admin@suspended-corp.test",
        });
        createResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? tenantBody = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Guid tenantId = Guid.Parse(tenantBody!["id"].ToString()!);

        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser suspendedUser = new()
        {
            UserName = "employee@suspended.local",
            Email = "employee@suspended.local",
            EmailConfirmed = true,
            TenantId = tenantId,
        };
        IdentityResult createResult = await userManager.CreateAsync(suspendedUser, "Employee@Test1234!");
        createResult.Succeeded.Should().BeTrue();

        // Suspend the tenant
        string tenantIdStr = tenantId.ToString();
        HttpResponseMessage suspendResp = await client.PostAsync($"/api/tenants/{tenantIdStr}/suspend", null);
        suspendResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Try to get token for the user — should fail
        client.DefaultRequestHeaders.Authorization = null;
        HttpResponseMessage badTokenResp = await client.PostAsync(
            "/connect/token",
            PasswordForm("employee@suspended.local", "Employee@Test1234!"));

        badTokenResp.IsSuccessStatusCode.Should().BeFalse();
    }
}
