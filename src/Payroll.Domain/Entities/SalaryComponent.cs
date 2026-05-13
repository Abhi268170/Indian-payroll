using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class SalaryComponent : AuditableEntity
{
    private SalaryComponent() { }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public ComponentFormulaType FormulaType { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal? Percentage { get; private set; }
    public bool IsTaxable { get; private set; }
    public bool IsSystemComponent { get; private set; }
    public Guid TenantId { get; private set; }

    public static SalaryComponent Create(
        string name,
        string code,
        ComponentFormulaType formulaType,
        bool isTaxable,
        Guid tenantId,
        Guid createdBy,
        decimal? fixedAmount = null,
        decimal? percentage = null) => new()
        {
            Name = name,
            Code = code,
            FormulaType = formulaType,
            FixedAmount = fixedAmount,
            Percentage = percentage,
            IsTaxable = isTaxable,
            TenantId = tenantId,
            CreatedBy = createdBy
        };
}
