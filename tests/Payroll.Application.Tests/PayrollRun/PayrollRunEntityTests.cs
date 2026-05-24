using FluentAssertions;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.ValueObjects;
using Xunit;

namespace Payroll.Application.Tests.PayrollRun;

public class PayrollRunEntityTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly PayPeriod Period = new(2025, 5);

    private static Domain.Entities.PayrollRun CreateDraft() =>
        Domain.Entities.PayrollRun.Create(
            TenantId, Period, PayrollRunType.Regular,
            new DateOnly(2025, 5, 31), null, 10, ActorId);

    // ── Approve ───────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_OnDraft_SetsApprovedStatus()
    {
        var run = CreateDraft();
        run.Approve(ActorId);
        run.Status.Should().Be(PayrollRunStatus.Approved);
        run.ApprovedAt.Should().NotBeNull();
        run.ApprovedBy.Should().Be(ActorId);
    }

    [Fact]
    public void Approve_OnPaid_Throws()
    {
        var run = CreateDraft();
        run.Approve(ActorId);
        run.RecordPayment(new DateOnly(2025, 5, 31), "BankTransfer", null, ActorId);

        Action act = () => run.Approve(ActorId);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── RejectApproval ────────────────────────────────────────────────────────

    [Fact]
    public void RejectApproval_OnApproved_RevertsToDraft()
    {
        var run = CreateDraft();
        run.Approve(ActorId);
        run.RejectApproval("Salary mismatch", ActorId);

        run.Status.Should().Be(PayrollRunStatus.Draft);
        run.ApprovalRejectionReason.Should().Be("Salary mismatch");
    }

    [Fact]
    public void RejectApproval_WithNullReason_Accepted()
    {
        var run = CreateDraft();
        run.Approve(ActorId);
        run.RejectApproval(null, ActorId);
        run.Status.Should().Be(PayrollRunStatus.Draft);
    }

    [Fact]
    public void RejectApproval_OnDraft_Throws()
    {
        var run = CreateDraft();
        Action act = () => run.RejectApproval("reason", ActorId);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── RecordPayment ─────────────────────────────────────────────────────────

    [Fact]
    public void RecordPayment_OnApproved_SetsPaidStatus()
    {
        var run = CreateDraft();
        run.Approve(ActorId);
        var payDate = new DateOnly(2025, 5, 31);
        run.RecordPayment(payDate, "BankTransfer", "REF001", ActorId);

        run.Status.Should().Be(PayrollRunStatus.Paid);
        run.PaymentDate.Should().Be(payDate);
        run.PaymentMode.Should().Be("BankTransfer");
        run.PaymentReference.Should().Be("REF001");
        run.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordPayment_OnDraft_Throws()
    {
        var run = CreateDraft();
        Action act = () => run.RecordPayment(new DateOnly(2025, 5, 31), "BankTransfer", null, ActorId);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── DeletePayment ─────────────────────────────────────────────────────────

    [Fact]
    public void DeletePayment_OnPaid_RevertsToApproved()
    {
        var run = CreateDraft();
        run.Approve(ActorId);
        run.RecordPayment(new DateOnly(2025, 5, 31), "BankTransfer", "REF001", ActorId);
        run.DeletePayment(ActorId);

        run.Status.Should().Be(PayrollRunStatus.Approved);
        run.PaymentDate.Should().BeNull();
        run.PaymentMode.Should().BeNull();
        run.PaidAt.Should().BeNull();
    }

    [Fact]
    public void DeletePayment_OnDraft_Throws()
    {
        var run = CreateDraft();
        Action act = () => run.DeletePayment(ActorId);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_OnDraft_SetsDeletedStatus()
    {
        var run = CreateDraft();
        run.Delete(ActorId);
        run.Status.Should().Be(PayrollRunStatus.Deleted);
    }

    [Fact]
    public void Delete_OnApproved_Throws()
    {
        var run = CreateDraft();
        run.Approve(ActorId);
        Action act = () => run.Delete(ActorId);
        act.Should().Throw<InvalidOperationException>();
    }
}
