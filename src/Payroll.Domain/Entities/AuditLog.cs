namespace Payroll.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid PerformedBy { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; } = DateTimeOffset.UtcNow;
    public string? IpAddress { get; private set; }

    public static AuditLog Create(
        Guid tenantId,
        string action,
        string entityType,
        Guid entityId,
        Guid performedBy,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null) => new()
        {
            TenantId = tenantId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PerformedBy = performedBy,
            OldValue = oldValue,
            NewValue = newValue,
            IpAddress = ipAddress
        };
}
