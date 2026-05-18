using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class PTCalculator
{
    public static PTResult Compute(decimal grossWage, string stateCode, StatutoryConfig config, PayrollRunInput run)
    {
        if (!config.PTEnabled) return new PTResult(0m, IsExempt: true);

        PTSlab? slab = config.PTSlabs
            .Where(s => s.StateCode == stateCode
                && s.EffectiveFrom <= new DateOnly(run.Year, run.Month, 1)
                && s.SalaryFrom <= grossWage
                && (s.SalaryTo is null || grossWage <= s.SalaryTo))
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefault();

        if (slab is null) return new PTResult(0m, IsExempt: true);

        // Check if PT should be deducted this month based on frequency
        bool deductThisMonth = slab.Frequency switch
        {
            "Monthly" => true,
            _ => slab.DeductionMonths.Contains(run.Month),
        };

        return deductThisMonth
            ? new PTResult(slab.Amount, IsExempt: false)
            : new PTResult(0m, IsExempt: false);
    }
}
