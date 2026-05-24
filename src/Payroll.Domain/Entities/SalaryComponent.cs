using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class SalaryComponent : AuditableEntity
{
    private SalaryComponent() { }

    // ── Identity ──────────────────────────────────────────────────────────
    public string Name { get; private set; } = string.Empty;
    public string NameInPayslip { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public ComponentCategory Category { get; private set; }
    public Guid TenantId { get; private set; }
    public bool IsSystemComponent { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Set true when any employee salary structure references this component.
    // Locks formula/EPF/ESI/ProRata fields.
    public bool IsAssociatedWithEmployee { get; private set; }

    // ── Earning-specific ──────────────────────────────────────────────────
    // EarningType: locked after creation.
    public EarningType? EarningType { get; private set; }
    // PayType/FormulaType/IsTaxable/EPF/ESI/ProRata: locked after employee association.
    public PayType? PayType { get; private set; }
    public ComponentFormulaType? FormulaType { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal? Percentage { get; private set; }
    public bool? IsTaxable { get; private set; }
    public bool? ConsiderForEpf { get; private set; }
    public EpfInclusionRule? EpfInclusionRule { get; private set; }
    public bool? ConsiderForEsi { get; private set; }
    public bool? CalculateOnProRata { get; private set; }
    public bool? ShowInPayslip { get; private set; }

    // True for components that are added ad-hoc inside a payroll run (e.g. Bonus,
    // Commission, Loan Recovery). Excluded from the salary structure builder and
    // surfaced in the "Add Earning"/"Add Deduction" dropdowns. Locked after
    // employee association.
    public bool IsOneTime { get; private set; }

    // ── Deduction-specific ────────────────────────────────────────────────
    public DeductionFrequency? DeductionFrequency { get; private set; }

    // ── Reimbursement-specific ────────────────────────────────────────────
    public ReimbursementType? ReimbursementType { get; private set; }
    public decimal? ReimbursementAmount { get; private set; }
    public UnclaimedReimbursementHandling? UnclaimedHandling { get; private set; }

    // ── Benefit-specific ──────────────────────────────────────────────────
    public BenefitType? BenefitType { get; private set; }
    // VPF: percentage of PF wage contributed voluntarily.
    public decimal? BenefitPercentage { get; private set; }
    public bool? IsApplicableToAllEmployees { get; private set; }
    // NPS only: true = government sector (14% employer contribution), false = private (10%).
    public bool? IsNpsGovernmentSector { get; private set; }

    // ── Correction-specific ───────────────────────────────────────────────
    // FK to the earning this correction adjusts. Locked after creation.
    public Guid? ForCorrectionOfComponentId { get; private set; }
    public SalaryComponent? ForCorrectionOfComponent { get; private set; }

    // ── Factory methods ───────────────────────────────────────────────────

    public static SalaryComponent CreateEarning(
        string name,
        string nameInPayslip,
        string code,
        EarningType earningType,
        PayType payType,
        ComponentFormulaType formulaType,
        decimal? fixedAmount,
        decimal? percentage,
        bool isTaxable,
        bool considerForEpf,
        EpfInclusionRule epfInclusionRule,
        bool considerForEsi,
        bool calculateOnProRata,
        bool showInPayslip,
        Guid tenantId,
        Guid createdBy,
        bool isOneTime = false) => new()
        {
            Name = name,
            NameInPayslip = nameInPayslip,
            Code = code,
            Category = ComponentCategory.Earning,
            EarningType = earningType,
            PayType = payType,
            FormulaType = formulaType,
            FixedAmount = fixedAmount,
            Percentage = percentage,
            IsTaxable = isTaxable,
            ConsiderForEpf = considerForEpf,
            EpfInclusionRule = epfInclusionRule,
            ConsiderForEsi = considerForEsi,
            CalculateOnProRata = calculateOnProRata,
            ShowInPayslip = showInPayslip,
            IsOneTime = isOneTime,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    public static SalaryComponent CreateSystemFixedAllowance(Guid tenantId, Guid createdBy) => new()
    {
        Name = "Special Allowance",
        NameInPayslip = "Special Allowance",
        Code = "FIXED_ALLOWANCE",
        Category = ComponentCategory.Earning,
        EarningType = Enums.EarningType.FixedAllowance,
        PayType = Enums.PayType.Monthly,
        FormulaType = ComponentFormulaType.ResidualCTC,
        IsTaxable = true,
        ConsiderForEpf = false,
        ConsiderForEsi = false,
        CalculateOnProRata = true,
        ShowInPayslip = true,
        IsSystemComponent = true,
        TenantId = tenantId,
        CreatedBy = createdBy,
    };

    public static SalaryComponent CreateDeduction(
        string name,
        string nameInPayslip,
        string code,
        DeductionFrequency frequency,
        Guid tenantId,
        Guid createdBy,
        bool isOneTime = false) => new()
        {
            Name = name,
            NameInPayslip = nameInPayslip,
            Code = code,
            Category = ComponentCategory.Deduction,
            DeductionFrequency = frequency,
            IsOneTime = isOneTime,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    public static SalaryComponent CreateReimbursement(
        string name,
        string nameInPayslip,
        string code,
        ReimbursementType reimbursementType,
        decimal amount,
        UnclaimedReimbursementHandling unclaimedHandling,
        Guid tenantId,
        Guid createdBy) => new()
        {
            Name = name,
            NameInPayslip = nameInPayslip,
            Code = code,
            Category = ComponentCategory.Reimbursement,
            ReimbursementType = reimbursementType,
            ReimbursementAmount = amount,
            UnclaimedHandling = unclaimedHandling,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    public static SalaryComponent CreateBenefit(
        string name,
        string nameInPayslip,
        string code,
        BenefitType benefitType,
        decimal? benefitPercentage,
        bool isApplicableToAllEmployees,
        bool? isNpsGovernmentSector,
        Guid tenantId,
        Guid createdBy) => new()
        {
            Name = name,
            NameInPayslip = nameInPayslip,
            Code = code,
            Category = ComponentCategory.Benefit,
            BenefitType = benefitType,
            BenefitPercentage = benefitPercentage,
            IsApplicableToAllEmployees = isApplicableToAllEmployees,
            IsNpsGovernmentSector = isNpsGovernmentSector,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    public static SalaryComponent CreateCorrection(
        string correctionName,
        string code,
        SalaryComponent parentEarning,
        Guid tenantId,
        Guid createdBy) => new()
        {
            Name = correctionName,
            NameInPayslip = correctionName,
            Code = code,
            Category = ComponentCategory.Correction,
            ForCorrectionOfComponentId = parentEarning.Id,
            // Inherit statutory config from parent earning
            EarningType = parentEarning.EarningType,
            IsTaxable = parentEarning.IsTaxable,
            ConsiderForEpf = parentEarning.ConsiderForEpf,
            EpfInclusionRule = parentEarning.EpfInclusionRule,
            ConsiderForEsi = parentEarning.ConsiderForEsi,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    // ── Mutations ─────────────────────────────────────────────────────────

    // Editable on all component types at any time (except system components: name is also locked).
    public void UpdateDisplayName(string name, string nameInPayslip)
    {
        if (IsSystemComponent)
            throw new InvalidOperationException("System component names cannot be changed.");
        Name = name;
        NameInPayslip = nameInPayslip;
    }

    // Earning formula fields: locked after employee association.
    public void UpdateEarningFormula(
        ComponentFormulaType formulaType,
        decimal? fixedAmount,
        decimal? percentage,
        bool isTaxable,
        bool considerForEpf,
        EpfInclusionRule epfInclusionRule,
        bool considerForEsi,
        bool calculateOnProRata,
        bool showInPayslip,
        bool isOneTime = false)
    {
        if (Category != ComponentCategory.Earning)
            throw new InvalidOperationException("Formula update only valid for earnings.");
        if (IsAssociatedWithEmployee)
            throw new InvalidOperationException("Cannot change formula after employee association.");
        FormulaType = formulaType;
        FixedAmount = fixedAmount;
        Percentage = percentage;
        IsTaxable = isTaxable;
        ConsiderForEpf = considerForEpf;
        EpfInclusionRule = epfInclusionRule;
        ConsiderForEsi = considerForEsi;
        CalculateOnProRata = calculateOnProRata;
        ShowInPayslip = showInPayslip;
        IsOneTime = isOneTime;
    }

    // Fixed-amount earnings: amount editable even after association (applies to new employees only).
    public void UpdateFixedAmount(decimal amount)
    {
        if (FormulaType != ComponentFormulaType.Fixed)
            throw new InvalidOperationException("Only Fixed formula components support direct amount update.");
        FixedAmount = amount;
    }

    public void UpdateDeduction(DeductionFrequency frequency)
    {
        if (Category != ComponentCategory.Deduction)
            throw new InvalidOperationException("Only deduction components can update frequency.");
        DeductionFrequency = frequency;
    }

    public void UpdateReimbursement(decimal amount, UnclaimedReimbursementHandling unclaimedHandling)
    {
        if (Category != ComponentCategory.Reimbursement)
            throw new InvalidOperationException("Only reimbursement components can update amount.");
        ReimbursementAmount = amount;
        UnclaimedHandling = unclaimedHandling;
    }

    // Benefit name-in-payslip: editable after association, applies to both new and existing employees.
    public void UpdateBenefitNameInPayslip(string nameInPayslip)
    {
        if (Category != ComponentCategory.Benefit)
            throw new InvalidOperationException("Only benefit components support payslip name update.");
        NameInPayslip = nameInPayslip;
    }

    public void MarkAssociatedWithEmployee() => IsAssociatedWithEmployee = true;

    public void SetActive(bool active)
    {
        if (IsSystemComponent)
            throw new InvalidOperationException("System components cannot be deactivated.");
        IsActive = active;
    }
}
