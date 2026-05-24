using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

// Per-template component slot with formula overrides.
// A SalaryComponent can appear in many templates with different amounts/percentages.
public sealed class SalaryStructureComponent : AuditableEntity
{
    private SalaryStructureComponent() { }

    public Guid TemplateId { get; private set; }
    public Guid ComponentId { get; private set; }
    public SalaryComponent? Component { get; private set; }

    public ComponentFormulaType FormulaType { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal? Percentage { get; private set; }

    public int DisplayOrder { get; private set; }

    public static SalaryStructureComponent Create(
        Guid templateId,
        Guid componentId,
        ComponentFormulaType formulaType,
        decimal? fixedAmount,
        decimal? percentage,
        int displayOrder) => new()
        {
            TemplateId = templateId,
            ComponentId = componentId,
            FormulaType = formulaType,
            FixedAmount = fixedAmount,
            Percentage = percentage,
            DisplayOrder = displayOrder,
        };
}
