using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class SalaryStructureTemplate : AuditableEntity
{
    private SalaryStructureTemplate() { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid TenantId { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Template-level statutory defaults. At hire time these pre-fill the employee's
    // EpfEnabled / EsiEnabled / PtEnabled / LwfEnabled flags. Operator can still
    // override per employee — engine reads employee flags, not template flags.
    // Precedence: template default → employee override → payroll snapshot immutable.
    // All default true so existing templates + tenants see no behavioural change
    // after migration backfill.
    public bool EpfEnabled { get; private set; } = true;
    public bool EsiEnabled { get; private set; } = true;
    public bool PtEnabled { get; private set; } = true;
    public bool LwfEnabled { get; private set; } = true;

    private readonly List<SalaryStructureComponent> _components = [];
    public IReadOnlyList<SalaryStructureComponent> Components => _components.AsReadOnly();

    public static SalaryStructureTemplate Create(
        string name,
        string? description,
        Guid tenantId,
        Guid createdBy,
        bool epfEnabled = true,
        bool esiEnabled = true,
        bool ptEnabled = true,
        bool lwfEnabled = true) => new()
        {
            Name = name,
            Description = description,
            TenantId = tenantId,
            CreatedBy = createdBy,
            EpfEnabled = epfEnabled,
            EsiEnabled = esiEnabled,
            PtEnabled = ptEnabled,
            LwfEnabled = lwfEnabled,
        };

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void UpdateStatutoryDefaults(bool epfEnabled, bool esiEnabled, bool ptEnabled, bool lwfEnabled)
    {
        EpfEnabled = epfEnabled;
        EsiEnabled = esiEnabled;
        PtEnabled = ptEnabled;
        LwfEnabled = lwfEnabled;
    }

    public void SetComponents(IEnumerable<SalaryStructureComponent> components)
    {
        _components.Clear();
        _components.AddRange(components);
    }

    public void SetActive(bool active) => IsActive = active;
}
