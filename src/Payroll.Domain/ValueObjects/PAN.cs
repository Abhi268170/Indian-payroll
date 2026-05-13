using System.Text.RegularExpressions;

namespace Payroll.Domain.ValueObjects;

public sealed record PAN
{
    private static readonly Regex Pattern =
        new(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", RegexOptions.Compiled);

    public string Value { get; }

    public PAN(string value)
    {
        if (!Pattern.IsMatch(value))
            throw new ArgumentException($"Invalid PAN format: {value}", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
}
