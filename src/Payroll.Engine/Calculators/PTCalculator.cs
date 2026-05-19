using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class PTCalculator
{
    public static PTResult Compute(
        decimal grossWage,
        EmployeeInput emp,
        StatutoryConfig config,
        PayrollRunInput run)
    {
        if (!config.PTEnabled) return new PTResult(0m, IsExempt: true);

        var today = new DateOnly(run.Year, run.Month, 1);

        // HalfYearlySplit: deduct every month, slab on half-year gross, Option-A rounding.
        PTSlab? splitSlab = config.PTSlabs
            .Where(s => s.StateCode == emp.WorkStateCode
                && s.Frequency == "HalfYearlySplit"
                && s.EffectiveFrom <= today)
            .OrderByDescending(s => s.EffectiveFrom)
            .Where(s =>
            {
                decimal halfYearGross = grossWage * emp.HalfYearTotalMonths;
                return s.SalaryFrom <= halfYearGross
                    && (s.SalaryTo is null || halfYearGross <= s.SalaryTo);
            })
            .FirstOrDefault();

        if (splitSlab is not null)
        {
            decimal halfYearGross = grossWage * emp.HalfYearTotalMonths;
            // re-lookup cleanly (LINQ above already filtered; just use it)
            decimal totalPt = splitSlab.Amount;
            if (totalPt == 0m) return new PTResult(0m, IsExempt: false);

            decimal floor = Math.Floor(totalPt / emp.HalfYearTotalMonths);
            bool isLastMonth = emp.HalfYearMonthIndex == emp.HalfYearTotalMonths;
            decimal amount = isLastMonth
                ? totalPt - floor * (emp.HalfYearTotalMonths - 1)
                : floor;

            return new PTResult(amount, IsExempt: false);
        }

        // Standard path: Monthly or HalfYearly lump-sum (deduct only in specific months).
        PTSlab? slab = config.PTSlabs
            .Where(s => s.StateCode == emp.WorkStateCode
                && s.Frequency != "HalfYearlySplit"
                && s.EffectiveFrom <= today
                && s.SalaryFrom <= grossWage
                && (s.SalaryTo is null || grossWage <= s.SalaryTo))
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefault();

        if (slab is null) return new PTResult(0m, IsExempt: true);

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
