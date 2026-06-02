using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExitYtdSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ytd_gross_snapshot", table: "employee_exits",
                type: "numeric(18,2)", nullable: true);
            migrationBuilder.AddColumn<decimal>(
                name: "ytd_taxable_snapshot", table: "employee_exits",
                type: "numeric(18,2)", nullable: true);
            migrationBuilder.AddColumn<decimal>(
                name: "ytd_tds_snapshot", table: "employee_exits",
                type: "numeric(18,2)", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ytd_gross_snapshot", table: "employee_exits");
            migrationBuilder.DropColumn(name: "ytd_taxable_snapshot", table: "employee_exits");
            migrationBuilder.DropColumn(name: "ytd_tds_snapshot", table: "employee_exits");
        }
    }
}
