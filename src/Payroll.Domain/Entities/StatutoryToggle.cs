using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class StatutoryToggle : AuditableEntity
{
    private StatutoryToggle() { }

    public Guid TenantId { get; private set; }
    public StatutoryModule Module { get; private set; }
    public bool IsEnabled { get; private set; }

    public static StatutoryToggle Create(Guid tenantId, StatutoryModule module, bool isEnabled, Guid createdBy) =>
        new() { TenantId = tenantId, Module = module, IsEnabled = isEnabled, CreatedBy = createdBy };

    public void Toggle(bool enabled, Guid updatedBy)
    {
        IsEnabled = enabled;
        SetUpdated(updatedBy);
    }
}
