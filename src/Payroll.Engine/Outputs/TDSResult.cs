namespace Payroll.Engine.Outputs;

public sealed record TDSResult(
    decimal MonthlyTDS,
    decimal AnnualProjectedTax,
    decimal Surcharge,
    decimal Cess,
    decimal TaxableIncome,
    decimal TaxBeforeRebate,
    bool Rebate87AApplied,
    bool HasPanOverride);
