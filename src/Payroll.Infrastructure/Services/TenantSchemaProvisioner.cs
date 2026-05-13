using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Services;

internal sealed class TenantSchemaProvisioner(IConfiguration configuration) : ITenantSchemaProvisioner
{
    // Defense-in-depth: schema names from Tenant.Create() are already validated by CreateTenantCommandValidator,
    // but we assert the format here since this executes raw SQL.
    private static readonly Regex SafeSchemaName = new(@"^tenant_[a-z0-9_]+$", RegexOptions.Compiled);

    public async Task ProvisionAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        AssertSafeSchemaName(schemaName);

        string cs = GetConnectionString();

        await using NpgsqlConnection conn = new(cs);
        await conn.OpenAsync(cancellationToken);
        await using (NpgsqlCommand createCmd = new($"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\"", conn))
            await createCmd.ExecuteNonQueryAsync(cancellationToken);

        // SearchPath routes unqualified DDL to the tenant schema so migration files
        // can remain schema-less (no hardcoded schema params in CreateTable calls).
        NpgsqlConnectionStringBuilder csb = new(cs) { SearchPath = schemaName };

        DbContextOptionsBuilder<PayrollDbContext> builder = new();
        builder
            .UseNpgsql(csb.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", schemaName);
                // No EnableRetryOnFailure here: EF Core migrations run DDL inside transactions,
                // but if the connection drops after the server commits, a retry would re-execute
                // CREATE TABLE statements that already landed → 42P07. Migrations run once; the
                // compensating DropAsync in CreateTenantHandler handles cleanup on failure.
            })
            .UseSnakeCaseNamingConvention();

        await using PayrollDbContext db = new(builder.Options, new ProvisioningTenantContext(schemaName));
        await db.Database.MigrateAsync(cancellationToken);
    }

    public async Task DropAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        AssertSafeSchemaName(schemaName);

        string cs = GetConnectionString();

        await using NpgsqlConnection conn = new(cs);
        await conn.OpenAsync(cancellationToken);
        await using NpgsqlCommand cmd = new($"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE", conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private string GetConnectionString() =>
        configuration.GetConnectionString("Payroll")
            ?? throw new InvalidOperationException("Connection string 'Payroll' not configured.");

    private static void AssertSafeSchemaName(string schemaName)
    {
        if (!SafeSchemaName.IsMatch(schemaName))
            throw new ArgumentException($"Unsafe schema name rejected: {schemaName}", nameof(schemaName));
    }

    private sealed class ProvisioningTenantContext(string schema) : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public string Schema => schema;
        public string Slug => "provisioning";
        public bool IsResolved => true;

        public void SetTenant(TenantInfo tenant) { }
    }
}
