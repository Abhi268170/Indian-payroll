using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class Designation : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;

    private Designation() { }

    public static Designation Create(string name, Guid createdBy)
    {
        return new Designation { Name = name, CreatedBy = createdBy };
    }

    public void Update(string name, Guid updatedBy)
    {
        Name = name;
        SetUpdated(updatedBy);
    }
}
