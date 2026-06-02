using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryRevisionArrearFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "arrear_paid_run_id",
                table: "salary_revisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resulting_salary_structure_id",
                table: "salary_revisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "salary_revision_component_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    salary_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    salary_component_id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula_type = table.Column<string>(type: "text", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(7,4)", nullable: true),
                    fixed_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
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
                    table.PrimaryKey("pk_salary_revision_component_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_salary_revision_component_overrides_salary_revisions_salary",
                        column: x => x.salary_revision_id,
                        principalTable: "salary_revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_salary_revisions_payout_year_payout_month_status",
                table: "salary_revisions",
                columns: new[] { "payout_year", "payout_month", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_salary_revision_component_overrides_salary_revision_id_sala",
                table: "salary_revision_component_overrides",
                columns: new[] { "salary_revision_id", "salary_component_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "salary_revision_component_overrides");

            migrationBuilder.DropIndex(
                name: "ix_salary_revisions_payout_year_payout_month_status",
                table: "salary_revisions");

            migrationBuilder.DropColumn(
                name: "arrear_paid_run_id",
                table: "salary_revisions");

            migrationBuilder.DropColumn(
                name: "resulting_salary_structure_id",
                table: "salary_revisions");
        }
    }
}
