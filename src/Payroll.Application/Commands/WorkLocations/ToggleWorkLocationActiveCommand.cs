using MediatR;

namespace Payroll.Application.Commands.WorkLocations;

public record ToggleWorkLocationActiveCommand(Guid Id, bool Activate, Guid ActorId) : IRequest<Unit>;
