using MediatR;

namespace Payroll.Application.Commands.SalaryStructureTemplates;

public sealed record SetSalaryStructureTemplateActiveCommand(
    Guid Id,
    bool IsActive,
    Guid ActorId
) : IRequest;
