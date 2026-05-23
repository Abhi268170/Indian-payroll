using FluentAssertions;
using NSubstitute;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;
using Payroll.Engine.Inputs;
using Xunit;

namespace Payroll.Application.Tests.PayrollRun;

public class VariableInputCommandTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();
    private static readonly PayPeriod Period = new(2025, 5);

    private static Domain.Entities.PayrollRun CreateDraft() =>
        Domain.Entities.PayrollRun.Create(
            TenantId, Period, PayrollRunType.Regular,
            new DateOnly(2025, 5, 31), null, 1, ActorId);

    private static PayrunEmployee CreateActiveEmployee(int baseDays = 31) =>
        PayrunEmployee.Create(RunId, EmpId, TenantId, baseDays, ActorId);

    // ── SetLop — Draft guard ───────────────────────────────────────────────────

    [Fact]
    public async Task SetLop_OnApprovedRun_ThrowsInvalidOperation()
    {
        var run = CreateDraft();
        run.Approve(ActorId);

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var handler = new SetLopCommandHandler(
            runRepo,
            Substitute.For<IPayrunEmployeeRepository>(),
            Substitute.For<IPayrunComponentBreakdownRepository>(),
            Substitute.For<IEmployeeRepository>(),
            Substitute.For<IWorkLocationRepository>(),
            Substitute.For<IPayScheduleRepository>(),
            Substitute.For<IEmployeeFyOpeningRepository>(),
            Substitute.For<ITdsWorksheetRepository>(),
            Substitute.For<Payroll.Application.Services.IPayrollCostCalculator>(),
            Substitute.For<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(new SetLopCommand(RunId, EmpId, 2, ActorId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public async Task SetLop_WhenLopGeqSalaryDivisor_ThrowsInvalidOperation()
    {
        var run = CreateDraft();
        var payrunEmp = CreateActiveEmployee(baseDays: 31);

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var empRepo = Substitute.For<IPayrunEmployeeRepository>();
        empRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(payrunEmp);

        var scheduleRepo = Substitute.For<IPayScheduleRepository>();
        var paySchedule = Domain.Entities.PaySchedule.Create(
            WorkWeekDay.Monday | WorkWeekDay.Friday,
            SalaryCalculationMethod.FixedDays, 26,
            PayDateType.LastDay, null, null, null, ActorId);
        scheduleRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(paySchedule);

        var handler = new SetLopCommandHandler(
            runRepo, empRepo,
            Substitute.For<IPayrunComponentBreakdownRepository>(),
            Substitute.For<IEmployeeRepository>(),
            Substitute.For<IWorkLocationRepository>(),
            scheduleRepo,
            Substitute.For<IEmployeeFyOpeningRepository>(),
            Substitute.For<ITdsWorksheetRepository>(),
            Substitute.For<Payroll.Application.Services.IPayrollCostCalculator>(),
            Substitute.For<IUnitOfWork>());

        // lopDays = 26 >= salaryDivisor = 26 (FixedDays/26)
        Func<Task> act = () => handler.Handle(new SetLopCommand(RunId, EmpId, 26, ActorId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("LOP days*");
    }

    // ── OverrideTds ──────────────────────────────────────────────────────────

    [Fact]
    public async Task OverrideTds_ValidReasonAndAmount_UpdatesTdsAndNetPay()
    {
        var run = CreateDraft();
        var payrunEmp = CreateActiveEmployee();

        // Set initial computed amounts
        payrunEmp.UpdateComputedAmounts(
            grossPay: 50_000m, netPay: 44_000m,
            taxesAmount: 6_000m, benefitsAmount: 0m, reimbursementsAmount: 0m,
            employeePf: 0m, employerPf: 0m, employeeEsi: 0m, employerEsi: 0m,
            ptAmount: 0m, tdsAmount: 6_000m, lwfEmployeeAmount: 0m, lwfEmployerAmount: 0m, gratuityAmount: 0m,
            epsAmount: 0m, monthlyCTC: 0m,
            actorId: ActorId);

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        payrunEmpRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(payrunEmp);

        var handler = new OverrideTdsHandler(
            runRepo, payrunEmpRepo, Substitute.For<ITdsWorksheetRepository>(), Substitute.For<IUnitOfWork>());

        await handler.Handle(new OverrideTdsCommand(RunId, EmpId, 5_000m, "Employee request", ActorId), CancellationToken.None);

        payrunEmp.TdsOverrideAmount.Should().Be(5_000m);
        payrunEmp.TdsOverrideReason.Should().Be("Employee request");
        payrunEmp.TdsAmount.Should().Be(5_000m);
        payrunEmp.NetPay.Should().Be(45_000m); // 44000 + (6000 - 5000)
    }

    [Fact]
    public async Task OverrideTds_OnApprovedRun_ThrowsInvalidOperation()
    {
        var run = CreateDraft();
        run.Approve(ActorId);

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var handler = new OverrideTdsHandler(
            runRepo,
            Substitute.For<IPayrunEmployeeRepository>(),
            Substitute.For<ITdsWorksheetRepository>(),
            Substitute.For<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(new OverrideTdsCommand(RunId, EmpId, 5_000m, "reason", ActorId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    // ── AddOneTimeEarning ────────────────────────────────────────────────────

    [Fact]
    public async Task AddOneTimeEarning_ValidAmount_IncreasesGrossAndNetPay()
    {
        var run = CreateDraft();
        var payrunEmp = CreateActiveEmployee();

        payrunEmp.UpdateComputedAmounts(
            grossPay: 50_000m, netPay: 44_000m,
            taxesAmount: 6_000m, benefitsAmount: 0m, reimbursementsAmount: 0m,
            employeePf: 0m, employerPf: 0m, employeeEsi: 0m, employerEsi: 0m,
            ptAmount: 0m, tdsAmount: 6_000m, lwfEmployeeAmount: 0m, lwfEmployerAmount: 0m, gratuityAmount: 0m,
            epsAmount: 0m, monthlyCTC: 0m,
            actorId: ActorId);

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        payrunEmpRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(payrunEmp);

        Guid componentId = Guid.NewGuid();
        var component = SalaryComponent.CreateEarning(
            "Bonus", "Bonus", "BONUS",
            EarningType.Bonus, PayType.Monthly,
            ComponentFormulaType.Fixed, null, null,
            isTaxable: true, considerForEpf: false,
            EpfInclusionRule.Always, considerForEsi: false,
            calculateOnProRata: false, showInPayslip: true,
            tenantId: TenantId, createdBy: ActorId);

        var componentRepo = Substitute.For<ISalaryComponentRepository>();
        componentRepo.GetByIdAsync(componentId, Arg.Any<CancellationToken>()).Returns(component);

        var breakdownRepo = Substitute.For<IPayrunComponentBreakdownRepository>();

        var handler = new AddOneTimeEarningHandler(
            runRepo, payrunEmpRepo, breakdownRepo, componentRepo, Substitute.For<IUnitOfWork>());

        await handler.Handle(new AddOneTimeEarningCommand(RunId, EmpId, componentId, 10_000m, ActorId), CancellationToken.None);

        payrunEmp.GrossPay.Should().Be(60_000m);
        payrunEmp.NetPay.Should().Be(54_000m);
    }

    // ── RemoveOneTimeEarning ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveOneTimeEarning_ValidRow_RevertsGrossAndNetPay()
    {
        var run = CreateDraft();
        var payrunEmp = CreateActiveEmployee();
        Guid bdId = Guid.NewGuid();

        // Simulate post-add state
        payrunEmp.UpdateComputedAmounts(
            grossPay: 60_000m, netPay: 54_000m,
            taxesAmount: 6_000m, benefitsAmount: 0m, reimbursementsAmount: 0m,
            employeePf: 0m, employerPf: 0m, employeeEsi: 0m, employerEsi: 0m,
            ptAmount: 0m, tdsAmount: 6_000m, lwfEmployeeAmount: 0m, lwfEmployerAmount: 0m, gratuityAmount: 0m,
            epsAmount: 0m, monthlyCTC: 0m,
            actorId: ActorId);

        var bd = PayrunComponentBreakdown.Create(
            RunId, EmpId, TenantId,
            Guid.NewGuid(), "BONUS", "Bonus",
            10_000m, 10_000m, isOneTimeEarning: true);

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        payrunEmpRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(payrunEmp);

        var breakdownRepo = Substitute.For<IPayrunComponentBreakdownRepository>();
        breakdownRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<PayrunComponentBreakdown> { bd });

        var handler = new RemoveOneTimeEarningHandler(
            runRepo, payrunEmpRepo, breakdownRepo, Substitute.For<ISalaryComponentRepository>(), Substitute.For<IUnitOfWork>());

        await handler.Handle(new RemoveOneTimeEarningCommand(RunId, EmpId, bd.Id, ActorId), CancellationToken.None);

        payrunEmp.GrossPay.Should().Be(50_000m);
        payrunEmp.NetPay.Should().Be(44_000m);
    }
}
