using FluentAssertions;
using NSubstitute;
using Payroll.Application.Commands.PaySchedule;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Xunit;

namespace Payroll.Application.Tests.PaySchedule;

public class UpsertPayScheduleHandlerTests
{
    private readonly IPayScheduleRepository _repo = Substitute.For<IPayScheduleRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly UpsertPayScheduleHandler _handler;

    public UpsertPayScheduleHandlerTests()
    {
        _handler = new UpsertPayScheduleHandler(_repo, _uow);
    }

    private static UpsertPayScheduleCommand StandardCommand() =>
        new(
            ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
            "ActualDays",
            null,
            "LastDay",
            null,
            Guid.NewGuid()
        );

    [Fact]
    public async Task Handle_WhenNoExistingSchedule_CreatesAndSaves()
    {
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns((Domain.Entities.PaySchedule?)null);

        await _handler.Handle(StandardCommand(), CancellationToken.None);

        await _repo.Received(1).AddAsync(
            Arg.Is<Domain.Entities.PaySchedule>(s =>
                s.SalaryCalculationMethod == SalaryCalculationMethod.ActualDays &&
                s.PayDateType == PayDateType.LastDay &&
                s.WorkWeekDays == (WorkWeekDay.Monday | WorkWeekDay.Tuesday |
                    WorkWeekDay.Wednesday | WorkWeekDay.Thursday | WorkWeekDay.Friday)),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExistingSchedule_UpdatesAndSaves()
    {
        Domain.Entities.PaySchedule existing = Domain.Entities.PaySchedule.Create(
            WorkWeekDay.Monday | WorkWeekDay.Friday,
            SalaryCalculationMethod.ActualDays,
            null,
            PayDateType.LastDay,
            null,
            Guid.NewGuid());

        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);

        UpsertPayScheduleCommand command = new(
            ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
            "FixedDays",
            26,
            "SpecificDay",
            25,
            Guid.NewGuid());

        await _handler.Handle(command, CancellationToken.None);

        existing.SalaryCalculationMethod.Should().Be(SalaryCalculationMethod.FixedDays);
        existing.FixedWorkingDaysPerMonth.Should().Be(26);
        existing.PayDateType.Should().Be(PayDateType.SpecificDay);
        existing.PayDateDay.Should().Be(25);

        _repo.Received(1).Update(existing);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLockedSchedule_ChangingPayDateOnly_Succeeds()
    {
        Domain.Entities.PaySchedule existing = Domain.Entities.PaySchedule.Create(
            WorkWeekDay.Monday | WorkWeekDay.Tuesday | WorkWeekDay.Wednesday |
            WorkWeekDay.Thursday | WorkWeekDay.Friday,
            SalaryCalculationMethod.ActualDays,
            null,
            PayDateType.LastDay,
            null,
            Guid.NewGuid());

        existing.LockAfterPayrun();

        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);

        UpsertPayScheduleCommand command = new(
            ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
            "ActualDays",
            null,
            "SpecificDay",
            28,
            Guid.NewGuid());

        await _handler.Handle(command, CancellationToken.None);

        existing.PayDateType.Should().Be(PayDateType.SpecificDay);
        existing.PayDateDay.Should().Be(28);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLockedSchedule_ChangingWorkWeekDays_ThrowsDomainException()
    {
        Domain.Entities.PaySchedule existing = Domain.Entities.PaySchedule.Create(
            WorkWeekDay.Monday | WorkWeekDay.Friday,
            SalaryCalculationMethod.ActualDays,
            null,
            PayDateType.LastDay,
            null,
            Guid.NewGuid());

        existing.LockAfterPayrun();

        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);

        UpsertPayScheduleCommand command = new(
            ["Monday", "Tuesday"],
            "ActualDays",
            null,
            "LastDay",
            null,
            Guid.NewGuid());

        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Work week*");
    }

    [Fact]
    public async Task Handle_WhenLockedSchedule_ChangingCalcMethod_ThrowsDomainException()
    {
        Domain.Entities.PaySchedule existing = Domain.Entities.PaySchedule.Create(
            WorkWeekDay.Monday | WorkWeekDay.Tuesday | WorkWeekDay.Wednesday |
            WorkWeekDay.Thursday | WorkWeekDay.Friday,
            SalaryCalculationMethod.ActualDays,
            null,
            PayDateType.LastDay,
            null,
            Guid.NewGuid());

        existing.LockAfterPayrun();

        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);

        UpsertPayScheduleCommand command = new(
            ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
            "FixedDays",
            26,
            "LastDay",
            null,
            Guid.NewGuid());

        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Salary calculation method*");
    }
}
