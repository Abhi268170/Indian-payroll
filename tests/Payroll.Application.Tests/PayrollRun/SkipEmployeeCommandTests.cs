using FluentAssertions;
using NSubstitute;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;
using Xunit;

namespace Payroll.Application.Tests.PayrollRun;

public class SkipEmployeeCommandTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();
    private static readonly PayPeriod Period = new(2025, 5);

    private static Domain.Entities.PayrollRun CreateDraft() =>
        Domain.Entities.PayrollRun.Create(TenantId, Period, PayrollRunType.Regular,
            new DateOnly(2025, 5, 31), null, 1, ActorId);

    private static PayrunEmployee CreateActiveEmployee(decimal grossPay = 50_000m, decimal netPay = 44_000m)
    {
        var emp = PayrunEmployee.Create(RunId, EmpId, TenantId, 31, ActorId);
        emp.UpdateComputedAmounts(grossPay, grossPay, netPay, 6_000m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 6_000m, 0m, 0m, 0m, 0m, 0m, ActorId);
        return emp;
    }

    [Fact]
    public async Task Skip_ValidReason_SetsStatusToSkipped()
    {
        var run = CreateDraft();
        var payrunEmp = CreateActiveEmployee();

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var empRepo = Substitute.For<IPayrunEmployeeRepository>();
        empRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(payrunEmp);
        empRepo.GetByRunIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<PayrunEmployee> { payrunEmp });

        var handler = new SkipEmployeeHandler(runRepo, empRepo, Substitute.For<IUnitOfWork>());

        await handler.Handle(new SkipEmployeeCommand(RunId, EmpId, "Missing bank details", ActorId), CancellationToken.None);

        payrunEmp.Status.Should().Be(PayrunEmployeeStatus.Skipped);
        payrunEmp.SkipReason.Should().Be("Missing bank details");
    }

    [Fact]
    public async Task Skip_OnApprovedRun_ThrowsInvalidOperation()
    {
        var run = CreateDraft();
        run.Approve(ActorId);

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var handler = new SkipEmployeeHandler(runRepo, Substitute.For<IPayrunEmployeeRepository>(), Substitute.For<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(new SkipEmployeeCommand(RunId, EmpId, "reason", ActorId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Draft*");
    }

    [Fact]
    public async Task UndoSkip_PreviouslySkipped_SetsStatusToActive()
    {
        var run = CreateDraft();
        var payrunEmp = CreateActiveEmployee();
        payrunEmp.Skip("Test skip", ActorId);

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var empRepo = Substitute.For<IPayrunEmployeeRepository>();
        empRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(payrunEmp);
        empRepo.GetByRunIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<PayrunEmployee> { payrunEmp });

        var recompute = Substitute.For<Payroll.Application.Services.IPayrollRecomputeService>();
        recompute.RecomputeEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(MakeRecomputeResult()));
        var handler = new UndoSkipEmployeeHandler(runRepo, empRepo, recompute, Substitute.For<IUnitOfWork>());

        await handler.Handle(new UndoSkipEmployeeCommand(RunId, EmpId, ActorId), CancellationToken.None);

        payrunEmp.Status.Should().Be(PayrunEmployeeStatus.Active);
        // Undo-skip must recompute — a row skipped at initiation would otherwise
        // come back Active with all-zero amounts.
        await recompute.Received(1).RecomputeEmployeeAsync(RunId, EmpId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UndoSkip_OnActiveEmployee_ThrowsInvalidOperation()
    {
        var emp = PayrunEmployee.Create(RunId, EmpId, TenantId, 31, ActorId);

        Action act = () => emp.UndoSkip(ActorId);

        act.Should().Throw<InvalidOperationException>();
    }

    private static Payroll.Application.Services.RecomputeResult MakeRecomputeResult()
    {
        var gross = new Payroll.Engine.Outputs.GrossResult(
            GrossWage: 10_000m, PFWage: 10_000m, FullPFWage: 10_000m,
            AnnualProjectedGross: 1_20_000m, LOPDeduction: 0m, ArrearAmount: 0m,
            ComponentBreakdown: [], TaxableGrossWage: 10_000m,
            AnnualProjectedTaxableGross: 1_20_000m, ESIWage: 10_000m);
        var result = new Payroll.Engine.Outputs.PayrollResult(
            EmpId, gross,
            new Payroll.Engine.Outputs.TDSResult(0m, 0m, 0m, 0m, 0m, 0m, false, false),
            new Payroll.Engine.Outputs.PFResult(1200m, 0m, 367m, 833m, false),
            new Payroll.Engine.Outputs.ESIResult(75m, 325m, false),
            new Payroll.Engine.Outputs.PTResult(0m, true),
            new Payroll.Engine.Outputs.LWFResult(0m, 0m, true),
            NetPay: 8_725m,
            new Payroll.Engine.Outputs.GratuityResult(0m, true));
        return new Payroll.Application.Services.RecomputeResult(result, 0m, 0m, 8_725m);
    }
}
