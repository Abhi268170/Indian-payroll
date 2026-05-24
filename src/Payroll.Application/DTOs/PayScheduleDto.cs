namespace Payroll.Application.DTOs;

public record PayScheduleDto(
    List<string> WorkWeekDays,
    string SalaryCalculationMethod,
    int? FixedWorkingDaysPerMonth,
    string PayDateType,
    int? PayDateDay,
    int? FirstPayPeriodMonth,
    int? FirstPayPeriodYear,
    bool IsLocked
);
