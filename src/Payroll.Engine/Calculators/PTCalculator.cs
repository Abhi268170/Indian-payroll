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
        if (!config.PTEnabled || !emp.PtApplicable) return new PTResult(0m, IsExempt: true);

        var today = new DateOnly(run.Year, run.Month, 1);

        // HalfYearlySplit: deduct every month, slab on half-year gross, Option-A rounding.
        decimal halfYearGrossForLookup = grossWage * emp.HalfYearTotalMonths;
        PTSlab? splitSlab = MatchSlab(
            config.PTSlabs, emp, today, halfYearGrossForLookup, s => s.Frequency == "HalfYearlySplit");

        if (splitSlab is not null)
        {
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
        PTSlab? slab = MatchSlab(
            config.PTSlabs, emp, today, grossWage, s => s.Frequency != "HalfYearlySplit");

        if (slab is null) return new PTResult(0m, IsExempt: true);

        bool deductThisMonth = slab.Frequency switch
        {
            "Monthly" => true,
            _ => slab.DeductionMonths.Contains(run.Month),
        };
        if (!deductThisMonth) return new PTResult(0m, IsExempt: false);

        // February override lets states like MH/KA collect the Article 276 remainder
        // (e.g. ₹200 × 11 + ₹300 in February).
        decimal due = run.Month == 2 && slab.FebruaryAmount.HasValue
            ? slab.FebruaryAmount.Value
            : slab.Amount;

        return new PTResult(due, IsExempt: false);
    }

    private static PTSlab? MatchSlab(
        IReadOnlyList<PTSlab> slabs,
        EmployeeInput emp,
        DateOnly today,
        decimal wage,
        Func<PTSlab, bool> frequencyFilter)
    {
        // Half-open range [SalaryFrom, SalaryTo) — contiguous seeds leave no gap
        // for fractional prorated wages at the boundary.
        List<PTSlab> candidates = slabs
            .Where(s => s.StateCode == emp.WorkStateCode
                && frequencyFilter(s)
                && s.EffectiveFrom <= today
                && s.SalaryFrom <= wage
                && (s.SalaryTo is null || wage < s.SalaryTo)
                && (s.Gender is null || s.Gender == emp.Gender))
            .OrderByDescending(s => s.EffectiveFrom)
            // Prefer a gender-specific slab over a genderless one at the same effective date.
            .ThenByDescending(s => s.Gender is not null)
            .ToList();

        PTSlab? match = candidates.FirstOrDefault();
        if (match is not null || emp.Gender is not null) return match;

        // Gender unknown but the state splits slabs by gender: fall back to the
        // "Male" slab — the broader/default schedule in gender-split states.
        return slabs
            .Where(s => s.StateCode == emp.WorkStateCode
                && frequencyFilter(s)
                && s.EffectiveFrom <= today
                && s.SalaryFrom <= wage
                && (s.SalaryTo is null || wage < s.SalaryTo)
                && s.Gender == "Male")
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefault();
    }
}
