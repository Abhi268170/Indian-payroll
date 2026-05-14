using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Payroll.Infrastructure.Persistence;
using Respawn;
using Xunit;

namespace Payroll.Api.Tests.Infrastructure;

public sealed class PayrollWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private readonly RedisFixture _redis;
    private Respawner? _respawner;

    // TestJwtFactory creates tokens with an in-memory key — used ONLY for negative tests
    // that verify the real WAF rejects tokens signed by an unknown key.
    public TestJwtFactory JwtFactory { get; } = new("https://localhost");

    public PayrollWebApplicationFactory(PostgresFixture postgres, RedisFixture redis)
    {
        _postgres = postgres;
        _redis = redis;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override connection strings via in-memory config — AddInfrastructure reads from IConfiguration,
        // so all DbContext and Redis registrations will use the Testcontainers instances.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Payroll"] = _postgres.ConnectionString,
                ["Redis:ConnectionString"] = _redis.ConnectionString,
                // Env vars for startup seeds
                ["SUPERADMIN_EMAIL"] = "superadmin@test.local",
                ["SUPERADMIN_PASSWORD"] = "SuperAdmin@Test1234!",
                ["OPENIDDICT_CLIENT_SECRET"] = "test-client-secret",
                // 32-byte test key for AesEncryptionService (not used in production)
                ["Encryption:Key"] = "dGVzdGtleXRlc3RrZXl0ZXN0a2V5dGVzdGtleXRlc3Q=",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Env vars that SeedDataService reads via Environment.GetEnvironmentVariable
            Environment.SetEnvironmentVariable("SUPERADMIN_EMAIL", "superadmin@test.local");
            Environment.SetEnvironmentVariable("SUPERADMIN_PASSWORD", "SuperAdmin@Test1234!");
            Environment.SetEnvironmentVariable("OPENIDDICT_CLIENT_SECRET", "test-client-secret");
        });

        builder.UseEnvironment("Test");
    }

    public async Task InitializeAsync()
    {
        NpgsqlConnection conn = new(_postgres.ConnectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });
        await conn.CloseAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        NpgsqlConnection conn = new(_postgres.ConnectionString);
        await conn.OpenAsync();
        await _respawner!.ResetAsync(conn);
        await conn.CloseAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
