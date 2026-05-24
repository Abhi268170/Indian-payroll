using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

public partial class FixKeralaPTSlabs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Remove old (incorrect) Kerala PT slabs
        migrationBuilder.Sql("DELETE FROM professional_tax_slabs WHERE state_code = 'KL';");

        // Seed correct 7-band Kerala HalfYearlySplit slabs.
        // Slab ranges are half-year gross (monthly * months-in-half).
        // created_by uses system sentinel; effective_date matches original seed.
        const string sys = "'00000000-0000-0000-0000-000000000001'";
        const string eff = "'2025-04-01'";

        migrationBuilder.Sql($@"
INSERT INTO professional_tax_slabs
    (id, state_code, effective_date, frequency, deduction_months_csv, gender,
     min_gross, max_gross, pt_amount, is_february_surcharge, is_active,
     created_at, updated_at, created_by, updated_by, is_deleted)
VALUES
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,      1,      11999,  0,    false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  12000,  17999,    320,  false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  18000,  29999,    450,  false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  30000,  44999,    600,  false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  45000,  99999,    750,  false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL, 100000, 124999,   1000,  false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL, 125000,   NULL,   1250,  false, true, NOW(), NOW(), {sys}, {sys}, false);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM professional_tax_slabs WHERE state_code = 'KL';");

        const string sys = "'00000000-0000-0000-0000-000000000001'";
        const string eff = "'2025-04-01'";

        migrationBuilder.Sql($@"
INSERT INTO professional_tax_slabs
    (id, state_code, effective_date, frequency, deduction_months_csv, gender,
     min_gross, max_gross, pt_amount, is_february_surcharge, is_active,
     created_at, updated_at, created_by, updated_by)
VALUES
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearly', '9,3', NULL,     0,  11999,   0, false, true, NOW(), NOW(), {sys}, {sys}),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearly', '9,3', NULL, 12000,  17999, 120, false, true, NOW(), NOW(), {sys}, {sys}),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearly', '9,3', NULL, 18000,  29999, 180, false, true, NOW(), NOW(), {sys}, {sys}),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearly', '9,3', NULL, 30000,   NULL, 240, false, true, NOW(), NOW(), {sys}, {sys});
");
    }
}
