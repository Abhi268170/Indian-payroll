using FluentAssertions;
using Payroll.Engine.Calculators;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;
using Xunit;

namespace Payroll.Engine.Tests;

public class GrossCalculatorTests
{
    private static readonly Guid BasicId = Guid.NewGuid();
    private static readonly Guid HraId = Guid.NewGuid();
    private static readonly Guid DaId = Guid.NewGuid();
    private static readonly Guid FixedId = Guid.NewGuid();

    private static EmployeeInput MakeEmployee(
        decimal lop = 0,
        decimal calendarDays = 31,
        IReadOnlyList<SalaryComponentInput>? components = null,
        decimal currentEmployerYTDGross = 0m,
        decimal currentEmployerYTDTaxable = 0m) =>
        new(
            EmployeeId: Guid.NewGuid(),
            EmployeeCode: "EMP001",
            WorkStateCode: "MH",
            EpfEnabled: true,
            IsESIExempt: false,
            IsPWD: false,
            MonthlyCTC: 70000m,
            Components: components ?? DefaultComponents(),
            LOPDays: lop,
            WorkingDaysInMonth: calendarDays,
            VPFAmount: 0,
            PriorEmployerYTDTaxableIncome: 0,
            PriorEmployerYTDTDSDeducted: 0,
            PriorEmployerYTDPF: 0,
            HalfYearMonthIndex: 1,
            HalfYearTotalMonths: 6,
            CurrentEmployerYTDGross: currentEmployerYTDGross,
            CurrentEmployerYTDTaxable: currentEmployerYTDTaxable);

    private static IReadOnlyList<SalaryComponentInput> DefaultComponents() =>
    [
        new(BasicId, "BASIC", 28000m, IsTaxable: true, ConsiderForEpf: true),
        new(HraId,   "HRA",   14000m, IsTaxable: false, ConsiderForEpf: false),
        new(FixedId, "FIXED_ALLOWANCE", 28000m, IsTaxable: true, ConsiderForEpf: false),
    ];

    private static IReadOnlyList<SalaryComponentInput> WithDa() =>
    [
        new(BasicId, "BASIC", 20000m, IsTaxable: true, ConsiderForEpf: true),
        new(DaId,    "DA",     5000m, IsTaxable: true, ConsiderForEpf: true),
        new(HraId,   "HRA",   10000m, IsTaxable: false, ConsiderForEpf: false),
        new(FixedId, "FIXED_ALLOWANCE", 10000m, IsTaxable: true, ConsiderForEpf: false),
    ];

    private static PayrollRunInput Run31() =>
        new(Year: 2025, Month: 5, CalendarDaysInMonth: 31, SalaryDivisor: 31, MonthsRemainingInFY: 10, FiscalYearLabel: "FY2025-26");

    private static PayrollRunInput Run30() =>
        new(Year: 2025, Month: 6, CalendarDaysInMonth: 30, SalaryDivisor: 30, MonthsRemainingInFY: 9, FiscalYearLabel: "FY2025-26");

    private static PayrollRunInput RunFixed26() =>
        new(Year: 2025, Month: 5, CalendarDaysInMonth: 31, SalaryDivisor: 26, MonthsRemainingInFY: 10, FiscalYearLabel: "FY2025-26");

    // ── No LOP ────────────────────────────────────────────────────────────────

    [Fact]
    public void NoLop_GrossWageEqualsFullSum()
    {
        GrossResult result = GrossCalculator.Compute(MakeEmployee(lop: 0), Run31());
        result.GrossWage.Should().Be(70000m);
        result.LOPDeduction.Should().Be(0m);
    }

    [Fact]
    public void NoLop_ComponentBreakdownProratedEqualsFull()
    {
        GrossResult result = GrossCalculator.Compute(MakeEmployee(lop: 0), Run31());
        result.ComponentBreakdown.Should().HaveCount(3);
        result.ComponentBreakdown.Should().AllSatisfy(c => c.ProratedAmount.Should().Be(c.FullAmount));
    }

