using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record DeleteDepartmentCommand(Guid Id, Guid ActorId) : IRequest;
