using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExitStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "employee_exits",
                type: "text",
                nullable: false,
                defaultValue: "InProgress");

            // Rebuild the unique index to scope to in-progress exits only so a
            // re-hired employee with a Completed/Reverted prior exit can exit again.
            migrationBuilder.DropIndex(
                name: "ix_employee_exits_employee_id",
                table: "employee_exits");

            migrationBuilder.CreateIndex(
                name: "ix_employee_exits_employee_id",
                table: "employee_exits",
                column: "employee_id",
                unique: true,
                filter: "is_deleted = false AND status = 'InProgress'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_employee_exits_employee_id",
                table: "employee_exits");

            migrationBuilder.DropColumn(
                name: "status",
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
