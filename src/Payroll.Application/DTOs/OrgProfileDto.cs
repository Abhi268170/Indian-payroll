namespace Payroll.Application.DTOs;

public record OrgProfileDto(
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
    bool HasLogo
);
