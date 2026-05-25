using FluentAssertions;
using Payroll.Engine.Calculators;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;
using Payroll.Engine.Tests.TestData;
using Xunit;

namespace Payroll.Engine.Tests;

// Direct unit tests for PTCalculator. Until this file, PT was only exercised
// indirectly via PayrollEngineWiringTests with empty slabs — so state slab
// regressions, frequency rules, deduction-month filters, and Kerala-style
// half-yearly-split rounding could all slip through silently.
public class PTCalculatorTests
{
    private static readonly DateOnly FromApr2025 = new(2025, 4, 1);

    private static StatutoryConfig WithPt(IReadOnlyList<PTSlab> slabs, bool ptEnabled = true)
    {
        StatutoryConfig baseConfig = StatutoryTestData.DefaultConfig_FY2026();
        return baseConfig with { PTSlabs = slabs, PTEnabled = ptEnabled };
    }

    private static EmployeeInput MakeEmployee(
        string stateCode = "MH",
        int halfYearMonthIndex = 1,
        int halfYearTotalMonths = 6) =>
        new(
            EmployeeId: Guid.NewGuid(),
            EmployeeCode: "EMP001",
            WorkStateCode: stateCode,
            EpfEnabled: false,
            IsESIExempt: true,
            IsPWD: false,
            MonthlyCTC: 50000m,
            Components: [],
            LOPDays: 0,
            WorkingDaysInMonth: 30,
            VPFAmount: 0,
            PriorEmployerYTDTaxableIncome: 0,
            PriorEmployerYTDTDSDeducted: 0,
            PriorEmployerYTDPF: 0,
            HalfYearMonthIndex: halfYearMonthIndex,
            HalfYearTotalMonths: halfYearTotalMonths);

    private static PayrollRunInput Run(int month) =>
        new(Year: 2025, Month: month, CalendarDaysInMonth: 30, SalaryDivisor: 30,
            MonthsRemainingInFY: 12 - month + 4, FiscalYearLabel: "FY2025-26");

    // ── Global toggle ─────────────────────────────────────────────────────────

    [Fact]
    public void PTEnabledFalse_AlwaysReturnsExempt()
    {
        PTSlab slab = new("MH", 10_001m, null, 200m, FromApr2025, "Monthly", []);
        PTResult result = PTCalculator.Compute(25_000m, MakeEmployee("MH"), WithPt([slab], ptEnabled: false), Run(5));
        result.Amount.Should().Be(0m);
        result.IsExempt.Should().BeTrue();
    }

    // ── No state coverage ─────────────────────────────────────────────────────

    [Fact]
    public void NoSlabForState_ReturnsExempt()
    {
        // PT slabs exist for MH but employee is in RJ (no PT state).
        PTSlab mhSlab = new("MH", 10_001m, null, 200m, FromApr2025, "Monthly", []);
        PTResult result = PTCalculator.Compute(25_000m, MakeEmployee("RJ"), WithPt([mhSlab]), Run(5));
        result.Amount.Should().Be(0m);
        result.IsExempt.Should().BeTrue();
    }

    [Fact]
    public void GrossBelowAllSlabs_ReturnsExempt()
    {
        // MH lowest slab starts at 7501.
        PTSlab slab = new("MH", 7_501m, 10_000m, 175m, FromApr2025, "Monthly", []);
        PTResult result = PTCalculator.Compute(5_000m, MakeEmployee("MH"), WithPt([slab]), Run(5));
        result.IsExempt.Should().BeTrue();
    }

    // ── Monthly frequency ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("MH", 8_000, 175)]   // 7501–10000 bracket
    [InlineData("MH", 12_000, 200)]  // > 10000 bracket
    [InlineData("KA", 25_001, 200)]  // Karnataka single-slab
    public void Monthly_ReturnsBracketAmount(string state, decimal gross, decimal expected)
    {
        PTSlab[] slabs =
        [
            new("MH", 7_501m, 10_000m, 175m, FromApr2025, "Monthly", []),
            new("MH", 10_001m, null,    200m, FromApr2025, "Monthly", []),
            new("KA", 25_001m, null,    200m, FromApr2025, "Monthly", []),
        ];
        PTResult result = PTCalculator.Compute(gross, MakeEmployee(state), WithPt(slabs), Run(5));
        result.Amount.Should().Be(expected);
        result.IsExempt.Should().BeFalse();
    }

    [Fact]
    public void Monthly_BracketBoundary_LowerInclusive()
    {
        // SalaryFrom = 10001 → exactly 10001 lands in upper bracket (not lower).
        PTSlab[] slabs =
        [
            new("MH", 7_501m,  10_000m, 175m, FromApr2025, "Monthly", []),
            new("MH", 10_001m, null,    200m, FromApr2025, "Monthly", []),
        ];
        PTCalculator.Compute(10_001m, MakeEmployee("MH"), WithPt(slabs), Run(5)).Amount.Should().Be(200m);
        PTCalculator.Compute(10_000m, MakeEmployee("MH"), WithPt(slabs), Run(5)).Amount.Should().Be(175m);
    }

