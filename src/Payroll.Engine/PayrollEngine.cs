using Payroll.Engine.Calculators;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine;

public static class PayrollEngine
{
    public static IReadOnlyList<PayrollResult> Compute(
        IReadOnlyList<EmployeeInput> employees,
        PayrollRunInput runInput,
        StatutoryConfig config)
    {
        List<PayrollResult> results = new(employees.Count);
        foreach (EmployeeInput emp in employees)
            results.Add(ComputeOne(emp, runInput, config));
        return results;
    }

    private static PayrollResult ComputeOne(
        EmployeeInput emp,
        PayrollRunInput run,
        StatutoryConfig config)
    {
        GrossResult gross = GrossCalculator.Compute(emp, run);
        PFResult pf = PFCalculator.Compute(gross.PFWage, gross.FullPFWage, emp.LOPDays, run.SalaryDivisor, config, !emp.EpfEnabled, emp.VPFAmount);
        ESIResult esi = ESICalculator.Compute(gross.GrossWage, config, emp.IsESIExempt, emp.IsPWD);
        PTResult pt = PTCalculator.Compute(gross.GrossWage, emp, config, run);
        LWFResult lwf = LWFCalculator.Compute(emp.WorkStateCode, gross.GrossWage, config, run);
        TDSResult tds = TDSCalculator.Compute(
            gross.AnnualProjectedGross,
            pt.Amount,
            pf.EmployeeContribution,
            emp.PriorEmployerYTDTDSDeducted,
            config,
            run.MonthsRemainingInFY);

        GratuityResult gratuity = GratuityCalculator.Compute(emp.BasicWage, emp.GratuityEnabled);

        decimal netPay = gross.GrossWage
            - tds.MonthlyTDS
            - pf.EmployeeContribution
            - pf.VPFContribution
            - esi.EmployeeContribution
            - pt.Amount
            - lwf.EmployeeAmount;

        return new PayrollResult(emp.EmployeeId, gross, tds, pf, esi, pt, lwf, netPay, gratuity);
    }
}
