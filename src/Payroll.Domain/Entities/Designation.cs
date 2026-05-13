using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class Designation : AuditableEntity
{
    private Designation() { }

    public string Name { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }

    public static Designation Create(string name, Guid tenantId, Guid createdBy) =>
        new() { Name = name, TenantId = tenantId, CreatedBy = createdBy };
}
