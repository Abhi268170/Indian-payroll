namespace Payroll.Application.DTOs;

public sealed record StatutoryConfigDto(
    // EPF
    bool EpfEnabled,
    string? EpfEstablishmentCode,
    string EpfEmployeeContributionRate,
    string EpfEmployerContributionRate,
    bool EpfIncludeEmployerInCtc,
    bool EpfOverrideAtEmployeeLevel,
    bool EpfProRateRestrictedPfWage,
    bool EpfConsiderSalaryOnLop,

    // ESI
    bool EsiEnabled,
    string? EsiEstablishmentCode,
    bool EsiNotifiedArea,

    // Gratuity — drives whether the salary-structure preview subtracts gratuity
    // accrual from the residual. The engine reads this same flag at run time.
    bool GratuityIncludedInCtc,

    // Statutory Bonus
    bool StatutoryBonusEnabled,
    decimal BonusRate,
    string BonusMode,
    int? BonusPayoutMonth
);

public sealed record PtSlabDto(
    string StateCode,
    DateOnly EffectiveDate,
    string Frequency,
    string? Gender,
    decimal MinGross,
    decimal? MaxGross,
    decimal PtAmount,
    bool IsFebruarySurcharge
);

public sealed record LwfStateConfigDto(
    string StateCode,
    DateOnly EffectiveDate,
    decimal EmployeeAmount,
    decimal EmployerAmount,
    bool IsPercentageBased,
    decimal? EmployeeRate,
    decimal? EmployerRate,
    string Frequency,
    int? DeductionMonth,
    decimal? WageThreshold,
    bool IsActive
);

public sealed record PtRegistrationDto(
    string StateCode,
    string RegistrationNumber
);
