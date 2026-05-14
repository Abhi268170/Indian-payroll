using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Payroll.Domain.Constants;
using Payroll.Infrastructure.Persistence;
using Payroll.Api.Tests.Infrastructure;
using Xunit;

namespace Payroll.Api.Tests.Auth;

[Collection("Integration")]
public sealed class RoleAuthorizationTests
{
    private readonly PayrollWebApplicationFactory _factory;

    public RoleAuthorizationTests(PostgresFixture postgres, RedisFixture redis)
    {
        _factory = new PayrollWebApplicationFactory(postgres, redis);
    }

    private async Task<string> GetSuperAdminTokenAsync(HttpClient client)
    {
        FormUrlEncodedContent form = new([
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "superadmin@test.local"),
            new KeyValuePair<string, string>("password", "SuperAdmin@Test1234!"),
            new KeyValuePair<string, string>("client_id", "payroll-api"),
            new KeyValuePair<string, string>("client_secret", "test-client-secret"),
            new KeyValuePair<string, string>("scope", "openid profile email offline_access payroll.api"),
        ]);
        HttpResponseMessage response = await client.PostAsync("/connect/token", form);
        response.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return body!["access_token"].ToString()!;
    }

    private async Task<string> CreateUserTokenAsync(
        HttpClient client, Guid? tenantId, string email, string password, string role)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            TenantId = tenantId,
            IsSuperAdmin = false,
        };
        IdentityResult result = await userManager.CreateAsync(user, password);
        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, role);

        FormUrlEncodedContent form = new([
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", email),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("client_id", "payroll-api"),
            new KeyValuePair<string, string>("client_secret", "test-client-secret"),
            new KeyValuePair<string, string>("scope", "openid profile email offline_access payroll.api"),
        ]);
        HttpResponseMessage tokenResponse = await client.PostAsync("/connect/token", form);
        tokenResponse.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return body!["access_token"].ToString()!;
    }

    private async Task<Guid> ProvisionTenantAsync(HttpClient client, string superToken, string slug)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", superToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = $"Role Test Corp {slug}",
            Slug = slug,
        });
        response.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        client.DefaultRequestHeaders.Remove("Authorization");
        return Guid.Parse(body!["id"].ToString()!);
    }

    [Fact]
    public async Task OrgAdmin_CannotCreateTenant_Returns403()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        Guid tenantId = await ProvisionTenantAsync(client, superToken, "role-test-org-admin");

        string orgAdminToken = await CreateUserTokenAsync(
            client, tenantId, "orgadmin@roletest.local", "OrgAdmin@Test1!", Roles.OrgAdmin);

        client.DefaultRequestHeaders.Authorization = new("Bearer", orgAdminToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = "Unauthorized Corp",
            Slug = "unauthorized-corp",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "OrgAdmin is not SuperAdmin and must not create tenants");
    }

    [Fact]
    public async Task Employee_CannotCreateTenant_Returns403()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        Guid tenantId = await ProvisionTenantAsync(client, superToken, "role-test-employee");

        string employeeToken = await CreateUserTokenAsync(
            client, tenantId, "employee@roletest.local", "Employee@Test1!", Roles.Employee);

        client.DefaultRequestHeaders.Authorization = new("Bearer", employeeToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = "Unauthorized Corp",
            Slug = "unauthorized-corp-emp",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Employee role must not create tenants");
    }

    [Fact]
    public async Task SuperAdmin_CanCreateTenant_Returns201()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", superToken);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = "Super Auth Corp",
            Slug = "super-auth-corp",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ToProtectedEndpoint_Returns401()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = "No Auth Corp",
            Slug = "no-auth-corp",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
