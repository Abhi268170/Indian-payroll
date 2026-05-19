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
        IReadOnlyList<SalaryComponentInput>? components = null) =>
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
            HalfYearTotalMonths: 6);

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

    // ── ComponentBreakdown structure ──────────────────────────────────────────

    [Fact]
    public void ComponentBreakdown_ContainsAllInputComponents()
    {
        GrossResult result = GrossCalculator.Compute(MakeEmployee(lop: 0), Run31());
        result.ComponentBreakdown.Select(c => c.Code)
            .Should().BeEquivalentTo(["BASIC", "HRA", "FIXED_ALLOWANCE"]);
    }
}
