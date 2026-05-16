using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record DeleteDesignationCommand(Guid Id, Guid ActorId) : IRequest;
