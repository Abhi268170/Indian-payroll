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
    public void StandardDeductionAndPt_Subtracted()
    {
        // 500000 - 75000 - 2500 = 422500
        PriorEmployerYtdMapper.TaxableIncomeFor(
            Make(gross: 5_00_000m, standardDed: 75_000m, pt: 2_500m))
            .Should().Be(4_22_500m);
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
        // 500000 - 75000 - 2500 + 10000 = 432500
        PriorEmployerYtdMapper.TaxableIncomeFor(
            Make(gross: 5_00_000m, standardDed: 75_000m, pt: 2_500m, otherIncome: 10_000m))
            .Should().Be(4_32_500m);
    }

    [Fact]
    public void NegativeResult_ClampedToZero()
    {
        // 10000 - 75000 = -65000 → clamped to 0 (negative taxable would skew TDS the wrong way)
        PriorEmployerYtdMapper.TaxableIncomeFor(
            Make(gross: 10_000m, standardDed: 75_000m))
            .Should().Be(0m);
    }
}
