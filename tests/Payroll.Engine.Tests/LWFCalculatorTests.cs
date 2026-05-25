using FluentAssertions;
using Payroll.Engine.Calculators;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;
using Payroll.Engine.Tests.TestData;
using Xunit;

namespace Payroll.Engine.Tests;

// Direct unit tests for LWFCalculator. Same motivation as PT — state matrix +
// frequency rules + wage threshold + percentage-vs-flat all need explicit coverage,
// otherwise a state slab change can ship with green tests.
public class LWFCalculatorTests
{
    private static StatutoryConfig WithLwf(IReadOnlyList<LwfStateInput> states)
    {
        StatutoryConfig baseConfig = StatutoryTestData.DefaultConfig_FY2026();
        return baseConfig with { LWFStates = states };
    }

    private static PayrollRunInput Run(int month) =>
        new(Year: 2025, Month: month, CalendarDaysInMonth: 30, SalaryDivisor: 30,
            MonthsRemainingInFY: 12 - month + 4, FiscalYearLabel: "FY2025-26");

    // ── State coverage ────────────────────────────────────────────────────────

    [Fact]
    public void NoConfigForState_ReturnsExempt()
    {
        // No state matches — config covers MH, employee in DL.
        LwfStateInput mh = new("MH", 25m, 75m, IsPercentageBased: false,
            EmployeeRate: null, EmployerRate: null, RateCapEmployee: null, RateCapEmployer: null,
            Frequency: "Monthly", DeductionMonth: null, WageThreshold: null);
        LWFResult result = LWFCalculator.Compute("DL", 30_000m, WithLwf([mh]), Run(5));
        result.EmployeeAmount.Should().Be(0m);
        result.EmployerAmount.Should().Be(0m);
        result.IsExempt.Should().BeTrue();
    }

    // ── Wage threshold ────────────────────────────────────────────────────────

    [Fact]
    public void WageAboveThreshold_ReturnsExempt()
    {
        // KA: applies only when gross <= 15000.
        LwfStateInput ka = new("KA", 20m, 40m, IsPercentageBased: false,
            EmployeeRate: null, EmployerRate: null, RateCapEmployee: null, RateCapEmployer: null,
            Frequency: "HalfYearly", DeductionMonth: null, WageThreshold: 15_000m);

        LWFCalculator.Compute("KA", 16_000m, WithLwf([ka]), Run(6)).IsExempt.Should().BeTrue();
        LWFCalculator.Compute("KA", 15_000m, WithLwf([ka]), Run(6)).EmployeeAmount.Should().Be(20m);
    }

    // ── Monthly frequency ─────────────────────────────────────────────────────

    [Fact]
    public void MonthlyFrequency_DeductsEveryMonth()
    {
        LwfStateInput mh = new("MH", 25m, 75m, IsPercentageBased: false,
            EmployeeRate: null, EmployerRate: null, RateCapEmployee: null, RateCapEmployer: null,
            Frequency: "Monthly", DeductionMonth: null, WageThreshold: null);

        foreach (int month in new[] { 4, 5, 7, 12, 1, 3 })
        {
            LWFResult r = LWFCalculator.Compute("MH", 30_000m, WithLwf([mh]), Run(month));
            r.EmployeeAmount.Should().Be(25m);
            r.EmployerAmount.Should().Be(75m);
        }
    }

    // ── Annual frequency ──────────────────────────────────────────────────────

    [Fact]
    public void AnnualFrequency_DeductsOnlyInDesignatedMonth()
    {
        // GJ deducts in December only.
        LwfStateInput gj = new("GJ", 6m, 12m, IsPercentageBased: false,
            EmployeeRate: null, EmployerRate: null, RateCapEmployee: null, RateCapEmployer: null,
            Frequency: "Annual", DeductionMonth: 12, WageThreshold: null);

        LWFCalculator.Compute("GJ", 25_000m, WithLwf([gj]), Run(12)).EmployeeAmount.Should().Be(6m);
        LWFCalculator.Compute("GJ", 25_000m, WithLwf([gj]), Run(11)).EmployeeAmount.Should().Be(0m);
        LWFCalculator.Compute("GJ", 25_000m, WithLwf([gj]), Run(11)).IsExempt.Should().BeFalse();
    }

    [Fact]
    public void AnnualFrequency_NoDeductionMonth_NeverDeducts()
    {
        // Misconfigured: Annual frequency but DeductionMonth = null.
        LwfStateInput bad = new("GJ", 6m, 12m, IsPercentageBased: false,
            EmployeeRate: null, EmployerRate: null, RateCapEmployee: null, RateCapEmployer: null,
            Frequency: "Annual", DeductionMonth: null, WageThreshold: null);

        LWFCalculator.Compute("GJ", 25_000m, WithLwf([bad]), Run(12)).EmployeeAmount.Should().Be(0m);
    }

