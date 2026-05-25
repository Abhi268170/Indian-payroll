using FluentAssertions;
using Payroll.Engine.Calculators;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;
using Payroll.Engine.Tests.TestData;
using Xunit;

namespace Payroll.Engine.Tests;

// Direct unit tests for ESICalculator. Wiring tests cover the upstream wage
// filtering (which components feed ESI). These tests cover the calculator
// itself: wage cap (21k), PWD wage cap (25k), exempt flag, ESIEnabled toggle,
// and the rate math.
public class ESICalculatorTests
{
    private static StatutoryConfig Default => StatutoryTestData.DefaultConfig_FY2026();

    [Fact]
    public void EsiDisabled_ReturnsExempt()
    {
        StatutoryConfig disabled = Default with { ESIEnabled = false };
        ESIResult r = ESICalculator.Compute(15_000m, disabled, isExempt: false, isPWD: false);
        r.EmployeeContribution.Should().Be(0m);
        r.EmployerContribution.Should().Be(0m);
        r.IsExempt.Should().BeTrue();
    }

    [Fact]
    public void IsExemptTrue_ReturnsZeroEvenBelowLimit()
    {
        ESIResult r = ESICalculator.Compute(10_000m, Default, isExempt: true, isPWD: false);
        r.EmployeeContribution.Should().Be(0m);
        r.IsExempt.Should().BeTrue();
    }

    // ── Wage limit (21,000) — non-PWD ────────────────────────────────────────

    [Fact]
    public void WageAtLimit_StillCovered()
    {
        // Standard wage limit is 21000 inclusive — exactly-at-limit must still
        // be calculated, only strictly-above tips to exempt.
        ESIResult r = ESICalculator.Compute(21_000m, Default, isExempt: false, isPWD: false);
        r.EmployeeContribution.Should().Be(Math.Round(21_000m * Default.ESIEmployeeRate, 2, MidpointRounding.AwayFromZero));
        r.IsExempt.Should().BeFalse();
    }

    [Fact]
    public void WageAboveLimit_NonPwd_ReturnsExempt()
    {
        ESIResult r = ESICalculator.Compute(21_001m, Default, isExempt: false, isPWD: false);
        r.EmployeeContribution.Should().Be(0m);
        r.EmployerContribution.Should().Be(0m);
        r.IsExempt.Should().BeTrue();
    }

    // ── PWD wage limit (25,000) ──────────────────────────────────────────────

    [Fact]
    public void PwdEmployee_GetsHigherWageLimit()
    {
        // Non-PWD at 22k → exempt. PWD at same 22k → covered.
        ESICalculator.Compute(22_000m, Default, isExempt: false, isPWD: false).IsExempt.Should().BeTrue();
        ESICalculator.Compute(22_000m, Default, isExempt: false, isPWD: true).IsExempt.Should().BeFalse();
    }

    [Fact]
    public void PwdEmployee_AboveExpandedLimit_ReturnsExempt()
    {
        ESICalculator.Compute(25_001m, Default, isExempt: false, isPWD: true).IsExempt.Should().BeTrue();
    }

    // ── Rate math ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(10_000, 75,    325)]    // 10000 × 0.75% = 75; 10000 × 3.25% = 325
    [InlineData(15_000, 112.5, 487.5)]  // 15000 × 0.75% = 112.50; 15000 × 3.25% = 487.50
    [InlineData(20_000, 150,   650)]
    public void StandardRates_AppliedToFullWage(decimal wage, decimal expEmp, decimal expEmpr)
    {
        ESIResult r = ESICalculator.Compute(wage, Default, isExempt: false, isPWD: false);
        r.EmployeeContribution.Should().Be(expEmp);
        r.EmployerContribution.Should().Be(expEmpr);
    }

    [Fact]
    public void Rounding_HalfAwayFromZero()
    {
        // Wage 13_133 → employee 13133 × 0.0075 = 98.4975 → 98.50
        ESIResult r = ESICalculator.Compute(13_133m, Default, isExempt: false, isPWD: false);
        r.EmployeeContribution.Should().Be(98.50m);
    }

    [Fact]
    public void ZeroWage_ZeroContributions_NotExempt()
    {
        // 0 ≤ limit → not exempt, contributions = 0.
        ESIResult r = ESICalculator.Compute(0m, Default, isExempt: false, isPWD: false);
        r.EmployeeContribution.Should().Be(0m);
        r.EmployerContribution.Should().Be(0m);
        r.IsExempt.Should().BeFalse();
    }
}
