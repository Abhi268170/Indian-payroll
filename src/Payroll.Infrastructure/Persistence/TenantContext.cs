using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

internal sealed class TenantContext : ITenantContext
{
    private TenantInfo? _tenant;

    public Guid TenantId => _tenant?.Id ?? throw new InvalidOperationException("Tenant not resolved.");
    public string Schema => _tenant?.Schema ?? throw new InvalidOperationException("Tenant not resolved.");
    public string Slug => _tenant?.Slug ?? throw new InvalidOperationException("Tenant not resolved.");
    public bool IsResolved => _tenant is not null;

    public void SetTenant(TenantInfo tenant) => _tenant = tenant;
}