    // ── Annual / specific-month frequency ────────────────────────────────────

    [Fact]
    public void AnnualFrequency_DeductsOnlyInListedMonth()
    {
        // Tamil Nadu deducts in Feb only (one slab). Slab effective from 2024-04 so
        // it covers both Feb 2025 (deduction month) and May 2025 (non-deduction).
        DateOnly fromApr2024 = new(2024, 4, 1);
        PTSlab slab = new("TN", 0m, null, 1_250m, fromApr2024, "Annual", [2]);

        PTCalculator.Compute(40_000m, MakeEmployee("TN"), WithPt([slab]), Run(2)).Amount.Should().Be(1_250m);
        PTCalculator.Compute(40_000m, MakeEmployee("TN"), WithPt([slab]), Run(5)).Amount.Should().Be(0m);
        PTCalculator.Compute(40_000m, MakeEmployee("TN"), WithPt([slab]), Run(5)).IsExempt.Should().BeFalse();
    }

    // ── HalfYearlySplit (Kerala) ──────────────────────────────────────────────

    [Fact]
    public void HalfYearlySplit_FloorEachMonth_LastMonthAbsorbsRemainder()
    {
        // Kerala: 6-month H1 (Apr–Sep). 250 total → floor(250/6) = 41 each month;
        // last month = 250 - 41*5 = 45.
        PTSlab slab = new("KL", 0m, null, 250m, FromApr2025, "HalfYearlySplit", []);

        // Month 1 of 6 → 41
        PTCalculator.Compute(
            grossWage: 25_000m,
            emp: MakeEmployee("KL", halfYearMonthIndex: 1, halfYearTotalMonths: 6),
            config: WithPt([slab]),
            run: Run(4)).Amount.Should().Be(41m);

        // Month 3 of 6 → 41
        PTCalculator.Compute(
            grossWage: 25_000m,
            emp: MakeEmployee("KL", halfYearMonthIndex: 3, halfYearTotalMonths: 6),
            config: WithPt([slab]),
            run: Run(6)).Amount.Should().Be(41m);

        // Month 6 of 6 → remainder
        PTCalculator.Compute(
            grossWage: 25_000m,
            emp: MakeEmployee("KL", halfYearMonthIndex: 6, halfYearTotalMonths: 6),
            config: WithPt([slab]),
            run: Run(9)).Amount.Should().Be(45m);
    }

    [Fact]
    public void HalfYearlySplit_SlabLookup_UsesHalfYearGross_NotMonthlyGross()
    {
        // Monthly gross 5000, total months 6 → half-year gross 30000.
        // Slab is keyed to half-year gross.
        PTSlab[] slabs =
        [
            new("KL", 0m,      24_999m, 120m, FromApr2025, "HalfYearlySplit", []),
            new("KL", 25_000m, null,    600m, FromApr2025, "HalfYearlySplit", []),
        ];

        // 5000 × 6 = 30000 → upper slab (600 total → 100 each)
        PTCalculator.Compute(
            grossWage: 5_000m,
            emp: MakeEmployee("KL", halfYearMonthIndex: 1, halfYearTotalMonths: 6),
            config: WithPt(slabs),
            run: Run(4)).Amount.Should().Be(100m);
    }

    [Fact]
    public void HalfYearlySplit_ZeroAmountSlab_ReturnsZero_NotExempt()
    {
        // A 0-amount split slab means "matched bracket but no PT this half".
        PTSlab slab = new("KL", 0m, null, 0m, FromApr2025, "HalfYearlySplit", []);
        PTResult r = PTCalculator.Compute(
            grossWage: 25_000m,
            emp: MakeEmployee("KL", halfYearMonthIndex: 1, halfYearTotalMonths: 6),
            config: WithPt([slab]),
            run: Run(4));
        r.Amount.Should().Be(0m);
        r.IsExempt.Should().BeFalse();
    }

    // ── EffectiveFrom + latest-slab-wins ─────────────────────────────────────

    [Fact]
    public void MultipleEffectiveDates_LatestApplicableSlabUsed()
    {
        // Old slab (effective Apr 2024): 175. New slab (effective Apr 2025): 200.
        // Run in May 2025 should pick the newer 200.
        PTSlab[] slabs =
        [
            new("MH", 10_001m, null, 175m, new DateOnly(2024, 4, 1), "Monthly", []),
            new("MH", 10_001m, null, 200m, FromApr2025,              "Monthly", []),
        ];
        PTCalculator.Compute(15_000m, MakeEmployee("MH"), WithPt(slabs), Run(5)).Amount.Should().Be(200m);
    }

    [Fact]
    public void SlabEffectiveAfterRunMonth_NotApplied()
    {
        // Slab effective from Jun 2025. Run is May 2025 — slab must not apply.
        PTSlab futureSlab = new("MH", 10_001m, null, 200m, new DateOnly(2025, 6, 1), "Monthly", []);
        PTCalculator.Compute(15_000m, MakeEmployee("MH"), WithPt([futureSlab]), Run(5)).IsExempt.Should().BeTrue();
    }
}
