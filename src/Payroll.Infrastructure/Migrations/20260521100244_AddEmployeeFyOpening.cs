using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations.PayrollDb
{
    /// <inheritdoc />
    public partial class AddEmployeeFyOpening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_fy_openings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    months_count = table.Column<int>(type: "integer", nullable: false),
                    gross_salary = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tds_deducted = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    pf_deducted = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_fy_openings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_fy_openings_employee_id_fiscal_year",
                table: "employee_fy_openings",
                columns: new[] { "employee_id", "fiscal_year" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_fy_openings");
        }
    }
}
