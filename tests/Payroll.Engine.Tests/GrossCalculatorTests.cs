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
            PriorEmployerYTDPF: 0);

    private static IReadOnlyList<SalaryComponentInput> DefaultComponents() =>
    [
        new(BasicId, "BASIC", 28000m, true),
        new(HraId, "HRA", 14000m, false),
        new(FixedId, "FIXED_ALLOWANCE", 28000m, true),
    ];

    private static IReadOnlyList<SalaryComponentInput> WithDa() =>
    [
        new(BasicId, "BASIC", 20000m, true),
        new(DaId, "DA", 5000m, true),
        new(HraId, "HRA", 10000m, false),
        new(FixedId, "FIXED_ALLOWANCE", 10000m, true),
    ];

    private static PayrollRunInput Run31() =>
        new(Year: 2025, Month: 5, CalendarDaysInMonth: 31, MonthsRemainingInFY: 10, FiscalYearLabel: "FY2025-26");

    private static PayrollRunInput Run30() =>
        new(Year: 2025, Month: 6, CalendarDaysInMonth: 30, MonthsRemainingInFY: 9, FiscalYearLabel: "FY2025-26");

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

    // ── PFWage uses prorated BASIC + DA ───────────────────────────────────────

    [Fact]
    public void PFWage_NoLop_IsFullBasicPlusDA()
    {
        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(lop: 0, components: WithDa()), Run31());

        result.PFWage.Should().Be(25000m); // BASIC 20000 + DA 5000
    }

    [Fact]
    public void PFWage_WithLop_IsProratedBasicPlusDA()
    {
        // 2 LOP days, 31 calendar days
        // PFWage = (20000 × 29/31) + (5000 × 29/31)
        GrossResult result = GrossCalculator.Compute(
            MakeEmployee(lop: 2, calendarDays: 31, components: WithDa()), Run31());

        decimal expectedBasic = Math.Round(20000m * 29m / 31m, 2, MidpointRounding.AwayFromZero);
        decimal expectedDa    = Math.Round(5000m  * 29m / 31m, 2, MidpointRounding.AwayFromZero);
        result.PFWage.Should().Be(expectedBasic + expectedDa);
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
