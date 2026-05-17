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

    public static IncomeTaxConfig Create(
        string fiscalYear, string regime,
        decimal standardDeduction, decimal rebate87ALimit, decimal rebate87AAmount,
        decimal employerStatutoryCap, decimal npsEmployerMaxRate,
        Guid createdBy) =>
        new()
        {
            FiscalYear = fiscalYear, Regime = regime,
            StandardDeduction = standardDeduction,
            Rebate87ALimit = rebate87ALimit, Rebate87AAmount = rebate87AAmount,
            EmployerStatutoryCap = employerStatutoryCap,
            NpsEmployerMaxRate = npsEmployerMaxRate,
            CreatedBy = createdBy,
        };
}
