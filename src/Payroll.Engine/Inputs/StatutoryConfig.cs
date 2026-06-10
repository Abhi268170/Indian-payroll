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
    bool GratuityIncludedInCtc,
    // Employer-side EPF charges. Defaults are neutral (0 = charge disabled);
    // real rates come from DB config — never hardcode statutory values here.
    decimal EdliRate = 0m,
    decimal EdliWageCap = 0m,
    decimal EdliMaxAmount = 0m,
    decimal EpfAdminRate = 0m,
    // Section 206AA rate when PAN not furnished. TDS = max(slab tax, this rate × income).
    // 0 = slab tax only (206AA disabled until config supplies the rate).
    decimal Pan206AARate = 0m
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
    decimal? SalaryTo,                    // half-open: wage matches when SalaryFrom <= wage < SalaryTo
    decimal Amount,
    DateOnly EffectiveFrom,
    string Frequency,
    IReadOnlyList<int> DeductionMonths,
    string? Gender = null,                // null = applies to all genders (e.g. Maharashtra splits by gender)
    decimal? FebruaryAmount = null);      // overrides Amount in February (e.g. MH/KA ₹300 to hit the annual cap)

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
