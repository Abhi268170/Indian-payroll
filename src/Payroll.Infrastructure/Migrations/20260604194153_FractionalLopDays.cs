using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations.PayrollDb
{
    /// <inheritdoc />
    public partial class FractionalLopDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "lop_days",
                table: "payrun_employees",
                type: "numeric(4,1)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "actual_payable_days",
                table: "payrun_employees",
                type: "numeric(4,1)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "lop_days",
                table: "payrun_employees",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(4,1)");

            migrationBuilder.AlterColumn<int>(
                name: "actual_payable_days",
                table: "payrun_employees",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(4,1)");
        }
    }
}
