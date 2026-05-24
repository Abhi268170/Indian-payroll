using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryComponentsAndStructureTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── salary_components: alter existing columns ─────────────────────
            // formula_type was text NOT NULL; new entity treats it as nullable varchar
            migrationBuilder.AlterColumn<string>(
                name: "formula_type",
                table: "salary_components",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            // is_taxable was bool NOT NULL; new entity treats it as nullable (category-specific)
            migrationBuilder.AlterColumn<bool>(
                name: "is_taxable",
                table: "salary_components",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            // ── salary_components: add new columns ────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "name_in_payslip",
                table: "salary_components",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "salary_components",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Earning");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "salary_components",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_associated_with_employee",
                table: "salary_components",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "earning_type",
                table: "salary_components",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pay_type",
                table: "salary_components",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "consider_for_epf",
                table: "salary_components",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "epf_inclusion_rule",
                table: "salary_components",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "consider_for_esi",
                table: "salary_components",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "calculate_on_pro_rata",
                table: "salary_components",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_in_payslip",
                table: "salary_components",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deduction_frequency",
                table: "salary_components",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reimbursement_type",
                table: "salary_components",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "reimbursement_amount",
                table: "salary_components",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unclaimed_handling",
                table: "salary_components",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "benefit_type",
                table: "salary_components",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "benefit_percentage",
                table: "salary_components",
                type: "numeric(7,4)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_applicable_to_all_employees",
                table: "salary_components",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_nps_government_sector",
                table: "salary_components",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "for_correction_of_component_id",
                table: "salary_components",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_salary_components_for_correction_of_component_id",
                table: "salary_components",
                column: "for_correction_of_component_id");

            migrationBuilder.AddForeignKey(
                name: "fk_salary_components_salary_components_for_correction_of_compo",
                table: "salary_components",
                column: "for_correction_of_component_id",
                principalTable: "salary_components",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // ── salary_structure_templates: new table ─────────────────────────
            migrationBuilder.CreateTable(
                name: "salary_structure_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_salary_structure_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_salary_structure_templates_tenant_id",
                table: "salary_structure_templates",
                column: "tenant_id");

            // ── salary_structure_components: new junction table ───────────────
            migrationBuilder.CreateTable(
                name: "salary_structure_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fixed_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    percentage = table.Column<decimal>(type: "numeric(7,4)", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_salary_structure_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_salary_structure_components_salary_components_component_id",
                        column: x => x.component_id,
                        principalTable: "salary_components",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_salary_structure_components_salary_structure_templates_temp",
                        column: x => x.template_id,
                        principalTable: "salary_structure_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_salary_structure_components_component_id",
                table: "salary_structure_components",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "ix_salary_structure_components_template_id_component_id",
                table: "salary_structure_components",
                columns: new[] { "template_id", "component_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "salary_structure_components");
            migrationBuilder.DropTable(name: "salary_structure_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_salary_components_salary_components_for_correction_of_compo",
                table: "salary_components");

            migrationBuilder.DropIndex(
                name: "ix_salary_components_for_correction_of_component_id",
                table: "salary_components");

            migrationBuilder.DropColumn(name: "name_in_payslip", table: "salary_components");
            migrationBuilder.DropColumn(name: "category", table: "salary_components");
            migrationBuilder.DropColumn(name: "is_active", table: "salary_components");
            migrationBuilder.DropColumn(name: "is_associated_with_employee", table: "salary_components");
            migrationBuilder.DropColumn(name: "earning_type", table: "salary_components");
            migrationBuilder.DropColumn(name: "pay_type", table: "salary_components");
            migrationBuilder.DropColumn(name: "consider_for_epf", table: "salary_components");
            migrationBuilder.DropColumn(name: "epf_inclusion_rule", table: "salary_components");
            migrationBuilder.DropColumn(name: "consider_for_esi", table: "salary_components");
            migrationBuilder.DropColumn(name: "calculate_on_pro_rata", table: "salary_components");
            migrationBuilder.DropColumn(name: "show_in_payslip", table: "salary_components");
            migrationBuilder.DropColumn(name: "deduction_frequency", table: "salary_components");
            migrationBuilder.DropColumn(name: "reimbursement_type", table: "salary_components");
            migrationBuilder.DropColumn(name: "reimbursement_amount", table: "salary_components");
            migrationBuilder.DropColumn(name: "unclaimed_handling", table: "salary_components");
            migrationBuilder.DropColumn(name: "benefit_type", table: "salary_components");
            migrationBuilder.DropColumn(name: "benefit_percentage", table: "salary_components");
            migrationBuilder.DropColumn(name: "is_applicable_to_all_employees", table: "salary_components");
            migrationBuilder.DropColumn(name: "is_nps_government_sector", table: "salary_components");
            migrationBuilder.DropColumn(name: "for_correction_of_component_id", table: "salary_components");

            migrationBuilder.AlterColumn<bool>(
                name: "is_taxable",
                table: "salary_components",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "formula_type",
                table: "salary_components",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);
        }
    }
}