    // ── HalfYearly frequency (June + December) ───────────────────────────────

    [Theory]
    [InlineData(6, true)]
    [InlineData(12, true)]
    [InlineData(4, false)]
    [InlineData(7, false)]
    [InlineData(1, false)]
    public void HalfYearlyFrequency_DeductsInJuneAndDecemberOnly(int month, bool expectDeduction)
    {
        LwfStateInput ka = new("KA", 20m, 40m, IsPercentageBased: false,
            EmployeeRate: null, EmployerRate: null, RateCapEmployee: null, RateCapEmployer: null,
            Frequency: "HalfYearly", DeductionMonth: null, WageThreshold: null);

        LWFResult r = LWFCalculator.Compute("KA", 12_000m, WithLwf([ka]), Run(month));
        if (expectDeduction)
        {
            r.EmployeeAmount.Should().Be(20m);
            r.EmployerAmount.Should().Be(40m);
        }
        else
        {
            r.EmployeeAmount.Should().Be(0m);
            r.IsExempt.Should().BeFalse();
        }
    }

    // ── Percentage-based + rate cap ──────────────────────────────────────────

    [Fact]
    public void PercentageBased_ComputesRateThenCaps()
    {
        // WB: employee = 0.5% of gross capped at 30; employer = 1.5% capped at 100.
        // Gross 8000 → emp 40 → cap 30; employer 120 → cap 100.
        LwfStateInput wb = new("WB", 0m, 0m, IsPercentageBased: true,
            EmployeeRate: 0.005m, EmployerRate: 0.015m, RateCapEmployee: 30m, RateCapEmployer: 100m,
            Frequency: "Monthly", DeductionMonth: null, WageThreshold: null);

        LWFResult r = LWFCalculator.Compute("WB", 8_000m, WithLwf([wb]), Run(5));
        r.EmployeeAmount.Should().Be(30m);
        r.EmployerAmount.Should().Be(100m);
    }

    [Fact]
    public void PercentageBased_BelowCap_UsesComputedAmount()
    {
        // Same WB config, gross small enough that neither side hits the cap.
        // Gross 3000 → emp 15 (< 30), employer 45 (< 100).
        LwfStateInput wb = new("WB", 0m, 0m, IsPercentageBased: true,
            EmployeeRate: 0.005m, EmployerRate: 0.015m, RateCapEmployee: 30m, RateCapEmployer: 100m,
            Frequency: "Monthly", DeductionMonth: null, WageThreshold: null);

        LWFResult r = LWFCalculator.Compute("WB", 3_000m, WithLwf([wb]), Run(5));
        r.EmployeeAmount.Should().Be(15m);
        r.EmployerAmount.Should().Be(45m);
    }

    [Fact]
    public void PercentageBased_NoCap_UsesComputedAmount()
    {
        LwfStateInput state = new("MH", 0m, 0m, IsPercentageBased: true,
            EmployeeRate: 0.01m, EmployerRate: 0.02m, RateCapEmployee: null, RateCapEmployer: null,
            Frequency: "Monthly", DeductionMonth: null, WageThreshold: null);

        LWFResult r = LWFCalculator.Compute("MH", 10_000m, WithLwf([state]), Run(5));
        r.EmployeeAmount.Should().Be(100m);
        r.EmployerAmount.Should().Be(200m);
    }

    [Fact]
    public void PercentageBased_NullRate_ContributesZero()
    {
        // Employer-only LWF state: employee rate null → 0; employer rate set.
        LwfStateInput state = new("AP", 0m, 0m, IsPercentageBased: true,
            EmployeeRate: null, EmployerRate: 0.02m, RateCapEmployee: null, RateCapEmployer: null,
            Frequency: "Monthly", DeductionMonth: null, WageThreshold: null);

        LWFResult r = LWFCalculator.Compute("AP", 10_000m, WithLwf([state]), Run(5));
        r.EmployeeAmount.Should().Be(0m);
        r.EmployerAmount.Should().Be(200m);
    }

    // ── Unknown frequency string → no deduction ──────────────────────────────

    [Fact]
    public void UnknownFrequency_FallsThroughToZero()
    {
        LwfStateInput weird = new("MH", 25m, 75m, IsPercentageBased: false,
            EmployeeRate: null, EmployerRate: null, RateCapEmployee: null, RateCapEmployer: null,
            Frequency: "Quarterly", DeductionMonth: null, WageThreshold: null);

        LWFCalculator.Compute("MH", 20_000m, WithLwf([weird]), Run(6)).EmployeeAmount.Should().Be(0m);
    }
}
