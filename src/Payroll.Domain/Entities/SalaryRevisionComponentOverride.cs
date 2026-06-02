using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

// Snapshot of a per-component override captured on a SalaryRevision at creation time.
// Mirrors EmployeeSalaryComponentOverride so that, on apply, the backdated
// EmployeeSalaryStructure can be reproduced exactly — CTC + template alone cannot
// reconstruct per-component arrear amounts.
public sealed class SalaryRevisionComponentOverride : AuditableEntity
{
    private SalaryRevisionComponentOverride() { }

    public Guid SalaryRevisionId { get; private set; }
    public Guid SalaryComponentId { get; private set; }
    public ComponentFormulaType FormulaType { get; private set; }
    public decimal? Percentage { get; private set; }
    public decimal? FixedAmount { get; private set; }

    public static SalaryRevisionComponentOverride Create(
        Guid salaryRevisionId,
        Guid salaryComponentId,
        ComponentFormulaType formulaType,
        decimal? percentage,
        decimal? fixedAmount,
        Guid createdBy) => new()
        {
            SalaryRevisionId = salaryRevisionId,
            SalaryComponentId = salaryComponentId,
            FormulaType = formulaType,
            Percentage = percentage,
            FixedAmount = fixedAmount,
            CreatedBy = createdBy
        };
}
