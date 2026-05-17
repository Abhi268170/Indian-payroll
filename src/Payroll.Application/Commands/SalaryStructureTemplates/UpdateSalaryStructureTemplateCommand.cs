using MediatR;

namespace Payroll.Application.Commands.SalaryStructureTemplates;

public sealed record UpdateSalaryStructureTemplateCommand(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<TemplateComponentInput> Components,
    Guid ActorId
) : IRequest;
