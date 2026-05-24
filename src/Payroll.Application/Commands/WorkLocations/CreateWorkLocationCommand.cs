using MediatR;

namespace Payroll.Application.Commands.WorkLocations;

public record CreateWorkLocationCommand(
    string Name,
    string State,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PinCode,
    Guid ActorId) : IRequest<Guid>;
