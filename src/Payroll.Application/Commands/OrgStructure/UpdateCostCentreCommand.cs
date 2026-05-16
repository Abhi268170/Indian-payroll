using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record UpdateCostCentreCommand(Guid Id, string Name, string? Code, Guid ActorId) : IRequest;
