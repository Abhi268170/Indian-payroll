using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollRunModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_run_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "text", nullable: false),
                    to_status = table.Column<string>(type: "text", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("pk_payroll_run_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payroll_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pay_period_year = table.Column<int>(type: "integer", nullable: false),
                    pay_period_month = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    payroll_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_net_pay = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_employer_pf = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_employer_esi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_edli = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_tds = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_pt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    employee_count = table.Column<int>(type: "integer", nullable: false),
                    pay_day = table.Column<DateOnly>(type: "date", nullable: true),
                    statutory_config_snapshot = table.Column<string>(type: "text", nullable: true),
                    variable_inputs_file_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_rejection_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    payment_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    bank_advice_file_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("pk_payroll_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payrun_component_breakdowns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    salary_component_id = table.Column<Guid>(type: "uuid", nullable: true),
                    component_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    component_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    full_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    prorated_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_one_time_earning = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_payrun_component_breakdowns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payrun_employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    base_days = table.Column<int>(type: "integer", nullable: false),
                    lop_days = table.Column<int>(type: "integer", nullable: false),
                    actual_payable_days = table.Column<int>(type: "integer", nullable: false),
                    gross_pay = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_pay = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    taxes_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    benefits_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reimbursements_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    employee_pf = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    employer_pf = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    employee_esi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    employer_esi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    pt_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tds_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    edli_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tds_override_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    tds_override_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    skip_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_withheld = table.Column<bool>(type: "boolean", nullable: false),
                    payment_mode_override = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_payrun_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payslips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pdf_storage_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    net_pay = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_pay_in_words = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ytd_data_json = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_payslips", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tds_worksheets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    tax_regime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    annual_projected_income = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    standard_deduction = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    taxable_income = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tax_before_rebate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    rebate87a = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    surcharge = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cess = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    annual_tax_liability = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ytd_tds_deducted = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    remaining_months_in_fy = table.Column<int>(type: "integer", nullable: false),
                    tds_this_month = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    has_pan_override = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_tds_worksheets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_run_audit_logs_payroll_run_id_timestamp",
                table: "payroll_run_audit_logs",
                columns: new[] { "payroll_run_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_tenant_id_status",
                table: "payroll_runs",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payrun_component_breakdowns_payroll_run_id_employee_id",
                table: "payrun_component_breakdowns",
                columns: new[] { "payroll_run_id", "employee_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payrun_employees_payroll_run_id_employee_id",
                table: "payrun_employees",
                columns: new[] { "payroll_run_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payrun_employees_tenant_id_employee_id",
                table: "payrun_employees",
                columns: new[] { "tenant_id", "employee_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payslips_payroll_run_id_employee_id",
                table: "payslips",
                columns: new[] { "payroll_run_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tds_worksheets_payroll_run_id_employee_id",
                table: "tds_worksheets",
                columns: new[] { "payroll_run_id", "employee_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_run_audit_logs");

            migrationBuilder.DropTable(
                name: "payroll_runs");

            migrationBuilder.DropTable(
                name: "payrun_component_breakdowns");

            migrationBuilder.DropTable(
                name: "payrun_employees");

            migrationBuilder.DropTable(
                name: "payslips");

            migrationBuilder.DropTable(
                name: "tds_worksheets");
        }
    }
}
