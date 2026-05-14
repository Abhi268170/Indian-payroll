using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record CreateCostCentreCommand(string Name, string? Code, Guid ActorId) : IRequest<Guid>;
