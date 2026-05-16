using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Persistence.EntityConfigurations;

namespace Payroll.Infrastructure.Persistence;

public sealed class PayrollDbContext(
    DbContextOptions<PayrollDbContext> options,
    ITenantContext tenantContext)
    : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WorkLocation> WorkLocations => Set<WorkLocation>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<CostCentre> CostCentres => Set<CostCentre>();
    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();
    public DbSet<OrgProfile> OrgProfiles => Set<OrgProfile>();

    // Exposed for TenantModelCacheKeyFactory which lives in EF Core's internal SP
    // and cannot use constructor injection of ITenantContext.
    internal string TenantSchema => tenantContext.Schema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Binds all tables in this context to the resolved tenant schema.
        // Empty at design time (migrations factory) so DDL stays schema-less.
        if (!string.IsNullOrEmpty(tenantContext.Schema))
            modelBuilder.HasDefaultSchema(tenantContext.Schema);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new WorkLocationConfiguration());
        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new DesignationConfiguration());
        modelBuilder.ApplyConfiguration(new CostCentreConfiguration());
        modelBuilder.ApplyConfiguration(new BusinessUnitConfiguration());
        modelBuilder.ApplyConfiguration(new OrgProfileConfiguration());
    }
}
