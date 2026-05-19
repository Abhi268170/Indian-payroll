using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Npgsql;
using Payroll.Api.Tests.Infrastructure;
using Xunit;

namespace Payroll.Api.Tests.Auth;

[Collection("Integration")]
public sealed class TenantProvisioningTests
{
    private readonly PostgresFixture _postgres;
    private readonly PayrollWebApplicationFactory _factory;

    public TenantProvisioningTests(PostgresFixture postgres, RedisFixture redis)
    {
        _postgres = postgres;
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
        HttpResponseMessage tokenResponse = await client.PostAsync("/connect/token", form);
        tokenResponse.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return body!["access_token"].ToString()!;
    }

    private async Task<bool> SchemaExistsAsync(string schemaName)
    {
        await using NpgsqlConnection conn = new(_postgres.ConnectionString);
        await conn.OpenAsync();
        await using NpgsqlCommand cmd = new(
            "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = $1", conn);
        cmd.Parameters.AddWithValue(schemaName);
        long count = (long)(await cmd.ExecuteScalarAsync())!;
        return count > 0;
    }

    [Fact]
    public async Task CreateTenant_AsSuperAdmin_Returns201AndCreatesSchema()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = "Acme Corp",
            Slug = "acme-corp",
            AdminEmail = "admin@acme-corp.test",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
        body!["slug"].ToString().Should().Be("acme-corp");

        bool schemaExists = await SchemaExistsAsync("tenant_acme_corp");
        schemaExists.Should().BeTrue("provisioner must create the tenant schema");
    }

    [Fact]
    public async Task CreateTenant_DuplicateSlug_Returns409()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        await client.PostAsJsonAsync("/api/tenants", new { DisplayName = "Beta Corp", Slug = "beta-corp", AdminEmail = "admin@beta-corp.test" });
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new { DisplayName = "Beta Corp 2", Slug = "beta-corp", AdminEmail = "admin2@beta-corp.test" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTenant_InvalidSlug_Returns400()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = "Bad Slug Corp",
            Slug = "UPPERCASE!!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTenant_NoAuthentication_Returns401()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = "No Auth Corp",
            Slug = "no-auth",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuspendTenant_AsSuperAdmin_Returns204()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = "Suspend Corp",
            Slug = "suspend-corp-x",
            AdminEmail = "admin@suspend-corp-x.test",
        });
        createResponse.EnsureSuccessStatusCode();
        Dictionary<string, object>? created = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        string tenantId = created!["id"].ToString()!;

        HttpResponseMessage suspendResponse = await client.PostAsync($"/api/tenants/{tenantId}/suspend", null);
        suspendResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SuspendTenant_NonExistentId_Returns404()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        HttpResponseMessage response = await client.PostAsync($"/api/tenants/{Guid.NewGuid()}/suspend", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
