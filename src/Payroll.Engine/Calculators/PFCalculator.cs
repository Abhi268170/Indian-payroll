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
        decimal vpfPercent = 0m)
    {
        if (!config.PFEnabled || optOut)
            return new PFResult(0m, 0m, 0m, 0m, IsExempt: true);

        decimal rawPfWage = config.EpfConsiderSalaryOnLop ? pfWage : fullPfWage;

        decimal employeePfWage = config.EpfRestrictEmployerWage
            ? Math.Min(rawPfWage, config.PFWageCap)
            : rawPfWage;
        // ECR format: each contribution is reported in whole rupees.
        decimal employee = Math.Round(employeePfWage * config.EPFEmployeeRate, 0, MidpointRounding.AwayFromZero);
        decimal epfVpf = Math.Round(employeePfWage * (vpfPercent / 100m), 0, MidpointRounding.AwayFromZero);

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
            Math.Round(epsWage * config.EPSEmployerRate, 0, MidpointRounding.AwayFromZero),
            config.EPSCap);

        decimal epfEmployer = Math.Round(employerPfWage * config.EPFEmployeeRate, 0, MidpointRounding.AwayFromZero) - eps;

        // Employer-side charges. EDLI wage is statutorily capped regardless of the
        // employer-contribution restriction flag; admin is charged on the employer PF wage.
        decimal edliWage = config.EdliWageCap > 0m ? Math.Min(rawPfWage, config.EdliWageCap) : rawPfWage;
        decimal edli = Math.Round(edliWage * config.EdliRate, 0, MidpointRounding.AwayFromZero);
        if (config.EdliMaxAmount > 0m)
            edli = Math.Min(edli, config.EdliMaxAmount);
        decimal admin = Math.Round(employerPfWage * config.EpfAdminRate, 0, MidpointRounding.AwayFromZero);

        return new PFResult(employee, epfVpf, epfEmployer, eps, IsExempt: false, EdliCharge: edli, AdminCharge: admin);
    }
}
