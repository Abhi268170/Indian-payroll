using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddStatutoryRatesToIncomeTaxConfig : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "cess_rate",
            table: "income_tax_configs",
            type: "numeric(7,4)",
            nullable: false,
            defaultValue: 0.04m);

        migrationBuilder.AddColumn<decimal>(
            name: "pf_wage_cap",
            table: "income_tax_configs",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 15000m);

        migrationBuilder.AddColumn<decimal>(
            name: "epf_employee_rate",
            table: "income_tax_configs",
            type: "numeric(7,4)",
            nullable: false,
            defaultValue: 0.12m);

        migrationBuilder.AddColumn<decimal>(
            name: "eps_employer_rate",
            table: "income_tax_configs",
            type: "numeric(7,4)",
            nullable: false,
            defaultValue: 0.0833m);

        migrationBuilder.AddColumn<decimal>(
            name: "eps_cap",
            table: "income_tax_configs",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 1250m);

        migrationBuilder.AddColumn<decimal>(
            name: "edli_employer_rate",
            table: "income_tax_configs",
            type: "numeric(7,4)",
            nullable: false,
            defaultValue: 0.005m);

        migrationBuilder.AddColumn<decimal>(
            name: "edli_cap",
            table: "income_tax_configs",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 75m);

        migrationBuilder.AddColumn<decimal>(
            name: "epf_admin_rate",
            table: "income_tax_configs",
            type: "numeric(7,4)",
            nullable: false,
            defaultValue: 0.005m);

        migrationBuilder.AddColumn<decimal>(
            name: "epf_admin_minimum",
            table: "income_tax_configs",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 500m);

        migrationBuilder.AddColumn<decimal>(
            name: "esi_wage_limit",
            table: "income_tax_configs",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 21000m);

        migrationBuilder.AddColumn<decimal>(
            name: "esi_pwd_wage_limit",
            table: "income_tax_configs",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 25000m);

        migrationBuilder.AddColumn<decimal>(
            name: "esi_employee_rate",
            table: "income_tax_configs",
            type: "numeric(7,4)",
            nullable: false,
            defaultValue: 0.0075m);

        migrationBuilder.AddColumn<decimal>(
            name: "esi_employer_rate",
            table: "income_tax_configs",
            type: "numeric(7,4)",
            nullable: false,
            defaultValue: 0.0325m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "cess_rate",         table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "pf_wage_cap",       table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "epf_employee_rate", table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "eps_employer_rate", table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "eps_cap",           table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "edli_employer_rate",table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "edli_cap",          table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "epf_admin_rate",    table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "epf_admin_minimum", table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "esi_wage_limit",    table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "esi_pwd_wage_limit",table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "esi_employee_rate", table: "income_tax_configs");
        migrationBuilder.DropColumn(name: "esi_employer_rate", table: "income_tax_configs");
    }
}
