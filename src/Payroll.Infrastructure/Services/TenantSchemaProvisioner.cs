using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
            .UseSnakeCaseNamingConvention()
            // Without this, EF Core's model cache uses (contextType, designTime) as the key,
            // so all provisioner contexts share the model compiled for the FIRST tenant schema,
            // causing queries to run against the wrong schema for subsequent tenants.
            .ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();

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
        HashSet<string> existing = (await db.SalaryComponents
            .Where(c => c.TenantId == tenantId)
            .Select(c => c.Code)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        void AddEarning(string code, string name, string payslip,
            Domain.Enums.EarningType earningType, Domain.Enums.PayType payType,
            Domain.Enums.ComponentFormulaType formulaType,
            decimal? fixedAmt, decimal? pct,
            bool taxable, bool epf, Domain.Enums.EpfInclusionRule epfRule, bool esi,
            bool proRata, bool showInPayslip, bool active = true)
        {
            if (existing.Contains(code)) return;
            var c = Domain.Entities.SalaryComponent.CreateEarning(
                name, payslip, code, earningType, payType, formulaType,
                fixedAmt, pct, taxable, epf, epfRule, esi, proRata, showInPayslip,
                tenantId, Guid.Empty);
            if (!active) c.SetActive(false);
            db.SalaryComponents.Add(c);
        }

        void AddDeduction(string code, string name, Domain.Enums.DeductionFrequency freq)
        {
            if (existing.Contains(code)) return;
            db.SalaryComponents.Add(Domain.Entities.SalaryComponent.CreateDeduction(
                name, name, code, freq, tenantId, Guid.Empty));
        }

        void AddReimbursement(string code, string name, Domain.Enums.ReimbursementType rt,
            decimal amount, bool active = true)
        {
            if (existing.Contains(code)) return;
            var c = Domain.Entities.SalaryComponent.CreateReimbursement(
                name, name, code, rt, amount,
                Domain.Enums.UnclaimedReimbursementHandling.DoNotPay, tenantId, Guid.Empty);
            if (!active) c.SetActive(false);
            db.SalaryComponents.Add(c);
        }

        // ── System component ─────────────────────────────────────────────
        if (!existing.Contains("FIXED_ALLOWANCE"))
            db.SalaryComponents.Add(Domain.Entities.SalaryComponent.CreateSystemFixedAllowance(tenantId, Guid.Empty));

        // ── Earnings (Zoho defaults) ──────────────────────────────────────
        // 1. Basic — 50% of CTC, fixed, EPF always, ESI yes
        AddEarning("BASICSALARY", "Basic", "Basic",
            Domain.Enums.EarningType.Basic, Domain.Enums.PayType.Monthly,
            Domain.Enums.ComponentFormulaType.PercentOfCTC, null, 50m,
            taxable: true, epf: true, Domain.Enums.EpfInclusionRule.Always, esi: true,
            proRata: true, showInPayslip: true);

        // 2. House Rent Allowance — 50% of Basic, fixed, EPF no, ESI yes
        AddEarning("HOUSERENTALLOWANCE", "House Rent Allowance", "House Rent Allowance",
            Domain.Enums.EarningType.HouseRentAllowance, Domain.Enums.PayType.Monthly,
            Domain.Enums.ComponentFormulaType.PercentOfBasic, null, 50m,
            taxable: true, epf: false, Domain.Enums.EpfInclusionRule.Always, esi: true,
            proRata: true, showInPayslip: true);

        // 3. Conveyance Allowance — flat, fixed, EPF if<15k, ESI no
        AddEarning("CONVEYANCEALLOWANCE", "Conveyance Allowance", "Conveyance Allowance",
            Domain.Enums.EarningType.ConveyanceAllowance, Domain.Enums.PayType.Monthly,
            Domain.Enums.ComponentFormulaType.Fixed, 0m, null,
            taxable: true, epf: true, Domain.Enums.EpfInclusionRule.OnlyWhenPfWageBelowLimit, esi: false,
            proRata: true, showInPayslip: true);

        // 4. Children Education Allowance — flat, fixed, EPF if<15k, ESI yes, inactive
        AddEarning("CHILDRENEDALLOWANCE", "Children Education Allowance", "Children Education Allowance",
            Domain.Enums.EarningType.ChildrenEducationAllowance, Domain.Enums.PayType.Monthly,
            Domain.Enums.ComponentFormulaType.Fixed, 0m, null,
            taxable: true, epf: true, Domain.Enums.EpfInclusionRule.OnlyWhenPfWageBelowLimit, esi: true,
            proRata: true, showInPayslip: true, active: false);

        // 5. Transport Allowance — flat 1600, fixed, EPF if<15k, ESI yes, inactive
        AddEarning("TRANSPORTALLOWANCE", "Transport Allowance", "Transport Allowance",
            Domain.Enums.EarningType.TransportAllowance, Domain.Enums.PayType.Monthly,
            Domain.Enums.ComponentFormulaType.Fixed, 1600m, null,
            taxable: true, epf: true, Domain.Enums.EpfInclusionRule.OnlyWhenPfWageBelowLimit, esi: true,
            proRata: true, showInPayslip: true, active: false);

        // 6. Travelling Allowance — flat, fixed, EPF if<15k, ESI no, inactive
        AddEarning("TRAVELLINGALLOWANCE", "Travelling Allowance", "Travelling Allowance",
            Domain.Enums.EarningType.TravellingAllowance, Domain.Enums.PayType.Monthly,
            Domain.Enums.ComponentFormulaType.Fixed, 0m, null,
            taxable: true, epf: true, Domain.Enums.EpfInclusionRule.OnlyWhenPfWageBelowLimit, esi: false,
            proRata: true, showInPayslip: true, active: false);

        // 7. Overtime Allowance — variable flat, EPF no, ESI yes, inactive
        AddEarning("OVERTIMEALLOWANCE", "Overtime Allowance", "Overtime Allowance",
            Domain.Enums.EarningType.OvertimeFlat, Domain.Enums.PayType.FlatAmount,
            Domain.Enums.ComponentFormulaType.Fixed, 0m, null,
            taxable: true, epf: false, Domain.Enums.EpfInclusionRule.Always, esi: true,
            proRata: false, showInPayslip: true, active: false);

        // 8. Gratuity — variable flat, EPF no, ESI no, non-taxable, active
        AddEarning("GRATUITY", "Gratuity", "Gratuity",
            Domain.Enums.EarningType.Gratuity, Domain.Enums.PayType.FlatAmount,
            Domain.Enums.ComponentFormulaType.Fixed, 0m, null,
            taxable: false, epf: false, Domain.Enums.EpfInclusionRule.Always, esi: false,
            proRata: false, showInPayslip: true);

        // 9. Bonus — variable flat, EPF no, ESI no, taxable, active
        AddEarning("BONUS", "Bonus", "Bonus",
            Domain.Enums.EarningType.Bonus, Domain.Enums.PayType.FlatAmount,
            Domain.Enums.ComponentFormulaType.Fixed, 0m, null,
            taxable: true, epf: false, Domain.Enums.EpfInclusionRule.Always, esi: false,
            proRata: false, showInPayslip: true);

        // 10. Commission — variable flat, EPF no, ESI yes, taxable, active
        AddEarning("COMMISSION", "Commission", "Commission",
            Domain.Enums.EarningType.CommissionOnSales, Domain.Enums.PayType.FlatAmount,
            Domain.Enums.ComponentFormulaType.Fixed, 0m, null,
            taxable: true, epf: false, Domain.Enums.EpfInclusionRule.Always, esi: true,
            proRata: false, showInPayslip: true);

        // 11. Leave Encashment — variable flat, EPF no, ESI no, taxable, active
        AddEarning("LEAVEENCASHMENT", "Leave Encashment", "Leave Encashment",
            Domain.Enums.EarningType.LeaveEncashment, Domain.Enums.PayType.FlatAmount,
            Domain.Enums.ComponentFormulaType.Fixed, 0m, null,
            taxable: true, epf: false, Domain.Enums.EpfInclusionRule.Always, esi: false,
            proRata: false, showInPayslip: true);

        // 12. Notice Pay — variable flat, EPF no, ESI no, taxable, active
        AddEarning("NOTICEPAY", "Notice Pay", "Notice Pay",
            Domain.Enums.EarningType.NoticePay, Domain.Enums.PayType.FlatAmount,
            Domain.Enums.ComponentFormulaType.Fixed, 0m, null,
            taxable: true, epf: false, Domain.Enums.EpfInclusionRule.Always, esi: false,
            proRata: false, showInPayslip: true);

        // 13. Hold Salary — variable flat, EPF no, ESI no, non-taxable, active
        AddEarning("HOLDSALARY", "Hold Salary", "Hold Salary",
            Domain.Enums.EarningType.HoldSalary, Domain.Enums.PayType.FlatAmount,
            Domain.Enums.ComponentFormulaType.Fixed, 0m, null,
            taxable: false, epf: false, Domain.Enums.EpfInclusionRule.Always, esi: false,
            proRata: false, showInPayslip: true);

        // ── Deductions ────────────────────────────────────────────────────
        AddDeduction("WITHHELDSALARY", "Withheld Salary", Domain.Enums.DeductionFrequency.OnceAYear);
        AddDeduction("NOTICEPAYDEDUC", "Notice Pay Deduction", Domain.Enums.DeductionFrequency.OnceAYear);
        AddDeduction("LOANRECOVERY", "Loan Recovery", Domain.Enums.DeductionFrequency.EveryMonth);

        // ── Benefits ──────────────────────────────────────────────────────
        if (!existing.Contains("VOLUNTARYPROVIDENTFUND"))
        {
            var vpf = Domain.Entities.SalaryComponent.CreateBenefit(
                "Voluntary Provident Fund", "Voluntary Provident Fund", "VOLUNTARYPROVIDENTFUND",
                Domain.Enums.BenefitType.VPF, null, false, null, tenantId, Guid.Empty);
            vpf.SetActive(false);
            db.SalaryComponents.Add(vpf);
        }

        // ── Reimbursements ────────────────────────────────────────────────
        AddReimbursement("FUELREIMBURSEMENT", "Fuel Reimbursement",
            Domain.Enums.ReimbursementType.FuelAndMaintenance, 0m, active: false);
        AddReimbursement("DRIVERREIMBURSEMENT", "Driver Reimbursement",
            Domain.Enums.ReimbursementType.DriverSalary, 0m, active: false);
        AddReimbursement("VEHICLEMAINTREIMBURSEMENT", "Vehicle Maintenance Reimbursement",
            Domain.Enums.ReimbursementType.FuelAndMaintenance, 0m, active: false);
        AddReimbursement("TELEPHONEREIMBURSEMENT", "Telephone Reimbursement",
            Domain.Enums.ReimbursementType.MobileAndInternet, 0m, active: false);
        AddReimbursement("LTAREIMBURSEMENT", "Leave Travel Allowance",
            Domain.Enums.ReimbursementType.LeaveTravelAssistance, 0m, active: false);
        AddReimbursement("FOODCOUPONS", "Food Coupons",
            Domain.Enums.ReimbursementType.FoodCoupons, 0m);

        // Correction for Basic (corrects salary arrears against Basic component).
        // FK requires Basic to exist, so look it up after adding above.
        if (!existing.Contains("BASICSALARYCORRECTION"))
        {
            // Can't use CreateCorrection (needs parent Id) until SaveChanges — skip for now;
            // tenants create corrections manually via the UI.
        }

        await db.SaveChangesAsync(cancellationToken);
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
