namespace Payroll.Engine.Inputs;

public sealed record PayrollRunInput(
    int Year,
    int Month,
    decimal WorkingDaysInMonth,
    int MonthsRemainingInFY,
    string FiscalYearLabel);
