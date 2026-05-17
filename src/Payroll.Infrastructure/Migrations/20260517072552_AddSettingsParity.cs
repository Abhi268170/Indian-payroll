using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsParity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pt_registration_number",
                table: "work_locations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bonus_mode",
                table: "statutory_org_configs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Yearly");

            migrationBuilder.AddColumn<int>(
                name: "bonus_payout_month",
                table: "statutory_org_configs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "bonus_rate",
                table: "statutory_org_configs",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.0833m);

            migrationBuilder.AddColumn<int>(
                name: "first_pay_period_month",
                table: "pay_schedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "first_pay_period_year",
                table: "pay_schedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ao_area_code",
                table: "org_profiles",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ao_number",
                table: "org_profiles",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ao_range_code",
                table: "org_profiles",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ao_type",
                table: "org_profiles",
                type: "character varying(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deductor_designation",
                table: "org_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deductor_fathers_name",
                table: "org_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deductor_name",
                table: "org_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deductor_type",
                table: "org_profiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tan",
                table: "org_profiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pt_registration_number",
                table: "work_locations");

            migrationBuilder.DropColumn(
                name: "bonus_mode",
                table: "statutory_org_configs");

            migrationBuilder.DropColumn(
                name: "bonus_payout_month",
                table: "statutory_org_configs");

            migrationBuilder.DropColumn(
                name: "bonus_rate",
                table: "statutory_org_configs");

            migrationBuilder.DropColumn(
                name: "first_pay_period_month",
                table: "pay_schedules");

            migrationBuilder.DropColumn(
                name: "first_pay_period_year",
                table: "pay_schedules");

            migrationBuilder.DropColumn(
                name: "ao_area_code",
                table: "org_profiles");

            migrationBuilder.DropColumn(
                name: "ao_number",
                table: "org_profiles");

            migrationBuilder.DropColumn(
                name: "ao_range_code",
                table: "org_profiles");

            migrationBuilder.DropColumn(
                name: "ao_type",
                table: "org_profiles");

            migrationBuilder.DropColumn(
                name: "deductor_designation",
                table: "org_profiles");

            migrationBuilder.DropColumn(
                name: "deductor_fathers_name",
                table: "org_profiles");

            migrationBuilder.DropColumn(
                name: "deductor_name",
                table: "org_profiles");

            migrationBuilder.DropColumn(
                name: "deductor_type",
                table: "org_profiles");

            migrationBuilder.DropColumn(
                name: "tan",
                table: "org_profiles");
        }
    }
}
