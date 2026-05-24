using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old schema-v1 tables created in InitialTenant migration.
            // These had different columns (work_state, pf_opt_out, branch_id, salary_structures).
            // No employee data exists in dev tenants — safe to drop and recreate.
            migrationBuilder.Sql("DROP TABLE IF EXISTS salary_structures CASCADE");
            migrationBuilder.Sql("DROP TABLE IF EXISTS employees CASCADE");

            migrationBuilder.AlterColumn<decimal>(
                name: "bonus_rate",
                table: "statutory_org_configs",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0.0833m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)");

            migrationBuilder.AlterColumn<string>(
                name: "bonus_mode",
                table: "statutory_org_configs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Yearly",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "employee_exits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_working_day = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    settlement_mode = table.Column<string>(type: "text", nullable: false),
                    settlement_date = table.Column<DateOnly>(type: "date", nullable: true),
                    personal_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("pk_employee_exits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_salary_structures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    salary_structure_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    annual_ctc = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("pk_employee_salary_structures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_vehicle_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintained_by_employer = table.Column<bool>(type: "boolean", nullable: false),
                    cubic_capacity_above1600 = table.Column<bool>(type: "boolean", nullable: false),
                    driver_provided = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_employee_vehicle_details", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    employee_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    work_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    mobile_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    gender = table.Column<string>(type: "text", nullable: false),
                    date_of_joining = table.Column<DateOnly>(type: "date", nullable: false),
                    date_of_leaving = table.Column<DateOnly>(type: "date", nullable: true),
                    employment_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    is_director = table.Column<bool>(type: "boolean", nullable: false),
                    enable_portal_access = table.Column<bool>(type: "boolean", nullable: false),
                    profile_complete = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    designation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cost_centre_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    fathers_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    personal_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    differently_abled_type = table.Column<string>(type: "text", nullable: false),
                    address_line1 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    address_line2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    residential_state = table.Column<string>(type: "text", nullable: true),
                    pin_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    payment_mode = table.Column<string>(type: "text", nullable: false),
                    account_holder_name = table.Column<string>(type: "text", nullable: true),
                    bank_name = table.Column<string>(type: "text", nullable: true),
                    account_type = table.Column<string>(type: "text", nullable: true),
                    encrypted_pan = table.Column<string>(type: "text", nullable: true),
                    encrypted_aadhaar = table.Column<string>(type: "text", nullable: true),
                    encrypted_bank_account = table.Column<string>(type: "text", nullable: true),
                    encrypted_ifsc = table.Column<string>(type: "text", nullable: true),
                    uan = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    esicip_number = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    epf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    esi_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pt_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lwf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_pwd = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prior_employer_ytds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    financial_year = table.Column<int>(type: "integer", nullable: false),
                    employer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    period_from = table.Column<DateOnly>(type: "date", nullable: false),
                    period_to = table.Column<DateOnly>(type: "date", nullable: false),
                    gross_salary = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    standard_deduction_claimed = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    professional_tax_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tds_deducted = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    other_income = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("pk_prior_employer_ytds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "salary_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_annual_ctc = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    new_annual_ctc = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    effective_from_month = table.Column<int>(type: "integer", nullable: false),
                    effective_from_year = table.Column<int>(type: "integer", nullable: false),
                    payout_month = table.Column<int>(type: "integer", nullable: false),
                    payout_year = table.Column<int>(type: "integer", nullable: false),
                    salary_structure_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_salary_revisions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_salary_component_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_salary_structure_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_employee_salary_component_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_salary_component_overrides_employee_salary_structu",
                        column: x => x.employee_salary_structure_id,
                        principalTable: "employee_salary_structures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_exits_employee_id",
                table: "employee_exits",
                column: "employee_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_employee_salary_component_overrides_employee_salary_structu",
                table: "employee_salary_component_overrides",
                columns: new[] { "employee_salary_structure_id", "salary_component_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employee_salary_structures_employee_id_effective_from",
                table: "employee_salary_structures",
                columns: new[] { "employee_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_vehicle_details_employee_id",
                table: "employee_vehicle_details",
                column: "employee_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id",
                table: "employees",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_employee_code",
                table: "employees",
                columns: new[] { "tenant_id", "employee_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_work_email",
                table: "employees",
                columns: new[] { "tenant_id", "work_email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prior_employer_ytds_employee_id_financial_year",
                table: "prior_employer_ytds",
                columns: new[] { "employee_id", "financial_year" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_salary_revisions_employee_id_status",
                table: "salary_revisions",
                columns: new[] { "employee_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore legacy schema (schema from InitialTenant migration)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS employees (
                    id uuid NOT NULL PRIMARY KEY,
                    first_name varchar(100) NOT NULL,
                    last_name varchar(100) NOT NULL,
                    employee_code varchar(50) NOT NULL,
                    encrypted_pan text NOT NULL,
                    date_of_birth date NOT NULL,
                    gender text NOT NULL,
                    date_of_joining date NOT NULL,
                    date_of_leaving date,
                    employment_type text NOT NULL,
                    status text NOT NULL,
                    work_state text NOT NULL,
                    pf_opt_out boolean NOT NULL DEFAULT false,
                    is_pwd boolean NOT NULL DEFAULT false,
                    tenant_id uuid NOT NULL,
                    department_id uuid NOT NULL,
                    designation_id uuid NOT NULL,
                    branch_id uuid,
                    cost_centre_id uuid,
                    encrypted_aadhaar text,
                    encrypted_bank_account text,
                    encrypted_ifsc text,
                    uan varchar(12),
                    esicip_number varchar(17),
                    created_at timestamptz NOT NULL,
                    created_by uuid NOT NULL,
                    updated_at timestamptz,
                    updated_by uuid,
                    is_deleted boolean NOT NULL DEFAULT false,
                    deleted_at timestamptz,
                    deleted_by uuid
                )");
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS salary_structures (
                    id uuid NOT NULL PRIMARY KEY,
                    employee_id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    annual_ctc numeric(18,4) NOT NULL,
                    effective_from date NOT NULL,
                    effective_to date,
                    created_at timestamptz NOT NULL,
                    created_by uuid NOT NULL,
                    updated_at timestamptz,
                    updated_by uuid,
                    is_deleted boolean NOT NULL DEFAULT false,
                    deleted_at timestamptz,
                    deleted_by uuid
                )");

            migrationBuilder.DropTable(
                name: "employee_exits");

            migrationBuilder.DropTable(
                name: "employee_salary_component_overrides");

            migrationBuilder.DropTable(
                name: "employee_vehicle_details");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "prior_employer_ytds");

            migrationBuilder.DropTable(
                name: "salary_revisions");

            migrationBuilder.DropTable(
                name: "employee_salary_structures");

            migrationBuilder.AlterColumn<decimal>(
                name: "bonus_rate",
                table: "statutory_org_configs",
                type: "numeric(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldDefaultValue: 0.0833m);

            migrationBuilder.AlterColumn<string>(
                name: "bonus_mode",
                table: "statutory_org_configs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Yearly");
        }
    }
}
