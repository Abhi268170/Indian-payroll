namespace Payroll.Application.DTOs;

public record BusinessUnitDto(
    Guid Id,
    string Name,
    string? Description);
