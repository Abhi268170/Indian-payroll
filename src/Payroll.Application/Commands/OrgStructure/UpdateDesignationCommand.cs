using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record UpdateDesignationCommand(Guid Id, string Name, Guid ActorId) : IRequest;
