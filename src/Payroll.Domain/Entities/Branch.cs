using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class Branch : AuditableEntity
{
    private Branch() { }

    public string Name { get; private set; } = string.Empty;
    public IndianState State { get; private set; }
    public Guid TenantId { get; private set; }

    public static Branch Create(string name, IndianState state, Guid tenantId, Guid createdBy) =>
        new() { Name = name, State = state, TenantId = tenantId, CreatedBy = createdBy };
}
