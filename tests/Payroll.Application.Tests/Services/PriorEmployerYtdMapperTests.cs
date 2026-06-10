using FluentAssertions;
using Payroll.Application.Services;
using Payroll.Domain.Entities;
using Xunit;

namespace Payroll.Application.Tests.Services;

public class PriorEmployerYtdMapperTests
{
    private static PriorEmployerYtd Make(
        decimal gross,
        decimal standardDed = 0m,
        decimal pt = 0m,
        decimal otherIncome = 0m) =>
        PriorEmployerYtd.Create(
            employeeId: Guid.NewGuid(),
            financialYear: 2025,
            employerName: "Prev Co",
            periodFrom: new DateOnly(2025, 4, 1),
            periodTo: new DateOnly(2025, 8, 31),
            grossSalary: gross,
            standardDeductionClaimed: standardDed,
            professionalTaxPaid: pt,
            tdsDeducted: 0m,
            otherIncome: otherIncome,
            createdBy: Guid.NewGuid());

    [Fact]
    public void Null_ReturnsZero()
    {
        PriorEmployerYtdMapper.TaxableIncomeFor(null).Should().Be(0m);
    }

    [Fact]
    public void GrossOnly_NoAdjustments_ReturnsGross()
    {
        PriorEmployerYtdMapper.TaxableIncomeFor(Make(gross: 5_00_000m))
            .Should().Be(5_00_000m);
    }

    [Fact]
    public void StandardDeductionAndPt_NotSubtracted()
    {
        // Section 16(ia): ONE standard deduction per taxpayer per FY — the engine
        // subtracts it from the combined projection, so the mapper must not.
        // PT (16(iii)) is not deductible under the new regime at all.
        PriorEmployerYtdMapper.TaxableIncomeFor(
            Make(gross: 5_00_000m, standardDed: 75_000m, pt: 2_500m))
            .Should().Be(5_00_000m);
    }

    [Fact]
    public void OtherIncome_Added()
    {
        // 500000 + 30000 = 530000
        PriorEmployerYtdMapper.TaxableIncomeFor(
            Make(gross: 5_00_000m, otherIncome: 30_000m))
            .Should().Be(5_30_000m);
    }

    [Fact]
    public void AllAdjustments_Combined()
    {
        // Only other income adds; SD/PT claims are ignored: 500000 + 10000
        PriorEmployerYtdMapper.TaxableIncomeFor(
            Make(gross: 5_00_000m, standardDed: 75_000m, pt: 2_500m, otherIncome: 10_000m))
            .Should().Be(5_10_000m);
    }

    [Fact]
    public void NegativeResult_ClampedToZero()
    {
        // Negative other income larger than gross → clamped to 0
        PriorEmployerYtdMapper.TaxableIncomeFor(
            Make(gross: 10_000m, otherIncome: -75_000m))
            .Should().Be(0m);
    }
}
