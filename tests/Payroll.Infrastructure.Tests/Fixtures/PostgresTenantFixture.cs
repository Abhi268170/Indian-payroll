using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Payroll.Infrastructure.Tests.Fixtures;

// Provisions a real Postgres container + a single tenant schema with all migrations
// applied. Each test gets a fresh DbContext scoped to that schema, so we can exercise
// the actual EF + repository code paths the runtime uses — not mocks.
public sealed class PostgresTenantFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithDatabase("payroll_test")
        .Build();

    private string _connectionStringWithSchema = null!;
    public const string TenantSchema = "tenant_intspec";
    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        await using NpgsqlConnection conn = new(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using (NpgsqlCommand cmd = new($"CREATE SCHEMA IF NOT EXISTS \"{TenantSchema}\"", conn))
            await cmd.ExecuteNonQueryAsync();

        NpgsqlConnectionStringBuilder csb = new(_pg.GetConnectionString()) { SearchPath = TenantSchema };
        _connectionStringWithSchema = csb.ConnectionString;

        await using PayrollDbContext db = NewContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _pg.DisposeAsync().AsTask();

    public PayrollDbContext NewContext()
    {
        DbContextOptionsBuilder<PayrollDbContext> builder = new();
        builder
            .UseNpgsql(_connectionStringWithSchema, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", TenantSchema);
            })
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();

        return new PayrollDbContext(builder.Options, new FixedTenantContext(TenantSchema, TenantId));
    }

    private sealed class FixedTenantContext(string schema, Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
        public string Schema { get; } = schema;
        public string Slug => "intspec";
        public bool IsResolved => true;
        public void SetTenant(TenantInfo tenant) =>
            throw new NotSupportedException("Test fixture tenant is fixed.");
    }
}
