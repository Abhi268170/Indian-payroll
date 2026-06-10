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
        // ECR whole-rupee rounding: 15000 × 8.33% = 1249.50 → 1250 (≤ ₹1,250 cap)
        result.EPSEmployerContribution.Should().Be(1250m);
        result.EPFEmployerContribution.Should().Be(550m); // 1800 − 1250
    }

    [Fact]
    public void AboveCap_Unrestricted_EmployerUsesActualWage()
    {
        // PF wage = 30,000; unrestricted — employer uses 30,000 but EPS ceiling still ₹15,000
        // EPS = min(15000 × 8.33%, 1250) = 1249.50
        var result = PFCalculator.Compute(30000m, 30000m, 0m, 26, UnrestrictedConfig(), optOut: false);

        // ECR whole-rupee rounding: EPS 1249.50 → 1250; employer EPF = 3600 − 1250
        result.EPFEmployerContribution.Should().Be(2350m);
        result.EPSEmployerContribution.Should().Be(1250m);
    }

    // ── LOP: EpfConsiderSalaryOnLop = true ───────────────────────────────────

    [Fact]
    public void Lop_ConsiderSalaryOnLop_True_UsesProrated()
    {
        // pfWage prorated (2 LOP out of 26 days) = 24/26 × 15000 ≈ 13846.15
        decimal prorated = Math.Round(15000m * 24m / 26m, 2, MidpointRounding.AwayFromZero);
        var result = PFCalculator.Compute(prorated, 15000m, lopDays: 2m, baseDays: 26, RestrictedConfig(considerSalaryOnLop: true), optOut: false);

        // 13,846.15 × 12% = 1,661.538 → whole-rupee 1,662
        result.EmployeeContribution.Should().Be(1662m);
    }

    [Fact]
    public void Lop_ConsiderSalaryOnLop_False_UsesFullStructure()
    {
        // Even with LOP, employee PF uses full structure amount
        decimal prorated = Math.Round(15000m * 24m / 26m, 2, MidpointRounding.AwayFromZero);
        var result = PFCalculator.Compute(prorated, 15000m, lopDays: 2m, baseDays: 26, RestrictedConfig(considerSalaryOnLop: false), optOut: false);

        result.EmployeeContribution.Should().Be(1800m);
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

        // ECR whole-rupee: EPS = round(13,846.15 × 8.33%) = round(1,153.38) = 1,153
        // employer EPF = round(13,846.15 × 12%) − 1,153 = 1,662 − 1,153 = 509
        decimal expectedEps = Math.Min(Math.Round(proratedCap * 0.0833m, 0, MidpointRounding.AwayFromZero), 1250m);
        decimal expectedEpfEmployer = Math.Round(proratedCap * 0.12m, 0, MidpointRounding.AwayFromZero) - expectedEps;
        result.EPFEmployerContribution.Should().Be(expectedEpfEmployer);
    }

    // ── EDLI + admin charges (employer-side) ─────────────────────────────────

    private static StatutoryConfig WithEdli(StatutoryConfig c) => c with
    {
        EdliRate = 0.005m,
        EdliWageCap = 15_000m,
        EdliMaxAmount = 75m,
        EpfAdminRate = 0.005m,
    };

    [Fact]
    public void Edli_BelowWageCap_HalfPercentOfPfWage()
    {
        var result = PFCalculator.Compute(10000m, 10000m, 0m, 26, WithEdli(RestrictedConfig()), optOut: false);
        result.EdliCharge.Should().Be(50m);   // 10,000 × 0.5%
        result.AdminCharge.Should().Be(50m);  // 10,000 × 0.5%
    }

    [Fact]
    public void Edli_AboveWageCap_CappedAt15kWageAndMaxAmount()
    {
        // EDLI wage capped at 15,000 even when employer wage is unrestricted.
        var result = PFCalculator.Compute(30000m, 30000m, 0m, 26, WithEdli(UnrestrictedConfig()), optOut: false);
        result.EdliCharge.Should().Be(75m);    // min(15,000 × 0.5%, 75)
        result.AdminCharge.Should().Be(150m);  // admin on employer PF wage 30,000
    }

    [Fact]
    public void Edli_ZeroRates_NoCharges()
    {
        var result = PFCalculator.Compute(10000m, 10000m, 0m, 26, RestrictedConfig(), optOut: false);
        result.EdliCharge.Should().Be(0m);
        result.AdminCharge.Should().Be(0m);
    }

    [Fact]
    public void Edli_OptOut_NoCharges()
    {
        var result = PFCalculator.Compute(10000m, 10000m, 0m, 26, WithEdli(RestrictedConfig()), optOut: true);
        result.EdliCharge.Should().Be(0m);
        result.AdminCharge.Should().Be(0m);
    }

    // ── VPF (percent of employee PF wage) ────────────────────────────────────

    [Fact]
    public void Vpf_IsPercentOfEmployeePfWage_NotARupeeAmount()
    {
        // 5 means 5% → 10,000 × 5% = 500
        var result = PFCalculator.Compute(10000m, 10000m, 0m, 26, RestrictedConfig(), optOut: false, vpfPercent: 5m);
        result.VPFContribution.Should().Be(500m);
    }

    [Fact]
    public void Vpf_OnCappedWage_WhenRestricted()
    {
        // wage 30,000 restricted to 15,000 → VPF 10% = 1,500
        var result = PFCalculator.Compute(30000m, 30000m, 0m, 26, RestrictedConfig(), optOut: false, vpfPercent: 10m);
        result.VPFContribution.Should().Be(1500m);
    }

    [Fact]
    public void Contributions_AreWholeRupees()
    {
        // 12,345 × 12% = 1,481.40 → 1,481; EPS 12,345 × 8.33% = 1,028.34 → 1,028
        var result = PFCalculator.Compute(12345m, 12345m, 0m, 26, RestrictedConfig(), optOut: false);
        result.EmployeeContribution.Should().Be(1481m);
        result.EPSEmployerContribution.Should().Be(1028m);
        result.EPFEmployerContribution.Should().Be(453m); // 1481 − 1028
    }
}
