namespace Payroll.Engine;

/// <summary>
/// Engine-side mirror of Domain WorkWeekDay flags enum.
/// Values must stay in sync with Payroll.Domain.Enums.WorkWeekDay.
/// </summary>
[Flags]
public enum EngineWorkWeekDay
{
    None      = 0,
    Sunday    = 1 << 0,
    Monday    = 1 << 1,
    Tuesday   = 1 << 2,
    Wednesday = 1 << 3,
    Thursday  = 1 << 4,
    Friday    = 1 << 5,
    Saturday  = 1 << 6,
    StandardFiveDay = Monday | Tuesday | Wednesday | Thursday | Friday,
}

public enum EngineSalaryCalculationMethod
{
    ActualDays,
    FixedDays,
}

public enum EnginePayDateType
{
    LastDay,
    SpecificDay,
}

public static class PayScheduleHelpers
{
    /// <summary>
    /// Returns the daily rate divisor for a given pay period.
    /// For ActualDays: calendar days in the month.
    /// For FixedDays: the configured fixed number.
    /// </summary>
    public static int GetDivisor(
        EngineSalaryCalculationMethod method,
        int? fixedDays,
        int year,
        int month)
    {
        return method switch
        {
            EngineSalaryCalculationMethod.ActualDays => DateTime.DaysInMonth(year, month),
            EngineSalaryCalculationMethod.FixedDays  => fixedDays!.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(method)),
        };
    }

    /// <summary>
    /// Returns the count of calendar days in the month that fall on a configured working day.
    /// Used for prorating mid-month joiners and computing LOP deductions.
    /// </summary>
    public static int GetPayableDaysInMonth(EngineWorkWeekDay workWeek, int year, int month)
    {
        int days = 0;
        int daysInMonth = DateTime.DaysInMonth(year, month);
        for (int d = 1; d <= daysInMonth; d++)
        {
            DayOfWeek dow = new DateTime(year, month, d).DayOfWeek;
            if ((workWeek & DayOfWeekToFlag(dow)) != 0)
                days++;
        }
        return days;
    }

    /// <summary>
    /// Resolves the actual pay date for a given month, applying the
    /// non-working day fallback rule: if the resolved date falls on a non-working
    /// day, walk back day by day until a working day is found.
    /// </summary>
    public static DateOnly ResolveActualPayDate(
        EnginePayDateType type,
        int? specificDay,
        int year,
        int month,
        EngineWorkWeekDay workWeek)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);

        int candidateDay = type switch
        {
            EnginePayDateType.LastDay     => daysInMonth,
            EnginePayDateType.SpecificDay => Math.Min(specificDay!.Value, daysInMonth),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        return WalkBackToWorkingDay(year, month, candidateDay, workWeek);
    }

    private static DateOnly WalkBackToWorkingDay(int year, int month, int day, EngineWorkWeekDay workWeek)
    {
        // Guard: if somehow no working days configured, return candidate as-is.
        if (workWeek == EngineWorkWeekDay.None) return new DateOnly(year, month, day);

        for (int d = day; d >= 1; d--)
        {
            DayOfWeek dow = new DateTime(year, month, d).DayOfWeek;
            if ((workWeek & DayOfWeekToFlag(dow)) != 0)
                return new DateOnly(year, month, d);
        }

        // Extremely unlikely: entire month has no working days matching work week.
        // Return the 1st as a safe fallback.
        return new DateOnly(year, month, 1);
    }

    private static EngineWorkWeekDay DayOfWeekToFlag(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Sunday    => EngineWorkWeekDay.Sunday,
        DayOfWeek.Monday    => EngineWorkWeekDay.Monday,
        DayOfWeek.Tuesday   => EngineWorkWeekDay.Tuesday,
        DayOfWeek.Wednesday => EngineWorkWeekDay.Wednesday,
        DayOfWeek.Thursday  => EngineWorkWeekDay.Thursday,
        DayOfWeek.Friday    => EngineWorkWeekDay.Friday,
        DayOfWeek.Saturday  => EngineWorkWeekDay.Saturday,
        _ => EngineWorkWeekDay.None,
    };
}
