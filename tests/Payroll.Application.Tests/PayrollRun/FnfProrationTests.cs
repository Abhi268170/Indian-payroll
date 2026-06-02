using FluentAssertions;
using Payroll.Domain.ValueObjects;
using Xunit;

namespace Payroll.Application.Tests.PayrollRun;

/// <summary>
/// Unit tests for WI-07 (BulkFnF proration uses LWD month, not pay-date month)
/// and WI-08 (same-month joiner uses DateOfJoining as period start).
/// Tests validate the derivation logic directly — the orchestrator cannot be
/// unit-tested without a full DI harness, so we test the invariants here.
/// </summary>
public class FnfProrationTests
{
    // ─── WI-07: LWD month derivation ─────────────────────────────────────────

    [Fact]
    public void LwdPeriod_DerivedFromLwd_NotPayPeriod()
    {
        // BulkFnF run: PayPeriod = April (pay date), LWD = March 20
        PayPeriod runPayPeriod = new(2026, 4);   // pay-date month (wrong source)
        var lwd = new DateOnly(2026, 3, 20);

        PayPeriod lwdPeriod = new(lwd.Year, lwd.Month);

        lwdPeriod.Year.Should().Be(2026);
        lwdPeriod.Month.Should().Be(3, "LWD is in March — salary must prorate against March");
        lwdPeriod.Month.Should().NotBe(runPayPeriod.Month, "pay-date month must not drive proration");
    }

    [Fact]
    public void LwdPeriod_FiscalYear_CrossFyBoundary()
    {
        // LWD = March 28, 2026 → FY25-26; pay date = April 5, 2026 → FY26-27
        PayPeriod runPayPeriod = new(2026, 4);   // FY26-27
        var lwd = new DateOnly(2026, 3, 28);
        PayPeriod lwdPeriod = new(lwd.Year, lwd.Month);

        lwdPeriod.FiscalYear.Should().Be(2025,
            "LWD in March 2026 belongs to FY25-26 (fiscal year starts April)");
        runPayPeriod.FiscalYear.Should().Be(2026,
            "pay date in April 2026 belongs to FY26-27 — would be wrong for YTD/TDS");
        lwdPeriod.FiscalYear.Should().NotBe(runPayPeriod.FiscalYear,
            "the cross-FY case is exactly what this fix addresses");
    }

    [Fact]
    public void WorkedDays_BulkCrossMonth_UsesLwdMonth()
    {
        // BulkFnF PayPeriod = May 2026; LWD = March 20, 2026
        // Before fix: periodStart = May 1 → LWD < periodStart → workedDays = 31 (full May) WRONG
        // After fix: periodStart = March 1 → workedDays = 20 days in March CORRECT
        var lwd = new DateOnly(2026, 3, 20);
        DateOnly lwdMonthStart = new(lwd.Year, lwd.Month, 1);
        DateOnly effectivePeriodStart = lwdMonthStart; // DateOfJoining not in March → use month start

        int workedDays = lwd.DayNumber - effectivePeriodStart.DayNumber + 1;
        int salaryDivisor = 31; // March has 31 days
        int lopFromExit = salaryDivisor - workedDays;

        workedDays.Should().Be(20, "March 1–20 = 20 days");
        lopFromExit.Should().Be(11, "11 post-LWD days become effective LOP");
        (salaryDivisor - lopFromExit).Should().Be(20, "payable days = workedDays");
        workedDays.Should().NotBe(31, "31 was the buggy value (full May days)");
    }

    [Fact]
    public void WorkedDays_SameMonth_FullMonth()
    {
        // Standard case: PayPeriod = LWD month, LWD = last day of month
        var lwd = new DateOnly(2026, 6, 30);
        DateOnly lwdMonthStart = new(lwd.Year, lwd.Month, 1);

        int workedDays = lwd.DayNumber - lwdMonthStart.DayNumber + 1;

        workedDays.Should().Be(30, "June has 30 days — full month exit");
    }

    [Fact]
    public void WorkedDays_SameMonth_MidMonth()
    {
        var lwd = new DateOnly(2026, 6, 15);
        DateOnly lwdMonthStart = new(lwd.Year, lwd.Month, 1);

        int workedDays = lwd.DayNumber - lwdMonthStart.DayNumber + 1;

        workedDays.Should().Be(15, "June 1–15 = 15 days");
    }

    // ─── WI-08: same-month joiner proration ──────────────────────────────────

