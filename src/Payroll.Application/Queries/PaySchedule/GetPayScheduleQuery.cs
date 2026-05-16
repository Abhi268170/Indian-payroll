using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using PayScheduleEntity = Payroll.Domain.Entities.PaySchedule;

namespace Payroll.Application.Queries.PaySchedule;

public record GetPayScheduleQuery : IRequest<PayScheduleDto?>;

public sealed class GetPayScheduleHandler(IPayScheduleRepository repo)
    : IRequestHandler<GetPayScheduleQuery, PayScheduleDto?>
{
    public async Task<PayScheduleDto?> Handle(GetPayScheduleQuery request, CancellationToken cancellationToken)
    {
        PayScheduleEntity? schedule = await repo.GetAsync(cancellationToken);
        if (schedule is null) return null;

        return new PayScheduleDto(
            WorkWeekDays: ToWorkWeekDayNames(schedule.WorkWeekDays),
            SalaryCalculationMethod: schedule.SalaryCalculationMethod.ToString(),
            FixedWorkingDaysPerMonth: schedule.FixedWorkingDaysPerMonth,
            PayDateType: schedule.PayDateType.ToString(),
            PayDateDay: schedule.PayDateDay,
            IsLocked: schedule.IsLockedAfterPayrun);
    }

    private static List<string> ToWorkWeekDayNames(WorkWeekDay mask)
    {
        List<string> days = [];
        foreach (WorkWeekDay day in Enum.GetValues<WorkWeekDay>())
        {
            if (day == WorkWeekDay.None || day == WorkWeekDay.StandardFiveDay) continue;
            if ((mask & day) != 0) days.Add(day.ToString());
        }
        return days;
    }
}
