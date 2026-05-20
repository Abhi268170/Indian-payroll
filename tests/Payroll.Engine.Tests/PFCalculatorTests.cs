using FluentAssertions;
using Payroll.Engine.Calculators;
using Payroll.Engine.Inputs;
using Xunit;

namespace Payroll.Engine.Tests;

public class PFCalculatorTests
{
    // Config: restricted employer wage (cap ₹15,000), LOP uses prorated wage, no pro-rate of cap
    private static StatutoryConfig RestrictedConfig(
        bool considerSalaryOnLop = true,
        bool proRateRestrictedPfWage = false) =>
        TestData.StatutoryTestData.DefaultConfig_FY2026() with
        {
            EpfRestrictEmployerWage = true,
            EpfConsiderSalaryOnLop = considerSalaryOnLop,
            EpfProRateRestrictedPfWage = proRateRestrictedPfWage
        };

    // Config: unrestricted employer wage (actual PF wage, no cap)
    private static StatutoryConfig UnrestrictedConfig() =>
        TestData.StatutoryTestData.DefaultConfig_FY2026() with
        {
            EpfRestrictEmployerWage = false,
            EpfConsiderSalaryOnLop = true,
            EpfProRateRestrictedPfWage = false
        };

    // ── Opt-out ────────────────────────────────────────────────────────────────

    [Fact]
    public void OptOut_ReturnsAllZero_IsExemptTrue()
    {
        var result = PFCalculator.Compute(20000m, 20000m, 0m, 26, RestrictedConfig(), optOut: true);
        result.EmployeeContribution.Should().Be(0m);
        result.EPFEmployerContribution.Should().Be(0m);
        result.EPSEmployerContribution.Should().Be(0m);
        result.EDLIEmployerContribution.Should().Be(0m);
        result.EPFAdminContribution.Should().Be(0m);
        result.IsExempt.Should().BeTrue();
    }

    // ── Below wage cap (₹15,000) ──────────────────────────────────────────────

    [Fact]
    public void BelowCap_Employee12Percent()
    {
        // PF wage = 10,000 < 15,000 cap
        var result = PFCalculator.Compute(10000m, 10000m, 0m, 26, RestrictedConfig(), optOut: false);
        result.EmployeeContribution.Should().Be(1200m);  // 12% × 10,000
    }

    [Fact]
    public void BelowCap_EPSIs8Point33PercentCappedAt1250()
    {
        var result = PFCalculator.Compute(10000m, 10000m, 0m, 26, RestrictedConfig(), optOut: false);
        decimal expectedEps = Math.Round(10000m * 0.0833m, 2, MidpointRounding.AwayFromZero); // 833
        result.EPSEmployerContribution.Should().Be(expectedEps);
    }

    [Fact]
    public void BelowCap_EPFEmployerIs12PercentMinusEPS()
    {
        var result = PFCalculator.Compute(10000m, 10000m, 0m, 26, RestrictedConfig(), optOut: false);
        decimal eps = Math.Round(10000m * 0.0833m, 2, MidpointRounding.AwayFromZero);
        decimal expectedEpfEmployer = Math.Round(10000m * 0.12m, 2, MidpointRounding.AwayFromZero) - eps;
        result.EPFEmployerContribution.Should().Be(expectedEpfEmployer);
    }

    // ── Above wage cap — restricted employer ─────────────────────────────────

    [Fact]
    public void AboveCap_Restricted_BothEmployeeAndEmployerCappedAt15000()
    {
        // PF wage = 30,000; restricted cap = 15,000 — both employee and employer capped
        // EPS = min(15000 × 8.33%, 1250) = min(1249.50, 1250) = 1249.50
        var result = PFCalculator.Compute(30000m, 30000m, 0m, 26, RestrictedConfig(), optOut: false);

        result.EmployeeContribution.Should().Be(1800m); // 12% × 15,000 (capped)
        decimal eps = Math.Round(15000m * 0.0833m, 2, MidpointRounding.AwayFromZero); // 1249.50
        result.EPSEmployerContribution.Should().Be(eps);
        decimal expectedEpfEmployer = Math.Round(15000m * 0.12m, 2, MidpointRounding.AwayFromZero) - eps; // 1800 - 1249.50 = 550.50
        result.EPFEmployerContribution.Should().Be(expectedEpfEmployer);
    }

