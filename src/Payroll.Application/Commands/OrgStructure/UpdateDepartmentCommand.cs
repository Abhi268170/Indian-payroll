using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    string? Code,
    string? Description,
    Guid ActorId) : IRequest;
