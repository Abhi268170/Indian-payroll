using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record CreateDepartmentCommand(
    string Name,
    string? Code,
    string? Description,
    Guid ActorId) : IRequest<Guid>;
