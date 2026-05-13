using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class LWFCalculator
{
    public static LWFResult Compute(string stateCode, StatutoryConfig config, PayrollRunInput run)
    {
        if (!config.LWFEnabled
            || config.LWFEmployeeAmount is null
            || config.LWFEmployerAmount is null)
            return new LWFResult(0m, 0m, IsExempt: true);

        return new LWFResult(
            config.LWFEmployeeAmount.Value,
            config.LWFEmployerAmount.Value,
            IsExempt: false);
    }
}
