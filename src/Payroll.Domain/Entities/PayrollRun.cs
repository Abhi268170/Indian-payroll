using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.ValueObjects;

namespace Payroll.Domain.Entities;

public sealed class PayrollRun : AuditableEntity
{
    private PayrollRun() { }

    public Guid TenantId { get; private set; }
    public PayPeriod PayPeriod { get; private set; } = default!;
    public PayrollRunStatus Status { get; private set; }
    public string? VariableInputsFileKey { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public string? UnlockReason { get; private set; }
    public int EmployeeCount { get; private set; }

    public static PayrollRun Create(Guid tenantId, PayPeriod payPeriod, Guid createdBy) =>
        new()
        {
            TenantId = tenantId,
            PayPeriod = payPeriod,
            Status = PayrollRunStatus.Pending,
            CreatedBy = createdBy
        };

    public void MarkProcessing()
    {
        Status = PayrollRunStatus.Processing;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDraft(int employeeCount)
    {
        Status = PayrollRunStatus.Draft;
        EmployeeCount = employeeCount;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFinalised() => Status = PayrollRunStatus.Finalised;

    public void MarkFailed(string reason)
    {
        Status = PayrollRunStatus.Failed;
        FailureReason = reason;
    }

    public void Unlock(string reason, Guid unlockedBy)
    {
        Status = PayrollRunStatus.Draft;
        UnlockReason = reason;
        SetUpdated(unlockedBy);
    }
}
