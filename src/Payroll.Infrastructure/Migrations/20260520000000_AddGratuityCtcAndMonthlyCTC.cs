using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGratuityCtcAndMonthlyCTC : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "gratuity_included_in_ctc",
            table: "statutory_org_configs",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<decimal>(
            name: "monthly_ctc",
            table: "payrun_employees",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "gratuity_included_in_ctc",
            table: "statutory_org_configs");

        migrationBuilder.DropColumn(
            name: "monthly_ctc",
            table: "payrun_employees");
    }
}
