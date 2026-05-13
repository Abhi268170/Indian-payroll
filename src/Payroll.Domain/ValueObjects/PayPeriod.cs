namespace Payroll.Domain.ValueObjects;

// Represents a calendar month. Indian FY: April = month 1.
public sealed record PayPeriod(int Year, int Month)
{
    public DateOnly StartDate => new(Year, Month, 1);
    public DateOnly EndDate => StartDate.AddMonths(1).AddDays(-1);

    // FY2026 = April 2025 – March 2026
    public int FiscalYear => Month >= 4 ? Year : Year - 1;
    public string FiscalYearLabel => $"FY{FiscalYear + 1}";

    // Months remaining in FY including current month: April = 12, March = 1
    public int MonthsRemainingInFiscalYear() =>
        Month >= 4 ? 12 - (Month - 4) : 4 - Month;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
