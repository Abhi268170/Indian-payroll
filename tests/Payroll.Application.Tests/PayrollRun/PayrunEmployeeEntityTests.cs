using FluentAssertions;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Xunit;

namespace Payroll.Application.Tests.PayrollRun;

public class PayrunEmployeeEntityTests
{
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private static PayrunEmployee CreateEmployee(int baseDays = 31) =>
        PayrunEmployee.Create(RunId, EmpId, TenantId, baseDays, ActorId);

    // ── SetLop ────────────────────────────────────────────────────────────────

    [Fact]
    public void SetLop_Valid_UpdatesLopAndPayableDays()
    {
        var emp = CreateEmployee(31);
        emp.SetLop(2, ActorId);
        emp.LopDays.Should().Be(2);
        emp.ActualPayableDays.Should().Be(29);
    }

    [Fact]
    public void SetLop_FullMonth_Throws()
    {
        var emp = CreateEmployee(31);
        Action act = () => emp.SetLop(31, ActorId);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetLop_Negative_Throws()
    {
        var emp = CreateEmployee(31);
        Action act = () => emp.SetLop(-1, ActorId);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── TDS override ──────────────────────────────────────────────────────────

    [Fact]
    public void SetTdsOverride_ValidReasonAndAmount_Sets()
    {
        var emp = CreateEmployee();
        emp.SetTdsOverride(5000m, "Employee request", ActorId);
        emp.TdsOverrideAmount.Should().Be(5000m);
        emp.TdsOverrideReason.Should().Be("Employee request");
    }

    [Fact]
    public void SetTdsOverride_NullReason_Throws()
    {
        var emp = CreateEmployee();
        Action act = () => emp.SetTdsOverride(5000m, null!, ActorId);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetTdsOverride_EmptyReason_Throws()
    {
        var emp = CreateEmployee();
        Action act = () => emp.SetTdsOverride(5000m, "   ", ActorId);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Skip / UndoSkip ───────────────────────────────────────────────────────

    [Fact]
    public void Skip_ValidReason_SetsSkippedStatus()
    {
        var emp = CreateEmployee();
        emp.Skip("No bank details", ActorId);
        emp.Status.Should().Be(PayrunEmployeeStatus.Skipped);
        emp.SkipReason.Should().Be("No bank details");
    }

    [Fact]
    public void Skip_NullReason_Throws()
    {
        var emp = CreateEmployee();
        Action act = () => emp.Skip(null!, ActorId);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Skip_AlreadySkipped_Throws()
    {
        var emp = CreateEmployee();
        emp.Skip("reason", ActorId);
        Action act = () => emp.Skip("reason", ActorId);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UndoSkip_OnSkipped_RestoresActive()
    {
        var emp = CreateEmployee();
        emp.Skip("reason", ActorId);
        emp.UndoSkip(ActorId);
        emp.Status.Should().Be(PayrunEmployeeStatus.Active);
        emp.SkipReason.Should().BeNull();
    }

    [Fact]
    public void UndoSkip_OnActive_Throws()
    {
        var emp = CreateEmployee();
        Action act = () => emp.UndoSkip(ActorId);
        act.Should().Throw<InvalidOperationException>();
    }
}
