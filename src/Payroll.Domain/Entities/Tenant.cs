namespace Payroll.Domain.Entities;

public sealed class Tenant
{
    private Tenant() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string DisplayName { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Schema { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static Tenant Create(string displayName, string slug) => new()
    {
        DisplayName = displayName,
        Slug = slug.ToLowerInvariant().Replace(" ", "-"),
        Schema = $"tenant_{slug.ToLowerInvariant().Replace("-", "_")}",
        IsActive = true
    };

    public void Suspend() => IsActive = false;
    public void Activate() => IsActive = true;
}
