using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record CreateBusinessUnitCommand(string Name, string? Description, Guid ActorId) : IRequest<Guid>;
