using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

public static class LWFCalculator
{
    public static LWFResult Compute(string stateCode, decimal grossWage, StatutoryConfig config, PayrollRunInput run)
    {
        LwfStateInput? state = config.LWFStates.FirstOrDefault(l => l.StateCode == stateCode);
        if (state is null) return new LWFResult(0m, 0m, IsExempt: true);

        // Wage threshold — employee exempt if gross exceeds threshold
        if (state.WageThreshold.HasValue && grossWage > state.WageThreshold.Value)
            return new LWFResult(0m, 0m, IsExempt: true);

        // Frequency check — deduct only in applicable month(s)
        bool deductThisMonth = state.Frequency switch
        {
            "Monthly" => true,
            "Annual" => state.DeductionMonth.HasValue && run.Month == state.DeductionMonth.Value,
            "HalfYearly" => run.Month is 6 or 12,
            _ => false,
        };
        if (!deductThisMonth) return new LWFResult(0m, 0m, IsExempt: false);

        if (state.IsPercentageBased)
        {
            decimal employeeAmt = state.EmployeeRate.HasValue
                ? Math.Min(Math.Round(grossWage * state.EmployeeRate.Value, 2, MidpointRounding.AwayFromZero),
                    state.RateCapEmployee ?? decimal.MaxValue)
                : 0m;
            decimal employerAmt = state.EmployerRate.HasValue
                ? Math.Min(Math.Round(grossWage * state.EmployerRate.Value, 2, MidpointRounding.AwayFromZero),
                    state.RateCapEmployer ?? decimal.MaxValue)
                : 0m;
            return new LWFResult(employeeAmt, employerAmt, IsExempt: false);
        }

        return new LWFResult(state.EmployeeAmount, state.EmployerAmount, IsExempt: false);
    }
}
