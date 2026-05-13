namespace Payroll.Domain.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    string Schema { get; }
    string Slug { get; }
    bool IsResolved { get; }
    void SetTenant(TenantInfo tenant);
}

public sealed record TenantInfo(
    Guid Id,
    string Schema,
    string Slug,
    bool IsActive);
