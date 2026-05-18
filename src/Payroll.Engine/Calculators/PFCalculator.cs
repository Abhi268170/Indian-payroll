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
            return new PFResult(0m, 0m, 0m, 0m, 0m, 0m, IsExempt: true);

        // Employee always contributes on actual earned PF wage (LOP-aware or full, per config)
        decimal employeePfWage = config.EpfConsiderSalaryOnLop ? pfWage : fullPfWage;
        decimal employee = Math.Round(employeePfWage * config.EPFEmployeeRate, 2, MidpointRounding.AwayFromZero);
        decimal epfVpf = Math.Round(employeePfWage * (vpf / 100m), 2, MidpointRounding.AwayFromZero);

        // Employer wage basis: restricted (capped at PFWageCap) or actual
        decimal employerPfWage = config.EpfRestrictEmployerWage
            ? Math.Min(employeePfWage, config.PFWageCap)
            : employeePfWage;

        // Pro-rate the cap itself when restricted + LOP + pro-rate flag enabled
        if (config.EpfProRateRestrictedPfWage && config.EpfRestrictEmployerWage && lopDays > 0 && baseDays > 0)
        {
            decimal proratedCap = Math.Round(config.PFWageCap * (baseDays - lopDays) / baseDays, 2, MidpointRounding.AwayFromZero);
            employerPfWage = Math.Min(employeePfWage, proratedCap);
        }

        // EPS always uses statutory ceiling (₹15,000), never exceeds EPSCap
        decimal epsWage = Math.Min(employerPfWage, config.PFWageCap);
        decimal eps = Math.Min(
            Math.Round(epsWage * config.EPSEmployerRate, 2, MidpointRounding.AwayFromZero),
            config.EPSCap);

        // EPF employer = total employer contribution (12%) minus EPS — correct statutory formula
        decimal epfEmployer = Math.Round(employerPfWage * config.EPFEmployeeRate, 2, MidpointRounding.AwayFromZero) - eps;

        // EDLI and admin always on PFWageCap-restricted wage
        decimal edliWage = Math.Min(employerPfWage, config.PFWageCap);
        decimal edli = Math.Min(
            Math.Round(edliWage * config.EDLIEmployerRate, 2, MidpointRounding.AwayFromZero),
            config.EDLICap);
        decimal admin = Math.Max(
            Math.Round(employerPfWage * config.EPFAdminRate, 2, MidpointRounding.AwayFromZero),
            config.EPFAdminMinimum);

        return new PFResult(employee, epfVpf, epfEmployer, eps, edli, admin, IsExempt: false);
    }
}