    [Fact]
    public void AboveCap_Unrestricted_EmployerUsesActualWage()
    {
        // PF wage = 30,000; unrestricted — employer uses 30,000 but EPS ceiling still ₹15,000
        // EPS = min(15000 × 8.33%, 1250) = 1249.50
        var result = PFCalculator.Compute(30000m, 30000m, 0m, 26, UnrestrictedConfig(), optOut: false);

        decimal eps = Math.Round(15000m * 0.0833m, 2, MidpointRounding.AwayFromZero); // 1249.50
        decimal expectedEpfEmployer = Math.Round(30000m * 0.12m, 2, MidpointRounding.AwayFromZero) - eps; // 3600 - 1249.50 = 2350.50
        result.EPFEmployerContribution.Should().Be(expectedEpfEmployer);
        result.EPSEmployerContribution.Should().Be(eps);
    }

    // ── EDLI ──────────────────────────────────────────────────────────────────

    [Fact]
    public void EDLI_CappedAt75WhenWageAboveCap()
    {
        var result = PFCalculator.Compute(30000m, 30000m, 0m, 26, RestrictedConfig(), optOut: false);
        result.EDLIEmployerContribution.Should().Be(75m); // 0.5% × 15,000 = 75
    }

    [Fact]
    public void EDLI_ProportionalWhenWageBelowCap()
    {
        var result = PFCalculator.Compute(10000m, 10000m, 0m, 26, RestrictedConfig(), optOut: false);
        result.EDLIEmployerContribution.Should().Be(Math.Round(10000m * 0.005m, 2, MidpointRounding.AwayFromZero)); // 50
    }

    // ── Admin ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Admin_MinimumFloorIs500()
    {
        // PF wage 1,000 → 0.5% = 5 → floor to 500
        var result = PFCalculator.Compute(1000m, 1000m, 0m, 26, RestrictedConfig(), optOut: false);
        result.EPFAdminContribution.Should().Be(500m);
    }

    // ── LOP: EpfConsiderSalaryOnLop = true ───────────────────────────────────

    [Fact]
    public void Lop_ConsiderSalaryOnLop_True_UsesProrated()
    {
        // pfWage prorated (2 LOP out of 26 days) = 24/26 × 15000 ≈ 13846.15
        decimal prorated = Math.Round(15000m * 24m / 26m, 2, MidpointRounding.AwayFromZero);
        var result = PFCalculator.Compute(prorated, 15000m, lopDays: 2m, baseDays: 26, RestrictedConfig(considerSalaryOnLop: true), optOut: false);

        result.EmployeeContribution.Should().Be(Math.Round(prorated * 0.12m, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void Lop_ConsiderSalaryOnLop_False_UsesFullStructure()
    {
        // Even with LOP, employee PF uses full structure amount
        decimal prorated = Math.Round(15000m * 24m / 26m, 2, MidpointRounding.AwayFromZero);
        var result = PFCalculator.Compute(prorated, 15000m, lopDays: 2m, baseDays: 26, RestrictedConfig(considerSalaryOnLop: false), optOut: false);

        result.EmployeeContribution.Should().Be(Math.Round(15000m * 0.12m, 2, MidpointRounding.AwayFromZero)); // 1800
    }

    // ── Pro-rate restricted PF wage cap ──────────────────────────────────────

    [Fact]
    public void ProRateRestrictedPfWage_ProratesCapByPaidDays()
    {
        // Wage = 20,000 > cap; 2 LOP out of 26 days
        // Prorated cap = 15,000 × 24/26 ≈ 13,846.15
        // Employer wage = min(prorated wage, prorated cap)
        // prorated wage = 20,000 × 24/26 ≈ 18,461.54
        // employer wage = min(18461.54, 13846.15) = 13,846.15
        decimal proratedPfWage = Math.Round(20000m * 24m / 26m, 2, MidpointRounding.AwayFromZero);
        decimal proratedCap = Math.Round(15000m * 24m / 26m, 2, MidpointRounding.AwayFromZero);

        var result = PFCalculator.Compute(proratedPfWage, 20000m, lopDays: 2m, baseDays: 26,
            RestrictedConfig(considerSalaryOnLop: true, proRateRestrictedPfWage: true), optOut: false);

        decimal expectedEps = Math.Min(Math.Round(proratedCap * 0.0833m, 2, MidpointRounding.AwayFromZero), 1250m);
        decimal expectedEpfEmployer = Math.Round(proratedCap * 0.12m, 2, MidpointRounding.AwayFromZero) - expectedEps;
        result.EPFEmployerContribution.Should().Be(expectedEpfEmployer);
    }
}
