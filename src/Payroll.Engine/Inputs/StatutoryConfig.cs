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
    decimal EPFEmployerRate,
    decimal EPSEmployerRate,
    decimal EPSCap,
    decimal EDLIEmployerRate,
    decimal EDLICap,
    decimal EPFAdminRate,
    decimal EPFAdminMinimum,
    decimal ESIWageLimit,
    decimal ESIPWDWageLimit,
    decimal ESIEmployeeRate,
    decimal ESIEmployerRate,
    IReadOnlyList<PTSlab> PTSlabs,
    decimal? LWFEmployeeAmount,
    decimal? LWFEmployerAmount,
    bool PFEnabled,
    bool ESIEnabled,
    bool PTEnabled,
    bool LWFEnabled
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
    decimal MonthlyAmount,
    DateOnly EffectiveFrom);
