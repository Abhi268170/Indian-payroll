using MediatR;

namespace Payroll.Application.Commands.WorkLocations;

public record DeleteWorkLocationCommand(Guid Id, Guid ActorId) : IRequest<Unit>;
