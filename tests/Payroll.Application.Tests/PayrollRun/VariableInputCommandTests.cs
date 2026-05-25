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
            Substitute.For<IPayScheduleRepository>(),
            Substitute.For<Payroll.Application.Services.IPayrollRecomputeService>(),
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
            scheduleRepo,
            Substitute.For<Payroll.Application.Services.IPayrollRecomputeService>(),
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
            grossPay: 50_000m, taxableGrossPay: 50_000m, netPay: 44_000m,
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

    // ── AddOneTimeEarning — validation guards ────────────────────────────────

    [Fact]
    public async Task AddOneTimeEarning_ComponentNotOneTime_ThrowsInvalidOperation()
    {
        var run = CreateDraft();
        var payrunEmp = CreateActiveEmployee();

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        payrunEmpRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(payrunEmp);

        // IsOneTime defaults to false here; handler must reject.
        var component = SalaryComponent.CreateEarning(
            "Basic", "Basic", "BASIC",
            EarningType.Basic, PayType.Monthly,
            ComponentFormulaType.Fixed, 25_000m, null,
            isTaxable: true, considerForEpf: true,
            EpfInclusionRule.Always, considerForEsi: false,
            calculateOnProRata: true, showInPayslip: true,
            tenantId: TenantId, createdBy: ActorId);

        var componentRepo = Substitute.For<ISalaryComponentRepository>();
        componentRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(component);

        var handler = new AddOneTimeEarningHandler(
            runRepo, payrunEmpRepo,
            Substitute.For<IPayrunComponentBreakdownRepository>(),
            componentRepo,
            Substitute.For<Payroll.Application.Services.IPayrollRecomputeService>(),
            Substitute.For<Payroll.Application.Services.IPayrollCostCalculator>(),
            Substitute.For<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(
            new AddOneTimeEarningCommand(RunId, EmpId, Guid.NewGuid(), 10_000m, ActorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a one-time component*");
    }

    [Fact]
    public async Task AddOneTimeEarning_DeductionCategory_ThrowsInvalidOperation()
    {
        var run = CreateDraft();
        var payrunEmp = CreateActiveEmployee();

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        payrunEmpRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(payrunEmp);

        var component = SalaryComponent.CreateDeduction(
            "Loan Recovery", "Loan Recovery", "LOANREC",
            DeductionFrequency.EveryMonth, TenantId, ActorId, isOneTime: true);

        var componentRepo = Substitute.For<ISalaryComponentRepository>();
        componentRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(component);

        var handler = new AddOneTimeEarningHandler(
            runRepo, payrunEmpRepo,
            Substitute.For<IPayrunComponentBreakdownRepository>(),
            componentRepo,
            Substitute.For<Payroll.Application.Services.IPayrollRecomputeService>(),
            Substitute.For<Payroll.Application.Services.IPayrollCostCalculator>(),
            Substitute.For<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(
            new AddOneTimeEarningCommand(RunId, EmpId, Guid.NewGuid(), 10_000m, ActorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not an Earning*");
    }

    [Fact]
    public async Task AddOneTimeDeduction_EarningCategory_ThrowsInvalidOperation()
    {
        var run = CreateDraft();
        var payrunEmp = CreateActiveEmployee();

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        payrunEmpRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(payrunEmp);

        var component = SalaryComponent.CreateEarning(
            "Bonus", "Bonus", "BONUS",
            EarningType.Bonus, PayType.FlatAmount,
            ComponentFormulaType.Fixed, 10_000m, null,
            isTaxable: true, considerForEpf: false,
            EpfInclusionRule.Always, considerForEsi: false,
            calculateOnProRata: false, showInPayslip: true,
            tenantId: TenantId, createdBy: ActorId,
            isOneTime: true);

        var componentRepo = Substitute.For<ISalaryComponentRepository>();
        componentRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(component);

        var handler = new AddOneTimeDeductionHandler(
            runRepo, payrunEmpRepo,
            Substitute.For<IPayrunComponentBreakdownRepository>(),
            componentRepo,
            Substitute.For<Payroll.Application.Services.IPayrollRecomputeService>(),
            Substitute.For<Payroll.Application.Services.IPayrollCostCalculator>(),
            Substitute.For<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(
            new AddOneTimeDeductionCommand(RunId, EmpId, Guid.NewGuid(), 5_000m, ActorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a Deduction*");
    }

    // ── RemoveOneTimeEarning — validation guards ─────────────────────────────

    [Fact]
    public async Task RemoveOneTimeEarning_OnNonOneTimeRow_ThrowsInvalidOperation()
    {
        var run = CreateDraft();
        var payrunEmp = CreateActiveEmployee();

        var bd = PayrunComponentBreakdown.Create(
            RunId, EmpId, TenantId,
            Guid.NewGuid(), "BASIC", "Basic Salary",
            25_000m, 25_000m, isOneTimeEarning: false);

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(run);

        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        payrunEmpRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(payrunEmp);

        var breakdownRepo = Substitute.For<IPayrunComponentBreakdownRepository>();
        breakdownRepo.GetByRunAndEmployeeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<PayrunComponentBreakdown> { bd });

        var handler = new RemoveOneTimeEarningHandler(
            runRepo, payrunEmpRepo, breakdownRepo,
            Substitute.For<Payroll.Application.Services.IPayrollRecomputeService>(),
            Substitute.For<Payroll.Application.Services.IPayrollCostCalculator>(),
            Substitute.For<IUnitOfWork>());

        Func<Task> act = () => handler.Handle(
            new RemoveOneTimeEarningCommand(RunId, EmpId, bd.Id, ActorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only one-time earnings can be removed*");
    }
}
