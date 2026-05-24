using FluentAssertions;
using Payroll.Engine;
using Xunit;

namespace Payroll.Engine.Tests;

public class PayScheduleHelpersTests
{
    // ── GetDivisor ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2024, 1,  31)]  // January
    [InlineData(2024, 2,  29)]  // February leap year
    [InlineData(2023, 2,  28)]  // February non-leap year
    [InlineData(2024, 3,  31)]  // March
    [InlineData(2024, 4,  30)]  // April
    [InlineData(2024, 12, 31)]  // December
    public void GetDivisor_ActualDays_ReturnsCalendarDaysInMonth(int year, int month, int expected)
    {
        int result = PayScheduleHelpers.GetDivisor(EngineSalaryCalculationMethod.ActualDays, null, year, month);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(26)]
    [InlineData(31)]
    [InlineData(1)]
    public void GetDivisor_FixedDays_ReturnsConfiguredValue(int fixedDays)
    {
        int result = PayScheduleHelpers.GetDivisor(EngineSalaryCalculationMethod.FixedDays, fixedDays, 2024, 2);
        result.Should().Be(fixedDays);
    }

    // ── GetPayableDaysInMonth ─────────────────────────────────────────────────

    [Fact]
    public void GetPayableDaysInMonth_MonToFri_May2024_Returns23()
    {
        // May 2024: 31 days; weekends = 4 Saturdays + 4 Sundays = 8 days off → 23 working days
        int result = PayScheduleHelpers.GetPayableDaysInMonth(
            EngineWorkWeekDay.Monday | EngineWorkWeekDay.Tuesday | EngineWorkWeekDay.Wednesday | EngineWorkWeekDay.Thursday | EngineWorkWeekDay.Friday, 2024, 5);
        result.Should().Be(23);
    }

    [Fact]
    public void GetPayableDaysInMonth_MonToFri_Feb2024_Returns21()
    {
        // Feb 2024 (leap): 29 days; 8 weekend days → 21 working days
        int result = PayScheduleHelpers.GetPayableDaysInMonth(
            EngineWorkWeekDay.Monday | EngineWorkWeekDay.Tuesday | EngineWorkWeekDay.Wednesday | EngineWorkWeekDay.Thursday | EngineWorkWeekDay.Friday, 2024, 2);
        result.Should().Be(21);
    }

    [Fact]
    public void GetPayableDaysInMonth_AllSevenDays_ReturnsAllDays()
    {
        EngineWorkWeekDay allDays = EngineWorkWeekDay.Sunday | EngineWorkWeekDay.Monday |
            EngineWorkWeekDay.Tuesday | EngineWorkWeekDay.Wednesday | EngineWorkWeekDay.Thursday |
            EngineWorkWeekDay.Friday | EngineWorkWeekDay.Saturday;

        int result = PayScheduleHelpers.GetPayableDaysInMonth(allDays, 2024, 2);
        result.Should().Be(29); // All 29 days in Feb 2024
    }

    [Fact]
    public void GetPayableDaysInMonth_SaturdayOnly_Feb2024_Returns5()
    {
        // Feb 2024: Saturdays are 3,10,17,24 → 4 Saturdays... wait let me count
        // Feb 1 2024 = Thursday. Saturdays: 3,10,17,24 = 4 Saturdays
        int result = PayScheduleHelpers.GetPayableDaysInMonth(EngineWorkWeekDay.Saturday, 2024, 2);
        result.Should().Be(4);
    }

    // ── ResolveActualPayDate ──────────────────────────────────────────────────

    private static readonly EngineWorkWeekDay StandardWeek =
        EngineWorkWeekDay.Monday | EngineWorkWeekDay.Tuesday | EngineWorkWeekDay.Wednesday |
        EngineWorkWeekDay.Thursday | EngineWorkWeekDay.Friday;

    [Fact]
    public void ResolveActualPayDate_LastDay_WhenLastDayIsWeekday_ReturnsLastDay()
    {
        // May 2024: May 31 = Friday (working day)
        DateOnly result = PayScheduleHelpers.ResolveActualPayDate(
            EnginePayDateType.LastDay, null, 2024, 5, StandardWeek);
        result.Should().Be(new DateOnly(2024, 5, 31));
    }

