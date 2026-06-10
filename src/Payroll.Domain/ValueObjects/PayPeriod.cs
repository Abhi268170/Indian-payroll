namespace Payroll.Domain.ValueObjects;

// Represents a calendar month. Indian FY: April = month 1.
public sealed record PayPeriod(int Year, int Month)
{
    public DateOnly StartDate => new(Year, Month, 1);
    public DateOnly EndDate => StartDate.AddMonths(1).AddDays(-1);

    // FY2026 = April 2025 – March 2026
    public int FiscalYear => Month >= 4 ? Year : Year - 1;
    public string FiscalYearLabel => $"FY{FiscalYear + 1}";

    // Canonical key for statutory config rows ("2025-26"). Every reader and
    // seeder MUST use this one format — mixed formats made TDS silently zero.
    public string FiscalYearKey => FiscalYearKeyFor(FiscalYear);

    public static string FiscalYearKeyFor(int fiscalYearStart) =>
        $"{fiscalYearStart}-{(fiscalYearStart + 1) % 100:D2}";

    // Months remaining in FY including current month: April = 12, March = 1
    public int MonthsRemainingInFiscalYear() =>
        Month >= 4 ? 12 - (Month - 4) : 4 - Month;

    // Half-year position for an employee.
    // H1 = Apr–Sep, H2 = Oct–Mar.
    // Returns (MonthIndex, TotalMonths) where MonthIndex == TotalMonths means last month.
    public (int MonthIndex, int TotalMonths) HalfYearPosition(DateOnly joinDate)
    {
        bool isH1 = Month is >= 4 and <= 9;
        int hStartYear = isH1 ? Year : (Month >= 10 ? Year : Year - 1);
        int hStartMonth = isH1 ? 4 : 10;
        int hEndYear = isH1 ? Year : (Month >= 10 ? Year + 1 : Year);
        int hEndMonth = isH1 ? 9 : 3;

        DateOnly halfStart = new DateOnly(hStartYear, hStartMonth, 1);
        DateOnly halfEnd = new DateOnly(hEndYear, hEndMonth, 1);
        DateOnly empStart = joinDate > halfStart
            ? new DateOnly(joinDate.Year, joinDate.Month, 1)
            : halfStart;

        int total = ((halfEnd.Year - empStart.Year) * 12 + (halfEnd.Month - empStart.Month)) + 1;
        total = Math.Clamp(total, 1, 6);

        DateOnly current = new DateOnly(Year, Month, 1);
        int index = ((current.Year - empStart.Year) * 12 + (current.Month - empStart.Month)) + 1;
        index = Math.Clamp(index, 1, total);

        return (index, total);
    }

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
