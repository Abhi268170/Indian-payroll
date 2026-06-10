using System.Globalization;

namespace Payroll.Application.Services;

// Import files promise ISO dates (yyyy-MM-dd, per the template hint row).
// Host-culture parsing silently flipped dd/MM vs MM/dd — always parse exact
// and culture-invariant. Amounts accept Indian digit grouping (12,00,000).
public static class ImportParsers
{
    private static readonly string[] DateFormats = ["yyyy-MM-dd"];

    public static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value?.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static DateOnly ParseDate(string value) =>
        TryParseDate(value, out DateOnly d)
            ? d
            : throw new FormatException($"Date '{value}' is not in the required yyyy-MM-dd format.");

    public static bool TryParseAmount(string value, out decimal amount) =>
        decimal.TryParse(value?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
}
