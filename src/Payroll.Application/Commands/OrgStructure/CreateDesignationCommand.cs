using MediatR;

namespace Payroll.Application.Commands.OrgStructure;

public record CreateDesignationCommand(string Name, Guid ActorId) : IRequest<Guid>;
