using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGratuityAmountToPayrunEmployee : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "gratuity_amount",
            table: "payrun_employees",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 0m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "gratuity_amount",
            table: "payrun_employees");
    }
}
