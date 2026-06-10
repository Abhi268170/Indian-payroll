using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class ProfessionalTaxSlab : AuditableEntity
{
    private ProfessionalTaxSlab() { }

    public string StateCode { get; private set; } = string.Empty;
    public DateOnly EffectiveDate { get; private set; }
    public string Frequency { get; private set; } = string.Empty;  // Monthly | HalfYearly | Annual
    // Comma-separated month numbers when PT is deducted (e.g. "9,3" for Sept+March). Null = Monthly.
    public string? DeductionMonthsCsv { get; private set; }
    public string? Gender { get; private set; }  // null = no split; Male/Female for Maharashtra
    public decimal MinGross { get; private set; }
    public decimal? MaxGross { get; private set; }
    public decimal PtAmount { get; private set; }
    public bool IsFebruarySurcharge { get; private set; }
    // Amount deducted in February instead of PtAmount (e.g. MH/KA ₹300 to land
    // exactly on the ₹2,500 Article 276 annual cap). Null = no override.
    public decimal? FebruaryAmount { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static ProfessionalTaxSlab Create(
        string stateCode,
        DateOnly effectiveDate,
        string frequency,
        string? gender,
        decimal minGross,
        decimal? maxGross,
        decimal ptAmount,
        bool isFebruarySurcharge,
        Guid createdBy,
        string? deductionMonthsCsv = null,
        decimal? februaryAmount = null) =>
        new()
        {
            StateCode = stateCode,
            EffectiveDate = effectiveDate,
            Frequency = frequency,
            DeductionMonthsCsv = deductionMonthsCsv,
            Gender = gender,
            MinGross = minGross,
            MaxGross = maxGross,
            PtAmount = ptAmount,
            IsFebruarySurcharge = isFebruarySurcharge,
            FebruaryAmount = februaryAmount,
            CreatedBy = createdBy,
        };
}
