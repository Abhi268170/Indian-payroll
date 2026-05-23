using FluentAssertions;
using Payroll.Engine.Calculators;
using Payroll.Engine.Outputs;
using Xunit;

namespace Payroll.Engine.Tests;

public sealed class GratuityCalculatorTests
{
    [Fact]
    public void BasicSalary_Enabled_ReturnsCorrectAccrual()
    {
        GratuityResult result = GratuityCalculator.Compute(25000m, enabled: true);
        decimal expected = Math.Round(25000m * 15m / 26m / 12m, 2, MidpointRounding.AwayFromZero);
        result.MonthlyAccrual.Should().Be(expected);
        result.IsExempt.Should().BeFalse();
    }

    [Fact]
    public void Disabled_ReturnsZero()
    {
        GratuityResult result = GratuityCalculator.Compute(25000m, enabled: false);
        result.MonthlyAccrual.Should().Be(0m);
        result.IsExempt.Should().BeTrue();
    }

    [Fact]
    public void ZeroBasic_ReturnsZero()
    {
        GratuityResult result = GratuityCalculator.Compute(0m, enabled: true);
        result.MonthlyAccrual.Should().Be(0m);
        result.IsExempt.Should().BeTrue();
    }
}