    [Fact]
    public void ResolveActualPayDate_LastDay_WhenLastDayIsSunday_ReturnsFriday()
    {
        // March 2024: March 31 = Sunday → falls back to Friday March 29
        DateOnly result = PayScheduleHelpers.ResolveActualPayDate(
            EnginePayDateType.LastDay, null, 2024, 3, StandardWeek);
        result.Should().Be(new DateOnly(2024, 3, 29));
    }

    [Fact]
    public void ResolveActualPayDate_LastDay_WhenLastDayIsSaturday_ReturnsFriday()
    {
        // August 2024: Aug 31 = Saturday → falls back to Friday Aug 30
        DateOnly result = PayScheduleHelpers.ResolveActualPayDate(
            EnginePayDateType.LastDay, null, 2024, 8, StandardWeek);
        result.Should().Be(new DateOnly(2024, 8, 30));
    }

    [Fact]
    public void ResolveActualPayDate_SpecificDay15_WhenDay15IsWeekday_ReturnsDay15()
    {
        // April 2024: April 15 = Monday (working day)
        DateOnly result = PayScheduleHelpers.ResolveActualPayDate(
            EnginePayDateType.SpecificDay, 15, 2024, 4, StandardWeek);
        result.Should().Be(new DateOnly(2024, 4, 15));
    }

    [Fact]
    public void ResolveActualPayDate_SpecificDay20_WhenDay20IsSunday_ReturnsFriday()
    {
        // October 2024: Oct 20 = Sunday → falls back to Friday Oct 18
        DateOnly result = PayScheduleHelpers.ResolveActualPayDate(
            EnginePayDateType.SpecificDay, 20, 2024, 10, StandardWeek);
        result.Should().Be(new DateOnly(2024, 10, 18));
    }

    [Fact]
    public void ResolveActualPayDate_SpecificDay30_InFebruary_ClampsToLastDayThenFallsBack()
    {
        // Feb 2024 has 29 days. Day 30 → clamp to 29. Feb 29 = Thursday (working day).
        DateOnly result = PayScheduleHelpers.ResolveActualPayDate(
            EnginePayDateType.SpecificDay, 30, 2024, 2, StandardWeek);
        result.Should().Be(new DateOnly(2024, 2, 29));
    }

    [Fact]
    public void ResolveActualPayDate_SpecificDay30_InFebruary2023_ClampsAndFallsBack()
    {
        // Feb 2023 has 28 days. Day 30 → clamp to 28. Feb 28 2023 = Tuesday (working day).
        DateOnly result = PayScheduleHelpers.ResolveActualPayDate(
            EnginePayDateType.SpecificDay, 30, 2023, 2, StandardWeek);
        result.Should().Be(new DateOnly(2023, 2, 28));
    }

    [Fact]
    public void ResolveActualPayDate_SpecificDay1_WhenDay1IsSunday_ReturnsSameDay_ForSixDayWeek()
    {
        // If Saturday is also a working day (6-day week), and day 1 is Sunday,
        // the fallback still skips Sunday and returns the previous Saturday.
        EngineWorkWeekDay sixDay = EngineWorkWeekDay.Monday | EngineWorkWeekDay.Tuesday |
            EngineWorkWeekDay.Wednesday | EngineWorkWeekDay.Thursday | EngineWorkWeekDay.Friday |
            EngineWorkWeekDay.Saturday;

        // September 2024: Sep 1 = Sunday. Previous working day (Sat Aug 31 is in August).
        // Sep 1 is Sunday, walk back: no Saturdays before Sep 1 in September.
        // This would go all the way to month boundary... fall back to Sep 1 (Sunday).
        // Actually: walk back from day 1 → only day 1 in Sep, which is Sunday → guard returns day 1.
        // The guard returns new DateOnly(year, month, 1) when no working day found walking back.
        DateOnly result = PayScheduleHelpers.ResolveActualPayDate(
            EnginePayDateType.SpecificDay, 1, 2024, 9, sixDay);

        // Sep 1 2024 is Sunday; walk back within September finds no Saturday (none before day 1)
        // so guard activates: returns DateOnly(2024, 9, 1)
        result.Should().Be(new DateOnly(2024, 9, 1));
    }
}
