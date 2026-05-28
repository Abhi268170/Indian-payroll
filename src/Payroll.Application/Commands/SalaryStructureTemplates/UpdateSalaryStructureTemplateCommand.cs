using MediatR;

namespace Payroll.Application.Commands.SalaryStructureTemplates;

public sealed record UpdateSalaryStructureTemplateCommand(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<TemplateComponentInput> Components,
    Guid ActorId,
    bool EpfEnabled = true,
    bool EsiEnabled = true,
    bool PtEnabled = true,
    bool LwfEnabled = true
) : IRequest;
