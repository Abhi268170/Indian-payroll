using Payroll.Domain.Enums;

namespace Payroll.Application.DTOs;

public sealed record SalaryStructureTemplateSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int ComponentCount);

public sealed record SalaryStructureTemplateDetailDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    bool EpfEnabled,
    bool EsiEnabled,
    bool PtEnabled,
    bool LwfEnabled,
    IReadOnlyList<SalaryStructureComponentDto> Components);

public sealed record SalaryStructureComponentDto(
    Guid ComponentId,
    string ComponentName,
    string ComponentCode,
    ComponentCategory Category,
    bool IsSystemComponent,
    ComponentFormulaType FormulaType,
    decimal? FixedAmount,
    decimal? Percentage,
    int DisplayOrder,
    // Surfaced for the client-side preview calculator: identifies the Basic row
    // (gratuity driver) and flags components that contribute to PF wage (so the
    // preview can subtract employer EPF from the residual). The engine still owns
    // run-time math — these fields only feed the preview.
    EarningType? EarningType,
    bool ConsiderForEpf);
