using FluentAssertions;
using Payroll.Engine.Calculators;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;
using Payroll.Engine.Tests.TestData;
using Xunit;

namespace Payroll.Engine.Tests;

/// <summary>
/// Verifies that PayrollEngine passes the correct wage bases to each sub-calculator.
/// Tests the flag-based filtering: ESIWage, TaxableGrossWage, and IsFlat/CalculateOnProRata.
/// </summary>
public class PayrollEngineWiringTests
{
    private static readonly Guid BasicId = Guid.NewGuid();
    private static readonly Guid HraId = Guid.NewGuid();
    private static readonly Guid MealId = Guid.NewGuid();

    private static PayrollRunInput Run31(int monthsRemaining = 10) =>
        new(Year: 2025, Month: 5, CalendarDaysInMonth: 31, SalaryDivisor: 31,
            MonthsRemainingInFY: monthsRemaining, FiscalYearLabel: "FY2025-26");

    private static StatutoryConfig Config => StatutoryTestData.DefaultConfig_FY2026();

    // ── ESI receives only ConsiderForEsi components ───────────────────────────

    [Fact]
    public void ESICalculator_ReceivesEsiWage_NotFullGross()
    {
        // BASIC: 10000, ConsiderForEsi=true  → ESI wage = 10000
        // HRA:    8000, ConsiderForEsi=false → excluded from ESI
        // Gross = 18000 (above ESI limit 21000 if both counted, but should be 10000)
        IReadOnlyList<SalaryComponentInput> components =
        [
            new(BasicId, "BASIC", 10000m, IsTaxable: true, ConsiderForEsi: true),
            new(HraId,   "HRA",    8000m, IsTaxable: false, ConsiderForEsi: false),
        ];

        EmployeeInput emp = MakeEmployee(components, lopDays: 0);
        IReadOnlyList<PayrollResult> results = PayrollEngine.Compute([emp], Run31(), Config);

        PayrollResult result = results[0];

        // ESI should be calculated on 10000 (below ESI limit) → employee = 10000 * 0.75%
        decimal expectedEsiEmployee = Math.Round(10000m * Config.ESIEmployeeRate, 2, MidpointRounding.AwayFromZero);
        result.ESI.EmployeeContribution.Should().Be(expectedEsiEmployee);

        // If full gross (18000) were used, ESI would be 18000 * 0.75% = 135
        // Our expected is 10000 * 0.75% = 75 — verifies the filter works
        result.ESI.EmployeeContribution.Should().NotBe(Math.Round(18000m * Config.ESIEmployeeRate, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void ESI_ExemptWhenEsiWageAboveLimit_NotTriggeredByNonEsiComponents()
    {
        // BASIC: 22000 ConsiderForEsi=false → ESI wage = 0 → exempt (not because > limit, but not in wage)
        // A second component: 5000 ConsiderForEsi=true → ESI wage = 5000 < limit → ESI applies
        Guid specialId = Guid.NewGuid();
        IReadOnlyList<SalaryComponentInput> components =
        [
            new(BasicId,   "BASIC",   22000m, IsTaxable: true, ConsiderForEsi: false),
            new(specialId, "SPECIAL",  5000m, IsTaxable: true, ConsiderForEsi: true),
        ];

        EmployeeInput emp = MakeEmployee(components, lopDays: 0);
        IReadOnlyList<PayrollResult> results = PayrollEngine.Compute([emp], Run31(), Config);

        // ESI wage = 5000 (below 21000 limit) → ESI applies
        decimal expectedEsiEmployee = Math.Round(5000m * Config.ESIEmployeeRate, 2, MidpointRounding.AwayFromZero);
        results[0].ESI.EmployeeContribution.Should().Be(expectedEsiEmployee);
    }

    // ── TDS uses AnnualProjectedTaxableGross, not full gross projection ───────

    [Fact]
    public void TDSCalculator_ReceivesTaxableProjection_NotFullGrossProjection()
    {
        // BASIC 10000 taxable, HRA 10000 non-taxable, monthsRemaining=12
        // Taxable annual = 10000 × 12 = 120000 (below standard deduction 75000 → taxable income = 45000 → no tax)
        // Full gross annual = 20000 × 12 = 240000 (after standard deduction = 165000 → would incur tax)
        IReadOnlyList<SalaryComponentInput> components =
        [
            new(BasicId, "BASIC", 10000m, IsTaxable: true),
            new(HraId,   "HRA",   10000m, IsTaxable: false),
        ];

        EmployeeInput emp = MakeEmployee(components, lopDays: 0);
        IReadOnlyList<PayrollResult> results = PayrollEngine.Compute([emp], Run31(monthsRemaining: 12), Config);

        // Taxable gross annual = 120000; after standard deduction = 45000 → within 0% slab → 0 TDS
        results[0].TDS.MonthlyTDS.Should().Be(0m);
    }

    // ── Flat component not pro-rated: affects gross and net correctly ─────────

    [Fact]
    public void FlatComponent_WithLop_GrossIncludesUnproratedFlatAmount()
    {
        // BASIC: 20000 pro-rata; MEAL_ALLOWANCE: 2000 flat (IsFlat=true)
        // 2 LOP days out of 31 → BASIC prorated, MEAL stays 2000
        IReadOnlyList<SalaryComponentInput> components =
        [
            new(BasicId, "BASIC", 20000m, IsTaxable: true),
            new(MealId,  "MEAL",   2000m, IsTaxable: true, IsFlat: true),
        ];

        EmployeeInput emp = MakeEmployee(components, lopDays: 2);
        IReadOnlyList<PayrollResult> results = PayrollEngine.Compute([emp], Run31(), Config);

        decimal expectedBasic = Math.Round(20000m * 29m / 31m, 2, MidpointRounding.AwayFromZero);
        decimal expectedGross = expectedBasic + 2000m;

        results[0].Gross.GrossWage.Should().Be(expectedGross);
        results[0].Gross.ComponentBreakdown.First(c => c.Code == "MEAL").ProratedAmount.Should().Be(2000m);
    }

    // ── CalculateOnProRata=false not pro-rated in full computation ────────────

    [Fact]
    public void NoProRataComponent_WithLop_NotDeductedFromGross()
    {
        Guid bonusId = Guid.NewGuid();
        IReadOnlyList<SalaryComponentInput> components =
        [
            new(BasicId, "BASIC",  15000m, IsTaxable: true),
            new(bonusId, "BONUS",   5000m, IsTaxable: true, CalculateOnProRata: false),
        ];

        EmployeeInput emp = MakeEmployee(components, lopDays: 3);
        IReadOnlyList<PayrollResult> results = PayrollEngine.Compute([emp], Run31(), Config);

        decimal expectedBasic = Math.Round(15000m * 28m / 31m, 2, MidpointRounding.AwayFromZero);
        results[0].Gross.GrossWage.Should().Be(expectedBasic + 5000m);
        results[0].Gross.LOPDeduction.Should().Be(15000m - expectedBasic); // only BASIC LOP deducted
    }

    private static EmployeeInput MakeEmployee(
        IReadOnlyList<SalaryComponentInput> components,
        decimal lopDays = 0) =>
        new(
            EmployeeId: Guid.NewGuid(),
            EmployeeCode: "EMP001",
            WorkStateCode: "MH",
            EpfEnabled: true,
            IsESIExempt: false,
            IsPWD: false,
            MonthlyCTC: 0m,
            Components: components,
            LOPDays: lopDays,
            WorkingDaysInMonth: 31,
            VPFAmount: 0,
            PriorEmployerYTDTaxableIncome: 0,
            PriorEmployerYTDTDSDeducted: 0,
            PriorEmployerYTDPF: 0,
            HalfYearMonthIndex: 1,
            HalfYearTotalMonths: 6,
            CurrentEmployerYTDGross: 0m);
}
