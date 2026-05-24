namespace Payroll.Domain.ValueObjects;

// Years/months between two dates. Months matter because the Payment of Gratuity
// Act rounds 6+ months up to a full year for eligibility — int years alone is wrong.
public readonly record struct Tenure(int Years, int Months)
{
    public int YearsForGratuity => Months >= 6 ? Years + 1 : Years;

    public override string ToString() => $"{Years}y {Months}m";
}
