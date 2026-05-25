namespace Payroll.Application.Services;

// FnF closes the period containing the employee's last working day. LWF is
// per-half-year (H1 = Apr–Sep, H2 = Oct–Mar) — if any earlier month in the same
// half-year already deducted LWF for this employee, the FnF run must skip it
// to avoid double-dipping. This pure helper returns the (year, firstMonth,
// lastMonth) windows we need to query so the orchestrator stays a thin wrapper
// around a mockable repository call and the wrap-around logic is unit-testable.
public static class LwfHalfYearLookback
{
    public static IReadOnlyList<(int Year, int FirstMonth, int LastMonth)> GetRanges(int year, int month)
    {
        // H1 (Apr–Sep): only need to look at earlier months of the same year.
        if (month is >= 4 and <= 9)
        {
            if (month == 4) return [];
            return [(year, 4, month - 1)];
        }

        // H2 first half (Oct–Dec): earlier months of same year, no wrap.
        if (month >= 10)
        {
            if (month == 10) return [];
            return [(year, 10, month - 1)];
        }

        // H2 second half (Jan–Mar): always inspect prior calendar year's Oct–Dec,
        // plus current-year Jan..(month-1) when month > 1.
        if (month == 1) return [(year - 1, 10, 12)];
        return [(year - 1, 10, 12), (year, 1, month - 1)];
    }
}
