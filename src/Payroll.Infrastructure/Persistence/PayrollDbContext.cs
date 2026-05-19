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
    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();
    public DbSet<OrgProfile> OrgProfiles => Set<OrgProfile>();
    public DbSet<PaySchedule> PaySchedules => Set<PaySchedule>();
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();
    public DbSet<SalaryStructureTemplate> SalaryStructureTemplates => Set<SalaryStructureTemplate>();
    public DbSet<SalaryStructureComponent> SalaryStructureComponents => Set<SalaryStructureComponent>();
    public DbSet<StatutoryToggle> StatutoryToggles => Set<StatutoryToggle>();
    public DbSet<StatutoryOrgConfig> StatutoryOrgConfigs => Set<StatutoryOrgConfig>();
    public DbSet<ProfessionalTaxSlab> ProfessionalTaxSlabs => Set<ProfessionalTaxSlab>();
    public DbSet<LwfStateConfig> LwfStateConfigs => Set<LwfStateConfig>();
    public DbSet<PtStateRegistration> PtStateRegistrations => Set<PtStateRegistration>();
    public DbSet<IncomeTaxSlab> IncomeTaxSlabs => Set<IncomeTaxSlab>();
    public DbSet<IncomeTaxSurchargeSlab> IncomeTaxSurchargeSlabs => Set<IncomeTaxSurchargeSlab>();
    public DbSet<IncomeTaxConfig> IncomeTaxConfigs => Set<IncomeTaxConfig>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeSalaryStructure> EmployeeSalaryStructures => Set<EmployeeSalaryStructure>();
    public DbSet<EmployeeSalaryComponentOverride> EmployeeSalaryComponentOverrides => Set<EmployeeSalaryComponentOverride>();
    public DbSet<EmployeeExit> EmployeeExits => Set<EmployeeExit>();
    public DbSet<SalaryRevision> SalaryRevisions => Set<SalaryRevision>();
    public DbSet<EmployeeVehicleDetail> EmployeeVehicleDetails => Set<EmployeeVehicleDetail>();
    public DbSet<PriorEmployerYtd> PriorEmployerYtds => Set<PriorEmployerYtd>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrunEmployee> PayrunEmployees => Set<PayrunEmployee>();
    public DbSet<PayrunComponentBreakdown> PayrunComponentBreakdowns => Set<PayrunComponentBreakdown>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<TdsWorksheet> TdsWorksheets => Set<TdsWorksheet>();
    public DbSet<PayrollRunAuditLog> PayrollRunAuditLogs => Set<PayrollRunAuditLog>();

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
        modelBuilder.ApplyConfiguration(new BusinessUnitConfiguration());
        modelBuilder.ApplyConfiguration(new OrgProfileConfiguration());
        modelBuilder.ApplyConfiguration(new PayScheduleConfiguration());
        modelBuilder.ApplyConfiguration(new SalaryComponentConfiguration());
        modelBuilder.ApplyConfiguration(new SalaryStructureTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new SalaryStructureComponentConfiguration());
        modelBuilder.ApplyConfiguration(new StatutoryToggleConfiguration());
        modelBuilder.ApplyConfiguration(new StatutoryOrgConfigConfiguration());
        modelBuilder.ApplyConfiguration(new ProfessionalTaxSlabConfiguration());
        modelBuilder.ApplyConfiguration(new LwfStateConfigConfiguration());
        modelBuilder.ApplyConfiguration(new PtStateRegistrationConfiguration());
        modelBuilder.ApplyConfiguration(new IncomeTaxSlabConfiguration());
        modelBuilder.ApplyConfiguration(new IncomeTaxSurchargeSlabConfiguration());
        modelBuilder.ApplyConfiguration(new IncomeTaxConfigConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeSalaryStructureConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeSalaryComponentOverrideConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeExitConfiguration());
        modelBuilder.ApplyConfiguration(new SalaryRevisionConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeVehicleDetailConfiguration());
        modelBuilder.ApplyConfiguration(new PriorEmployerYtdConfiguration());
        modelBuilder.ApplyConfiguration(new PayrollRunConfiguration());
        modelBuilder.ApplyConfiguration(new PayrunEmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new PayrunComponentBreakdownConfiguration());
        modelBuilder.ApplyConfiguration(new PayslipConfiguration());
        modelBuilder.ApplyConfiguration(new TdsWorksheetConfiguration());
        modelBuilder.ApplyConfiguration(new PayrollRunAuditLogConfiguration());
    }
}
