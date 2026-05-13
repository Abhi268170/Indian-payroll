using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class GrossCalculator
{
    public static GrossResult Compute(EmployeeInput employee, PayrollRunInput run)
    {
        decimal gross = employee.Components.Sum(c => c.Amount);
        decimal lopDeduction = employee.LOPDays > 0
            ? Math.Round(gross / run.WorkingDaysInMonth * employee.LOPDays, 2, MidpointRounding.AwayFromZero)
            : 0m;
        decimal netGross = gross - lopDeduction;
        decimal pfWage = employee.Components
            .Where(c => c.Code is "BASIC" or "DA")
            .Sum(c => c.Amount);
        decimal annualProjected = netGross * run.MonthsRemainingInFY
            + employee.PriorEmployerYTDTaxableIncome;

        return new GrossResult(
            GrossWage: netGross,
            PFWage: pfWage,
            AnnualProjectedGross: annualProjected,
            LOPDeduction: lopDeduction,
            ArrearAmount: 0m);
    }
}
