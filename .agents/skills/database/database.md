# Skill: Database — EF Core, Dapper, Migrations, Schema-per-Tenant

## Stack

- **EF Core 8:** writes, migrations, schema management
- **Dapper:** complex reads, reports, bulk queries
- **PostgreSQL 16** via Npgsql
- **PgBouncer:** connection pooling (transaction mode)
- **Schema-per-tenant:** each tenant = isolated PostgreSQL schema

---

## Schema-per-Tenant: Core Concept

```
public schema:     platform_tenants (tenant registry)
                   platform_users   (super-admin only)

tenant schema:     {tenant_slug}.employees
                   {tenant_slug}.payroll_runs
                   {tenant_slug}.payroll_results
                   ... all tenant data tables
```

Never cross-schema joins. Tenant isolation enforced at DbContext construction — no runtime tenant switching.

---

## DbContext Setup

```csharp
// TenantDbContextFactory — creates schema-bound context per request
public sealed class TenantDbContextFactory(
    IDbContextFactory<PayrollDbContext> factory,
    ITenantContext tenant) : ITenantDbContextFactory
{
    public PayrollDbContext CreateForCurrentTenant()
    {
        var ctx = factory.CreateDbContext();
        ctx.Database.SetDefaultSchema(tenant.Schema); // e.g. "acme_corp"
        return ctx;
    }
}

// PayrollDbContext — schema injected at construction
public sealed class PayrollDbContext(DbContextOptions<PayrollDbContext> options)
    : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    // ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema is set via SetDefaultSchema — applies to all tables
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PayrollDbContext).Assembly);
    }
}
```

---

## Entity Configuration Pattern

```csharp
// Use IEntityTypeConfiguration<T> — never configure in OnModelCreating directly
internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever(); // domain sets ID

        builder.Property(e => e.EncryptedPAN)
            .HasColumnName("pan_encrypted")
            .IsRequired()
            .HasMaxLength(500);  // encrypted ciphertext is longer

        builder.Property(e => e.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.DateOfJoining)
            .HasColumnType("date");  // PostgreSQL date, not timestamp

        builder.Property(e => e.CreatedAt)
            .HasColumnType("timestamptz")  // always UTC timestamps
            .IsRequired();

        // Soft delete
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.EmployeeCode).IsUnique();

        // Table name: snake_case convention
        builder.ToTable("employees");
    }
}
```

---

## Naming Convention (PostgreSQL snake_case)

Register globally — never use EF defaults (PascalCase):

```csharp
// In DbContext or via Npgsql convention
protected override void ConfigureConventions(ModelConfigurationBuilder config)
{
    config.Conventions.Add(_ => new SnakeCaseNamingConvention());
}
```

Column/table naming:
- Tables: `employees`, `payroll_runs`, `payroll_results`
- Columns: `first_name`, `date_of_joining`, `pan_encrypted`, `created_at`
- PKs: `id` (UUID)
- FKs: `{entity}_id` (e.g., `employee_id`, `department_id`)

---

## Migrations

### Creating migrations
```bash
dotnet ef migrations add AddEmployeePTState \
  --project src/Payroll.Infrastructure \
  --startup-project src/Payroll.Api \
  --output-dir Migrations
```

### Migration rules
1. Every migration has `Up` AND `Down` — test rollback before merge.
2. No data migration in schema migration files — separate script.
3. Multi-tenant migrations: runner iterates all tenant schemas.
4. Never delete columns in same migration as removing from model — two-step: nullable first, drop later.
5. Always add `NOT NULL` columns with a default for existing rows.

### Multi-tenant migration runner
```csharp
public sealed class TenantMigrationRunner(
    IServiceProvider services,
    ITenantRepository tenantRepo,
    ILogger<TenantMigrationRunner> logger)
{
    public async Task RunAllTenantsAsync(CancellationToken ct)
    {
        var tenants = await tenantRepo.GetAllActiveAsync(ct);

        foreach (var tenant in tenants)
        {
            using var scope = services.CreateScope();
            var factory = scope.ServiceProvider
                .GetRequiredService<ITenantDbContextFactory>();

            // Set tenant context before creating DbContext
            var tenantCtx = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantCtx.SetTenant(tenant);

            await using var ctx = factory.CreateForCurrentTenant();
            logger.LogInformation("Migrating tenant {Schema}", tenant.Schema);
            await ctx.Database.MigrateAsync(ct);
        }
    }
}
```

---

## EF Core: Write Patterns

```csharp
// Repository pattern — EF Core behind interface, Dapper read models separate

public sealed class EmployeeRepository(ITenantDbContextFactory ctxFactory)
    : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var ctx = ctxFactory.CreateForCurrentTenant();
        return await ctx.Employees.FindAsync([id], ct);
    }

    public async Task AddAsync(Employee employee, CancellationToken ct)
    {
        await using var ctx = ctxFactory.CreateForCurrentTenant();
        ctx.Employees.Add(employee);
        await ctx.SaveChangesAsync(ct);
    }

    // Bulk: never SaveChanges in a loop
    public async Task AddRangeAsync(
        IReadOnlyList<Employee> employees, CancellationToken ct)
    {
        await using var ctx = ctxFactory.CreateForCurrentTenant();
        ctx.Employees.AddRange(employees);
        await ctx.SaveChangesAsync(ct);
    }
}
```

