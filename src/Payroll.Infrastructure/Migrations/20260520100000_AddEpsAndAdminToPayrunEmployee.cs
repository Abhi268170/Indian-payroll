using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEpsAndAdminToPayrunEmployee : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "eps_amount",
            table: "payrun_employees",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "admin_amount",
            table: "payrun_employees",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 0m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "eps_amount", table: "payrun_employees");
        migrationBuilder.DropColumn(name: "admin_amount", table: "payrun_employees");
    }
}