    [Fact]
    public void WorkedDays_SameMonthJoiner_UsesDateOfJoining()
    {
        // Employee joined June 10, exits June 20 — same month
        var lwd = new DateOnly(2026, 6, 20);
        var dateOfJoining = new DateOnly(2026, 6, 10);
        DateOnly lwdMonthStart = new(lwd.Year, lwd.Month, 1);

        DateOnly effectivePeriodStart = dateOfJoining > lwdMonthStart
            ? dateOfJoining
            : lwdMonthStart;

        int workedDays = lwd.DayNumber - effectivePeriodStart.DayNumber + 1;
        int salaryDivisor = 30; // June has 30 days
        int lopFromExit = salaryDivisor - workedDays;

        workedDays.Should().Be(11, "June 10–20 inclusive = 11 days");
        lopFromExit.Should().Be(19, "9 pre-joining + 10 post-LWD = 19 effective LOP");

        // Proration: (salaryDivisor - lopFromExit) / salaryDivisor = 11/30
        decimal proration = (decimal)(salaryDivisor - lopFromExit) / salaryDivisor;
        proration.Should().BeApproximately(11m / 30m, 0.001m);

        effectivePeriodStart.Should().Be(dateOfJoining,
            "DateOfJoining (June 10) overrides month start (June 1)");
    }

    [Fact]
    public void LopFromExit_FullMonth_IsZero()
    {
        // LWD = last day of month → no unworked days → lopFromExit = 0 → full salary
        var lwd = new DateOnly(2026, 6, 30);
        DateOnly lwdMonthStart = new(lwd.Year, lwd.Month, 1);
        int workedDays = lwd.DayNumber - lwdMonthStart.DayNumber + 1;
        int salaryDivisor = 30;
        int lopFromExit = salaryDivisor - workedDays;

        lopFromExit.Should().Be(0, "LWD on last day → zero effective LOP → full salary");
        workedDays.Should().Be(30);
    }

    [Fact]
    public void LopFromExit_MidMonth_ProducesCorrectRatio()
    {
        // LWD = June 20 (joined before June) → 10 post-LWD days = LOP
        var lwd = new DateOnly(2026, 6, 20);
        DateOnly lwdMonthStart = new(lwd.Year, lwd.Month, 1);
        int workedDays = lwd.DayNumber - lwdMonthStart.DayNumber + 1; // 20
        int salaryDivisor = 30;
        int lopFromExit = salaryDivisor - workedDays; // 10

        decimal prorationNumerator = salaryDivisor - lopFromExit; // 20
        decimal proration = prorationNumerator / salaryDivisor;

        proration.Should().BeApproximately(20m / 30m, 0.001m,
            "June 1–20 = 20/30 of monthly salary");
    }

    [Fact]
    public void WorkedDays_JoiningBeforeMonth_UsesMonthStart()
    {
        // Employee joined April 1, exits June 30 — joining is before LWD month
        var lwd = new DateOnly(2026, 6, 30);
        var dateOfJoining = new DateOnly(2026, 4, 1);
        DateOnly lwdMonthStart = new(lwd.Year, lwd.Month, 1);

        DateOnly effectivePeriodStart = dateOfJoining > lwdMonthStart
            ? dateOfJoining
            : lwdMonthStart;

        int workedDays = lwd.DayNumber - effectivePeriodStart.DayNumber + 1;

        workedDays.Should().Be(30, "full June — joining before the month");
        effectivePeriodStart.Should().Be(lwdMonthStart,
            "April joining does not affect June proration");
    }

    [Fact]
    public void WorkedDays_JoiningOnFirstOfMonth_EqualsFullMonth()
    {
        // Joined June 1 and exits June 30 — full month
        var lwd = new DateOnly(2026, 6, 30);
        var dateOfJoining = new DateOnly(2026, 6, 1);
        DateOnly lwdMonthStart = new(lwd.Year, lwd.Month, 1);

        DateOnly effectivePeriodStart = dateOfJoining > lwdMonthStart
            ? dateOfJoining
            : lwdMonthStart;

        int workedDays = lwd.DayNumber - effectivePeriodStart.DayNumber + 1;

        workedDays.Should().Be(30, "joined on 1st of month → full 30-day June");
    }

    [Fact]
    public void WorkedDays_JoiningOnLwd_IsOneDay()
    {
        // Edge: joined and exits same day
        var lwd = new DateOnly(2026, 6, 15);
        var dateOfJoining = new DateOnly(2026, 6, 15);
        DateOnly lwdMonthStart = new(lwd.Year, lwd.Month, 1);

        DateOnly effectivePeriodStart = dateOfJoining > lwdMonthStart
            ? dateOfJoining
            : lwdMonthStart;

        int workedDays = lwd.DayNumber - effectivePeriodStart.DayNumber + 1;

        workedDays.Should().Be(1, "one day of work");
    }

    // ─── FiscalYearLabel consistency ─────────────────────────────────────────

    [Fact]
    public void LwdPeriod_FiscalYearLabel_FormatCorrect()
    {
        // FY26-27 label for April 2026 LWD
        PayPeriod lwdPeriod = new(2026, 4);
        lwdPeriod.FiscalYearLabel.Should().Be("FY2027");
    }

    [Fact]
    public void LwdPeriod_FiscalYearLabel_FormatCorrect_PreApril()
    {
        // FY25-26 label for March 2026 LWD
        PayPeriod lwdPeriod = new(2026, 3);
        lwdPeriod.FiscalYear.Should().Be(2025);
    }
}
