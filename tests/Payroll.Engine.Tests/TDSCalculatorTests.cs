using FluentAssertions;
using Payroll.Engine.Calculators;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;
using Xunit;

namespace Payroll.Engine.Tests;

// FY 2026-27 New Regime (Section 115BAC) — Finance Act 2025
public sealed class TDSCalculatorTests
{
    private static readonly IReadOnlyList<TaxSlab> Slabs2627 =
    [
        new(0m,         400_000m,  0.00m),
        new(400_000m,   800_000m,  0.05m),
        new(800_000m,  1_200_000m, 0.10m),
        new(1_200_000m, 1_600_000m, 0.15m),
        new(1_600_000m, 2_000_000m, 0.20m),
        new(2_000_000m, 2_400_000m, 0.25m),
        new(2_400_000m, null,       0.30m),
    ];

    private static readonly IReadOnlyList<SurchargeConfig> Surcharge2627 =
    [
        new(5_000_000m,  10_000_000m, 0.10m),
        new(10_000_000m, 20_000_000m, 0.15m),
        new(20_000_000m, null,        0.25m),
    ];

    private static StatutoryConfig Config => new(
        NewRegimeSlabs: Slabs2627,
        SurchargeSlabs: Surcharge2627,
        StandardDeduction: 75_000m,
        Rebate87ALimit: 1_200_000m,
        Rebate87AAmount: 60_000m,
        CessRate: 0.04m,
        PFWageCap: 15_000m,
        EPFEmployeeRate: 0.12m,
        EPSEmployerRate: 0.0833m,
        EPSCap: 1_250m,
        EpfRestrictEmployerWage: false,
        EpfConsiderSalaryOnLop: false,
        EpfProRateRestrictedPfWage: false,
        ESIWageLimit: 21_000m,
        ESIPWDWageLimit: 25_000m,
        ESIEmployeeRate: 0.0075m,
        ESIEmployerRate: 0.0325m,
        PTSlabs: [],
        LWFStates: [],
        PFEnabled: false,
        ESIEnabled: false,
        PTEnabled: false,
        EpfIncludeEmployerInCtc: false,
        GratuityIncludedInCtc: false
    );

    [Fact]
    public void Income_BelowStandardDeduction_ReturnsZero()
    {
        TDSResult result = TDSCalculator.Compute(50_000m, 0m, 0m, hasPan: true, Config, monthsRemainingInFY: 12);

        result.MonthlyTDS.Should().Be(0m);
        result.AnnualProjectedTax.Should().Be(0m);
        result.TaxableIncome.Should().Be(0m);
        result.Rebate87AApplied.Should().BeFalse();
        result.HasPanOverride.Should().BeFalse();
    }

    [Fact]
    public void Income_At87AThreshold_FullRebateApplied_ZeroTax()
    {
        // gross 12,75,000 → taxable 12,00,000 → slab tax 60,000 → rebate 60,000 → net 0
        TDSResult result = TDSCalculator.Compute(12_75_000m, 0m, 0m, hasPan: true, Config, monthsRemainingInFY: 12);

        result.TaxableIncome.Should().Be(12_00_000m);
        result.TaxBeforeRebate.Should().Be(60_000m);
        result.Rebate87AApplied.Should().BeTrue();
        result.AnnualProjectedTax.Should().Be(0m);
        result.MonthlyTDS.Should().Be(0m);
    }

    [Fact]
    public void Income_OneRupeeAbove87AThreshold_NoRebate()
    {
        // gross 12,75,001 → taxable 12,00,001 → slab tax 60,000.15 → no rebate (>12L)
        // cess = 60,000.15 × 4% = 2,400.01
        // total = 62,400.16 → monthly/12 = 5,200.01
        TDSResult result = TDSCalculator.Compute(12_75_001m, 0m, 0m, hasPan: true, Config, monthsRemainingInFY: 12);

        result.TaxableIncome.Should().Be(12_00_001m);
        result.TaxBeforeRebate.Should().Be(60_000.15m);
        result.Rebate87AApplied.Should().BeFalse();
        result.Surcharge.Should().Be(0m);
        result.Cess.Should().Be(2_400.01m);
        result.AnnualProjectedTax.Should().Be(62_400.16m);
        result.MonthlyTDS.Should().Be(5_200.01m);
    }

