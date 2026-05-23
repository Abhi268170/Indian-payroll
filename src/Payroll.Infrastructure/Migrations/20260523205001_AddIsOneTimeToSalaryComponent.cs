using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOneTimeToSalaryComponent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_one_time",
                table: "salary_components",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "calculate_on_pro_rata",
                table: "payrun_component_breakdowns",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "consider_for_esi",
                table: "payrun_component_breakdowns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "epf_inclusion_rule",
                table: "payrun_component_breakdowns",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Always");

            migrationBuilder.AddColumn<bool>(
                name: "is_taxable",
                table: "payrun_component_breakdowns",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "ix_salary_components_tenant_id_is_one_time_category_is_active",
                table: "salary_components",
                columns: new[] { "tenant_id", "is_one_time", "category", "is_active" });

            // Backfill: existing earnings whose EarningType matches Zoho's one-time
            // catalogue (Bonus, Commission, LeaveEncashment, ArrearsEarning) are
            // flagged so they appear in the Add Earning dropdown without manual
            // toggling. Admins can adjust via Edit Component later.
            migrationBuilder.Sql(@"
                UPDATE salary_components
                SET is_one_time = true
                WHERE category = 'Earning'
                  AND earning_type IN ('Bonus', 'Commission', 'LeaveEncashment', 'ArrearsEarning');
            ");

            // Backfill: existing breakdown rows inherit statutory flags from their
            // linked SalaryComponent so engine recompute (Phase 2) produces the
            // same numbers as the original run. Reimbursement rows
            // (salary_component_id IS NULL) keep defaults — they are filtered out
            // of engine input regardless.
            migrationBuilder.Sql(@"
                UPDATE payrun_component_breakdowns b
                SET is_taxable           = COALESCE(c.is_taxable, true),
                    consider_for_esi     = COALESCE(c.consider_for_esi, false),
                    calculate_on_pro_rata = COALESCE(c.calculate_on_pro_rata, true),
                    epf_inclusion_rule   = COALESCE(c.epf_inclusion_rule, 'Always')
                FROM salary_components c
                WHERE b.salary_component_id = c.id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_salary_components_tenant_id_is_one_time_category_is_active",
                table: "salary_components");

            migrationBuilder.DropColumn(
                name: "is_one_time",
                table: "salary_components");

            migrationBuilder.DropColumn(
                name: "calculate_on_pro_rata",
                table: "payrun_component_breakdowns");

            migrationBuilder.DropColumn(
                name: "consider_for_esi",
                table: "payrun_component_breakdowns");

            migrationBuilder.DropColumn(
                name: "epf_inclusion_rule",
                table: "payrun_component_breakdowns");

            migrationBuilder.DropColumn(
                name: "is_taxable",
                table: "payrun_component_breakdowns");
        }
    }
}
