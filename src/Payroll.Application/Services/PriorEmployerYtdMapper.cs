using Payroll.Domain.Entities;

namespace Payroll.Application.Services;

// Converts a stored PriorEmployerYtd row into the taxable-income figure the engine
// expects as PriorEmployerYTDTaxableIncome.
//
// The engine subtracts ONE standard deduction from the combined (current + prior)
// projection — Section 16(ia) allows a single SD per taxpayer per FY — so the
// prior employer's SD claim must NOT be subtracted here too. Professional tax
// (16(iii)) is not deductible at all under the new regime (115BAC), which is the
// only regime in v1. Form 12B "other income" adds to the taxable base.
// Clamped at 0 — negative taxable income would shift TDS the wrong direction.
public static class PriorEmployerYtdMapper
{
    public static decimal TaxableIncomeFor(PriorEmployerYtd? ytd)
    {
        if (ytd is null) return 0m;
        decimal adjusted = ytd.GrossSalary + ytd.OtherIncome;
        return adjusted < 0m ? 0m : adjusted;
    }
}
