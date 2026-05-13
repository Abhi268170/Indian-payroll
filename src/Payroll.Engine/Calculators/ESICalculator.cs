using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class ESICalculator
{
    public static ESIResult Compute(decimal grossWage, StatutoryConfig config, bool isExempt, bool isPWD)
    {
        decimal limit = isPWD ? config.ESIPWDWageLimit : config.ESIWageLimit;
        if (!config.ESIEnabled || isExempt || grossWage > limit)
            return new ESIResult(0m, 0m, IsExempt: true);

        decimal employee = Math.Round(grossWage * config.ESIEmployeeRate, 2, MidpointRounding.AwayFromZero);
        decimal employer = Math.Round(grossWage * config.ESIEmployerRate, 2, MidpointRounding.AwayFromZero);
        return new ESIResult(employee, employer, IsExempt: false);
    }
}
