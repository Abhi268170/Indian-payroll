namespace Payroll.Engine.Inputs;

// All statutory rates and thresholds come from DB config — zero hardcoded values.
public sealed record StatutoryConfig(
    IReadOnlyList<TaxSlab> NewRegimeSlabs,
    IReadOnlyList<SurchargeConfig> SurchargeSlabs,
    decimal StandardDeduction,
    decimal Rebate87ALimit,
    decimal Rebate87AAmount,
    decimal CessRate,
    decimal PFWageCap,
    decimal EPFEmployeeRate,
    decimal EPSEmployerRate,
    decimal EPSCap,
    bool EpfRestrictEmployerWage,    // true = cap employer wage at PFWageCap; false = use actual
    bool EpfConsiderSalaryOnLop,     // true = use LOP-reduced PF wage; false = use full structure
    bool EpfProRateRestrictedPfWage, // true = pro-rate PFWageCap by paid days when restricted
    decimal ESIWageLimit,
    decimal ESIPWDWageLimit,
    decimal ESIEmployeeRate,
    decimal ESIEmployerRate,
    IReadOnlyList<PTSlab> PTSlabs,
    IReadOnlyList<LwfStateInput> LWFStates,
    bool PFEnabled,
    bool ESIEnabled,
    bool PTEnabled,
    bool EpfIncludeEmployerInCtc,
    bool GratuityIncludedInCtc
);

public sealed record TaxSlab(
    decimal IncomeFrom,
    decimal? IncomeTo,
    decimal Rate);

public sealed record SurchargeConfig(
    decimal IncomeFrom,
    decimal? IncomeTo,
    decimal Rate);

public sealed record PTSlab(
    string StateCode,
    decimal SalaryFrom,
    decimal? SalaryTo,
    decimal Amount,
    DateOnly EffectiveFrom,
    string Frequency,
    IReadOnlyList<int> DeductionMonths);

public sealed record LwfStateInput(
    string StateCode,
    decimal EmployeeAmount,
    decimal EmployerAmount,
    bool IsPercentageBased,
    decimal? EmployeeRate,
    decimal? EmployerRate,
    decimal? RateCapEmployee,
    decimal? RateCapEmployer,
    string Frequency,
    int? DeductionMonth,
    decimal? WageThreshold);
