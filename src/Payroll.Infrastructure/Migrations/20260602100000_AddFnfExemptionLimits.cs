using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFnfExemptionLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "gratuity_exemption_limit",
                table: "statutory_org_configs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 2_000_000m);

            migrationBuilder.AddColumn<decimal>(
                name: "leave_encashment_exemption_limit",
                table: "statutory_org_configs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 2_500_000m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gratuity_exemption_limit",
                table: "statutory_org_configs");

            migrationBuilder.DropColumn(
                name: "leave_encashment_exemption_limit",
                table: "statutory_org_configs");
        }
    }
}
