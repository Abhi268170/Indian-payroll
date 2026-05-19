using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

/// <inheritdoc />
public partial class DropCostCentre : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            DO $$
            DECLARE
                s TEXT;
            BEGIN
                FOR s IN
                    SELECT schema_name FROM information_schema.schemata
                    WHERE schema_name NOT IN ('public','pg_catalog','information_schema','hangfire')
                      AND schema_name NOT LIKE 'pg_%'
                LOOP
                    EXECUTE format('ALTER TABLE %I.employees DROP COLUMN IF EXISTS cost_centre_id', s);
                    EXECUTE format('DROP TABLE IF EXISTS %I.cost_centres', s);
                END LOOP;
            END $$;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            DO $$
            DECLARE
                s TEXT;
            BEGIN
                FOR s IN
                    SELECT schema_name FROM information_schema.schemata
                    WHERE schema_name NOT IN ('public','pg_catalog','information_schema','hangfire')
                      AND schema_name NOT LIKE 'pg_%'
                LOOP
                    EXECUTE format('
                        CREATE TABLE IF NOT EXISTS %I.cost_centres (
                            id uuid NOT NULL,
                            name character varying(150) NOT NULL,
                            code character varying(20) NULL,
                            is_deleted boolean NOT NULL DEFAULT false,
                            created_at timestamptz NOT NULL,
                            created_by uuid NOT NULL,
                            updated_at timestamptz NULL,
                            updated_by uuid NULL,
                            deleted_at timestamptz NULL,
                            deleted_by uuid NULL,
                            CONSTRAINT pk_cost_centres PRIMARY KEY (id)
                        )', s);
                    EXECUTE format('ALTER TABLE %I.employees ADD COLUMN IF NOT EXISTS cost_centre_id uuid NULL', s);
                END LOOP;
            END $$;
        ");
    }
}
