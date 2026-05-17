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

    public async Task ProvisionAsync(string schemaName, Guid tenantId, CancellationToken cancellationToken = default)
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
        await SeedSystemComponentsAsync(db, tenantId, cancellationToken);
        await SeedStatutorySlabsAsync(db, cancellationToken);
    }

    private static async Task SeedSystemComponentsAsync(
        PayrollDbContext db,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        bool hasFixedAllowance = await db.SalaryComponents
            .AnyAsync(c => c.IsSystemComponent && c.TenantId == tenantId, cancellationToken);

        if (!hasFixedAllowance)
        {
            db.SalaryComponents.Add(
                Payroll.Domain.Entities.SalaryComponent.CreateSystemFixedAllowance(tenantId, Guid.Empty));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedStatutorySlabsAsync(PayrollDbContext db, CancellationToken ct)
    {
        bool hasSlabs = await db.ProfessionalTaxSlabs.AnyAsync(ct);
        if (!hasSlabs)
        {
            DateOnly eff = new(2025, 4, 1);
            Guid sys = Guid.Empty;

            // Professional Tax slabs (FY2025-26, monthly states only for v1)
            // Maharashtra — monthly, gender-split, Feb surcharge in top bracket
            db.ProfessionalTaxSlabs.AddRange(
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("MH", eff, "Monthly", "Male",    0m,      7499m,   0m,    false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("MH", eff, "Monthly", "Male",    7500m,   9999m,   175m,  false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("MH", eff, "Monthly", "Male",    10000m,  null,    200m,  true,  sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("MH", eff, "Monthly", "Female",  0m,      9999m,   0m,    false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("MH", eff, "Monthly", "Female",  10000m,  null,    200m,  true,  sys),
                // Karnataka — monthly
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("KA", eff, "Monthly", null, 0m,      14999m,  0m,    false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("KA", eff, "Monthly", null, 15000m,  24999m,  150m,  false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("KA", eff, "Monthly", null, 25000m,  null,    200m,  true,  sys),
                // Andhra Pradesh — monthly
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("AP", eff, "Monthly", null, 0m,      14999m,  0m,    false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("AP", eff, "Monthly", null, 15000m,  19999m,  150m,  false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("AP", eff, "Monthly", null, 20000m,  null,    200m,  false, sys),
                // Telangana — monthly
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("TS", eff, "Monthly", null, 0m,      14999m,  0m,    false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("TS", eff, "Monthly", null, 15000m,  19999m,  150m,  false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("TS", eff, "Monthly", null, 20000m,  null,    200m,  false, sys),
                // West Bengal — monthly
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("WB", eff, "Monthly", null, 0m,      8499m,   0m,    false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("WB", eff, "Monthly", null, 8500m,   9999m,   90m,   false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("WB", eff, "Monthly", null, 10000m,  14999m,  110m,  false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("WB", eff, "Monthly", null, 15000m,  24999m,  130m,  false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("WB", eff, "Monthly", null, 25000m,  39999m,  150m,  false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("WB", eff, "Monthly", null, 40000m,  null,    200m,  false, sys),
                // Tamil Nadu — half-yearly
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("TN", eff, "HalfYearly", null, 0m,     21000m,  0m,    false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("TN", eff, "HalfYearly", null, 21001m, null,    510m,  false, sys),
                // Kerala — half-yearly
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("KL", eff, "HalfYearly", null, 0m,     11999m,  0m,    false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("KL", eff, "HalfYearly", null, 12000m, 17999m,  120m,  false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("KL", eff, "HalfYearly", null, 18000m, 29999m,  180m,  false, sys),
                Payroll.Domain.Entities.ProfessionalTaxSlab.Create("KL", eff, "HalfYearly", null, 30000m, null,    240m,  false, sys));

            // LWF state configs (select active states)
            db.LwfStateConfigs.AddRange(
                Payroll.Domain.Entities.LwfStateConfig.Create("MH", eff, 6m,  12m,  false, null, null, null, null, "Monthly",   null, null, null,   sys),
                Payroll.Domain.Entities.LwfStateConfig.Create("KA", eff, 20m, 40m,  false, null, null, null, null, "Annual",    12,   31,   null,   sys),
                Payroll.Domain.Entities.LwfStateConfig.Create("AP", eff, 20m, 40m,  false, null, null, null, null, "Annual",    12,   31,   null,   sys),
                Payroll.Domain.Entities.LwfStateConfig.Create("TS", eff, 20m, 40m,  false, null, null, null, null, "Annual",    12,   31,   null,   sys),
                Payroll.Domain.Entities.LwfStateConfig.Create("WB", eff, 3m,  15m,  false, null, null, null, null, "Monthly",   null, null, null,   sys),
                Payroll.Domain.Entities.LwfStateConfig.Create("GJ", eff, 6m,  12m,  false, null, null, null, null, "Monthly",   null, null, null,   sys),
                Payroll.Domain.Entities.LwfStateConfig.Create("MP", eff, 10m, 10m,  false, null, null, null, null, "Monthly",   null, null, 10000m, sys),
                Payroll.Domain.Entities.LwfStateConfig.Create("CH", eff, 25m, 25m,  false, null, null, null, null, "Monthly",   null, null, null,   sys),
                Payroll.Domain.Entities.LwfStateConfig.Create("HR", eff, 0m,  0m,   true,  0.002m, 0.002m, 25m, 25m, "Monthly", null, null, 25000m, sys));

            // Income Tax — FY2025-26, New Regime
            db.IncomeTaxSlabs.AddRange(
                Payroll.Domain.Entities.IncomeTaxSlab.Create("2025-26", "New", 0m,       400000m,    0m,     sys),
                Payroll.Domain.Entities.IncomeTaxSlab.Create("2025-26", "New", 400000m,  800000m,    0.05m,  sys),
                Payroll.Domain.Entities.IncomeTaxSlab.Create("2025-26", "New", 800000m,  1200000m,   0.10m,  sys),
                Payroll.Domain.Entities.IncomeTaxSlab.Create("2025-26", "New", 1200000m, 1600000m,   0.15m,  sys),
                Payroll.Domain.Entities.IncomeTaxSlab.Create("2025-26", "New", 1600000m, 2000000m,   0.20m,  sys),
                Payroll.Domain.Entities.IncomeTaxSlab.Create("2025-26", "New", 2000000m, 2400000m,   0.25m,  sys),
                Payroll.Domain.Entities.IncomeTaxSlab.Create("2025-26", "New", 2400000m, null,       0.30m,  sys));

            db.IncomeTaxSurchargeSlabs.AddRange(
                Payroll.Domain.Entities.IncomeTaxSurchargeSlab.Create("2025-26", "New", 5000000m,   10000000m, 0.10m, sys),
                Payroll.Domain.Entities.IncomeTaxSurchargeSlab.Create("2025-26", "New", 10000000m,  20000000m, 0.15m, sys),
                Payroll.Domain.Entities.IncomeTaxSurchargeSlab.Create("2025-26", "New", 20000000m,  50000000m, 0.25m, sys),
                Payroll.Domain.Entities.IncomeTaxSurchargeSlab.Create("2025-26", "New", 50000000m,  null,      0.25m, sys));

            db.IncomeTaxConfigs.Add(
                Payroll.Domain.Entities.IncomeTaxConfig.Create(
                    "2025-26", "New",
                    standardDeduction: 75000m,
                    rebate87ALimit: 1200000m,
                    rebate87AAmount: 60000m,
                    employerStatutoryCap: 750000m,
                    npsEmployerMaxRate: 0.14m,
                    createdBy: sys));

            await db.SaveChangesAsync(ct);
        }
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
