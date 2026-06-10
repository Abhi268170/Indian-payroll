using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class GrossCalculator
{
    public static GrossResult Compute(EmployeeInput employee, PayrollRunInput run)
    {
        int baseDays = run.SalaryDivisor;
        // LOP can exceed the divisor (e.g. fixed 30-day divisor in a 31-day month,
        // or operator-entered FnF LOP) — clamp so pro-rata never goes negative.
        decimal payableDays = Math.Max(0m, baseDays - employee.LOPDays);

        List<ComponentAmountResult> breakdown = new List<ComponentAmountResult>(employee.Components.Count);
        decimal grossWage = 0m;
        decimal pfWage = 0m;
        decimal fullPfWage = 0m;
        decimal taxableWage = 0m;
        decimal esiWage = 0m;
        decimal lopDeduction = 0m;

        // One-time amounts (bonus, commission, salary-revision arrears) are paid and
        // taxed this month only. They are split out so the annual projection multiplies
        // ONLY the recurring portion by MonthsRemainingInFY and adds the one-time portion
        // once — projecting a one-time payment ×N would massively over-tax it.
        decimal oneTimeGross = 0m;
        decimal oneTimeTaxable = 0m;

        foreach (SalaryComponentInput c in employee.Components)
        {
            bool skipProRata = !c.CalculateOnProRata || c.IsFlat;
            decimal prorated = (!skipProRata && employee.LOPDays > 0)
                ? Math.Round(c.Amount * payableDays / baseDays, 2, MidpointRounding.AwayFromZero)
                : c.Amount;

            breakdown.Add(new ComponentAmountResult(c.ComponentId, c.Code, c.Amount, prorated));
            grossWage += prorated;
            lopDeduction += c.Amount - prorated;

            if (c.ConsiderForEpf)
            {
                pfWage += prorated;
                fullPfWage += c.Amount;
            }

            if (c.IsTaxable)
                taxableWage += prorated;

            if (c.ConsiderForEsi)
                esiWage += prorated;

            if (c.IsOneTime)
            {
                oneTimeGross += prorated;
                if (c.IsTaxable)
                    oneTimeTaxable += prorated;
            }
        }

        // Recurring portion projects ×N; one-time portion adds ×1.
        decimal recurringGross = grossWage - oneTimeGross;
        decimal recurringTaxable = taxableWage - oneTimeTaxable;
        decimal annualProjected =
            employee.CurrentEmployerYTDGross + recurringGross * run.MonthsRemainingInFY + oneTimeGross;
        decimal annualProjectedTaxable =
            employee.CurrentEmployerYTDTaxable + recurringTaxable * run.MonthsRemainingInFY + oneTimeTaxable;

        return new GrossResult(
            GrossWage: grossWage,
            PFWage: pfWage,
            FullPFWage: fullPfWage,
            AnnualProjectedGross: annualProjected,
            LOPDeduction: lopDeduction,
            ArrearAmount: oneTimeGross,
            ComponentBreakdown: breakdown,
            TaxableGrossWage: taxableWage,
            AnnualProjectedTaxableGross: annualProjectedTaxable,
            ESIWage: esiWage);
    }
}