    [Fact]
    public void Income_15L_Gross_CorrectSlabTax()
    {
        // gross 15,75,000 → taxable 15,00,000
        // 0+20000+40000+45000 = 1,05,000 | cess 4,200 | total 1,09,200 | monthly 9,100
        TDSResult result = TDSCalculator.Compute(15_75_000m, 0m, 0m, hasPan: true, Config, monthsRemainingInFY: 12);

        result.TaxableIncome.Should().Be(15_00_000m);
        result.TaxBeforeRebate.Should().Be(1_05_000m);
        result.Rebate87AApplied.Should().BeFalse();
        result.Surcharge.Should().Be(0m);
        result.Cess.Should().Be(4_200m);
        result.AnnualProjectedTax.Should().Be(1_09_200m);
        result.MonthlyTDS.Should().Be(9_100m);
    }

    [Fact]
    public void Section206AA_NoPan_TwentyPercentFlatRate()
    {
        // hasPan=false: 15,75,000 × 20% = 3,15,000 annual → 26,250/month
        // All slab/rebate/cess logic bypassed
        TDSResult result = TDSCalculator.Compute(15_75_000m, 0m, 0m, hasPan: false, Config, monthsRemainingInFY: 12);

        result.HasPanOverride.Should().BeTrue();
        result.AnnualProjectedTax.Should().Be(3_15_000m);
        result.MonthlyTDS.Should().Be(26_250m);
    }

    [Fact]
    public void Section206AA_NoPan_IsHigherThanSlabTax_SlabIgnored()
    {
        // Low income where slab would give 0 but §206AA gives 20%
        // gross 5,00,000 → slab would give 0 (87A rebate) → §206AA gives 5,00,000×20%=1,00,000
        TDSResult result = TDSCalculator.Compute(5_00_000m, 0m, 0m, hasPan: false, Config, monthsRemainingInFY: 12);

        result.HasPanOverride.Should().BeTrue();
        result.AnnualProjectedTax.Should().Be(1_00_000m);
        result.MonthlyTDS.Should().Be(8_333.33m); // 1,00,000/12 = 8,333.33
    }

    [Fact]
    public void PriorEmployerYTD_TaxableIncome_AddsToProjectedGross()
    {
        // current gross 10,75,000 → current taxable = 10,00,000 (alone, 87A rebate applies → 0 TDS)
        // prior employer taxable income = 2,00,000 → total projected = 12,75,000 → taxable 12,00,000
        // 87A rebate still applies (12L ≤ 12L limit) → 0 TDS
        TDSResult result = TDSCalculator.Compute(10_75_000m, 2_00_000m, 0m, hasPan: true, Config, monthsRemainingInFY: 12);

        result.TaxableIncome.Should().Be(12_00_000m);
        result.Rebate87AApplied.Should().BeTrue();
        result.MonthlyTDS.Should().Be(0m);
    }

    [Fact]
    public void PriorEmployerYTD_TaxableIncome_PushesAbove87AThreshold()
    {
        // current gross 10,75,001 + prior 2,00,000 = total 12,75,001 → taxable 12,00,001 → no rebate
        // TaxBeforeRebate = 60,000.15, cess = 2,400.01, total = 62,400.16 → monthly/12 = 5,200.01
        TDSResult result = TDSCalculator.Compute(10_75_001m, 2_00_000m, 0m, hasPan: true, Config, monthsRemainingInFY: 12);

        result.TaxableIncome.Should().Be(12_00_001m);
        result.Rebate87AApplied.Should().BeFalse();
        result.AnnualProjectedTax.Should().Be(62_400.16m);
        result.MonthlyTDS.Should().Be(5_200.01m);
    }

