using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class PFCalculator
{
    public static PFResult Compute(decimal pfWage, StatutoryConfig config, bool optOut, decimal vpf = 0m)
    {
        if (!config.PFEnabled || optOut)
            return new PFResult(0m, 0m, 0m, 0m, 0m, 0m, IsExempt: true);

        decimal cappedWage = Math.Min(pfWage, config.PFWageCap);
        decimal employee = Math.Round(cappedWage * config.EPFEmployeeRate, 2, MidpointRounding.AwayFromZero);
        decimal epf = Math.Round(cappedWage * config.EPFEmployerRate, 2, MidpointRounding.AwayFromZero);
        decimal eps = Math.Min(
            Math.Round(cappedWage * config.EPSEmployerRate, 2, MidpointRounding.AwayFromZero),
            config.EPSCap);
        decimal edli = Math.Min(
            Math.Round(cappedWage * config.EDLIEmployerRate, 2, MidpointRounding.AwayFromZero),
            config.EDLICap);
        decimal admin = Math.Max(
            Math.Round(cappedWage * config.EPFAdminRate, 2, MidpointRounding.AwayFromZero),
            config.EPFAdminMinimum);

        return new PFResult(employee, vpf, epf, eps, edli, admin, IsExempt: false);
    }
}
