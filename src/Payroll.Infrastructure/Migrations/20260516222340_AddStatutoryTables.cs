using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatutoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_filing_address",
                table: "work_locations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "income_tax_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    regime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    standard_deduction = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    rebate87a_limit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    rebate87a_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    employer_statutory_cap = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    nps_employer_max_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
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
                    table.PrimaryKey("pk_income_tax_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "income_tax_slabs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    regime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    bracket_min = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    bracket_max = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
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
                    table.PrimaryKey("pk_income_tax_slabs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "income_tax_surcharge_slabs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    regime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    income_from = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    income_to = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    surcharge_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
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
                    table.PrimaryKey("pk_income_tax_surcharge_slabs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lwf_state_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    employee_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    employer_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_percentage_based = table.Column<bool>(type: "boolean", nullable: false),
                    employee_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: true),
                    employer_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: true),
                    rate_cap_employee = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    rate_cap_employer = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    deduction_month = table.Column<int>(type: "integer", nullable: true),
                    deposit_due_day = table.Column<int>(type: "integer", nullable: true),
                    wage_threshold = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
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
                    table.PrimaryKey("pk_lwf_state_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "professional_tax_slabs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    min_gross = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    max_gross = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    pt_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_february_surcharge = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_professional_tax_slabs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "statutory_org_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    epf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    epf_establishment_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    epf_employee_contribution_rate = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    epf_employer_contribution_rate = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    epf_include_employer_in_ctc = table.Column<bool>(type: "boolean", nullable: false),
                    epf_include_edli_in_ctc = table.Column<bool>(type: "boolean", nullable: false),
                    epf_include_admin_in_ctc = table.Column<bool>(type: "boolean", nullable: false),
                    epf_override_at_employee_level = table.Column<bool>(type: "boolean", nullable: false),
                    epf_pro_rate_restricted_pf_wage = table.Column<bool>(type: "boolean", nullable: false),
                    epf_consider_salary_on_lop = table.Column<bool>(type: "boolean", nullable: false),
                    esi_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    esi_establishment_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    esi_notified_area = table.Column<bool>(type: "boolean", nullable: false),
                    statutory_bonus_enabled = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_statutory_org_configs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_income_tax_configs_fiscal_year_regime",
                table: "income_tax_configs",
                columns: new[] { "fiscal_year", "regime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_income_tax_slabs_fiscal_year_regime_bracket_min",
                table: "income_tax_slabs",
                columns: new[] { "fiscal_year", "regime", "bracket_min" });

            migrationBuilder.CreateIndex(
                name: "ix_income_tax_surcharge_slabs_fiscal_year_regime",
                table: "income_tax_surcharge_slabs",
                columns: new[] { "fiscal_year", "regime" });

            migrationBuilder.CreateIndex(
                name: "ix_lwf_state_configs_state_code_effective_date",
                table: "lwf_state_configs",
                columns: new[] { "state_code", "effective_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_professional_tax_slabs_state_code_effective_date",
                table: "professional_tax_slabs",
                columns: new[] { "state_code", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "ix_statutory_org_configs_tenant_id",
                table: "statutory_org_configs",
                column: "tenant_id",
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "income_tax_configs");

            migrationBuilder.DropTable(
                name: "income_tax_slabs");

            migrationBuilder.DropTable(
                name: "income_tax_surcharge_slabs");

            migrationBuilder.DropTable(
                name: "lwf_state_configs");

            migrationBuilder.DropTable(
                name: "professional_tax_slabs");

            migrationBuilder.DropTable(
                name: "statutory_org_configs");

            migrationBuilder.DropColumn(
                name: "is_filing_address",
                table: "work_locations");
        }
    }
}
