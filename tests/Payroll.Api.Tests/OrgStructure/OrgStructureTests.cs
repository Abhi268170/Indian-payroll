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
            AdminEmail = $"admin@{slug}.test",
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

    // ── Work Locations ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateWorkLocation_AsOrgAdmin_Returns201()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "WorkLoc Corp A", "workloc-a");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@workloc-a.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/v1/work-locations", orgToken, slug,
            new
            {
                Name = "Head Office",
                State = "Maharashtra",
                AddressLine1 = "1st Floor, Bandra Kurla Complex",
                City = "Mumbai",
                PinCode = "400051",
            });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task CreateWorkLocation_Unauthenticated_Returns401()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (_, string slug) = await ProvisionTenantAsync(
            client, superToken, "WorkLoc Corp B", "workloc-b");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/work-locations")
        {
            Content = JsonContent.Create(new { Name = "Branch 1", State = "Karnataka" }),
        };
        request.Headers.Host = TenantHost(slug);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWorkLocation_EmptyName_Returns400()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "WorkLoc Corp C", "workloc-c");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@workloc-c.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/v1/work-locations", orgToken, slug,
            new { Name = "", State = "Maharashtra" });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWorkLocation_InvalidState_Returns400()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "WorkLoc Corp D", "workloc-d");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@workloc-d.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/v1/work-locations", orgToken, slug,
            new { Name = "Bangalore Office", State = "NotARealState" });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWorkLocation_InvalidPinCode_Returns400()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "WorkLoc Corp E", "workloc-e");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@workloc-e.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/v1/work-locations", orgToken, slug,
            new { Name = "Office", State = "Delhi", PinCode = "ABCDEF" });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListWorkLocations_ReturnsOnlyTenantLocations()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantIdA, string slugA) = await ProvisionTenantAsync(
            client, superToken, "List WL Tenant A", "listwl-a");
        (Guid tenantIdB, string slugB) = await ProvisionTenantAsync(
            client, superToken, "List WL Tenant B", "listwl-b");

        string tokenA = await CreateTenantUserAndGetTokenAsync(
            client, tenantIdA, "orguser@listwl-a.test", "Admin@Test1!", Roles.OrgAdmin);
        string tokenB = await CreateTenantUserAndGetTokenAsync(
            client, tenantIdB, "orguser@listwl-b.test", "Admin@Test1!", Roles.OrgAdmin);

        // Create a work location in tenant A
        using HttpRequestMessage createReq = TenantRequest(
            HttpMethod.Post, "/api/v1/work-locations", tokenA, slugA,
            new { Name = "Tenant A Office", State = "Delhi" });
        (await client.SendAsync(createReq)).EnsureSuccessStatusCode();

        // List work locations as tenant B — should see zero
        using HttpRequestMessage listReq = TenantRequest(
            HttpMethod.Get, "/api/v1/work-locations", tokenB, slugB);
        HttpResponseMessage listResponse = await client.SendAsync(listReq);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<Dictionary<string, object>>? locations =
            await listResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        locations.Should().BeEmpty("tenant B must not see tenant A work locations");
    }

    [Fact]
    public async Task GetWorkLocation_Returns200()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Get WL Corp", "getwl-a");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@getwl-a.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        // Create
        using HttpRequestMessage createReq = TenantRequest(
            HttpMethod.Post, "/api/v1/work-locations", orgToken, slug,
            new { Name = "Chennai Hub", State = "TamilNadu", PinCode = "600001" });
        HttpResponseMessage createResp = await client.SendAsync(createReq);
        createResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        string id = created!["id"].ToString()!;

        // Get
        using HttpRequestMessage getReq = TenantRequest(
            HttpMethod.Get, $"/api/v1/work-locations/{id}", orgToken, slug);
        HttpResponseMessage getResp = await client.SendAsync(getReq);

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        Dictionary<string, object>? body = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
        body!["name"].ToString().Should().Be("Chennai Hub");
    }

    [Fact]
    public async Task GetWorkLocation_NotFound_Returns404()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Get WL 404", "getwl-404");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@getwl-404.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        Guid randomId = Guid.NewGuid();
        using HttpRequestMessage getReq = TenantRequest(
            HttpMethod.Get, $"/api/v1/work-locations/{randomId}", orgToken, slug);
        HttpResponseMessage response = await client.SendAsync(getReq);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateWorkLocation_Returns204()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Update WL Corp", "updatewl-a");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@updatewl-a.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        // Create
        using HttpRequestMessage createReq = TenantRequest(
            HttpMethod.Post, "/api/v1/work-locations", orgToken, slug,
            new { Name = "Pune Office", State = "Maharashtra" });
        HttpResponseMessage createResp = await client.SendAsync(createReq);
        createResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        string id = created!["id"].ToString()!;

        // Update — state must not change even if we want it to (not in payload)
        using HttpRequestMessage updateReq = TenantRequest(
            HttpMethod.Put, $"/api/v1/work-locations/{id}", orgToken, slug,
            new { Name = "Pune HQ", AddressLine1 = "Hinjewadi Phase 1", City = "Pune", PinCode = "411057" });
        HttpResponseMessage updateResp = await client.SendAsync(updateReq);

        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify name updated and state unchanged
        using HttpRequestMessage getReq = TenantRequest(
            HttpMethod.Get, $"/api/v1/work-locations/{id}", orgToken, slug);
        HttpResponseMessage getResp = await client.SendAsync(getReq);
        Dictionary<string, object>? body = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["name"].ToString().Should().Be("Pune HQ");
        body["state"].ToString().Should().Be("Maharashtra");
    }

    [Fact]
    public async Task DeleteWorkLocation_Returns204()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Delete WL Corp", "deletewl-a");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@deletewl-a.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        // Create
        using HttpRequestMessage createReq = TenantRequest(
            HttpMethod.Post, "/api/v1/work-locations", orgToken, slug,
            new { Name = "Hyderabad Branch", State = "Telangana" });
        HttpResponseMessage createResp = await client.SendAsync(createReq);
        createResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        string id = created!["id"].ToString()!;

        // Delete
        using HttpRequestMessage deleteReq = TenantRequest(
            HttpMethod.Delete, $"/api/v1/work-locations/{id}", orgToken, slug);
        HttpResponseMessage deleteResp = await client.SendAsync(deleteReq);

        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone (soft deleted, hidden by query filter)
        using HttpRequestMessage getReq = TenantRequest(
            HttpMethod.Get, $"/api/v1/work-locations/{id}", orgToken, slug);
        HttpResponseMessage getResp = await client.SendAsync(getReq);
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleActive_Deactivate_Returns204()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Toggle WL Corp", "togglewl-a");
        string orgToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "orgadmin@togglewl-a.test", "OrgAdmin@Test1!", Roles.OrgAdmin);

        // Create
        using HttpRequestMessage createReq = TenantRequest(
            HttpMethod.Post, "/api/v1/work-locations", orgToken, slug,
            new { Name = "Kolkata Office", State = "WestBengal" });
        HttpResponseMessage createResp = await client.SendAsync(createReq);
        createResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        string id = created!["id"].ToString()!;

        // Deactivate
        using HttpRequestMessage deactivateReq = TenantRequest(
            HttpMethod.Post, $"/api/v1/work-locations/{id}/deactivate", orgToken, slug);
        HttpResponseMessage deactivateResp = await client.SendAsync(deactivateReq);

        deactivateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify isActive = false
        using HttpRequestMessage getReq = TenantRequest(
            HttpMethod.Get, $"/api/v1/work-locations/{id}", orgToken, slug);
        HttpResponseMessage getResp = await client.SendAsync(getReq);
        Dictionary<string, object>? body = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["isActive"].ToString().Should().Be("False");
    }
}
