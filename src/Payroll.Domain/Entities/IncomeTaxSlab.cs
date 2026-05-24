using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class IncomeTaxSlab : AuditableEntity
{
    private IncomeTaxSlab() { }

    public string FiscalYear { get; private set; } = string.Empty;  // e.g. "2025-26"
    public string Regime { get; private set; } = string.Empty;       // New | Old (Old: DEFERRED)
    public decimal BracketMin { get; private set; }
    public decimal? BracketMax { get; private set; }
    public decimal Rate { get; private set; }

    public static IncomeTaxSlab Create(
        string fiscalYear, string regime,
        decimal bracketMin, decimal? bracketMax, decimal rate,
        Guid createdBy) =>
        new()
        {
            FiscalYear = fiscalYear, Regime = regime,
            BracketMin = bracketMin, BracketMax = bracketMax,
            Rate = rate, CreatedBy = createdBy,
        };
}
