using MediatR;

namespace Payroll.Application.Commands.SalaryStructureTemplates;

public sealed record TemplateComponentInput(
    Guid ComponentId,
    string FormulaType,
    decimal? FixedAmount,
    decimal? Percentage,
    int DisplayOrder);

public sealed record CreateSalaryStructureTemplateCommand(
    string Name,
    string? Description,
    IReadOnlyList<TemplateComponentInput> Components,
    Guid ActorId,
    // Template-level statutory defaults. Default true preserves the historical
    // behaviour for clients that don't yet send these fields.
    bool EpfEnabled = true,
    bool EsiEnabled = true,
    bool PtEnabled = true,
    bool LwfEnabled = true
) : IRequest<Guid>;
