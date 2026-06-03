using FluentAssertions;
using Payroll.Application.Services;
using Xunit;

namespace Payroll.Application.Tests.Services;

// WI-018 step 4 — pure arrear diff math.
public class SalaryArrearCalculatorTests
{
    private static readonly Guid BasicId = Guid.NewGuid();
    private static readonly Guid HraId = Guid.NewGuid();

    private static ArrearNewComponent NewComp(Guid id, string code, decimal full, bool taxable = true) =>
        new(id, code, code, full, taxable);

    // No-LOP convenience: prorated == full.
    private static ArrearOldComponent Old(string code, decimal amount) => new(code, amount, amount);

    private static ArrearMonth Month(
        int year, int month, IReadOnlyList<ArrearOldComponent> old, IReadOnlyList<ArrearNewComponent> @new) =>
        new(year, month, old, @new);

    [Fact]
    public void SingleMonthRaise_NoLop_ArrearIsFullDiff()
    {
        var months = new[]
        {
            Month(2026, 3, [Old("BASIC", 28000m)], [NewComp(BasicId, "BASIC", 33600m)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.Lines.Should().ContainSingle().Which.Amount.Should().Be(5600m);
        r.TotalArrear.Should().Be(5600m);
        r.TotalTaxableArrear.Should().Be(5600m);
    }

    [Fact]
    public void MultiMonth_AccumulatesPerComponent()
    {
        ArrearMonth M(int mo) => Month(2026, mo, [Old("BASIC", 28000m)], [NewComp(BasicId, "BASIC", 33600m)]);

        ArrearComputation r = SalaryArrearCalculator.Compute([M(3), M(4), M(5)]);

        r.Lines.Should().ContainSingle().Which.Amount.Should().Be(5600m * 3);
        r.TotalArrear.Should().Be(16800m);
    }

    [Fact]
    public void LopMonth_ProrationReproducedFromRealizedFactor()
    {
        // Old run had 2 LOP of 31 → BASIC prorated 26193.55 against full 28000.
        decimal oldProrated = Math.Round(28000m * 29m / 31m, 2, MidpointRounding.AwayFromZero); // 26193.55
        decimal expectedNewProrated = Math.Round(33600m * 29m / 31m, 2, MidpointRounding.AwayFromZero); // 31432.26

        var months = new[]
        {
            Month(2026, 3, [new ArrearOldComponent("BASIC", oldProrated, 28000m)], [NewComp(BasicId, "BASIC", 33600m)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        // ratio approach (33600 * oldProrated/28000) must equal the GrossCalculator proration.
        r.Lines[0].Amount.Should().Be(expectedNewProrated - oldProrated);
    }

    [Fact]
    public void FlatComponentInLopMonth_NotProrated()
    {
        // Discriminating case (advisor): a flat component (full == prorated even with LOP)
        // must NOT be prorated in the arrear; a recompute-from-days approach would wrongly
        // prorate it. BASIC was prorated (LOP), MEAL is flat.
        decimal basicOldProrated = Math.Round(28000m * 29m / 31m, 2, MidpointRounding.AwayFromZero); // 26193.55
        decimal basicNewProrated = Math.Round(33600m * 29m / 31m, 2, MidpointRounding.AwayFromZero); // 31432.26

        var months = new[]
        {
            Month(2026, 3,
                old:
                [
                    new ArrearOldComponent("BASIC", basicOldProrated, 28000m),
                    new ArrearOldComponent("MEAL", 5000m, 5000m), // flat: prorated == full
                ],
                @new: [NewComp(BasicId, "BASIC", 33600m), NewComp(HraId, "MEAL", 6000m)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.Lines.Should().Contain(l => l.Code == "BASIC").Which.Amount.Should().Be(basicNewProrated - basicOldProrated);
        r.Lines.Should().Contain(l => l.Code == "MEAL").Which.Amount.Should().Be(1000m); // 6000 - 5000, no proration
    }

    [Fact]
    public void AddedComponentInLopMonth_UsesMonthFactorFromOldRun()
    {
        // HRA exists only in the new structure. In a LOP month it must be prorated by the
        // month's realized factor (recovered from BASIC's prorated/full), not paid in full.
        decimal basicOldProrated = Math.Round(28000m * 29m / 31m, 2, MidpointRounding.AwayFromZero);
        decimal factor = basicOldProrated / 28000m;
        decimal expectedHra = Math.Round(10000m * factor, 2, MidpointRounding.AwayFromZero);

        var months = new[]
        {
            Month(2026, 3,
                old: [new ArrearOldComponent("BASIC", basicOldProrated, 28000m)],
                @new: [NewComp(BasicId, "BASIC", 28000m), NewComp(HraId, "HRA", 10000m)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.Lines.Should().ContainSingle(l => l.Code == "HRA").Which.Amount.Should().Be(expectedHra);
    }

    [Fact]
    public void AddedComponent_NoLop_FullNewAmountIsArrear()
    {
        var months = new[]
        {
            Month(2026, 3, [Old("BASIC", 28000m)],
                [NewComp(BasicId, "BASIC", 28000m), NewComp(HraId, "HRA", 10000m)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.Lines.Should().ContainSingle(l => l.Code == "HRA").Which.Amount.Should().Be(10000m);
        r.TotalArrear.Should().Be(10000m);
    }

    [Fact]
    public void RemovedComponent_PresentOnlyInOld_IsNegative_NettedIntoTotal()
    {
        var months = new[]
        {
            Month(2026, 3, [Old("BASIC", 28000m), Old("SPECIAL", 2000m)], [NewComp(BasicId, "BASIC", 40000m)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.Lines.Should().Contain(l => l.Code == "SPECIAL" && l.Amount == -2000m);
        r.Lines.Should().Contain(l => l.Code == "BASIC" && l.Amount == 12000m);
        r.TotalArrear.Should().Be(10000m);
    }

    [Fact]
    public void NetNegative_DecreaseRevision_ClampsToZero()
    {
        var months = new[]
        {
            Month(2026, 3, [Old("BASIC", 40000m)], [NewComp(BasicId, "BASIC", 30000m)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.Lines.Should().BeEmpty();
        r.TotalArrear.Should().Be(0m);
        r.TotalTaxableArrear.Should().Be(0m);
    }

    [Fact]
    public void NonTaxableComponent_InTotalButNotTaxableTotal()
    {
        var months = new[]
        {
            Month(2026, 3, [Old("BASIC", 28000m), Old("LTA", 5000m)],
                [NewComp(BasicId, "BASIC", 30000m), NewComp(HraId, "LTA", 8000m, taxable: false)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.TotalArrear.Should().Be(2000m + 3000m);
        r.TotalTaxableArrear.Should().Be(2000m);
    }

    [Fact]
    public void NoMonths_ReturnsZero()
    {
        ArrearComputation r = SalaryArrearCalculator.Compute([]);
        r.Lines.Should().BeEmpty();
        r.TotalArrear.Should().Be(0m);
    }
}
