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
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CostCentre> CostCentres => Set<CostCentre>();
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();
    public DbSet<EmployeeSalaryStructure> SalaryStructures => Set<EmployeeSalaryStructure>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StatutoryToggle> StatutoryToggles => Set<StatutoryToggle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Binds all tables in this context to the resolved tenant schema.
        modelBuilder.HasDefaultSchema(tenantContext.Schema);

        base.OnModelCreating(modelBuilder);

        // Apply only tenant-level configs — not PlatformDbContext entity configs
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new DesignationConfiguration());
        modelBuilder.ApplyConfiguration(new BranchConfiguration());
        modelBuilder.ApplyConfiguration(new CostCentreConfiguration());
        modelBuilder.ApplyConfiguration(new SalaryComponentConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeSalaryStructureConfiguration());
        modelBuilder.ApplyConfiguration(new PayrollRunConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new StatutoryToggleConfiguration());
    }
}
