namespace Payroll.Application.DTOs;

public record TaxDetailsDto(
    string? Pan,
    string? Tan,
    string? AoAreaCode,
    string? AoType,
    string? AoRangeCode,
    string? AoNumber,
    string? DeductorType,
    string? DeductorName,
    string? DeductorFathersName,
    string? DeductorDesignation
);
