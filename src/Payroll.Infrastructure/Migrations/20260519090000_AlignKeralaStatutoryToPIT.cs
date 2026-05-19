using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

public partial class AlignKeralaStatutoryToPIT : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Fix Kerala PT slabs: previous migration had wrong amounts and was missing
        // three band boundaries (₹45k–₹60k, ₹60k–₹75k, ₹75k–₹100k were collapsed).
        // Correct amounts per PIT Solutions reference manual (half-yearly gross basis).
        migrationBuilder.Sql("DELETE FROM professional_tax_slabs WHERE state_code = 'KL';");

        const string sys = "'00000000-0000-0000-0000-000000000000'";
        const string eff = "'2025-04-01'";

        migrationBuilder.Sql($@"
INSERT INTO professional_tax_slabs
    (id, state_code, effective_date, frequency, deduction_months_csv, gender,
     min_gross, max_gross, pt_amount, is_february_surcharge, is_active,
     created_at, updated_at, created_by, updated_by, is_deleted)
VALUES
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,      1,   11999,    0, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  12000,   17999,  120, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  18000,   29999,  180, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  30000,   44999,  300, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  45000,   59999,  450, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  60000,   74999,  600, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  75000,   99999,  750, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL, 100000,  124999, 1000, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL, 125000,    NULL, 1250, false, true, NOW(), NOW(), {sys}, {sys}, false);
");

        // Fix Kerala LWF: was Annual (December only), correct frequency is Monthly.
        migrationBuilder.Sql(@"
UPDATE lwf_state_configs
SET    frequency        = 'Monthly',
       deduction_month  = NULL,
       deposit_due_day  = NULL,
       updated_at       = NOW()
WHERE  state_code = 'KL';
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM professional_tax_slabs WHERE state_code = 'KL';");

        const string sys = "'00000000-0000-0000-0000-000000000000'";
        const string eff = "'2025-04-01'";

        migrationBuilder.Sql($@"
INSERT INTO professional_tax_slabs
    (id, state_code, effective_date, frequency, deduction_months_csv, gender,
     min_gross, max_gross, pt_amount, is_february_surcharge, is_active,
     created_at, updated_at, created_by, updated_by, is_deleted)
VALUES
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,      1,   11999,    0, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  12000,   17999,  320, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  18000,   29999,  450, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  30000,   44999,  600, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL,  45000,   99999,  750, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL, 100000,  124999, 1000, false, true, NOW(), NOW(), {sys}, {sys}, false),
    (gen_random_uuid(), 'KL', {eff}, 'HalfYearlySplit', NULL, NULL, 125000,    NULL, 1250, false, true, NOW(), NOW(), {sys}, {sys}, false);
");

        migrationBuilder.Sql(@"
UPDATE lwf_state_configs
SET    frequency        = 'Annual',
       deduction_month  = 12,
       deposit_due_day  = 31,
       updated_at       = NOW()
WHERE  state_code = 'KL';
");
    }
}
