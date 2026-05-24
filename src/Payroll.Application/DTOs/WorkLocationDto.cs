namespace Payroll.Application.DTOs;

public record WorkLocationDto(
    Guid Id,
    string Name,
    string? AddressLine1,
    string? AddressLine2,
    string State,
    string? City,
    string? PinCode,
    string? PtRegistrationNumber,
    bool IsActive,
    int EmployeeCount
);
