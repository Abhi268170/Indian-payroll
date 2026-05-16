using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using PayScheduleEntity = Payroll.Domain.Entities.PaySchedule;

namespace Payroll.Application.Commands.PaySchedule;

public sealed class UpsertPayScheduleHandler(IPayScheduleRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpsertPayScheduleCommand>
{
    public async Task Handle(UpsertPayScheduleCommand request, CancellationToken cancellationToken)
    {
        WorkWeekDay workWeekDays = ParseWorkWeekDays(request.WorkWeekDays);
        SalaryCalculationMethod method = Enum.Parse<SalaryCalculationMethod>(request.SalaryCalculationMethod);
        PayDateType payDateType = Enum.Parse<PayDateType>(request.PayDateType);

        PayScheduleEntity? existing = await repo.GetAsync(cancellationToken);

        if (existing is null)
        {
            PayScheduleEntity schedule = PayScheduleEntity.Create(
                workWeekDays,
                method,
                request.FixedWorkingDaysPerMonth,
                payDateType,
                request.PayDateDay,
                request.ActorId);

            await repo.AddAsync(schedule, cancellationToken);
        }
        else
        {
            existing.Update(
                workWeekDays,
                method,
                request.FixedWorkingDaysPerMonth,
                payDateType,
                request.PayDateDay,
                request.ActorId);

            repo.Update(existing);
        }

        await uow.SaveChangesAsync(cancellationToken);
    }

    private static WorkWeekDay ParseWorkWeekDays(List<string> dayNames)
    {
        WorkWeekDay result = WorkWeekDay.None;
        foreach (string name in dayNames)
            result |= Enum.Parse<WorkWeekDay>(name);
        return result;
    }
}
