using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddLwfAmountsToPayrunEmployee : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "lwf_employee_amount",
            table: "payrun_employees",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "lwf_employer_amount",
            table: "payrun_employees",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "lwf_employee_amount",
            table: "payrun_employees");

        migrationBuilder.DropColumn(
            name: "lwf_employer_amount",
            table: "payrun_employees");
    }
}
