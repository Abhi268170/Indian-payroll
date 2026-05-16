using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class BusinessUnit : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private BusinessUnit() { }

    public static BusinessUnit Create(string name, string? description, Guid createdBy)
    {
        return new BusinessUnit
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedBy = createdBy,
        };
    }

    public void Update(string name, string? description, Guid updatedBy)
    {
        Name = name;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SetUpdated(updatedBy);
    }
}
