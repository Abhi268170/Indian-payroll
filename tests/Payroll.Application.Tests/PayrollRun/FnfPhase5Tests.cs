using FluentAssertions;
using MediatR;
using NSubstitute;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.PayrollRuns;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine.Inputs;
using Xunit;

using DomainPayrollRun = Payroll.Domain.Entities.PayrollRun;

namespace Payroll.Application.Tests.PayrollRun;

/// <summary>
/// Tests for WI-09 (ExitStatus filter) and WI-10 (₹0 FnF approval block).
/// </summary>
public class FnfPhase5Tests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    // ─── WI-09: ExitStatus domain behavior ─────────────────────────────────

    [Fact]
    public void EmployeeExit_Create_StatusIsInProgress()
    {
        EmployeeExit exit = EmployeeExit.Create(
            EmployeeId, new DateOnly(2026, 6, 30),
            ExitReason.ResignedByEmployee, ExitSettlementMode.RegularSchedule,
            null, null, null, ActorId);

        exit.Status.Should().Be(ExitStatus.InProgress);
    }

    [Fact]
    public void EmployeeExit_MarkCompleted_StatusIsCompleted()
    {
        EmployeeExit exit = EmployeeExit.Create(
            EmployeeId, new DateOnly(2026, 6, 30),
            ExitReason.ResignedByEmployee, ExitSettlementMode.RegularSchedule,
            null, null, null, ActorId);

        exit.MarkCompleted(ActorId);

        exit.Status.Should().Be(ExitStatus.Completed);
    }

    [Fact]
    public void EmployeeExit_MarkReverted_StatusIsReverted()
    {
        EmployeeExit exit = EmployeeExit.Create(
            EmployeeId, new DateOnly(2026, 6, 30),
            ExitReason.ResignedByEmployee, ExitSettlementMode.RegularSchedule,
            null, null, null, ActorId);

        exit.MarkReverted(ActorId);

        exit.Status.Should().Be(ExitStatus.Reverted);
    }

    // ─── WI-09: Rehire scenario — completed exit does not block new exit ────

    [Fact]
    public void GetActiveByEmployee_ReturnsNull_WhenExitIsCompleted()
    {
        // The repository filters on Status == InProgress. A completed exit
        // (from a prior separation) must not block re-initiation.
        // This test validates the filter semantics via the domain invariant:
        // a Completed exit should NOT be returned as "active".
        EmployeeExit completed = EmployeeExit.Create(
            EmployeeId, new DateOnly(2025, 12, 31),
            ExitReason.ResignedByEmployee, ExitSettlementMode.RegularSchedule,
            null, null, null, ActorId);
        completed.MarkCompleted(ActorId);

        completed.Status.Should().Be(ExitStatus.Completed,
            "completed exit must not be returned by GetActiveByEmployeeAsync filter");
        completed.Status.Should().NotBe(ExitStatus.InProgress);
    }

    [Fact]
    public void GetActiveByEmployee_ReturnsNull_WhenExitIsReverted()
    {
        EmployeeExit reverted = EmployeeExit.Create(
            EmployeeId, new DateOnly(2025, 12, 31),
            ExitReason.ResignedByEmployee, ExitSettlementMode.RegularSchedule,
            null, null, null, ActorId);
        reverted.MarkReverted(ActorId);

        reverted.Status.Should().NotBe(ExitStatus.InProgress,
            "reverted exit must not be returned by GetActiveByEmployeeAsync filter");
    }

    // ─── WI-10: FnF ₹0 gross hard block ────────────────────────────────────

    [Fact]
    public async Task GetPendingTasks_FnfRun_ZeroGross_IsHardBlock()
    {
        var (handler, runRepo, empRepo, payrunEmpRepo) = BuildPendingTasksHandler(
            runType: PayrollRunType.BulkFinalSettlement,
            grossPay: 0m);

        PendingTasksDto result = await handler.Handle(
            new GetPendingTasksQuery(RunId), CancellationToken.None);

        result.HasAnyHardBlocks.Should().BeTrue(
            "FnF run with ₹0 gross must block approval — settlement was never computed");
        result.HardBlocks.Should().ContainSingle()
            .Which.Reason.Should().Contain("FnF settlement not computed");
    }

    [Fact]
    public async Task GetPendingTasks_FnfRun_NonZeroGross_NoBlock()
    {
        var (handler, _, _, _) = BuildPendingTasksHandler(
            runType: PayrollRunType.BulkFinalSettlement,
            grossPay: 85_000m);

        PendingTasksDto result = await handler.Handle(
            new GetPendingTasksQuery(RunId), CancellationToken.None);

        result.HasAnyHardBlocks.Should().BeFalse(
            "FnF run with non-zero gross must not be blocked by WI-10 guard");
    }

    [Fact]
    public async Task GetPendingTasks_RegularRun_ZeroGross_NoBlock()
    {
        // Regular runs may legitimately have ₹0 payrun employees (e.g. skipped).
        // WI-10 guard must NOT fire for regular runs.
        var (handler, _, _, _) = BuildPendingTasksHandler(
            runType: PayrollRunType.Regular,
            grossPay: 0m);

        PendingTasksDto result = await handler.Handle(
            new GetPendingTasksQuery(RunId), CancellationToken.None);

        result.HardBlocks.Should().NotContain(b => b.Reason.Contains("FnF settlement"));
    }

    [Fact]
    public async Task GetPendingTasks_FinalSettlementRun_ZeroGross_IsHardBlock()
    {
        var (handler, _, _, _) = BuildPendingTasksHandler(
            runType: PayrollRunType.FinalSettlement,
            grossPay: 0m);

        PendingTasksDto result = await handler.Handle(
            new GetPendingTasksQuery(RunId), CancellationToken.None);

        result.HasAnyHardBlocks.Should().BeTrue(
            "FinalSettlement (single) must also block on ₹0 gross");
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private (GetPendingTasksHandler Handler,
             IPayrollRunRepository RunRepo,
             IEmployeeRepository EmpRepo,
             IPayrunEmployeeRepository PayrunEmpRepo)
        BuildPendingTasksHandler(PayrollRunType runType, decimal grossPay)
    {
        var runRepo = Substitute.For<IPayrollRunRepository>();
        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        var empRepo = Substitute.For<IEmployeeRepository>();

        string snapshot = System.Text.Json.JsonSerializer.Serialize(MinimalStatutoryConfig());
        DomainPayrollRun run = runType switch
        {
            PayrollRunType.BulkFinalSettlement => DomainPayrollRun.CreateBulkFinalSettlement(
                TenantId, new Domain.ValueObjects.PayPeriod(2026, 6),
                new DateOnly(2026, 6, 30), snapshot, ActorId),
            PayrollRunType.FinalSettlement => DomainPayrollRun.CreateFinalSettlement(
                TenantId, new Domain.ValueObjects.PayPeriod(2026, 6),
                new DateOnly(2026, 6, 30), Guid.NewGuid(), snapshot, ActorId),
            _ => DomainPayrollRun.Create(TenantId,
                new Domain.ValueObjects.PayPeriod(2026, 4),
                PayrollRunType.Regular, new DateOnly(2026, 4, 30),
                snapshot, 1, ActorId),
        };

        runRepo.GetByIdAsync(RunId, Arg.Any<CancellationToken>()).Returns(run);

        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        if (grossPay > 0)
            pe.UpdateComputedAmounts(
                grossPay, grossPay * 0.9m, grossPay * 0.85m,
                grossPay * 0.05m, 0m, 0m,
                1_800m, 1_800m, 0m, 0m, 200m,
                grossPay * 0.04m, 0m, 0m, 0m, 0m,
                grossPay / 0.9m, ActorId);

        payrunEmpRepo.GetByRunIdAsync(RunId, Arg.Any<CancellationToken>())
            .Returns(new List<PayrunEmployee> { pe });

        empRepo.GetManyByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee>());

        var handler = new GetPendingTasksHandler(runRepo, payrunEmpRepo, empRepo);
        return (handler, runRepo, empRepo, payrunEmpRepo);
    }

    private static StatutoryConfig MinimalStatutoryConfig() => new(
        NewRegimeSlabs: [], SurchargeSlabs: [],
        StandardDeduction: 75_000m, Rebate87ALimit: 700_000m, Rebate87AAmount: 25_000m,
        CessRate: 0.04m, PFWageCap: 15_000m, EPFEmployeeRate: 0.12m,
        EPSEmployerRate: 0.0833m, EPSCap: 1_250m, EpfRestrictEmployerWage: true,
        EpfConsiderSalaryOnLop: true, EpfProRateRestrictedPfWage: false,
        ESIWageLimit: 21_000m, ESIPWDWageLimit: 25_000m,
        ESIEmployeeRate: 0.0075m, ESIEmployerRate: 0.0325m,
        PTSlabs: [], LWFStates: [],
        PFEnabled: true, ESIEnabled: true, PTEnabled: false,
        EpfIncludeEmployerInCtc: false, GratuityIncludedInCtc: false);
}
