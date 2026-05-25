using Payroll.Domain.Entities;

namespace Payroll.Application.Services;

// Converts a stored PriorEmployerYtd row into the taxable-income figure the engine
// expects as PriorEmployerYTDTaxableIncome.
//
// Prior-employer GrossSalary is reported gross-of-everything. The engine compares
// it against the current employer's taxable basis, so we must remove the slabs
// the prior employer already netted out (standard deduction, professional tax)
// and add any non-salary income the employee declared via Form 12B (interest etc).
// Clamped at 0 — negative taxable income would shift TDS the wrong direction.
public static class PriorEmployerYtdMapper
{
    public static decimal TaxableIncomeFor(PriorEmployerYtd? ytd)
    {
        if (ytd is null) return 0m;
        decimal adjusted = ytd.GrossSalary
            - ytd.StandardDeductionClaimed
            - ytd.ProfessionalTaxPaid
            + ytd.OtherIncome;
        return adjusted < 0m ? 0m : adjusted;
    }
}
