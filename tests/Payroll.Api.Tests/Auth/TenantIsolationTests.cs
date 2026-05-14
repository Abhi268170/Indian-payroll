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
public sealed class TenantIsolationTests
{
    private readonly PayrollWebApplicationFactory _factory;

    public TenantIsolationTests(PostgresFixture postgres, RedisFixture redis)
    {
        _factory = new PayrollWebApplicationFactory(postgres, redis);
    }

    // Returns a host string with 3+ parts so TenantResolutionMiddleware extracts the slug.
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
        return (Guid.Parse(body!["id"].ToString()!), slug);
    }

    private async Task<string> CreateTenantUserAndGetTokenAsync(
        HttpClient client, Guid tenantId, string slug, string email, string password, string role)
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

    [Fact]
    public async Task TenantUser_CorrectSubdomain_PassesThroughMiddleware()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Isolation Corp A", "isolation-corp-a");
        client.DefaultRequestHeaders.Remove("Authorization");

        string tenantToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, slug,
            "user-a@isolation.test", "Isolation@Test1!", Roles.OrgAdmin);

        // Send to correct subdomain — middleware passes, endpoint returns 404 (no such route yet),
        // which is NOT 403. This proves TenantClaimValidationMiddleware did not block.
        using HttpRequestMessage request = TenantRequest(HttpMethod.Get, "/api/nonexistent", tenantToken, slug);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "correct-tenant token on correct subdomain must pass claim validation");
    }

    [Fact]
    public async Task TenantUser_WrongSubdomain_Returns403()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantAId, string slugA) = await ProvisionTenantAsync(
            client, superToken, "Cross Tenant A", "cross-tenant-a");
        (_, string slugB) = await ProvisionTenantAsync(
            client, superToken, "Cross Tenant B", "cross-tenant-b");
        client.DefaultRequestHeaders.Remove("Authorization");

        string tenantAToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantAId, slugA,
            "user-a@cross.test", "CrossA@Test1!", Roles.OrgAdmin);

        // Tenant A token → Tenant B subdomain: mismatch → 403
        using HttpRequestMessage request = TenantRequest(HttpMethod.Get, "/api/nonexistent", tenantAToken, slugB);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "tenant A token must be rejected on tenant B subdomain");
    }

    [Fact]
    public async Task SuperAdmin_BaseDomain_PassesThroughMiddleware()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);

        // No Host override → base domain → IsResolved=false, no tenant_id claim → pass
        HttpRequestMessage request = new(HttpMethod.Get, "/api/nonexistent");
        request.Headers.Authorization = new("Bearer", superToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "SuperAdmin on base domain must not be blocked by claim validation");
    }

    [Fact]
    public async Task SuperAdmin_TenantSubdomain_Returns403()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (_, string slug) = await ProvisionTenantAsync(
            client, superToken, "SA Subdomain Corp", "sa-subdomain-corp");
        client.DefaultRequestHeaders.Remove("Authorization");

        // SuperAdmin token (no tenant_id claim) on a tenant subdomain: IsResolved=true, claim absent → 403
        using HttpRequestMessage request = TenantRequest(HttpMethod.Get, "/api/nonexistent", superToken, slug);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "SuperAdmin token has no tenant_id claim, so tenant subdomain must 403");
    }

    [Fact]
    public async Task TenantUser_BaseDomain_Returns403()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Base Domain Corp", "base-domain-corp");
        client.DefaultRequestHeaders.Remove("Authorization");

        string tenantToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, slug,
            "user@basedomain.test", "BaseDom@Test1!", Roles.OrgAdmin);

        // Tenant token on base domain (no subdomain Host): IsResolved=false, claim present → 403
        HttpRequestMessage request = new(HttpMethod.Get, "/api/nonexistent");
        request.Headers.Authorization = new("Bearer", tenantToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "tenant user with tenant_id claim must be rejected on base domain");
    }

    [Fact]
    public async Task UnknownSubdomain_Returns404()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Get, "/api/nonexistent", superToken, "nonexistent-tenant");
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "TenantResolutionMiddleware returns 404 for unknown slugs");
    }

    [Fact]
    public async Task TestJwtKey_RejectedByProductionOpenIddict()
    {
        // Use POST /api/tenants (requires SuperAdmin auth) so UseAuthorization returns 401
        // when authentication fails. A nonexistent endpoint returns 404 regardless of auth,
        // which would be a false pass. An existing protected endpoint forces the real check.
        HttpClient client = _factory.CreateClient();
        string fakeToken = _factory.JwtFactory.CreateToken(
            Guid.NewGuid(), "fake@test.local", [Roles.SuperAdmin]);

        HttpRequestMessage request = new(HttpMethod.Post, "/api/tenants")
        {
            Content = JsonContent.Create(new { DisplayName = "Fake Corp", Slug = "fake-corp" }),
        };
        request.Headers.Authorization = new("Bearer", fakeToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "tokens signed with test key must be rejected by the real OpenIddict validator");
    }
}
