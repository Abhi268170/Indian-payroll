using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class EmployeeSalaryStructure : AuditableEntity
{
    private EmployeeSalaryStructure() { }

    public Guid EmployeeId { get; private set; }
    public Guid TenantId { get; private set; }
    public decimal AnnualCTC { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    public static EmployeeSalaryStructure Create(
        Guid employeeId,
        Guid tenantId,
        decimal annualCTC,
        DateOnly effectiveFrom,
        Guid createdBy) => new()
        {
            EmployeeId = employeeId,
            TenantId = tenantId,
            AnnualCTC = annualCTC,
            EffectiveFrom = effectiveFrom,
            CreatedBy = createdBy
        };
}
