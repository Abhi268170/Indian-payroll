using FluentAssertions;
using Payroll.Application.Services;
using Xunit;

namespace Payroll.Application.Tests.Services;

// WI-018 step 4 — pure arrear diff math.
public class SalaryArrearCalculatorTests
{
    private static readonly Guid BasicId = Guid.NewGuid();
    private static readonly Guid HraId = Guid.NewGuid();

    private static ArrearNewComponent NewComp(
        Guid id, string code, decimal full, bool taxable = true, bool prorate = true) =>
        new(id, code, code, full, taxable, ConsiderForEpf: false, ConsiderForEsi: false, CalculateOnProRata: prorate);

    private static ArrearMonth Month(
        int year, int month, int baseDays, int lopDays,
        IReadOnlyList<ArrearOldComponent> old, IReadOnlyList<ArrearNewComponent> @new) =>
        new(year, month, baseDays, lopDays, old, @new);

    [Fact]
    public void SingleMonthRaise_NoLop_ArrearIsFullDiff()
    {
        var months = new[]
        {
            Month(2026, 3, 31, 0,
                old: [new ArrearOldComponent("BASIC", 28000m)],
                @new: [NewComp(BasicId, "BASIC", 33600m)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.Lines.Should().ContainSingle();
        r.Lines[0].Amount.Should().Be(5600m);
        r.TotalArrear.Should().Be(5600m);
        r.TotalTaxableArrear.Should().Be(5600m);
    }

    [Fact]
    public void MultiMonth_AccumulatesPerComponent()
    {
        var month = new Func<int, ArrearMonth>(mo => Month(2026, mo, 30, 0,
            old: [new ArrearOldComponent("BASIC", 28000m)],
            @new: [NewComp(BasicId, "BASIC", 33600m)]));

        ArrearComputation r = SalaryArrearCalculator.Compute([month(3), month(4), month(5)]);

        r.Lines.Should().ContainSingle().Which.Amount.Should().Be(5600m * 3);
        r.TotalArrear.Should().Be(16800m);
    }

    [Fact]
    public void MidMonthLop_NewProratedOnSameBasisAsOldRun()
    {
        // 2 LOP of 31 → payable 29/31. New full 33600 prorated; old already prorated.
        decimal oldProrated = Math.Round(28000m * 29m / 31m, 2, MidpointRounding.AwayFromZero);
        decimal newProrated = Math.Round(33600m * 29m / 31m, 2, MidpointRounding.AwayFromZero);

        var months = new[]
        {
            Month(2026, 3, 31, 2,
                old: [new ArrearOldComponent("BASIC", oldProrated)],
                @new: [NewComp(BasicId, "BASIC", 33600m)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.Lines[0].Amount.Should().Be(newProrated - oldProrated);
    }

    [Fact]
    public void AddedComponent_PresentOnlyInNew_FullNewAmountIsArrear()
    {
        // New structure introduces HRA that the old run didn't have.
        var months = new[]
        {
            Month(2026, 3, 31, 0,
                old: [new ArrearOldComponent("BASIC", 28000m)],
                @new: [NewComp(BasicId, "BASIC", 28000m), NewComp(HraId, "HRA", 10000m)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.Lines.Should().ContainSingle(l => l.Code == "HRA").Which.Amount.Should().Be(10000m);
        r.TotalArrear.Should().Be(10000m);
    }

    [Fact]
    public void RemovedComponent_PresentOnlyInOld_IsNegative_NettedIntoTotal()
    {
        // BASIC rises by 12000; a 2000 allowance is dropped. Net arrear = 10000.
        var months = new[]
        {
            Month(2026, 3, 31, 0,
                old: [new ArrearOldComponent("BASIC", 28000m), new ArrearOldComponent("SPECIAL", 2000m)],
                @new: [NewComp(BasicId, "BASIC", 40000m)]),
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
            Month(2026, 3, 31, 0,
                old: [new ArrearOldComponent("BASIC", 40000m)],
                @new: [NewComp(BasicId, "BASIC", 30000m)]),
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
            Month(2026, 3, 31, 0,
                old: [new ArrearOldComponent("BASIC", 28000m), new ArrearOldComponent("LTA", 5000m)],
                @new: [NewComp(BasicId, "BASIC", 30000m), NewComp(HraId, "LTA", 8000m, taxable: false)]),
        };

        ArrearComputation r = SalaryArrearCalculator.Compute(months);

        r.TotalArrear.Should().Be(2000m + 3000m);      // BASIC 2000 + LTA 3000
        r.TotalTaxableArrear.Should().Be(2000m);        // only BASIC is taxable
    }

    [Fact]
    public void NoMonths_ReturnsZero()
    {
        ArrearComputation r = SalaryArrearCalculator.Compute([]);
        r.Lines.Should().BeEmpty();
        r.TotalArrear.Should().Be(0m);
    }
}