    // ── Fixed-days divisor ────────────────────────────────────────────────────

    [Fact]
    public void WithLop_FixedDaysDivisor26_ProratesUsing26NotCalendarDays()
    {
        // May has 31 calendar days; fixed divisor = 26; 2 LOP days
        // BASIC: 28000 × 24/26 = 25846.15 (rounded)
        GrossResult result = GrossCalculator.Compute(MakeEmployee(lop: 2, calendarDays: 31), RunFixed26());

        decimal expectedBasic = Math.Round(28000m * 24m / 26m, 2, MidpointRounding.AwayFromZero);
        result.ComponentBreakdown.First(c => c.Code == "BASIC").ProratedAmount.Should().Be(expectedBasic);
    }

    // ── Per-component proration ───────────────────────────────────────────────

    [Fact]
    public void WithLop_EachComponentProrated_UsingCalendarDays()
    {
        // 2 LOP days out of 31 calendar days
        // BASIC: 28000 × 29/31 = 26193.55
        // HRA:   14000 × 29/31 = 13096.77
        // FIXED: 28000 × 29/31 = 26193.55
        // Total gross: 65483.87
        GrossResult result = GrossCalculator.Compute(MakeEmployee(lop: 2, calendarDays: 31), Run31());

        decimal expectedBasic = Math.Round(28000m * 29m / 31m, 2, MidpointRounding.AwayFromZero);
        decimal expectedHra   = Math.Round(14000m * 29m / 31m, 2, MidpointRounding.AwayFromZero);
        decimal expectedFixed = Math.Round(28000m * 29m / 31m, 2, MidpointRounding.AwayFromZero);

        ComponentAmountResult basic = result.ComponentBreakdown.First(c => c.Code == "BASIC");
        basic.FullAmount.Should().Be(28000m);
        basic.ProratedAmount.Should().Be(expectedBasic);

        result.GrossWage.Should().Be(expectedBasic + expectedHra + expectedFixed);
        result.LOPDeduction.Should().Be(70000m - result.GrossWage);
    }

    [Fact]
    public void WithLop_30DayMonth_ProrationUsesCalendarDays30()
    {
        // June: 30 calendar days, 1 LOP
        // BASIC: 28000 × 29/30
        GrossResult result = GrossCalculator.Compute(MakeEmployee(lop: 1, calendarDays: 30), Run30());
        decimal expectedBasic = Math.Round(28000m * 29m / 30m, 2, MidpointRounding.AwayFromZero);

        result.ComponentBreakdown.First(c => c.Code == "BASIC").ProratedAmount.Should().Be(expectedBasic);
    }

    // ── PFWage uses ConsiderForEpf flag ──────────────────────────────────────

    [Fact]
    public void PFWage_NoLop_SumsConsiderForEpfComponents()
    {
        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(lop: 0, components: WithDa()), Run31());

