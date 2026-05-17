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
    Guid ActorId
) : IRequest<Guid>;
