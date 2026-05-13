using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

// New regime ONLY (Section 115BAC). Old regime: // DEFERRED: old-regime
public static class TDSCalculator
{
    public static TDSResult Compute(
        decimal annualProjectedGross,
        decimal ptDeduction,
        decimal pfEmployeeContribution,
        decimal priorEmployerYTDTDSDeducted,
        StatutoryConfig config,
        int monthsRemainingInFY)
    {
        decimal taxableIncome = annualProjectedGross - config.StandardDeduction;
        if (taxableIncome <= 0m)
            return new TDSResult(0m, 0m, 0m, 0m, TaxableIncome: 0m, Rebate87AApplied: false);

        decimal annualTax = ComputeSlabTax(taxableIncome, config.NewRegimeSlabs);

        // Section 87A rebate
        bool rebateApplied = false;
        if (taxableIncome <= config.Rebate87ALimit && annualTax <= config.Rebate87AAmount)
        {
            annualTax = 0m;
            rebateApplied = true;
        }

        decimal surcharge = ComputeSurcharge(taxableIncome, annualTax, config.SurchargeSlabs);
        decimal cess = Math.Round((annualTax + surcharge) * config.CessRate, 2, MidpointRounding.AwayFromZero);
        decimal totalAnnualTax = annualTax + surcharge + cess;

        decimal remainingTax = totalAnnualTax - priorEmployerYTDTDSDeducted;
        decimal monthlyTDS = monthsRemainingInFY > 0
            ? Math.Max(0m, Math.Round(remainingTax / monthsRemainingInFY, 2, MidpointRounding.AwayFromZero))
            : 0m;

        return new TDSResult(monthlyTDS, totalAnnualTax, surcharge, cess, taxableIncome, rebateApplied);
    }

    private static decimal ComputeSlabTax(decimal income, IReadOnlyList<TaxSlab> slabs)
    {
        decimal tax = 0m;
        foreach (TaxSlab slab in slabs)
        {
            if (income <= slab.IncomeFrom) break;
            decimal upper = slab.IncomeTo.HasValue ? Math.Min(income, slab.IncomeTo.Value) : income;
            decimal slabIncome = upper - slab.IncomeFrom;
            tax += Math.Round(slabIncome * slab.Rate, 2, MidpointRounding.AwayFromZero);
        }
        return tax;
    }

    private static decimal ComputeSurcharge(decimal income, decimal tax, IReadOnlyList<SurchargeConfig> slabs)
    {
        SurchargeConfig? slab = slabs
            .Where(s => income > s.IncomeFrom && (s.IncomeTo is null || income <= s.IncomeTo))
            .OrderByDescending(s => s.IncomeFrom)
            .FirstOrDefault();
        return slab is null ? 0m : Math.Round(tax * slab.Rate, 2, MidpointRounding.AwayFromZero);
    }
}
