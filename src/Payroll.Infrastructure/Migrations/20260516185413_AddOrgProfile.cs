using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop InitialTenant tables that get recreated by later migrations.
            // branches: removed in final schema; no later migration recreates it.
            // payroll_runs: AddPayrollRunModule creates a new incompatible version.
            migrationBuilder.DropTable(name: "branches");
            migrationBuilder.DropTable(name: "payroll_runs");

            migrationBuilder.CreateTable(
                name: "business_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_business_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    address_line1 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    address_line2 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    pin_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
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
                    table.PrimaryKey("pk_work_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "org_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    pan = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    gstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    industry = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    incorporation_date = table.Column<DateOnly>(type: "date", nullable: true),
                    address_line1 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    address_line2 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pin_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    filing_address_work_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    logo_object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_org_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_org_profiles_work_locations_filing_address_work_location_id",
                        column: x => x.filing_address_work_location_id,
                        principalTable: "work_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_org_profiles_filing_address_work_location_id",
                table: "org_profiles",
                column: "filing_address_work_location_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "org_profiles");
            migrationBuilder.DropTable(name: "business_units");
            migrationBuilder.DropTable(name: "work_locations");
        }
    }
}
