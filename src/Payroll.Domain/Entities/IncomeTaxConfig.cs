using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class IncomeTaxConfig : AuditableEntity
{
    private IncomeTaxConfig() { }

    public string FiscalYear { get; private set; } = string.Empty;
    public string Regime { get; private set; } = string.Empty;
    public decimal StandardDeduction { get; private set; }
    public decimal Rebate87ALimit { get; private set; }
    public decimal Rebate87AAmount { get; private set; }
    public decimal EmployerStatutoryCap { get; private set; }  // ₹7.5L aggregate (EPF+NPS+Super)
    public decimal NpsEmployerMaxRate { get; private set; }    // 0.14 for FY2025-26

    // Statutory rates — regime-invariant (duplicated per-regime row until Old regime exits deferred status).
    // PF rates: EPF Act 1952. ESI rates: ESI Act 1948. Cess rate: Finance Act.
    public decimal CessRate { get; private set; }
    public decimal PfWageCap { get; private set; }
    public decimal EpfEmployeeRate { get; private set; }
    public decimal EpsEmployerRate { get; private set; }
    public decimal EpsCap { get; private set; }
    public decimal EdliEmployerRate { get; private set; }
    public decimal EdliCap { get; private set; }
    public decimal EpfAdminRate { get; private set; }
    public decimal EpfAdminMinimum { get; private set; }
    public decimal EsiWageLimit { get; private set; }
    public decimal EsiPwdWageLimit { get; private set; }
    public decimal EsiEmployeeRate { get; private set; }
    public decimal EsiEmployerRate { get; private set; }

    public static IncomeTaxConfig Create(
        string fiscalYear, string regime,
        decimal standardDeduction, decimal rebate87ALimit, decimal rebate87AAmount,
        decimal employerStatutoryCap, decimal npsEmployerMaxRate,
        decimal cessRate,
        decimal pfWageCap, decimal epfEmployeeRate, decimal epsEmployerRate, decimal epsCap,
        decimal edliEmployerRate, decimal edliCap, decimal epfAdminRate, decimal epfAdminMinimum,
        decimal esiWageLimit, decimal esiPwdWageLimit, decimal esiEmployeeRate, decimal esiEmployerRate,
        Guid createdBy) =>
        new()
        {
            FiscalYear = fiscalYear, Regime = regime,
            StandardDeduction = standardDeduction,
            Rebate87ALimit = rebate87ALimit, Rebate87AAmount = rebate87AAmount,
            EmployerStatutoryCap = employerStatutoryCap,
            NpsEmployerMaxRate = npsEmployerMaxRate,
            CessRate = cessRate,
            PfWageCap = pfWageCap,
            EpfEmployeeRate = epfEmployeeRate,
            EpsEmployerRate = epsEmployerRate,
            EpsCap = epsCap,
            EdliEmployerRate = edliEmployerRate,
            EdliCap = edliCap,
            EpfAdminRate = epfAdminRate,
            EpfAdminMinimum = epfAdminMinimum,
            EsiWageLimit = esiWageLimit,
            EsiPwdWageLimit = esiPwdWageLimit,
            EsiEmployeeRate = esiEmployeeRate,
            EsiEmployerRate = esiEmployerRate,
            CreatedBy = createdBy,
        };
}
