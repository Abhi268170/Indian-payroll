using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations.PayrollDb
{
    /// <inheritdoc />
    public partial class CatchupFnfAndOneTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_one_time",
                table: "salary_components",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "employee_exit_id",
                table: "payrun_employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "calculate_on_pro_rata",
                table: "payrun_component_breakdowns",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "consider_for_esi",
                table: "payrun_component_breakdowns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "epf_inclusion_rule",
                table: "payrun_component_breakdowns",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Always");

            migrationBuilder.AddColumn<bool>(
                name: "is_taxable",
                table: "payrun_component_breakdowns",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_in_payslip",
                table: "payrun_component_breakdowns",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "employee_exit_id",
                table: "payroll_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deductor_employee_id",
                table: "org_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fnf_payroll_run_id",
                table: "employee_exits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_salary_components_tenant_id_is_one_time_category_is_active",
                table: "salary_components",
                columns: new[] { "tenant_id", "is_one_time", "category", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_tenant_id_type_status_pay_day",
                table: "payroll_runs",
                columns: new[] { "tenant_id", "type", "status", "pay_day" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_salary_components_tenant_id_is_one_time_category_is_active",
                table: "salary_components");

            migrationBuilder.DropIndex(
                name: "ix_payroll_runs_tenant_id_type_status_pay_day",
                table: "payroll_runs");

            migrationBuilder.DropColumn(
                name: "is_one_time",
                table: "salary_components");

            migrationBuilder.DropColumn(
                name: "employee_exit_id",
                table: "payrun_employees");

            migrationBuilder.DropColumn(
                name: "calculate_on_pro_rata",
                table: "payrun_component_breakdowns");

            migrationBuilder.DropColumn(
                name: "consider_for_esi",
                table: "payrun_component_breakdowns");

            migrationBuilder.DropColumn(
                name: "epf_inclusion_rule",
                table: "payrun_component_breakdowns");

            migrationBuilder.DropColumn(
                name: "is_taxable",
                table: "payrun_component_breakdowns");

            migrationBuilder.DropColumn(
                name: "show_in_payslip",
                table: "payrun_component_breakdowns");

            migrationBuilder.DropColumn(
                name: "employee_exit_id",
                table: "payroll_runs");

            migrationBuilder.DropColumn(
                name: "deductor_employee_id",
                table: "org_profiles");

            migrationBuilder.DropColumn(
                name: "fnf_payroll_run_id",
                table: "employee_exits");
        }
    }
}
