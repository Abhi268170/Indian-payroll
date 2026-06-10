using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class ESICalculator
{
    public static ESIResult Compute(
        decimal grossWage,
        StatutoryConfig config,
        bool isExempt,
        bool isPWD,
        bool continueInPeriod = false)
    {
        if (!config.ESIEnabled || isExempt)
            return new ESIResult(0m, 0m, IsExempt: true);

        // Once covered at the start of a contribution period (Apr–Sep / Oct–Mar),
        // the employee keeps contributing on full wages until the period ends,
        // even if the wage crosses the limit mid-period.
        decimal limit = isPWD ? config.ESIPWDWageLimit : config.ESIWageLimit;
        if (grossWage > limit && !continueInPeriod)
            return new ESIResult(0m, 0m, IsExempt: true);

        // ESI (Central) Rules: each contribution is rounded UP to the next rupee.
        decimal employee = Math.Ceiling(grossWage * config.ESIEmployeeRate);
        decimal employer = Math.Ceiling(grossWage * config.ESIEmployerRate);
        return new ESIResult(employee, employer, IsExempt: false);
    }
}
