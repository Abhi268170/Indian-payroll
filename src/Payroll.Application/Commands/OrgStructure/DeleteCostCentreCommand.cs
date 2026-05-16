using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record DeleteCostCentreCommand(Guid Id, Guid ActorId) : IRequest;
