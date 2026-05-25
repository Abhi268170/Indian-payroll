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

        var handler = new UndoSkipEmployeeHandler(runRepo, empRepo, Substitute.For<IUnitOfWork>());

        await handler.Handle(new UndoSkipEmployeeCommand(RunId, EmpId, ActorId), CancellationToken.None);

        payrunEmp.Status.Should().Be(PayrunEmployeeStatus.Active);
    }

    [Fact]
    public void UndoSkip_OnActiveEmployee_ThrowsInvalidOperation()
    {
        var emp = PayrunEmployee.Create(RunId, EmpId, TenantId, 31, ActorId);

        Action act = () => emp.UndoSkip(ActorId);

        act.Should().Throw<InvalidOperationException>();
    }
}
