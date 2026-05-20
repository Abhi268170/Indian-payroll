using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class PFCalculator
{
    public static PFResult Compute(
        decimal pfWage,
        decimal fullPfWage,
        decimal lopDays,
        int baseDays,
        StatutoryConfig config,
        bool optOut,
        decimal vpf = 0m)
    {
        if (!config.PFEnabled || optOut)
            return new PFResult(0m, 0m, 0m, 0m, IsExempt: true);

        decimal rawPfWage = config.EpfConsiderSalaryOnLop ? pfWage : fullPfWage;

        decimal employeePfWage = config.EpfRestrictEmployerWage
            ? Math.Min(rawPfWage, config.PFWageCap)
            : rawPfWage;
        decimal employee = Math.Round(employeePfWage * config.EPFEmployeeRate, 2, MidpointRounding.AwayFromZero);
        decimal epfVpf = Math.Round(employeePfWage * (vpf / 100m), 2, MidpointRounding.AwayFromZero);

        decimal employerPfWage = config.EpfRestrictEmployerWage
            ? Math.Min(rawPfWage, config.PFWageCap)
            : rawPfWage;

        if (config.EpfProRateRestrictedPfWage && config.EpfRestrictEmployerWage && lopDays > 0 && baseDays > 0)
        {
            decimal proratedCap = Math.Round(config.PFWageCap * (baseDays - lopDays) / baseDays, 2, MidpointRounding.AwayFromZero);
            employerPfWage = Math.Min(employeePfWage, proratedCap);
        }

        decimal epsWage = Math.Min(employerPfWage, config.PFWageCap);
        decimal eps = Math.Min(
            Math.Round(epsWage * config.EPSEmployerRate, 2, MidpointRounding.AwayFromZero),
            config.EPSCap);

        decimal epfEmployer = Math.Round(employerPfWage * config.EPFEmployeeRate, 2, MidpointRounding.AwayFromZero) - eps;

        return new PFResult(employee, epfVpf, epfEmployer, eps, IsExempt: false);
    }
}
