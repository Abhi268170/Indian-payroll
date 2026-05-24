using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class Department : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public string? Description { get; private set; }

    private Department() { }

    public static Department Create(string name, string? code, string? description, Guid createdBy)
    {
        return new Department
        {
            Name = name,
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedBy = createdBy,
        };
    }

    public void Update(string name, string? code, string? description, Guid updatedBy)
    {
        Name = name;
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SetUpdated(updatedBy);
    }
}
