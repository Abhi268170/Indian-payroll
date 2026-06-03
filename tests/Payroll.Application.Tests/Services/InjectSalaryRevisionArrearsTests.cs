using System.Reflection;
using FluentAssertions;
using NSubstitute;
using Payroll.Application.Commands.SalaryRevisions;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;
using Xunit;
using DomainPayrollRun = Payroll.Domain.Entities.PayrollRun;

namespace Payroll.Application.Tests.Services;

// WI-018 step 5 — arrear injection into a Draft payout run.
public class InjectSalaryRevisionArrearsTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid RunId = Guid.NewGuid();

    private static void SetId<T>(T entity, Guid id)
    {
        PropertyInfo prop = typeof(T).GetProperty("Id") ?? typeof(T).BaseType!.GetProperty("Id")!;
        prop.SetValue(entity, id);
    }

    // ── BuildArrearRows (pure) ──────────────────────────────────────────────────

    private static ArrearLine Line(Guid id, string code, decimal amount, bool taxable = true) =>
        new(id, code, code, amount, taxable, ConsiderForEpf: false, ConsiderForEsi: false);

    [Fact]
    public void BuildArrearRows_EmitsOneTimeRowsWithRealComponentId()
    {
        Guid basicId = Guid.NewGuid();
        List<PayrunComponentBreakdown> rows = InjectSalaryRevisionArrearsHandler.BuildArrearRows(
            RunId, EmployeeId, TenantId,
            [Line(basicId, "BASICSALARY", 5600m), Line(Guid.NewGuid(), "LTA", 1000m, taxable: false)]);

        rows.Should().HaveCount(2);
        PayrunComponentBreakdown basic = rows.Single(r => r.ComponentCode == "ARREAR_BASICSALARY");
        basic.SalaryComponentId.Should().Be(basicId);
        basic.ComponentName.Should().Be("Arrears - BASICSALARY");
        basic.ProratedAmount.Should().Be(5600m);
        basic.IsOneTimeEarning.Should().BeTrue();
        basic.IsTaxable.Should().BeTrue();
        basic.ConsiderForEpf.Should().BeFalse();
        basic.ConsiderForEsi.Should().BeFalse();
        rows.Single(r => r.ComponentCode == "ARREAR_LTA").IsTaxable.Should().BeFalse();
    }

    [Fact]
    public void BuildArrearRows_SkipsZeroAmount()
    {
        List<PayrunComponentBreakdown> rows = InjectSalaryRevisionArrearsHandler.BuildArrearRows(
            RunId, EmployeeId, TenantId, [Line(Guid.NewGuid(), "BASICSALARY", 0m)]);
        rows.Should().BeEmpty();
    }

    [Fact]
    public void BuildArrearRows_RemovedComponent_EmptyId_Throws()
    {
        Action act = () => InjectSalaryRevisionArrearsHandler.BuildArrearRows(
            RunId, EmployeeId, TenantId, [Line(Guid.Empty, "SPECIAL", -2000m)]);
        act.Should().Throw<DomainException>().WithMessage("*fully-removed component*");
    }

    // ── Handler guards / early-return ───────────────────────────────────────────

    private sealed class Mocks
    {
        public IPayrollRunRepository RunRepo = Substitute.For<IPayrollRunRepository>();
        public IPayrunEmployeeRepository PayrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        public IPayrunComponentBreakdownRepository BreakdownRepo = Substitute.For<IPayrunComponentBreakdownRepository>();
        public ISalaryRevisionRepository RevisionRepo = Substitute.For<ISalaryRevisionRepository>();
        public ISalaryArrearService ArrearService = Substitute.For<ISalaryArrearService>();
        public IPayrollRecomputeService RecomputeService = Substitute.For<IPayrollRecomputeService>();
        public IPayrollCostCalculator CostCalculator = Substitute.For<IPayrollCostCalculator>();
        public IUnitOfWork Uow = Substitute.For<IUnitOfWork>();

        public InjectSalaryRevisionArrearsHandler Build() =>
            new(RunRepo, PayrunEmpRepo, BreakdownRepo, RevisionRepo, ArrearService, RecomputeService, CostCalculator, Uow);
    }

    private static DomainPayrollRun MakeRun(PayrollRunType type, PayrollRunStatus status)
    {
        DomainPayrollRun run = DomainPayrollRun.Create(
            TenantId, new PayPeriod(2026, 5), type, new DateOnly(2026, 5, 31),
            statutoryConfigSnapshot: "{}", employeeCount: 1, createdBy: ActorId);
        SetId(run, RunId);
        if (status == PayrollRunStatus.Approved) run.Approve(ActorId);
        return run;
    }

    private static SalaryRevision MakeAppliedRevision()
    {
        SalaryRevision r = SalaryRevision.Create(
            EmployeeId, TenantId, 600000m, 720000m, 3, 2026, 5, 2026, Guid.NewGuid(), null, ActorId);
        SetId(r, Guid.NewGuid());
        r.Apply(Guid.NewGuid(), ActorId);
        return r;
    }

    [Fact]
    public async Task NonRegularRun_Throws()
    {
        var m = new Mocks();
        m.RunRepo.GetByIdAsync(RunId, Arg.Any<CancellationToken>())
            .Returns(MakeRun(PayrollRunType.FinalSettlement, PayrollRunStatus.Draft));

        Func<Task> act = () => m.Build().Handle(new InjectSalaryRevisionArrearsCommand(RunId, ActorId), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Regular runs only*");
    }

    [Fact]
    public async Task NonDraftRun_Throws()
    {
        var m = new Mocks();
        m.RunRepo.GetByIdAsync(RunId, Arg.Any<CancellationToken>())
            .Returns(MakeRun(PayrollRunType.Regular, PayrollRunStatus.Approved));

        Func<Task> act = () => m.Build().Handle(new InjectSalaryRevisionArrearsCommand(RunId, ActorId), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Draft*");
    }

    [Fact]
    public async Task NoRevisions_NoOp()
    {
        var m = new Mocks();
        m.RunRepo.GetByIdAsync(RunId, Arg.Any<CancellationToken>())
            .Returns(MakeRun(PayrollRunType.Regular, PayrollRunStatus.Draft));
        m.RevisionRepo.GetAppliedUnpaidForPayoutAsync(2026, 5, Arg.Any<CancellationToken>())
            .Returns(new List<SalaryRevision>());

        await m.Build().Handle(new InjectSalaryRevisionArrearsCommand(RunId, ActorId), CancellationToken.None);

        await m.RecomputeService.DidNotReceive().RecomputeEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await m.Uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmployeeNotActiveInRun_Skipped_NoRecompute()
    {
        var m = new Mocks();
        m.RunRepo.GetByIdAsync(RunId, Arg.Any<CancellationToken>())
            .Returns(MakeRun(PayrollRunType.Regular, PayrollRunStatus.Draft));
        m.RevisionRepo.GetAppliedUnpaidForPayoutAsync(2026, 5, Arg.Any<CancellationToken>())
            .Returns(new List<SalaryRevision> { MakeAppliedRevision() });
        m.PayrunEmpRepo.GetByRunAndEmployeeAsync(RunId, EmployeeId, Arg.Any<CancellationToken>())
            .Returns((PayrunEmployee?)null); // not in this run

        await m.Build().Handle(new InjectSalaryRevisionArrearsCommand(RunId, ActorId), CancellationToken.None);

        await m.ArrearService.DidNotReceive().ComputeForRevisionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await m.RecomputeService.DidNotReceive().RecomputeEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
