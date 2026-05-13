using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class CostCentre : AuditableEntity
{
    private CostCentre() { }

    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public Guid TenantId { get; private set; }

    public static CostCentre Create(string name, Guid tenantId, Guid createdBy, string? code = null) =>
        new() { Name = name, TenantId = tenantId, Code = code, CreatedBy = createdBy };
}
