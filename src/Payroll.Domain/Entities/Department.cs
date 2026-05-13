using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class Department : AuditableEntity
{
    private Department() { }

    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? ParentDepartmentId { get; private set; }

    public static Department Create(string name, Guid tenantId, Guid createdBy, string? code = null) =>
        new() { Name = name, TenantId = tenantId, Code = code, CreatedBy = createdBy };
}
