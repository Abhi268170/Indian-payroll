namespace Payroll.Domain.ValueObjects;

// All monetary values are decimal — never float or double.
public sealed record Money(decimal Amount)
{
    public static readonly Money Zero = new(0m);

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator *(Money left, decimal factor) =>
        new(Math.Round(left.Amount * factor, 2, MidpointRounding.AwayFromZero));

    public bool IsNegative => Amount < 0m;

    public override string ToString() => Amount.ToString("F2");
}
