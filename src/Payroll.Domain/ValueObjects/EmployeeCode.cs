namespace Payroll.Domain.ValueObjects;

public sealed record EmployeeCode
{
    public string Value { get; }

    public EmployeeCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Employee code cannot be empty.", nameof(value));
        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString() => Value;
}
