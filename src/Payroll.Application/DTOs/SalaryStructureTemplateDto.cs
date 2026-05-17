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
    int DisplayOrder);
