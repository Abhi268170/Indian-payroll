using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class SalaryStructureTemplate : AuditableEntity
{
    private SalaryStructureTemplate() { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid TenantId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<SalaryStructureComponent> _components = [];
    public IReadOnlyList<SalaryStructureComponent> Components => _components.AsReadOnly();

    public static SalaryStructureTemplate Create(
        string name,
        string? description,
        Guid tenantId,
        Guid createdBy) => new()
        {
            Name = name,
            Description = description,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void SetComponents(IEnumerable<SalaryStructureComponent> components)
    {
        _components.Clear();
        _components.AddRange(components);
    }

    public void SetActive(bool active) => IsActive = active;
}
