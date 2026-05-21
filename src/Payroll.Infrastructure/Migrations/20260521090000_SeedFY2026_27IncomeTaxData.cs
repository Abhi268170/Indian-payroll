using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

public partial class SeedFY2026_27IncomeTaxData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($@"
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
) ON CONFLICT (fiscal_year, regime) DO NOTHING;");

        migrationBuilder.Sql($@"
INSERT INTO income_tax_slabs (id, fiscal_year, regime, bracket_min, bracket_max, rate, created_at, created_by, is_deleted) VALUES
    (gen_random_uuid(), '2026-27', 'New',       0,  400000, 0.00, now(), '00000000-0000-0000-0000-000000000001', false),
    (gen_random_uuid(), '2026-27', 'New',  400000,  800000, 0.05, now(), '00000000-0000-0000-0000-000000000001', false),
    (gen_random_uuid(), '2026-27', 'New',  800000, 1200000, 0.10, now(), '00000000-0000-0000-0000-000000000001', false),
    (gen_random_uuid(), '2026-27', 'New', 1200000, 1600000, 0.15, now(), '00000000-0000-0000-0000-000000000001', false),
    (gen_random_uuid(), '2026-27', 'New', 1600000, 2000000, 0.20, now(), '00000000-0000-0000-0000-000000000001', false),
    (gen_random_uuid(), '2026-27', 'New', 2000000, 2400000, 0.25, now(), '00000000-0000-0000-0000-000000000001', false),
    (gen_random_uuid(), '2026-27', 'New', 2400000,    NULL, 0.30, now(), '00000000-0000-0000-0000-000000000001', false);");

        migrationBuilder.Sql($@"
INSERT INTO income_tax_surcharge_slabs (id, fiscal_year, regime, income_from, income_to, surcharge_rate, created_at, created_by, is_deleted) VALUES
    (gen_random_uuid(), '2026-27', 'New',  5000000, 10000000, 0.10, now(), '00000000-0000-0000-0000-000000000001', false),
    (gen_random_uuid(), '2026-27', 'New', 10000000, 20000000, 0.15, now(), '00000000-0000-0000-0000-000000000001', false),
    (gen_random_uuid(), '2026-27', 'New', 20000000,     NULL, 0.25, now(), '00000000-0000-0000-0000-000000000001', false);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM income_tax_surcharge_slabs WHERE fiscal_year = '2026-27' AND regime = 'New';");
        migrationBuilder.Sql("DELETE FROM income_tax_slabs WHERE fiscal_year = '2026-27' AND regime = 'New';");
        migrationBuilder.Sql("DELETE FROM income_tax_configs WHERE fiscal_year = '2026-27' AND regime = 'New';");
    }
}