        result.PFWage.Should().Be(25000m);     // BASIC 20000 + DA 5000
        result.FullPFWage.Should().Be(25000m); // same — no LOP
    }

    [Fact]
    public void PFWage_WithLop_IsProratedAndFullPFWageIsUnprorated()
    {
        // 2 LOP days, 31 calendar days
        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(lop: 2, calendarDays: 31, components: WithDa()), Run31());

        decimal expectedBasic = Math.Round(20000m * 29m / 31m, 2, MidpointRounding.AwayFromZero);
        decimal expectedDa    = Math.Round(5000m  * 29m / 31m, 2, MidpointRounding.AwayFromZero);
        result.PFWage.Should().Be(expectedBasic + expectedDa);
        result.FullPFWage.Should().Be(25000m); // full structure amount, no proration
    }

    [Fact]
    public void PFWage_DefaultComponents_ExcludesHraAndFixedAllowance()
    {
        GrossResult result = GrossCalculator.Compute(MakeEmployee(lop: 0), Run31());
        result.PFWage.Should().Be(28000m);     // BASIC only (ConsiderForEpf=true)
        result.FullPFWage.Should().Be(28000m);
    }

    // ── AnnualProjectedGross ──────────────────────────────────────────────────

    [Fact]
    public void AnnualProjectedGross_NoYtd_IsGrossWageTimesMonthsRemaining()
    {
        // grossWage = 70,000, monthsRemaining = 10 → annualProjected = 7,00,000
        GrossResult result = GrossCalculator.Compute(MakeEmployee(), Run31());
        result.AnnualProjectedGross.Should().Be(7_00_000m);
    }

    [Fact]
    public void AnnualProjectedGross_WithCurrentYtd_AddsYtdToProjection()
    {
        // currentYTD = 3,50,000 (5 months already paid), grossWage = 70,000, monthsRemaining = 10
        // annualProjected = 3,50,000 + 70,000 × 10 = 10,50,000
        GrossResult result = GrossCalculator.Compute(MakeEmployee(currentEmployerYTDGross: 3_50_000m), Run31());
        result.AnnualProjectedGross.Should().Be(10_50_000m);
    }

    // ── ComponentBreakdown structure ──────────────────────────────────────────

    [Fact]
    public void ComponentBreakdown_ContainsAllInputComponents()
    {
        GrossResult result = GrossCalculator.Compute(MakeEmployee(lop: 0), Run31());
        result.ComponentBreakdown.Select(c => c.Code)
            .Should().BeEquivalentTo(["BASIC", "HRA", "FIXED_ALLOWANCE"]);
    }

    // ── IsFlat skips LOP proration ────────────────────────────────────────────

    [Fact]
    public void WithLop_FlatComponent_NotProrated()
    {
        // IsFlat = true → skip pro-rata even when LOP > 0
        Guid flatId = Guid.NewGuid();
        IReadOnlyList<SalaryComponentInput> components =
        [
            new(BasicId, "BASIC", 20000m, IsTaxable: true, ConsiderForEpf: true),
            new(flatId,  "MEAL",   5000m, IsTaxable: true, IsFlat: true),
        ];

        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(lop: 5, calendarDays: 31, components: components), Run31());

        decimal expectedBasic = Math.Round(20000m * 26m / 31m, 2, MidpointRounding.AwayFromZero);
        result.ComponentBreakdown.First(c => c.Code == "MEAL").ProratedAmount.Should().Be(5000m);
        result.ComponentBreakdown.First(c => c.Code == "BASIC").ProratedAmount.Should().Be(expectedBasic);
        result.GrossWage.Should().Be(expectedBasic + 5000m);
    }

    // ── CalculateOnProRata=false skips proration ──────────────────────────────

    [Fact]
    public void WithLop_CalculateOnProRataFalse_NotProrated()
    {
        Guid allowanceId = Guid.NewGuid();
        IReadOnlyList<SalaryComponentInput> components =
        [
            new(BasicId,      "BASIC",         20000m, IsTaxable: true, ConsiderForEpf: true),
            new(allowanceId,  "RETENTION_BONUS", 8000m, IsTaxable: true, CalculateOnProRata: false),
        ];

        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(lop: 3, calendarDays: 31, components: components), Run31());

        decimal expectedBasic = Math.Round(20000m * 28m / 31m, 2, MidpointRounding.AwayFromZero);
        result.ComponentBreakdown.First(c => c.Code == "RETENTION_BONUS").ProratedAmount.Should().Be(8000m);
        result.ComponentBreakdown.First(c => c.Code == "BASIC").ProratedAmount.Should().Be(expectedBasic);
    }

    // ── ESIWage sums only ConsiderForEsi components ───────────────────────────

    [Fact]
    public void ESIWage_NoLop_SumsOnlyEsiComponents()
    {
        Guid convId = Guid.NewGuid();
        IReadOnlyList<SalaryComponentInput> components =
        [
            new(BasicId, "BASIC", 10000m, IsTaxable: true,  ConsiderForEsi: true),
            new(HraId,   "HRA",    5000m, IsTaxable: false, ConsiderForEsi: true),
            new(convId,  "CONV",   2000m, IsTaxable: true,  ConsiderForEsi: false),
        ];

        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(lop: 0, components: components), Run31());

        result.ESIWage.Should().Be(15000m);  // BASIC + HRA only
        result.GrossWage.Should().Be(17000m); // all three
    }

    [Fact]
    public void ESIWage_WithLop_UsesProRatedAmountsForEsiComponents()
    {
        IReadOnlyList<SalaryComponentInput> components =
        [
            new(BasicId, "BASIC", 10000m, IsTaxable: true, ConsiderForEsi: true),
            new(HraId,   "HRA",    5000m, IsTaxable: false, ConsiderForEsi: false),
        ];

        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(lop: 1, calendarDays: 31, components: components), Run31());

        decimal expectedBasic = Math.Round(10000m * 30m / 31m, 2, MidpointRounding.AwayFromZero);
        result.ESIWage.Should().Be(expectedBasic);  // pro-rated BASIC only
    }

    // ── AnnualProjectedTaxableGross uses only IsTaxable components ────────────

    [Fact]
    public void AnnualProjectedTaxableGross_NoLop_SumsOnlyTaxableComponents()
    {
        // BASIC taxable=true, HRA taxable=false, FIXED_ALLOWANCE taxable=true
        // taxableMonthly = 28000 + 28000 = 56000; monthsRemaining=10
        // annualProjectedTaxable = 0 + 56000 × 10 = 560000
        GrossResult result = GrossCalculator.Compute(MakeEmployee(lop: 0), Run31());

        result.TaxableGrossWage.Should().Be(56000m);
        result.AnnualProjectedTaxableGross.Should().Be(5_60_000m);
    }

    [Fact]
    public void AnnualProjectedTaxableGross_AllTaxable_MatchesFullGrossProjection()
    {
        IReadOnlyList<SalaryComponentInput> allTaxable =
        [
            new(BasicId, "BASIC", 30000m, IsTaxable: true),
            new(HraId,   "HRA",   20000m, IsTaxable: true),
        ];

        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(lop: 0, components: allTaxable), Run31());

        // All taxable — taxable gross = full gross
        result.AnnualProjectedTaxableGross.Should().Be(result.AnnualProjectedGross);
    }

    [Fact]
    public void AnnualProjectedTaxableGross_WithTaxableYtd_TaxableYtdAddedToProjection()
    {
        // Taxable YTD = 2,80,000 (sum of taxable components from prior months),
        // taxable monthly = 56,000 (BASIC+FIXED, no HRA).
        // projected = 280000 + 56000 × 10 = 840000
        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(
                currentEmployerYTDGross: 3_50_000m,
                currentEmployerYTDTaxable: 2_80_000m),
            Run31());

        result.AnnualProjectedTaxableGross.Should().Be(8_40_000m);
        // Gross projection still uses gross YTD — the two are independent.
        result.AnnualProjectedGross.Should().Be(3_50_000m + 70_000m * 10);
    }

    [Fact]
    public void AnnualProjectedTaxableGross_GrossYtdDoesNotInflateTaxableProjection()
    {
        // Regression for the prior bug where GrossCalculator used CurrentEmployerYTDGross
        // for the taxable projection. With gross YTD set but taxable YTD = 0, the
        // taxable projection must only reflect the current-month taxable amount × remaining.
        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(
                currentEmployerYTDGross: 3_50_000m,
                currentEmployerYTDTaxable: 0m),
            Run31());

        result.AnnualProjectedTaxableGross.Should().Be(56_000m * 10);
    }
}
