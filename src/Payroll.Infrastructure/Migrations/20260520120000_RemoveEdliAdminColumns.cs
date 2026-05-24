using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

public partial class RemoveEdliAdminColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "edli_amount", table: "payrun_employees");
        migrationBuilder.DropColumn(name: "admin_amount", table: "payrun_employees");
        migrationBuilder.DropColumn(name: "total_edli", table: "payroll_runs");
        migrationBuilder.DropColumn(name: "edli_employer_rate", table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "edli_cap", table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "epf_admin_rate", table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "epf_admin_minimum", table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "epf_include_edli_in_ctc", table: "statutory_org_configs");
        migrationBuilder.DropColumn(name: "epf_include_admin_in_ctc", table: "statutory_org_configs");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "edli_amount", table: "payrun_employees", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "admin_amount", table: "payrun_employees", type: "numeric(18,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "total_edli", table: "payroll_runs", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "edli_employer_rate", table: "income_tax_configs", type: "numeric(7,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "edli_cap", table: "income_tax_configs", type: "numeric(18,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "epf_admin_rate", table: "income_tax_configs", type: "numeric(7,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "epf_admin_minimum", table: "income_tax_configs", type: "numeric(18,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<bool>(name: "epf_include_edli_in_ctc", table: "statutory_org_configs", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "epf_include_admin_in_ctc", table: "statutory_org_configs", nullable: false, defaultValue: false);
    }
}
