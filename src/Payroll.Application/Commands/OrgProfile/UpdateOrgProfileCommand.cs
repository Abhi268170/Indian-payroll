using MediatR;

namespace Payroll.Application.Commands.OrgProfile;

public record UpdateOrgProfileCommand(
    string CompanyName,
    string? LegalName,
    string? Pan,
    string? Gstin,
    string? Website,
    string? Industry,
    DateOnly? IncorporationDate,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PinCode,
    Guid? FilingAddressWorkLocationId,
    Guid ActorId
) : IRequest;
