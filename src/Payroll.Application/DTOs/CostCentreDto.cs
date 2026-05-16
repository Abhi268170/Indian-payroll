namespace Payroll.Application.DTOs;

public record CostCentreDto(
    Guid Id,
    string Name,
    string? Code);
