using FluentAssertions;
using NSubstitute;
using Payroll.Application.Commands.Employees;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;
using System.Reflection;
using Xunit;

using DomainPayrollRun = Payroll.Domain.Entities.PayrollRun;

namespace Payroll.Application.Tests.Employees;

/// <summary>Tests for WI-14 CancelExitCommand.</summary>
public class CancelExitTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid FnfRunId = Guid.NewGuid();

    private sealed class Mocks
    {
        public IEmployeeRepository EmployeeRepo = Substitute.For<IEmployeeRepository>();
        public IEmployeeExitRepository ExitRepo = Substitute.For<IEmployeeExitRepository>();
        public IPayrollRunRepository RunRepo = Substitute.For<IPayrollRunRepository>();
        public IPayrunEmployeeRepository PayrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        public IPayrunComponentBreakdownRepository BreakdownRepo = Substitute.For<IPayrunComponentBreakdownRepository>();
        public ITdsWorksheetRepository TdsWorksheetRepo = Substitute.For<ITdsWorksheetRepository>();
        public IUnitOfWork Uow = Substitute.For<IUnitOfWork>();

        public CancelExitHandler Build() => new(
            EmployeeRepo, ExitRepo, RunRepo, PayrunEmpRepo, BreakdownRepo, TdsWorksheetRepo, Uow);
    }

    private static Employee MakeExitingEmployee()
    {
        Employee e = Employee.CreateStep1(
            firstName: "Test", middleName: null, lastName: "Emp",
            employeeCode: "EMP001", workEmail: "t@acme.com", mobileNumber: null,
            gender: Gender.Male, dateOfJoining: new DateOnly(2020, 1, 1),
            employmentType: EmploymentType.FullTime, isDirector: false,
            enablePortalAccess: false, tenantId: TenantId,
            departmentId: Guid.NewGuid(), designationId: Guid.NewGuid(),
            workLocationId: Guid.NewGuid(), businessUnitId: null,
            dateOfBirth: new DateOnly(1990, 1, 1), createdBy: ActorId);
        PropertyInfo idProp = typeof(Employee).GetProperty("Id")
            ?? typeof(Employee).BaseType!.GetProperty("Id")!;
        idProp.SetValue(e, EmployeeId);
        e.ScheduleExit(new DateOnly(2026, 6, 30), ActorId); // sets DateOfLeaving
        return e;
    }

    private static EmployeeExit MakeInProgressExit()
    {
        EmployeeExit exit = EmployeeExit.Create(
            EmployeeId, new DateOnly(2026, 6, 30),
            ExitReason.ResignedByEmployee, ExitSettlementMode.CustomDate,
            new DateOnly(2026, 6, 30), null, null, ActorId);
        exit.LinkFnfRun(FnfRunId, ActorId);
        return exit;
    }

    private static DomainPayrollRun MakeFnfRun(PayrollRunType type, int employeeCount, PayrollRunStatus status = PayrollRunStatus.Draft)
    {
        DomainPayrollRun run = type == PayrollRunType.BulkFinalSettlement
            ? DomainPayrollRun.CreateBulkFinalSettlement(TenantId, new PayPeriod(2026, 6), new DateOnly(2026, 6, 30), "{}", ActorId)
            : DomainPayrollRun.CreateFinalSettlement(TenantId, new PayPeriod(2026, 6), new DateOnly(2026, 6, 30), Guid.NewGuid(), "{}", ActorId);
        run.SetEmployeeCount(employeeCount, ActorId);
        PropertyInfo idProp = typeof(DomainPayrollRun).GetProperty("Id")
            ?? typeof(DomainPayrollRun).BaseType!.GetProperty("Id")!;
        idProp.SetValue(run, FnfRunId);
        if (status == PayrollRunStatus.Approved) run.Approve(ActorId);
        return run;
    }

    private void WireCommon(Mocks m, Employee emp, EmployeeExit exit, DomainPayrollRun? run,
        bool draftRegularExists = false)
    {
        m.EmployeeRepo.GetByIdAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(emp);
        m.ExitRepo.GetActiveByEmployeeAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(exit);
        m.RunRepo.FindDraftRegularRunsCoveringDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(draftRegularExists
                ? new List<DomainPayrollRun> { MakeFnfRun(PayrollRunType.Regular, 1) }
                : new List<DomainPayrollRun>());
        if (run != null)
            m.RunRepo.GetByIdAsync(FnfRunId, Arg.Any<CancellationToken>()).Returns(run);
        m.PayrunEmpRepo.GetByRunAndEmployeeAsync(FnfRunId, EmployeeId, Arg.Any<CancellationToken>())
            .Returns(PayrunEmployee.Create(FnfRunId, EmployeeId, TenantId, 30, ActorId));
    }

    // ─── No exit in progress ─────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_NoActiveExit_Throws()
    {
        var m = new Mocks();
        m.EmployeeRepo.GetByIdAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(MakeExitingEmployee());
        m.ExitRepo.GetActiveByEmployeeAsync(EmployeeId, Arg.Any<CancellationToken>())
            .Returns((EmployeeExit?)null);

        Func<Task> act = () => m.Build().Handle(new CancelExitCommand(EmployeeId, ActorId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*No exit is in progress*");
    }

    // ─── Common case: single FinalSettlement run deleted, employee reverted ──

    [Fact]
    public async Task Cancel_SingleRun_RevertsEmployee_DeletesRun_MarksReverted()
    {
        var m = new Mocks();
        Employee emp = MakeExitingEmployee();
        EmployeeExit exit = MakeInProgressExit();
        DomainPayrollRun run = MakeFnfRun(PayrollRunType.FinalSettlement, employeeCount: 1);
        WireCommon(m, emp, exit, run);

        await m.Build().Handle(new CancelExitCommand(EmployeeId, ActorId), CancellationToken.None);

        emp.Status.Should().Be(EmployeeStatus.Active);
        emp.DateOfLeaving.Should().BeNull();
        exit.Status.Should().Be(ExitStatus.Reverted);
        run.Status.Should().Be(PayrollRunStatus.Deleted, "single FnF run must be soft-deleted on cancel");
    }

    [Fact]
    public async Task Cancel_SingleRun_RemovesPayrunEmployeeAndBreakdownsAndWorksheet()
    {
        var m = new Mocks();
        WireCommon(m, MakeExitingEmployee(), MakeInProgressExit(),
            MakeFnfRun(PayrollRunType.FinalSettlement, 1));

        await m.Build().Handle(new CancelExitCommand(EmployeeId, ActorId), CancellationToken.None);

        m.PayrunEmpRepo.Received().Remove(Arg.Any<PayrunEmployee>());
        await m.BreakdownRepo.Received().RemoveRangeByRunAndEmployeeAsync(FnfRunId, EmployeeId, Arg.Any<CancellationToken>());
        await m.TdsWorksheetRepo.Received().DeleteByRunAndEmployeeAsync(FnfRunId, EmployeeId, Arg.Any<CancellationToken>());
    }

    // ─── Bulk run with others: decrement, keep run ───────────────────────────

    [Fact]
    public async Task Cancel_BulkRunWithOthers_DecrementsCount_KeepsRun()
    {
        var m = new Mocks();
        DomainPayrollRun run = MakeFnfRun(PayrollRunType.BulkFinalSettlement, employeeCount: 2);
        WireCommon(m, MakeExitingEmployee(), MakeInProgressExit(), run);

        await m.Build().Handle(new CancelExitCommand(EmployeeId, ActorId), CancellationToken.None);

        run.EmployeeCount.Should().Be(1, "removing one of two employees decrements the count");
        run.Status.Should().Be(PayrollRunStatus.Draft, "bulk run with remaining employees must NOT be deleted");
        m.RunRepo.Received().Update(run);
    }

    [Fact]
    public async Task Cancel_BulkRunLastEmployee_DeletesRun()
    {
        var m = new Mocks();
        DomainPayrollRun run = MakeFnfRun(PayrollRunType.BulkFinalSettlement, employeeCount: 1);
        WireCommon(m, MakeExitingEmployee(), MakeInProgressExit(), run);

        await m.Build().Handle(new CancelExitCommand(EmployeeId, ActorId), CancellationToken.None);

        run.Status.Should().Be(PayrollRunStatus.Deleted,
            "bulk run with no remaining employees must be deleted");
    }

    // ─── Approved FnF run: cannot cancel ─────────────────────────────────────

    [Fact]
    public async Task Cancel_ApprovedRun_Throws()
    {
        var m = new Mocks();
        DomainPayrollRun run = MakeFnfRun(PayrollRunType.FinalSettlement, 1, PayrollRunStatus.Approved);
        WireCommon(m, MakeExitingEmployee(), MakeInProgressExit(), run);

        Func<Task> act = () => m.Build().Handle(new CancelExitCommand(EmployeeId, ActorId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already been approved*");
    }

    // ─── Stripped case: Draft regular run covers LWD month → block ───────────

    [Fact]
    public async Task Cancel_DraftRegularRunCoversLwd_Throws()
    {
        var m = new Mocks();
        WireCommon(m, MakeExitingEmployee(), MakeInProgressExit(),
            MakeFnfRun(PayrollRunType.FinalSettlement, 1), draftRegularExists: true);

        Func<Task> act = () => m.Build().Handle(new CancelExitCommand(EmployeeId, ActorId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*regular pay run*recompute*");
    }
}
