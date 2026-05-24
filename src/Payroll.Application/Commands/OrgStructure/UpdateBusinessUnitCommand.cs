using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record UpdateBusinessUnitCommand(Guid Id, string Name, string? Description, Guid ActorId) : IRequest;
