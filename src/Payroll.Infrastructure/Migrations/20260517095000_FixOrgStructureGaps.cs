using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOrgStructureGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // description column was missing from departments when AddOrgStructureEntities
            // failed to recreate the table (InitialTenant had already created it without this column)
            migrationBuilder.Sql("""
                ALTER TABLE departments ADD COLUMN IF NOT EXISTS description character varying(500);
                """);

            // InitialTenant created departments/designations/cost_centres with tenant_id NOT NULL,
            // but AddOrgStructureEntities (schema-per-tenant design) doesn't use tenant_id on these tables.
            // Make the column nullable so EF inserts don't fail.
            migrationBuilder.Sql("""
                ALTER TABLE departments ALTER COLUMN tenant_id DROP NOT NULL;
                ALTER TABLE departments ALTER COLUMN parent_department_id DROP NOT NULL;
                ALTER TABLE designations ALTER COLUMN tenant_id DROP NOT NULL;
                ALTER TABLE cost_centres ALTER COLUMN tenant_id DROP NOT NULL;
                """);

            // business_units table was never created because AddOrgStructureEntities migration
            // failed partway through (departments already existed from InitialTenant)
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS business_units (
                    id uuid NOT NULL,
                    name character varying(150) NOT NULL,
                    description character varying(500) NULL,
                    created_at timestamptz NOT NULL,
                    created_by uuid NOT NULL,
                    updated_at timestamptz NULL,
                    updated_by uuid NULL,
                    is_deleted boolean NOT NULL,
                    deleted_at timestamptz NULL,
                    deleted_by uuid NULL,
                    CONSTRAINT pk_business_units PRIMARY KEY (id)
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS business_units;");
            migrationBuilder.Sql("ALTER TABLE departments DROP COLUMN IF EXISTS description;");
        }
    }
}
