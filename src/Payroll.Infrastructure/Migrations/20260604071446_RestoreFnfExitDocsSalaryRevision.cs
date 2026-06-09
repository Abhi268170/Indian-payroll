using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations.PayrollDb
{
    /// <inheritdoc />
    public partial class RestoreFnfExitDocsSalaryRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_employee_exits_employee_id",
                table: "employee_exits");

            migrationBuilder.AddColumn<decimal>(
                name: "gratuity_exemption_limit",
                table: "statutory_org_configs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 2000000m);

            migrationBuilder.AddColumn<decimal>(
                name: "leave_encashment_exemption_limit",
                table: "statutory_org_configs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 2500000m);

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

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "employee_exits",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ytd_gross_snapshot",
                table: "employee_exits",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ytd_taxable_snapshot",
                table: "employee_exits",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ytd_tds_snapshot",
                table: "employee_exits",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "employee_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_documents", x => x.id);
                });

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
                name: "ix_employee_exits_employee_id",
                table: "employee_exits",
                column: "employee_id",
                unique: true,
                filter: "is_deleted = false AND status = 'InProgress'");

            migrationBuilder.CreateIndex(
                name: "ix_employee_documents_employee_id",
                table: "employee_documents",
                column: "employee_id");

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
                name: "employee_documents");

            migrationBuilder.DropTable(
                name: "salary_revision_component_overrides");

            migrationBuilder.DropIndex(
                name: "ix_salary_revisions_payout_year_payout_month_status",
                table: "salary_revisions");

            migrationBuilder.DropIndex(
                name: "ix_employee_exits_employee_id",
                table: "employee_exits");

            migrationBuilder.DropColumn(
                name: "gratuity_exemption_limit",
                table: "statutory_org_configs");

            migrationBuilder.DropColumn(
                name: "leave_encashment_exemption_limit",
                table: "statutory_org_configs");

            migrationBuilder.DropColumn(
                name: "arrear_paid_run_id",
                table: "salary_revisions");

            migrationBuilder.DropColumn(
                name: "resulting_salary_structure_id",
                table: "salary_revisions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "employee_exits");

            migrationBuilder.DropColumn(
                name: "ytd_gross_snapshot",
                table: "employee_exits");

            migrationBuilder.DropColumn(
                name: "ytd_taxable_snapshot",
                table: "employee_exits");

            migrationBuilder.DropColumn(
                name: "ytd_tds_snapshot",
                table: "employee_exits");

            migrationBuilder.CreateIndex(
                name: "ix_employee_exits_employee_id",
                table: "employee_exits",
                column: "employee_id",
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
