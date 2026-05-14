using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record CreateDepartmentCommand(string Name, string? Code, Guid ActorId) : IRequest<Guid>;
