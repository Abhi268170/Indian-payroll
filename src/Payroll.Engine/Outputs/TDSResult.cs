namespace Payroll.Engine.Outputs;

public sealed record TDSResult(
    decimal MonthlyTDS,
    decimal AnnualProjectedTax,
    decimal Surcharge,
    decimal Cess,
    decimal TaxableIncome,
    bool Rebate87AApplied);
