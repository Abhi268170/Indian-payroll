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
    public decimal EmployerStatutoryCap { get; private set; }
    public decimal NpsEmployerMaxRate { get; private set; }

    public decimal CessRate { get; private set; }
    public decimal PfWageCap { get; private set; }
    public decimal EpfEmployeeRate { get; private set; }
    public decimal EpsEmployerRate { get; private set; }
    public decimal EpsCap { get; private set; }
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
            EsiWageLimit = esiWageLimit,
            EsiPwdWageLimit = esiPwdWageLimit,
            EsiEmployeeRate = esiEmployeeRate,
            EsiEmployerRate = esiEmployerRate,
            CreatedBy = createdBy,
        };
}
