using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixStatutoryComponentGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add deduction_months_csv to professional_tax_slabs for half-yearly/annual PT states
            migrationBuilder.Sql("""
                ALTER TABLE professional_tax_slabs ADD COLUMN IF NOT EXISTS deduction_months_csv character varying(30);
                """);

            // Seed known half-yearly deduction months for TN and KL (September + March)
            migrationBuilder.Sql("""
                UPDATE professional_tax_slabs SET deduction_months_csv = '9,3' WHERE state_code IN ('TN', 'KL') AND frequency = 'HalfYearly';
                """);

            // New table for PT registration numbers per state
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS pt_state_registrations (
                    id uuid NOT NULL,
                    state_code character varying(10) NOT NULL,
                    registration_number character varying(100) NOT NULL,
                    created_at timestamptz NOT NULL,
                    created_by uuid NOT NULL,
                    updated_at timestamptz NULL,
                    updated_by uuid NULL,
                    is_deleted boolean NOT NULL DEFAULT false,
                    deleted_at timestamptz NULL,
                    deleted_by uuid NULL,
                    CONSTRAINT pk_pt_state_registrations PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_pt_state_registrations_state_code ON pt_state_registrations (state_code) WHERE is_deleted = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS pt_state_registrations;");
            migrationBuilder.Sql("ALTER TABLE professional_tax_slabs DROP COLUMN IF EXISTS deduction_months_csv;");
        }
    }
}