---

## Dapper: Read Patterns

Use for complex queries, reports, payroll summary reads — anything with joins, aggregates, or CTEs.

```csharp
public sealed class PayrollReadRepository(
    ITenantConnectionFactory connectionFactory) : IPayrollReadRepository
{
    public async Task<PayrollSummaryDto?> GetSummaryAsync(
        Guid runId, CancellationToken ct)
    {
        await using var conn = connectionFactory.CreateForCurrentTenant();

        const string sql = """
            SELECT
                pr.id,
                pr.pay_period_start,
                pr.pay_period_end,
                pr.status,
                COUNT(res.id) AS employee_count,
                SUM(res.net_pay) AS total_net_pay,
                SUM(res.tds_amount) AS total_tds,
                SUM(res.pf_employee) AS total_pf_employee
            FROM payroll_runs pr
            LEFT JOIN payroll_results res ON res.payroll_run_id = pr.id
            WHERE pr.id = @RunId
              AND pr.is_deleted = false
            GROUP BY pr.id, pr.pay_period_start, pr.pay_period_end, pr.status
            """;

        return await conn.QuerySingleOrDefaultAsync<PayrollSummaryDto>(
            new CommandDefinition(sql, new { RunId = runId },
                cancellationToken: ct));
    }
}
```

**Dapper rules:**
- Always use `@ParameterName` — never string interpolation in SQL.
- `CommandDefinition` with `cancellationToken` — always pass CT.
- Schema is set at connection string level via `search_path` — not in SQL.
- Use `QueryAsync<T>` for lists, `QuerySingleOrDefaultAsync<T>` for single.

---

## Tenant Connection Factory (Dapper)

```csharp
public sealed class TenantConnectionFactory(
    IConfiguration config,
    ITenantContext tenant) : ITenantConnectionFactory
{
    public NpgsqlConnection CreateForCurrentTenant()
    {
        var connStr = config.GetConnectionString("Payroll");
        var builder = new NpgsqlConnectionStringBuilder(connStr)
        {
            // search_path sets the schema for Dapper queries
            SearchPath = tenant.Schema
        };
        return new NpgsqlConnection(builder.ConnectionString);
    }
}
```

---

## Soft Deletes

Entities with audit trail (employees, salary structures, payroll runs) use soft delete:

```csharp
public abstract class AuditableEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; set; }

    public void SoftDelete(Guid deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
```

EF global query filter: `builder.HasQueryFilter(e => !e.IsDeleted)` — soft-deleted records are invisible to all queries.

---

## Statutory Config Tables

These are tenant-level config (overridable) with platform defaults:

```sql
-- platform defaults (public schema)
CREATE TABLE public.tax_slabs (
    id UUID PRIMARY KEY,
    regime VARCHAR(20) NOT NULL,  -- 'new', 'old' (old: future)
    fiscal_year CHAR(7) NOT NULL, -- 'FY2026'
    income_from NUMERIC(15,2) NOT NULL,
    income_to NUMERIC(15,2),      -- NULL = no upper bound
    rate NUMERIC(5,4) NOT NULL,
    effective_from DATE NOT NULL,
    effective_to DATE
);

-- Per-tenant PT config
CREATE TABLE {schema}.pt_config (
    id UUID PRIMARY KEY,
    state_code CHAR(2) NOT NULL,
    monthly_salary_from NUMERIC(15,2) NOT NULL,
    monthly_salary_to NUMERIC(15,2),
    monthly_pt_amount NUMERIC(15,2) NOT NULL,
    effective_from DATE NOT NULL
);
```

---

## Performance Guidelines

- **Pagination:** all list queries use keyset pagination (not `OFFSET`).
- **Indexes:** add explicit indexes for all FK columns and query filter columns.
- **N+1:** use `.Include()` or explicit joins — never lazy loading in production code.
- **Bulk insert:** EF Core `AddRange` + single `SaveChanges`, or `Npgsql COPY` for very large datasets.
- **Payroll results:** bulk-insert via Npgsql binary COPY for 10k employees — `SaveChangesAsync` on 10k rows is too slow.

```csharp
// Bulk insert payroll results via COPY
await using var writer = await conn.BeginBinaryImportAsync(
    $"""COPY "{tenant.Schema}".payroll_results (id, payroll_run_id, employee_id, ...) FROM STDIN (FORMAT BINARY)""");
foreach (var result in results)
{
    await writer.StartRowAsync();
    await writer.WriteAsync(result.Id, NpgsqlDbType.Uuid);
    // ...
}
await writer.CompleteAsync();
```
