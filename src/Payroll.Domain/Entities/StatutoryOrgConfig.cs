using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class StatutoryOrgConfig : AuditableEntity
{
    private StatutoryOrgConfig() { }

    public Guid TenantId { get; private set; }

    // EPF
    public bool EpfEnabled { get; private set; }
    public string? EpfEstablishmentCode { get; private set; }
    public string EpfEmployeeContributionRate { get; private set; } = "ActualPfWage12";
    public string EpfEmployerContributionRate { get; private set; } = "ActualPfWage12";
    public bool EpfIncludeEmployerInCtc { get; private set; } = true;
    public bool EpfOverrideAtEmployeeLevel { get; private set; }
    public bool EpfProRateRestrictedPfWage { get; private set; }
    public bool EpfConsiderSalaryOnLop { get; private set; } = true;

    // ESI
    public bool EsiEnabled { get; private set; }
    public string? EsiEstablishmentCode { get; private set; }
    public bool EsiNotifiedArea { get; private set; } = true;

    // Gratuity CTC inclusion
    public bool GratuityIncludedInCtc { get; private set; } = true;

    // Statutory Bonus
    public bool StatutoryBonusEnabled { get; private set; }
    public decimal BonusRate { get; private set; } = 8.33m;
    public string BonusMode { get; private set; } = "Yearly";  // Monthly | Yearly
    public int? BonusPayoutMonth { get; private set; }  // 1-12, only for Yearly mode

    public static StatutoryOrgConfig CreateDefault(Guid tenantId, Guid createdBy) =>
        new() { TenantId = tenantId, CreatedBy = createdBy };

    public void ConfigureEpf(
        bool enabled,
        string? establishmentCode,
        string employeeContributionRate,
        string employerContributionRate,
        bool includeEmployerInCtc,
        bool overrideAtEmployeeLevel,
        bool proRateRestrictedPfWage,
        bool considerSalaryOnLop,
        Guid updatedBy)
    {
        EpfEnabled = enabled;
        EpfEstablishmentCode = establishmentCode;
        EpfEmployeeContributionRate = employeeContributionRate;
        EpfEmployerContributionRate = employerContributionRate;
        EpfIncludeEmployerInCtc = includeEmployerInCtc;
        EpfOverrideAtEmployeeLevel = overrideAtEmployeeLevel;
        EpfProRateRestrictedPfWage = proRateRestrictedPfWage;
        EpfConsiderSalaryOnLop = considerSalaryOnLop;
        SetUpdated(updatedBy);
    }

    public void ConfigureEsi(
        bool enabled,
        string? establishmentCode,
        bool notifiedArea,
        Guid updatedBy)
    {
        EsiEnabled = enabled;
        EsiEstablishmentCode = establishmentCode;
        EsiNotifiedArea = notifiedArea;
        SetUpdated(updatedBy);
    }

    public void ConfigureGratuity(bool includedInCtc, Guid updatedBy)
    {
        GratuityIncludedInCtc = includedInCtc;
        SetUpdated(updatedBy);
    }

    public void ConfigureStatutoryBonus(bool enabled, decimal bonusRate, string bonusMode, int? bonusPayoutMonth, Guid updatedBy)
    {
        StatutoryBonusEnabled = enabled;
        BonusRate = bonusRate;
        BonusMode = bonusMode;
        BonusPayoutMonth = bonusPayoutMonth;
        SetUpdated(updatedBy);
    }
}
