namespace Payroll.Application.DTOs;

public record JobProgressDto(
    string JobId,
    string Status,
    int Processed,
    int Total,
    string? ResultJson,
    string? Error);
