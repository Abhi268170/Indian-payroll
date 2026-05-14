using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Payroll.Api.Tests.Infrastructure;
using StackExchange.Redis;
using Xunit;

namespace Payroll.Api.Tests.Platform;

[Collection("Integration")]
public sealed class TenantLifecycleTests
{
    private readonly RedisFixture _redis;
    private readonly PayrollWebApplicationFactory _factory;

    public TenantLifecycleTests(PostgresFixture postgres, RedisFixture redis)
    {
        _redis = redis;
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

    private async Task<Guid> CreateTenantAsync(HttpClient client, string slug)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = $"Lifecycle {slug}",
            Slug = slug,
            AdminEmail = $"admin@{slug}.com",
        });
        response.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return Guid.Parse(body!["id"].ToString()!);
    }

    private IDatabase GetRedisDb() =>
        ConnectionMultiplexer.Connect(_redis.ConnectionString).GetDatabase();

    [Fact]
    public async Task SuspendTenant_EvictsCachedEntry()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        string slug = $"suspend-evict-{Guid.NewGuid():N[..8]}";
        Guid id = await CreateTenantAsync(client, slug[..20]);

        // Seed a fake cache entry to simulate a warm cache
        IDatabase redis = GetRedisDb();
        await redis.StringSetAsync($"tenant:slug:{slug[..20]}", "cached-value", TimeSpan.FromMinutes(10));

        HttpResponseMessage suspendResponse = await client.PostAsync($"/api/tenants/{id}/suspend", null);
        suspendResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        RedisValue cached = await redis.StringGetAsync($"tenant:slug:{slug[..20]}");
        cached.IsNull.Should().BeTrue("suspend must evict the Redis cache entry");
    }

    [Fact]
    public async Task ActivateTenant_EvictsCachedEntry()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        string slug = $"activate-evict-{Guid.NewGuid():N[..8]}";
        Guid id = await CreateTenantAsync(client, slug[..20]);

        // Suspend first so we can activate
        await client.PostAsync($"/api/tenants/{id}/suspend", null);

        // Seed a fake cache entry
        IDatabase redis = GetRedisDb();
        await redis.StringSetAsync($"tenant:slug:{slug[..20]}", "cached-value", TimeSpan.FromMinutes(10));

        HttpResponseMessage activateResponse = await client.PostAsync($"/api/tenants/{id}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        RedisValue cached = await redis.StringGetAsync($"tenant:slug:{slug[..20]}");
        cached.IsNull.Should().BeTrue("activate must evict the Redis cache entry");
    }

    [Fact]
    public async Task ActivateTenant_UnknownId_Returns404()
    {
        HttpClient client = _factory.CreateClient();
        string token = await GetSuperAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        HttpResponseMessage response = await client.PostAsync($"/api/tenants/{Guid.NewGuid()}/activate", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActivateTenant_NoToken_Returns401()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsync($"/api/tenants/{Guid.NewGuid()}/activate", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
