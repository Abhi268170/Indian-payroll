using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations.PayrollDb
{
    /// <summary>
    /// Consolidates three migrations that were committed without Designer files and so were
    /// invisible to EF (silently skipped by MigrateAsync). On any tenant that didn't have
    /// them manually applied, this catch-up adds the eps_amount column on payrun_employees,
    /// drops the deprecated EDLI/Admin columns from statutory_org_configs / payrun_employees
    /// / payroll_runs / income_tax_configs, and seeds FY 2026-27 income-tax data.
    /// All operations are written defensively (IF EXISTS / ON CONFLICT) so the migration is
    /// safe to run on tenants where some changes already landed via earlier manual fixes.
    /// </summary>
    public partial class CatchupEdliAdminEpsAndIT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 0a) DropCostCentre — best-effort drop on whatever schema MigrateAsync runs in
            //     (the tenant-loop in the original orphan iterated all tenants; here we're
            //     scoped to the tenant via SearchPath, so IF EXISTS is enough).
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS employees DROP COLUMN IF EXISTS cost_centre_id;
                DROP TABLE IF EXISTS cost_centres;
            ");

            // 0b) AddUniquePayrollRunPeriodConstraint
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_runs_tenant_period
                ON payroll_runs (tenant_id, pay_period_year, pay_period_month)
                WHERE deleted_at IS NULL;
            ");

            // 0c) AddStatutoryRatesToIncomeTaxConfig — adds 9 statutory rate columns. The
            //     edli_employer_rate / edli_cap / epf_admin_rate / epf_admin_minimum are
            //     dropped again by step 2 below, so we skip them here.
            migrationBuilder.Sql(@"
                ALTER TABLE income_tax_configs
                    ADD COLUMN IF NOT EXISTS cess_rate          numeric(7,4)  NOT NULL DEFAULT 0.04,
                    ADD COLUMN IF NOT EXISTS pf_wage_cap        numeric(18,4) NOT NULL DEFAULT 15000,
                    ADD COLUMN IF NOT EXISTS epf_employee_rate  numeric(7,4)  NOT NULL DEFAULT 0.12,
                    ADD COLUMN IF NOT EXISTS eps_employer_rate  numeric(7,4)  NOT NULL DEFAULT 0.0833,
                    ADD COLUMN IF NOT EXISTS eps_cap            numeric(18,4) NOT NULL DEFAULT 1250,
                    ADD COLUMN IF NOT EXISTS esi_wage_limit     numeric(18,4) NOT NULL DEFAULT 21000,
                    ADD COLUMN IF NOT EXISTS esi_pwd_wage_limit numeric(18,4) NOT NULL DEFAULT 25000,
                    ADD COLUMN IF NOT EXISTS esi_employee_rate  numeric(7,4)  NOT NULL DEFAULT 0.0075,
                    ADD COLUMN IF NOT EXISTS esi_employer_rate  numeric(7,4)  NOT NULL DEFAULT 0.0325;
            ");

            // 1) AddEpsAndAdminToPayrunEmployee — keep only eps_amount; admin_amount is
            //    immediately dropped again by step 2, so we don't bother adding it.
            migrationBuilder.Sql(@"
                ALTER TABLE payrun_employees
                    ADD COLUMN IF NOT EXISTS eps_amount numeric(18,4) NOT NULL DEFAULT 0;
            ");

            // 2) RemoveEdliAdminColumns — drop the deprecated columns wherever they still
            //    exist. IF EXISTS makes this idempotent for tenants already migrated.
            migrationBuilder.Sql(@"
                ALTER TABLE payrun_employees       DROP COLUMN IF EXISTS edli_amount;
                ALTER TABLE payrun_employees       DROP COLUMN IF EXISTS admin_amount;
                ALTER TABLE payroll_runs           DROP COLUMN IF EXISTS total_edli;
                ALTER TABLE income_tax_configs     DROP COLUMN IF EXISTS edli_employer_rate;
                ALTER TABLE income_tax_configs     DROP COLUMN IF EXISTS edli_cap;
                ALTER TABLE income_tax_configs     DROP COLUMN IF EXISTS epf_admin_rate;
                ALTER TABLE income_tax_configs     DROP COLUMN IF EXISTS epf_admin_minimum;
                ALTER TABLE statutory_org_configs  DROP COLUMN IF EXISTS epf_include_edli_in_ctc;
                ALTER TABLE statutory_org_configs  DROP COLUMN IF EXISTS epf_include_admin_in_ctc;
            ");

            // 3) SeedFY2026_27IncomeTaxData — ON CONFLICT DO NOTHING so reruns are safe.
            migrationBuilder.Sql(@"
                INSERT INTO income_tax_configs (
                    id, fiscal_year, regime,
                    standard_deduction, rebate87a_limit, rebate87a_amount,
                    employer_statutory_cap, nps_employer_max_rate,
                    cess_rate,
                    pf_wage_cap, epf_employee_rate, eps_employer_rate, eps_cap,
                    esi_wage_limit, esi_pwd_wage_limit, esi_employee_rate, esi_employer_rate,
                    created_at, created_by, is_deleted
                ) VALUES (
                    gen_random_uuid(), '2026-27', 'New',
                    75000, 1200000, 60000,
                    150000, 0.10,
                    0.04,
                    15000, 0.12, 0.0833, 1250,
                    21000, 25000, 0.0075, 0.0325,
                    now(), '00000000-0000-0000-0000-000000000001', false
                ) ON CONFLICT (fiscal_year, regime) DO NOTHING;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO income_tax_slabs (id, fiscal_year, regime, bracket_min, bracket_max, rate, created_at, created_by, is_deleted)
                SELECT * FROM (VALUES
                    (gen_random_uuid(), '2026-27', 'New',       0::bigint,  400000::bigint, 0.00::numeric, now(), '00000000-0000-0000-0000-000000000001'::uuid, false),
                    (gen_random_uuid(), '2026-27', 'New',  400000::bigint,  800000::bigint, 0.05::numeric, now(), '00000000-0000-0000-0000-000000000001'::uuid, false),
                    (gen_random_uuid(), '2026-27', 'New',  800000::bigint, 1200000::bigint, 0.10::numeric, now(), '00000000-0000-0000-0000-000000000001'::uuid, false),
                    (gen_random_uuid(), '2026-27', 'New', 1200000::bigint, 1600000::bigint, 0.15::numeric, now(), '00000000-0000-0000-0000-000000000001'::uuid, false),
                    (gen_random_uuid(), '2026-27', 'New', 1600000::bigint, 2000000::bigint, 0.20::numeric, now(), '00000000-0000-0000-0000-000000000001'::uuid, false),
                    (gen_random_uuid(), '2026-27', 'New', 2000000::bigint, 2400000::bigint, 0.25::numeric, now(), '00000000-0000-0000-0000-000000000001'::uuid, false),
                    (gen_random_uuid(), '2026-27', 'New', 2400000::bigint,         NULL,    0.30::numeric, now(), '00000000-0000-0000-0000-000000000001'::uuid, false)
                ) AS new_rows
                WHERE NOT EXISTS (
                    SELECT 1 FROM income_tax_slabs WHERE fiscal_year = '2026-27' AND regime = 'New'
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO income_tax_surcharge_slabs (id, fiscal_year, regime, income_from, income_to, surcharge_rate, created_at, created_by, is_deleted)
                SELECT * FROM (VALUES
                    (gen_random_uuid(), '2026-27', 'New',  5000000::bigint, 10000000::bigint, 0.10::numeric, now(), '00000000-0000-0000-0000-000000000001'::uuid, false),
                    (gen_random_uuid(), '2026-27', 'New', 10000000::bigint, 20000000::bigint, 0.15::numeric, now(), '00000000-0000-0000-0000-000000000001'::uuid, false),
                    (gen_random_uuid(), '2026-27', 'New', 20000000::bigint,         NULL,     0.25::numeric, now(), '00000000-0000-0000-0000-000000000001'::uuid, false)
                ) AS new_rows
                WHERE NOT EXISTS (
                    SELECT 1 FROM income_tax_surcharge_slabs WHERE fiscal_year = '2026-27' AND regime = 'New'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM income_tax_surcharge_slabs WHERE fiscal_year = '2026-27' AND regime = 'New';");
            migrationBuilder.Sql(@"DELETE FROM income_tax_slabs WHERE fiscal_year = '2026-27' AND regime = 'New';");
            migrationBuilder.Sql(@"DELETE FROM income_tax_configs WHERE fiscal_year = '2026-27' AND regime = 'New';");

            migrationBuilder.Sql(@"
                ALTER TABLE payrun_employees       DROP COLUMN IF EXISTS eps_amount;
                ALTER TABLE statutory_org_configs  ADD COLUMN IF NOT EXISTS epf_include_admin_in_ctc boolean NOT NULL DEFAULT false;
                ALTER TABLE statutory_org_configs  ADD COLUMN IF NOT EXISTS epf_include_edli_in_ctc boolean NOT NULL DEFAULT false;
                ALTER TABLE income_tax_configs     ADD COLUMN IF NOT EXISTS epf_admin_minimum numeric(18,4) NOT NULL DEFAULT 0;
                ALTER TABLE income_tax_configs     ADD COLUMN IF NOT EXISTS epf_admin_rate numeric(7,4) NOT NULL DEFAULT 0;
                ALTER TABLE income_tax_configs     ADD COLUMN IF NOT EXISTS edli_cap numeric(18,4) NOT NULL DEFAULT 0;
                ALTER TABLE income_tax_configs     ADD COLUMN IF NOT EXISTS edli_employer_rate numeric(7,4) NOT NULL DEFAULT 0;
                ALTER TABLE payroll_runs           ADD COLUMN IF NOT EXISTS total_edli numeric(18,2) NOT NULL DEFAULT 0;
                ALTER TABLE payrun_employees       ADD COLUMN IF NOT EXISTS admin_amount numeric(18,4) NOT NULL DEFAULT 0;
                ALTER TABLE payrun_employees       ADD COLUMN IF NOT EXISTS edli_amount numeric(18,2) NOT NULL DEFAULT 0;
            ");
        }
    }
}