    [Fact]
    public void PriorEmployerYTD_ReducesRemainingTax()
    {
        // total annual tax 1,09,200, priorYTD 50,000 → remaining 59,200 → /12 = 4,933.33
        TDSResult result = TDSCalculator.Compute(15_75_000m, 0m, priorEmployerYTDTDSDeducted: 50_000m, hasPan: true, Config, monthsRemainingInFY: 12);

        result.AnnualProjectedTax.Should().Be(1_09_200m);
        result.MonthlyTDS.Should().Be(4_933.33m);
    }

    [Fact]
    public void PriorEmployerYTD_ExceedsTotalTax_ZeroMonthlyTDS()
    {
        // priorYTD 2,00,000 > totalAnnualTax 1,09,200 → remaining is negative → clamp to 0
        TDSResult result = TDSCalculator.Compute(15_75_000m, 0m, priorEmployerYTDTDSDeducted: 2_00_000m, hasPan: true, Config, monthsRemainingInFY: 12);

        result.AnnualProjectedTax.Should().Be(1_09_200m);
        result.MonthlyTDS.Should().Be(0m);
    }

    [Fact]
    public void MonthsRemaining_6_DoublesTDSVsFullYear()
    {
        // total 1,09,200 / 6 = 18,200
        TDSResult result = TDSCalculator.Compute(15_75_000m, 0m, 0m, hasPan: true, Config, monthsRemainingInFY: 6);

        result.MonthlyTDS.Should().Be(18_200m);
        result.AnnualProjectedTax.Should().Be(1_09_200m);
    }

    [Fact]
    public void MonthsRemaining_Zero_ReturnsZeroMonthlyTDS()
    {
        TDSResult result = TDSCalculator.Compute(15_75_000m, 0m, 0m, hasPan: true, Config, monthsRemainingInFY: 0);

        result.MonthlyTDS.Should().Be(0m);
        result.AnnualProjectedTax.Should().Be(1_09_200m);
    }

    [Fact]
    public void Surcharge_10Pct_For_IncomeBetween50L_And_1Cr()
    {
        // gross 60,75,000 → taxable 60,00,000
        // slab tax: 0+20000+40000+60000+80000+100000+1080000 = 13,80,000
        // surcharge 10%: 1,38,000 (no marginal relief — income well above 50L)
        // cess: (13,80,000+1,38,000)×4% = 60,720
        // total: 15,78,720
        TDSResult result = TDSCalculator.Compute(60_75_000m, 0m, 0m, hasPan: true, Config, monthsRemainingInFY: 12);

        result.TaxableIncome.Should().Be(60_00_000m);
        result.TaxBeforeRebate.Should().Be(13_80_000m);
        result.Surcharge.Should().Be(1_38_000m);
        result.Cess.Should().Be(60_720m);
        result.AnnualProjectedTax.Should().Be(15_78_720m);
    }

    [Fact]
    public void MarginalRelief_IncomeOneRupeeAbove50LThreshold()
    {
        // taxable 50,00,001 (gross 50,75,001)
        // slab tax = 0+20000+40000+60000+80000+100000+(26L+1)×30% = 10,80,000.30
        // surcharge without relief = 10% × 10,80,000.30 = 1,08,000.03
        // tax+surcharge without relief = 11,88,000.33
        // marginal relief limit = taxAt50L + 1 = 10,80,000 + 1 = 10,80,001
        // 11,88,000.33 > 10,80,001 → relief applies
        // effective surcharge = 10,80,001 - 10,80,000.30 = 0.70
        TDSResult result = TDSCalculator.Compute(50_75_001m, 0m, 0m, hasPan: true, Config, monthsRemainingInFY: 12);

        result.TaxableIncome.Should().Be(50_00_001m);
        result.TaxBeforeRebate.Should().Be(10_80_000.30m);
        result.Surcharge.Should().Be(0.70m);
    }
}
