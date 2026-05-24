using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class TdsWorksheet : AuditableEntity
{
    private TdsWorksheet() { }

    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid TenantId { get; private set; }

    public int FiscalYear { get; private set; }
    public string TaxRegime { get; private set; } = "New";

    // TDS computation detail
    public decimal AnnualProjectedIncome { get; private set; }
    public decimal StandardDeduction { get; private set; }
    public decimal TaxableIncome { get; private set; }
    public decimal TaxBeforeRebate { get; private set; }
    public decimal Rebate87A { get; private set; }
    public decimal Surcharge { get; private set; }
    public decimal Cess { get; private set; }
    public decimal AnnualTaxLiability { get; private set; }
    public decimal YtdTdsDeducted { get; private set; }
    public int RemainingMonthsInFy { get; private set; }
    public decimal TdsThisMonth { get; private set; }

    // §206AA: 20% flat if PAN missing
    public bool HasPanOverride { get; private set; }

    public void UpdateTdsThisMonth(decimal newTdsThisMonth, Guid updatedBy)
    {
        TdsThisMonth = newTdsThisMonth;
        SetUpdated(updatedBy);
    }

    public static TdsWorksheet Create(
        Guid payrollRunId,
        Guid employeeId,
        Guid tenantId,
        int fiscalYear,
        decimal annualProjectedIncome,
        decimal standardDeduction,
        decimal taxableIncome,
        decimal taxBeforeRebate,
        decimal rebate87A,
        decimal surcharge,
        decimal cess,
        decimal annualTaxLiability,
        decimal ytdTdsDeducted,
        int remainingMonthsInFy,
        decimal tdsThisMonth,
        bool hasPanOverride,
        Guid createdBy) =>
        new()
        {
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            TenantId = tenantId,
            FiscalYear = fiscalYear,
            AnnualProjectedIncome = annualProjectedIncome,
            StandardDeduction = standardDeduction,
            TaxableIncome = taxableIncome,
            TaxBeforeRebate = taxBeforeRebate,
            Rebate87A = rebate87A,
            Surcharge = surcharge,
            Cess = cess,
            AnnualTaxLiability = annualTaxLiability,
            YtdTdsDeducted = ytdTdsDeducted,
            RemainingMonthsInFy = remainingMonthsInFy,
            TdsThisMonth = tdsThisMonth,
            HasPanOverride = hasPanOverride,
            CreatedBy = createdBy
        };
}
