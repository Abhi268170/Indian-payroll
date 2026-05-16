using MediatR;

namespace Payroll.Application.Commands.PaySchedule;

public record UpsertPayScheduleCommand(
    List<string> WorkWeekDays,
    string SalaryCalculationMethod,
    int? FixedWorkingDaysPerMonth,
    string PayDateType,
    int? PayDateDay,
    Guid ActorId
) : IRequest;
