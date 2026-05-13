using System.Text.RegularExpressions;

namespace Payroll.Domain.ValueObjects;

public sealed record Aadhaar
{
    private static readonly Regex DigitsOnly = new(@"^\d{12}$", RegexOptions.Compiled);

    public string Value { get; }

    public Aadhaar(string value)
    {
        if (!DigitsOnly.IsMatch(value))
            throw new ArgumentException("Aadhaar must be exactly 12 digits.", nameof(value));
        Value = value;
    }

    // Always masked in API responses — full value only revealed with authorised role + audit log
    public string Masked => $"XXXX-XXXX-{Value[^4..]}";

    public override string ToString() => Masked;
}
