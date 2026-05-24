using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class GratuityCalculator
{
    public static GratuityResult Compute(decimal basicWage, bool enabled)
    {
        if (!enabled || basicWage <= 0m)
            return new GratuityResult(0m, IsExempt: true);

        decimal accrual = Math.Round(basicWage * 15m / 26m / 12m, 2, MidpointRounding.AwayFromZero);
        return new GratuityResult(accrual, IsExempt: false);
    }
}
