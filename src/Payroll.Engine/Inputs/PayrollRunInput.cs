namespace Payroll.Engine.Inputs;

public sealed record PayrollRunInput(
    int Year,
    int Month,
    int CalendarDaysInMonth,
    int SalaryDivisor,
    int MonthsRemainingInFY,
    string FiscalYearLabel);
