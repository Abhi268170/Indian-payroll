using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddUniquePayrollRunPeriodConstraint : Migration
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
                    EXECUTE format(
                        'CREATE UNIQUE INDEX IF NOT EXISTS ix_payroll_runs_tenant_period
                         ON %I.payroll_runs (tenant_id, pay_period_year, pay_period_month)
                         WHERE deleted_at IS NULL',
                        s);
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
                    EXECUTE format('DROP INDEX IF EXISTS %I.ix_payroll_runs_tenant_period', s);
                END LOOP;
            END $$;
        ");
    }
}
