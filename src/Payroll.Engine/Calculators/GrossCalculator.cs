using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class GrossCalculator
{
    public static GrossResult Compute(EmployeeInput employee, PayrollRunInput run)
    {
        int baseDays = run.CalendarDaysInMonth;
        decimal payableDays = baseDays - employee.LOPDays;

        var breakdown = new List<ComponentAmountResult>(employee.Components.Count);
        decimal grossWage = 0m;
        decimal pfWage = 0m;

        foreach (SalaryComponentInput c in employee.Components)
        {
            decimal prorated = employee.LOPDays > 0
                ? Math.Round(c.Amount * payableDays / baseDays, 2, MidpointRounding.AwayFromZero)
                : c.Amount;

            breakdown.Add(new ComponentAmountResult(c.ComponentId, c.Code, c.Amount, prorated));
            grossWage += prorated;

            if (c.Code is "BASIC" or "DA")
                pfWage += prorated;
        }

        decimal lopDeduction = employee.Components.Sum(c => c.Amount) - grossWage;
        decimal annualProjected = grossWage * run.MonthsRemainingInFY
            + employee.PriorEmployerYTDTaxableIncome;

        return new GrossResult(
            GrossWage: grossWage,
            PFWage: pfWage,
            AnnualProjectedGross: annualProjected,
            LOPDeduction: lopDeduction,
            ArrearAmount: 0m,
            ComponentBreakdown: breakdown);
    }
}
