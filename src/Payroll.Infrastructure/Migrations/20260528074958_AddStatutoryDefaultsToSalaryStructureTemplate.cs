using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatutoryDefaultsToSalaryStructureTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "epf_enabled",
                table: "salary_structure_templates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "esi_enabled",
                table: "salary_structure_templates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "lwf_enabled",
                table: "salary_structure_templates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "pt_enabled",
                table: "salary_structure_templates",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "epf_enabled",
                table: "salary_structure_templates");

            migrationBuilder.DropColumn(
                name: "esi_enabled",
                table: "salary_structure_templates");

            migrationBuilder.DropColumn(
                name: "lwf_enabled",
                table: "salary_structure_templates");

            migrationBuilder.DropColumn(
                name: "pt_enabled",
                table: "salary_structure_templates");
        }
    }
}
