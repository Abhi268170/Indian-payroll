using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Payroll.Api.Tests.Infrastructure;
using Xunit;

namespace Payroll.Api.Tests.Platform;

[Collection("Integration")]
public sealed class ResendSetupEmailTests
{
    private readonly PayrollWebApplicationFactory _factory;

    public ResendSetupEmailTests(PostgresFixture postgres, RedisFixture redis)
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
        HttpResponseMessage tokenResponse = await client.PostAsync("/connect/token", form);
        tokenResponse.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return body!["access_token"].ToString()!;
    }

    private async Task<Guid> CreateTenantAsync(HttpClient client, string slug, string adminEmail)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = $"Resend {slug}",
            Slug = slug,
            AdminEmail = adminEmail,
        });
        response.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return Guid.Parse(body!["id"].ToString()!);
    }

    [Fact]
    public async Task ResendSetupEmail_ActiveTenant_Returns204()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        Guid id = await CreateTenantAsync(client, "resend-active", "admin@resend-active.com");

        HttpResponseMessage response = await client.PostAsync($"/api/tenants/{id}/resend-setup-email", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResendSetupEmail_SuspendedTenant_Returns409()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        Guid id = await CreateTenantAsync(client, "resend-suspended", "admin@resend-suspended.com");
        await client.PostAsync($"/api/tenants/{id}/suspend", null);

        HttpResponseMessage response = await client.PostAsync($"/api/tenants/{id}/resend-setup-email", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ResendSetupEmail_UnknownId_Returns404()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        HttpResponseMessage response = await client.PostAsync($"/api/tenants/{Guid.NewGuid()}/resend-setup-email", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResendSetupEmail_NoToken_Returns401()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync($"/api/tenants/{Guid.NewGuid()}/resend-setup-email", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
