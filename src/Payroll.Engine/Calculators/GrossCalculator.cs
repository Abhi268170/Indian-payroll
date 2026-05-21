using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class GrossCalculator
{
    public static GrossResult Compute(EmployeeInput employee, PayrollRunInput run)
    {
        int baseDays = run.SalaryDivisor;
        decimal payableDays = baseDays - employee.LOPDays;

        var breakdown = new List<ComponentAmountResult>(employee.Components.Count);
        decimal grossWage = 0m;
        decimal pfWage = 0m;
        decimal fullPfWage = 0m;

        foreach (SalaryComponentInput c in employee.Components)
        {
            decimal prorated = employee.LOPDays > 0
                ? Math.Round(c.Amount * payableDays / baseDays, 2, MidpointRounding.AwayFromZero)
                : c.Amount;

            breakdown.Add(new ComponentAmountResult(c.ComponentId, c.Code, c.Amount, prorated));
            grossWage += prorated;

            if (c.ConsiderForEpf)
            {
                pfWage += prorated;
                fullPfWage += c.Amount;
            }
        }

        decimal lopDeduction = employee.Components.Sum(c => c.Amount) - grossWage;
        decimal annualProjected = employee.CurrentEmployerYTDGross + grossWage * run.MonthsRemainingInFY;

        return new GrossResult(
            GrossWage: grossWage,
            PFWage: pfWage,
            FullPFWage: fullPfWage,
            AnnualProjectedGross: annualProjected,
            LOPDeduction: lopDeduction,
            ArrearAmount: 0m,
            ComponentBreakdown: breakdown);
    }
}
