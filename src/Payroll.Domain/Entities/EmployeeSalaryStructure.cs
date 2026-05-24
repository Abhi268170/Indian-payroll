using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class EmployeeSalaryStructure : AuditableEntity
{
    private EmployeeSalaryStructure() { }

    public Guid EmployeeId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? SalaryStructureTemplateId { get; private set; }
    public decimal AnnualCTC { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    public ICollection<EmployeeSalaryComponentOverride> ComponentOverrides { get; private set; } = new List<EmployeeSalaryComponentOverride>();

    public static EmployeeSalaryStructure Create(
        Guid employeeId,
        Guid tenantId,
        Guid? salaryStructureTemplateId,
        decimal annualCTC,
        DateOnly effectiveFrom,
        Guid createdBy) => new()
        {
            EmployeeId = employeeId,
            TenantId = tenantId,
            SalaryStructureTemplateId = salaryStructureTemplateId,
            AnnualCTC = annualCTC,
            EffectiveFrom = effectiveFrom,
            CreatedBy = createdBy
        };

    public void AddOverride(EmployeeSalaryComponentOverride o) =>
        ComponentOverrides.Add(o);

    public void Close(DateOnly effectiveTo, Guid updatedBy)
    {
        EffectiveTo = effectiveTo;
        SetUpdated(updatedBy);
    }
}
