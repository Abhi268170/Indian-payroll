using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations.PayrollDb
{
    /// <inheritdoc />
    public partial class AuditFixStatutoryConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "february_amount",
                table: "professional_tax_slabs",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "edli_max_amount",
                table: "income_tax_configs",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "edli_rate",
                table: "income_tax_configs",
                type: "numeric(7,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "edli_wage_cap",
                table: "income_tax_configs",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "epf_admin_rate",
                table: "income_tax_configs",
                type: "numeric(7,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pan206aa_rate",
                table: "income_tax_configs",
                type: "numeric(7,4)",
                nullable: false,
                defaultValue: 0m);

            // ── Data fixes (per-tenant schema; unqualified table names) ──────────

            // 1. Unify fiscal-year keys to the canonical "YYYY-YY" format.
            //    Old provisioner seeded "2026"/"2027"; the catch-up migration seeded
            //    '2026-27'. Run/preview paths now all read "YYYY-YY".
            migrationBuilder.Sql("""
                DELETE FROM income_tax_slabs
                 WHERE fiscal_year = '2027'
                   AND EXISTS (SELECT 1 FROM income_tax_slabs WHERE fiscal_year = '2026-27');
                UPDATE income_tax_slabs SET fiscal_year = '2026-27' WHERE fiscal_year = '2027';
                UPDATE income_tax_slabs SET fiscal_year = '2025-26' WHERE fiscal_year = '2026';

                DELETE FROM income_tax_surcharge_slabs
                 WHERE fiscal_year = '2027'
                   AND EXISTS (SELECT 1 FROM income_tax_surcharge_slabs WHERE fiscal_year = '2026-27');
                UPDATE income_tax_surcharge_slabs SET fiscal_year = '2026-27' WHERE fiscal_year = '2027';
                UPDATE income_tax_surcharge_slabs SET fiscal_year = '2025-26' WHERE fiscal_year = '2026';

                DELETE FROM income_tax_configs AS c
                 WHERE c.fiscal_year = '2027'
                   AND EXISTS (SELECT 1 FROM income_tax_configs c2
                                WHERE c2.fiscal_year = '2026-27' AND c2.regime = c.regime);
                UPDATE income_tax_configs SET fiscal_year = '2026-27' WHERE fiscal_year = '2027';
                UPDATE income_tax_configs SET fiscal_year = '2025-26' WHERE fiscal_year = '2026';
                """);

            // 2. Backfill FY 2025-26 from 2026-27 where a tenant has neither key
            //    (slabs are identical across both FYs — Budget 2026 changed nothing).
            migrationBuilder.Sql("""
                INSERT INTO income_tax_slabs (id, fiscal_year, regime, bracket_min, bracket_max, rate, created_at, created_by, is_deleted)
                SELECT gen_random_uuid(), '2025-26', regime, bracket_min, bracket_max, rate, now(), created_by, false
                  FROM income_tax_slabs
                 WHERE fiscal_year = '2026-27'
                   AND NOT EXISTS (SELECT 1 FROM income_tax_slabs WHERE fiscal_year = '2025-26');

                INSERT INTO income_tax_surcharge_slabs (id, fiscal_year, regime, income_from, income_to, surcharge_rate, created_at, created_by, is_deleted)
                SELECT gen_random_uuid(), '2025-26', regime, income_from, income_to, surcharge_rate, now(), created_by, false
                  FROM income_tax_surcharge_slabs
                 WHERE fiscal_year = '2026-27'
                   AND NOT EXISTS (SELECT 1 FROM income_tax_surcharge_slabs WHERE fiscal_year = '2025-26');

                INSERT INTO income_tax_configs (id, fiscal_year, regime, standard_deduction, rebate87a_limit, rebate87a_amount,
                        employer_statutory_cap, nps_employer_max_rate, cess_rate, pf_wage_cap, epf_employee_rate,
                        eps_employer_rate, eps_cap, esi_wage_limit, esi_pwd_wage_limit, esi_employee_rate, esi_employer_rate,
                        created_at, created_by, is_deleted)
                SELECT gen_random_uuid(), '2025-26', regime, standard_deduction, rebate87a_limit, rebate87a_amount,
                        employer_statutory_cap, nps_employer_max_rate, cess_rate, pf_wage_cap, epf_employee_rate,
                        eps_employer_rate, eps_cap, esi_wage_limit, esi_pwd_wage_limit, esi_employee_rate, esi_employer_rate,
                        now(), created_by, false
                  FROM income_tax_configs
                 WHERE fiscal_year = '2026-27'
                   AND NOT EXISTS (SELECT 1 FROM income_tax_configs WHERE fiscal_year = '2025-26');
                """);

            // 3. Fix the wrong employer-NPS values inserted by the catch-up migration
            //    (Sec 17(2)(vii) aggregate cap ₹7.5L; 80CCD(2) employer NPS 14% in new regime).
            migrationBuilder.Sql("""
                UPDATE income_tax_configs
                   SET employer_statutory_cap = 750000, nps_employer_max_rate = 0.14
                 WHERE regime = 'New' AND employer_statutory_cap = 150000;
                """);

            // 4. EDLI/admin/§206AA rates for all New-regime FY rows.
            migrationBuilder.Sql("""
                UPDATE income_tax_configs
                   SET edli_rate = 0.005, edli_wage_cap = 15000, edli_max_amount = 75,
                       epf_admin_rate = 0.005, pan206aa_rate = 0.20
                 WHERE regime = 'New';
                """);

            // 5. Replace SYSTEM-SEEDED PT slabs with the corrected schedules
            //    (MH women-25k exemption, KA 25k exemption, TN half-yearly basis,
            //    WB defunct ₹90 band removed, contiguous half-open boundaries,
            //    February amounts for the Article 276 remainder).
            //    Tenant-authored revisions (created_by = a real user) are preserved.
            migrationBuilder.Sql("""
                DELETE FROM professional_tax_slabs
                 WHERE created_by IN ('00000000-0000-0000-0000-000000000000',
                                      '00000000-0000-0000-0000-000000000001');
                INSERT INTO professional_tax_slabs
                    (id, state_code, effective_date, frequency, deduction_months_csv, gender,
                     min_gross, max_gross, pt_amount, is_february_surcharge, february_amount,
                     is_active, created_at, created_by, is_deleted)
                VALUES
                    (gen_random_uuid(), 'MH', '2025-04-01', 'Monthly', NULL, 'Male', 0, 7500, 0, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'MH', '2025-04-01', 'Monthly', NULL, 'Male', 7500, 10000, 175, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'MH', '2025-04-01', 'Monthly', NULL, 'Male', 10000, NULL, 200, true, 300, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'MH', '2025-04-01', 'Monthly', NULL, 'Female', 0, 25000, 0, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'MH', '2025-04-01', 'Monthly', NULL, 'Female', 25000, NULL, 200, true, 300, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KA', '2025-04-01', 'Monthly', NULL, NULL, 0, 25000, 0, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KA', '2025-04-01', 'Monthly', NULL, NULL, 25000, NULL, 200, true, 300, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'AP', '2025-04-01', 'Monthly', NULL, NULL, 0, 15001, 0, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'AP', '2025-04-01', 'Monthly', NULL, NULL, 15001, 20001, 150, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'AP', '2025-04-01', 'Monthly', NULL, NULL, 20001, NULL, 200, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'TS', '2025-04-01', 'Monthly', NULL, NULL, 0, 15001, 0, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'TS', '2025-04-01', 'Monthly', NULL, NULL, 15001, 20001, 150, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'TS', '2025-04-01', 'Monthly', NULL, NULL, 20001, NULL, 200, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'WB', '2025-04-01', 'Monthly', NULL, NULL, 0, 10001, 0, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'WB', '2025-04-01', 'Monthly', NULL, NULL, 10001, 15001, 110, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'WB', '2025-04-01', 'Monthly', NULL, NULL, 15001, 25001, 130, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'WB', '2025-04-01', 'Monthly', NULL, NULL, 25001, 40001, 150, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'WB', '2025-04-01', 'Monthly', NULL, NULL, 40001, NULL, 200, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'TN', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 0, 21001, 0, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'TN', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 21001, 30001, 180, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'TN', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 30001, 45001, 425, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'TN', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 45001, 60001, 930, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'TN', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 60001, 75001, 1025, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'TN', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 75001, NULL, 1250, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KL', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 0, 12000, 0, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KL', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 12000, 18000, 120, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KL', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 18000, 30000, 180, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KL', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 30000, 45000, 300, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KL', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 45000, 60000, 450, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KL', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 60000, 75000, 600, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KL', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 75000, 100000, 750, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KL', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 100000, 125000, 1000, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KL', '2025-04-01', 'HalfYearlySplit', NULL, NULL, 125000, NULL, 1250, false, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false);
                """);

            // 6. Replace SYSTEM-SEEDED LWF configs with 2025-26 amounts/frequencies.
            //    Kerala employer share stays 0 by explicit business decision.
            migrationBuilder.Sql("""
                DELETE FROM lwf_state_configs
                 WHERE created_by IN ('00000000-0000-0000-0000-000000000000',
                                      '00000000-0000-0000-0000-000000000001');
                INSERT INTO lwf_state_configs
                    (id, state_code, effective_date, employee_amount, employer_amount,
                     is_percentage_based, employee_rate, employer_rate, rate_cap_employee,
                     rate_cap_employer, frequency, deduction_month, deposit_due_day,
                     wage_threshold, is_active, created_at, created_by, is_deleted)
                VALUES
                    (gen_random_uuid(), 'MH', '2025-04-01', 25, 75, false, NULL, NULL, NULL, NULL, 'HalfYearly', NULL, NULL, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KA', '2025-04-01', 50, 100, false, NULL, NULL, NULL, NULL, 'Annual', 12, 31, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'AP', '2025-04-01', 30, 70, false, NULL, NULL, NULL, NULL, 'Annual', 12, 31, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'TS', '2025-04-01', 2, 5, false, NULL, NULL, NULL, NULL, 'Annual', 12, 31, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'WB', '2025-04-01', 3, 30, false, NULL, NULL, NULL, NULL, 'HalfYearly', NULL, NULL, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'GJ', '2025-04-01', 6, 12, false, NULL, NULL, NULL, NULL, 'HalfYearly', NULL, NULL, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'MP', '2025-04-01', 10, 30, false, NULL, NULL, NULL, NULL, 'HalfYearly', NULL, NULL, 10000, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'CH', '2025-04-01', 5, 20, false, NULL, NULL, NULL, NULL, 'Monthly', NULL, NULL, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'HR', '2025-04-01', 0, 0, true, 0.002, 0.004, 35, 70, 'Monthly', NULL, NULL, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false),
                    (gen_random_uuid(), 'KL', '2025-04-01', 50, 0, false, NULL, NULL, NULL, NULL, 'Monthly', NULL, NULL, NULL, true, now(), '00000000-0000-0000-0000-000000000000', false);
                """);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort data reversal: restore the old FY key format. The
            // corrected PT/LWF rows and NPS values are intentionally NOT
            // reverted — the old values were statutorily wrong.
            migrationBuilder.Sql("""
                UPDATE income_tax_slabs SET fiscal_year = '2026' WHERE fiscal_year = '2025-26';
                UPDATE income_tax_slabs SET fiscal_year = '2027' WHERE fiscal_year = '2026-27';
                UPDATE income_tax_surcharge_slabs SET fiscal_year = '2026' WHERE fiscal_year = '2025-26';
                UPDATE income_tax_surcharge_slabs SET fiscal_year = '2027' WHERE fiscal_year = '2026-27';
                UPDATE income_tax_configs SET fiscal_year = '2026' WHERE fiscal_year = '2025-26';
                UPDATE income_tax_configs SET fiscal_year = '2027' WHERE fiscal_year = '2026-27';
                """);

            migrationBuilder.DropColumn(
                name: "february_amount",
                table: "professional_tax_slabs");

            migrationBuilder.DropColumn(
                name: "edli_max_amount",
                table: "income_tax_configs");

            migrationBuilder.DropColumn(
                name: "edli_rate",
                table: "income_tax_configs");

            migrationBuilder.DropColumn(
                name: "edli_wage_cap",
                table: "income_tax_configs");

            migrationBuilder.DropColumn(
                name: "epf_admin_rate",
                table: "income_tax_configs");

            migrationBuilder.DropColumn(
                name: "pan206aa_rate",
                table: "income_tax_configs");
        }
    }
}
