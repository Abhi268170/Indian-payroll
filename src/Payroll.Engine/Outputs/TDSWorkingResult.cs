namespace Payroll.Engine.Outputs;

public sealed record SlabTax(
    decimal IncomeFrom,
    decimal? IncomeTo,
    decimal Rate,
    decimal SlabIncome,
    decimal Tax);

public sealed record TDSWorkingResult(
    decimal MonthlyTDS,
    decimal AnnualProjectedTax,
    decimal TotalProjectedIncome,
    decimal StandardDeduction,
    decimal TaxableIncome,
    IReadOnlyList<SlabTax> SlabBreakdown,
    decimal TaxBeforeRebate,
    bool Rebate87AApplied,
    decimal Rebate87AAmount,
    decimal TaxAfterRebate,
    decimal? SurchargeRate,
    decimal RawSurcharge,
    bool MarginalReliefApplied,
    decimal SurchargeAfterRelief,
    decimal CessRate,
    decimal CessAmount,
    decimal PriorEmployerTDS,
    decimal CurrentEmployerYTDTDS,
    decimal RemainingTaxForFY,
    bool HasPanOverride,
    decimal? Pan206AAAnnual,
    decimal? Pan206AAMonthly);
