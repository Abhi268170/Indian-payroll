using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class CostCentre : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }

    private CostCentre() { }

    public static CostCentre Create(string name, string? code, Guid createdBy)
    {
        return new CostCentre
        {
            Name = name,
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            CreatedBy = createdBy,
        };
    }

    public void Update(string name, string? code, Guid updatedBy)
    {
        Name = name;
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        SetUpdated(updatedBy);
    }
}
