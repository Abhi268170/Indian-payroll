using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class IncomeTaxSurchargeSlab : AuditableEntity
{
    private IncomeTaxSurchargeSlab() { }

    public string FiscalYear { get; private set; } = string.Empty;
    public string Regime { get; private set; } = string.Empty;
    public decimal IncomeFrom { get; private set; }
    public decimal? IncomeTo { get; private set; }
    public decimal SurchargeRate { get; private set; }

    public static IncomeTaxSurchargeSlab Create(
        string fiscalYear, string regime,
        decimal incomeFrom, decimal? incomeTo, decimal surchargeRate,
        Guid createdBy) =>
        new()
        {
            FiscalYear = fiscalYear, Regime = regime,
            IncomeFrom = incomeFrom, IncomeTo = incomeTo,
            SurchargeRate = surchargeRate, CreatedBy = createdBy,
        };
}
