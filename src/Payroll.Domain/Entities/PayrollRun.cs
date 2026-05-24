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
    public PayrollRunType Type { get; private set; }

    // Financial summary (populated at initiation, updated on each LOP/variable-input change)
    public decimal PayrollCost { get; private set; }
    public decimal TotalNetPay { get; private set; }
    public decimal TotalEmployerPf { get; private set; }
    public decimal TotalEmployerEsi { get; private set; }
    public decimal TotalTds { get; private set; }
    public decimal TotalPt { get; private set; }
    public int EmployeeCount { get; private set; }

    // Pay day
    public DateOnly? PayDay { get; private set; }

    // Statutory config snapshot (JSON) — captured at initiation, read for all subsequent engine calls
    public string? StatutoryConfigSnapshot { get; private set; }

    // Variable inputs file
    public string? VariableInputsFileKey { get; private set; }

    // Approval
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public string? ApprovalRejectionReason { get; private set; }

    // Payment
    public DateOnly? PaymentDate { get; private set; }
    public string? PaymentMode { get; private set; }
    public string? PaymentReference { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? BankAdviceFileKey { get; private set; }

    // Legacy/failure tracking
    public string? FailureReason { get; private set; }

    public static PayrollRun Create(
        Guid tenantId,
        PayPeriod payPeriod,
        PayrollRunType type,
        DateOnly? payDay,
        string? statutoryConfigSnapshot,
        int employeeCount,
        Guid createdBy) =>
        new()
        {
            TenantId = tenantId,
            PayPeriod = payPeriod,
            Type = type,
            Status = PayrollRunStatus.Draft,
            PayDay = payDay,
            StatutoryConfigSnapshot = statutoryConfigSnapshot,
            EmployeeCount = employeeCount,
            CreatedBy = createdBy
        };

    public void UpdateFinancialSummary(
        decimal payrollCost,
        decimal totalNetPay,
        decimal totalEmployerPf,
        decimal totalEmployerEsi,
        decimal totalTds,
        decimal totalPt,
        int employeeCount,
        Guid actorId)
    {
        PayrollCost = payrollCost;
        TotalNetPay = totalNetPay;
        TotalEmployerPf = totalEmployerPf;
        TotalEmployerEsi = totalEmployerEsi;
        TotalTds = totalTds;
        TotalPt = totalPt;
        EmployeeCount = employeeCount;
        SetUpdated(actorId);
    }

    public void Approve(Guid actorId)
    {
        if (Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException($"Cannot approve a payroll run in {Status} status.");

        Status = PayrollRunStatus.Approved;
        ApprovedAt = DateTimeOffset.UtcNow;
        ApprovedBy = actorId;
        ApprovalRejectionReason = null;
        SetUpdated(actorId);
    }

    public void RejectApproval(string? reason, Guid actorId)
    {
        if (Status != PayrollRunStatus.Approved)
            throw new InvalidOperationException($"Cannot reject approval of a payroll run in {Status} status.");

        Status = PayrollRunStatus.Draft;
        ApprovalRejectionReason = reason;
        SetUpdated(actorId);
    }

    public void RecordPayment(DateOnly paymentDate, string paymentMode, string? paymentReference, Guid actorId)
    {
        if (Status != PayrollRunStatus.Approved)
            throw new InvalidOperationException($"Cannot record payment for a payroll run in {Status} status.");

        Status = PayrollRunStatus.Paid;
        PaymentDate = paymentDate;
        PaymentMode = paymentMode;
        PaymentReference = paymentReference;
        PaidAt = DateTimeOffset.UtcNow;
        SetUpdated(actorId);
    }

    public void DeletePayment(Guid actorId)
    {
        if (Status != PayrollRunStatus.Paid)
            throw new InvalidOperationException($"Cannot delete payment record for a payroll run in {Status} status.");

        Status = PayrollRunStatus.Approved;
        PaymentDate = null;
        PaymentMode = null;
        PaymentReference = null;
        PaidAt = null;
        SetUpdated(actorId);
    }

    public void Delete(Guid actorId)
    {
        if (Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException($"Cannot delete a payroll run in {Status} status. Only Draft runs can be deleted.");

        Status = PayrollRunStatus.Deleted;
        SoftDelete(actorId);
    }

    public void MarkFailed(string reason)
    {
        Status = PayrollRunStatus.Failed;
        FailureReason = reason;
    }
}
