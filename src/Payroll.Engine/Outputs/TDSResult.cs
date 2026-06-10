namespace Payroll.Engine.Outputs;

public sealed record TDSResult(
    decimal MonthlyTDS,
    decimal AnnualProjectedTax,
    decimal Surcharge,
    decimal Cess,
    decimal TaxableIncome,
    decimal TaxBeforeRebate,
    bool Rebate87AApplied,
    bool HasPanOverride,
    // Combined current + prior employer projected income BEFORE standard
    // deduction — what the worksheet stores as "annual projected income".
    decimal TotalProjectedIncome = 0m);
