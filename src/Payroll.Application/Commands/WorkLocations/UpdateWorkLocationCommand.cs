using MediatR;

namespace Payroll.Application.Commands.WorkLocations;

public record UpdateWorkLocationCommand(
    Guid Id,
    string Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PinCode,
    Guid ActorId) : IRequest<Unit>;
