using Payroll.Domain.Enums;

namespace Payroll.Application.DTOs;

public sealed record SalaryComponentSummaryDto(
    Guid Id,
    string Name,
    string NameInPayslip,
    string Code,
    ComponentCategory Category,
    bool IsActive,
    bool IsSystemComponent,
    bool IsAssociatedWithEmployee,
    bool IsOneTime,
    // Calculation display fields
    ComponentFormulaType? FormulaType,
    decimal? FixedAmount,
    decimal? Percentage,
    DeductionFrequency? DeductionFrequency,
    decimal? ReimbursementAmount,
    decimal? BenefitPercentage);

public sealed record SalaryComponentDetailDto(
    Guid Id,
    string Name,
    string NameInPayslip,
    string Code,
    ComponentCategory Category,
    bool IsActive,
    bool IsSystemComponent,
    bool IsAssociatedWithEmployee,

    // Earning
    EarningType? EarningType,
    PayType? PayType,
    ComponentFormulaType? FormulaType,
    decimal? FixedAmount,
    decimal? Percentage,
    bool? IsTaxable,
    bool? ConsiderForEpf,
    EpfInclusionRule? EpfInclusionRule,
    bool? ConsiderForEsi,
    bool? CalculateOnProRata,
    bool? ShowInPayslip,

    // Deduction
    DeductionFrequency? DeductionFrequency,

    // Reimbursement
    ReimbursementType? ReimbursementType,
    decimal? ReimbursementAmount,
    UnclaimedReimbursementHandling? UnclaimedHandling,

    // Benefit
    BenefitType? BenefitType,
    decimal? BenefitPercentage,
    bool? IsApplicableToAllEmployees,
    bool? IsNpsGovernmentSector,

    // Correction
    Guid? ForCorrectionOfComponentId,
    string? ForCorrectionOfComponentName);
