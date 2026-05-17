using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class PayrollRunAuditLog : AuditableEntity
{
    private PayrollRunAuditLog() { }

    public Guid PayrollRunId { get; private set; }
    public Guid TenantId { get; private set; }
    public PayrollRunStatus FromStatus { get; private set; }
    public PayrollRunStatus ToStatus { get; private set; }
    public Guid ActorUserId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string? Reason { get; private set; }

    public static PayrollRunAuditLog Create(
        Guid payrollRunId,
        Guid tenantId,
        PayrollRunStatus fromStatus,
        PayrollRunStatus toStatus,
        Guid actorUserId,
        string? reason) =>
        new()
        {
            PayrollRunId = payrollRunId,
            TenantId = tenantId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            Timestamp = DateTimeOffset.UtcNow,
            Reason = reason,
            CreatedBy = actorUserId
        };
}
