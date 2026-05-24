using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record DeleteBusinessUnitCommand(Guid Id, Guid ActorId) : IRequest;
