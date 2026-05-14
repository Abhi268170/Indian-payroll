using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Payroll.Domain.Constants;
using Payroll.Infrastructure.Persistence;
using Payroll.Api.Tests.Infrastructure;
using Xunit;

namespace Payroll.Api.Tests.OrgStructure;

[Collection("Integration")]
public sealed class OrgStructureTests
{
    private readonly PayrollWebApplicationFactory _factory;

    public OrgStructureTests(PostgresFixture postgres, RedisFixture redis)
    {
        _factory = new PayrollWebApplicationFactory(postgres, redis);
    }

    private static string TenantHost(string slug) => $"{slug}.payroll.localhost";

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

    private async Task<(Guid TenantId, string Slug)> ProvisionTenantAsync(
        HttpClient client, string superAdminToken, string displayName, string slug)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", superAdminToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = displayName,
            Slug = slug,
        });
        response.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        client.DefaultRequestHeaders.Remove("Authorization");
        return (Guid.Parse(body!["id"].ToString()!), slug);
    }

    private async Task<string> CreateTenantUserAndGetTokenAsync(
        HttpClient client, Guid tenantId, string email, string password, string role)
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

    private static HttpRequestMessage TenantRequest(HttpMethod method, string path, string token, string slug)
    {
        HttpRequestMessage request = new(method, path);
        request.Headers.Host = TenantHost(slug);
        request.Headers.Authorization = new("Bearer", token);
        return request;
    }

    private static HttpRequestMessage TenantRequest(HttpMethod method, string path, string token, string slug, object body)
    {
        HttpRequestMessage request = new(method, path)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Host = TenantHost(slug);
        request.Headers.Authorization = new("Bearer", token);
        return request;
    }

    [Fact]
    public async Task CreateBranch_AsOrgAdmin_Returns201()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "OrgStruct Corp A", "orgstruct-a");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@orgstruct-a.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/org/branches", orgToken, slug,
            new { Name = "Head Office", State = "MH" });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task CreateBranch_Unauthenticated_Returns401()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (_, string slug) = await ProvisionTenantAsync(
            client, superToken, "OrgStruct Corp B", "orgstruct-b");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/org/branches")
        {
            Content = JsonContent.Create(new { Name = "Branch 1", State = "KA" }),
        };
        request.Headers.Host = TenantHost(slug);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateBranch_InvalidName_Returns400()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "OrgStruct Corp C", "orgstruct-c");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@orgstruct-c.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/org/branches", orgToken, slug,
            new { Name = "", State = "MH" });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListBranches_ReturnsOnlyTenantBranches()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantIdA, string slugA) = await ProvisionTenantAsync(
            client, superToken, "List Branches A", "listbranch-a");
        (Guid tenantIdB, string slugB) = await ProvisionTenantAsync(
            client, superToken, "List Branches B", "listbranch-b");

        string tokenA = await CreateTenantUserAndGetTokenAsync(
            client, tenantIdA, "admin@listbranch-a.test", "Admin@Test1!", Roles.OrgAdmin);
        string tokenB = await CreateTenantUserAndGetTokenAsync(
            client, tenantIdB, "admin@listbranch-b.test", "Admin@Test1!", Roles.OrgAdmin);

        // Create a branch in tenant A
        using HttpRequestMessage createReq = TenantRequest(
            HttpMethod.Post, "/api/org/branches", tokenA, slugA,
            new { Name = "Tenant A Branch", State = "DL" });
        (await client.SendAsync(createReq)).EnsureSuccessStatusCode();

        // List branches as tenant B — should see zero
        using HttpRequestMessage listReq = TenantRequest(HttpMethod.Get, "/api/org/branches", tokenB, slugB);
        HttpResponseMessage listResponse = await client.SendAsync(listReq);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<Dictionary<string, object>>? branches =
            await listResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        branches.Should().BeEmpty("tenant B must not see tenant A branches");
    }

    [Fact]
    public async Task CreateDepartment_AsOrgAdmin_Returns201()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Dept Corp A", "dept-corp-a");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@dept-a.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/org/departments", orgToken, slug,
            new { Name = "Engineering", Code = "ENG" });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateDesignation_AsOrgAdmin_Returns201()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Desig Corp A", "desig-corp-a");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@desig-a.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/org/designations", orgToken, slug,
            new { Name = "Software Engineer" });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCostCentre_AsOrgAdmin_Returns201()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "CC Corp A", "cc-corp-a");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@cc-a.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/org/cost-centres", orgToken, slug,
            new { Name = "IT Department", Code = "CC-IT" });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
