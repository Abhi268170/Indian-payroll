using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class EmployeeSalaryComponentOverride : AuditableEntity
{
    private EmployeeSalaryComponentOverride() { }

    public Guid EmployeeSalaryStructureId { get; private set; }
    public Guid SalaryComponentId { get; private set; }
    public ComponentFormulaType FormulaType { get; private set; }
    public decimal? Percentage { get; private set; }
    public decimal? FixedAmount { get; private set; }

    public static EmployeeSalaryComponentOverride Create(
        Guid employeeSalaryStructureId,
        Guid salaryComponentId,
        ComponentFormulaType formulaType,
        decimal? percentage,
        decimal? fixedAmount,
        Guid createdBy) => new()
        {
            EmployeeSalaryStructureId = employeeSalaryStructureId,
            SalaryComponentId = salaryComponentId,
            FormulaType = formulaType,
            Percentage = percentage,
            FixedAmount = fixedAmount,
            CreatedBy = createdBy
        };
}
