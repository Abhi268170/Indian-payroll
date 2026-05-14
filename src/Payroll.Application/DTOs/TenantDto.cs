namespace Payroll.Application.DTOs;

public record TenantDto(
    Guid Id,
    string DisplayName,
    string Slug,
    bool IsActive,
    DateTimeOffset CreatedAt);
