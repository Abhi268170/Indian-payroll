using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations.PayrollDb
{
    /// <inheritdoc />
    public partial class AddFnfLinkColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "employee_exit_id",
                table: "payrun_employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "employee_exit_id",
                table: "payroll_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fnf_payroll_run_id",
                table: "employee_exits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_tenant_id_type_status_pay_day",
                table: "payroll_runs",
                columns: new[] { "tenant_id", "type", "status", "pay_day" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payroll_runs_tenant_id_type_status_pay_day",
                table: "payroll_runs");

            migrationBuilder.DropColumn(
                name: "employee_exit_id",
                table: "payrun_employees");

            migrationBuilder.DropColumn(
                name: "employee_exit_id",
                table: "payroll_runs");

            migrationBuilder.DropColumn(
                name: "fnf_payroll_run_id",
                table: "employee_exits");
        }
    }
}
