using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Engine.Calculators;

// New regime ONLY (Section 115BAC). Old regime: // DEFERRED: old-regime
public static class TDSCalculator
{
    public static TDSResult Compute(
        decimal annualProjectedGross,
        decimal priorEmployerYTDTaxableIncome,
        decimal priorEmployerYTDTDSDeducted,
        decimal currentEmployerYTDTDSDeducted,
        bool hasPan,
        StatutoryConfig config,
        int monthsRemainingInFY)
    {
        decimal totalProjected = annualProjectedGross + priorEmployerYTDTaxableIncome;

        // Section 206AA: 20% flat if PAN not furnished, overrides all slab logic
        if (!hasPan)
        {
            decimal flatAnnualTax = Math.Round(totalProjected * 0.20m, 2, MidpointRounding.AwayFromZero);
            decimal flatMonthly = monthsRemainingInFY > 0
                ? Math.Round(flatAnnualTax / monthsRemainingInFY, 2, MidpointRounding.AwayFromZero)
                : 0m;
            return new TDSResult(flatMonthly, flatAnnualTax, 0m, 0m, TaxableIncome: 0m, TaxBeforeRebate: 0m, Rebate87AApplied: false, HasPanOverride: true);
        }

        decimal taxableIncome = totalProjected - config.StandardDeduction;
        if (taxableIncome <= 0m)
            return new TDSResult(0m, 0m, 0m, 0m, TaxableIncome: 0m, TaxBeforeRebate: 0m, Rebate87AApplied: false, HasPanOverride: false);

        decimal annualTax = ComputeSlabTax(taxableIncome, config.NewRegimeSlabs);
        decimal taxBeforeRebate = annualTax;

        // Section 87A rebate
        bool rebateApplied = false;
        if (taxableIncome <= config.Rebate87ALimit)
        {
            annualTax -= Math.Min(annualTax, config.Rebate87AAmount);
            rebateApplied = true;
        }

        decimal surcharge = ComputeSurcharge(taxableIncome, annualTax, config.SurchargeSlabs, config.NewRegimeSlabs);
        decimal cess = Math.Round((annualTax + surcharge) * config.CessRate, 2, MidpointRounding.AwayFromZero);
        decimal totalAnnualTax = annualTax + surcharge + cess;

        decimal remainingTax = totalAnnualTax - currentEmployerYTDTDSDeducted - priorEmployerYTDTDSDeducted;
        decimal monthlyTDS = monthsRemainingInFY > 0
            ? Math.Max(0m, Math.Round(remainingTax / monthsRemainingInFY, 2, MidpointRounding.AwayFromZero))
            : 0m;

        return new TDSResult(monthlyTDS, totalAnnualTax, surcharge, cess, taxableIncome, taxBeforeRebate, rebateApplied, HasPanOverride: false);
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

    private static decimal ComputeSurcharge(
        decimal income,
        decimal tax,
        IReadOnlyList<SurchargeConfig> surchargeSlabs,
        IReadOnlyList<TaxSlab> taxSlabs)
    {
        SurchargeConfig? slab = surchargeSlabs
            .Where(s => income > s.IncomeFrom && (s.IncomeTo is null || income <= s.IncomeTo))
            .OrderByDescending(s => s.IncomeFrom)
            .FirstOrDefault();
        if (slab is null) return 0m;

        decimal rawSurcharge = Math.Round(tax * slab.Rate, 2, MidpointRounding.AwayFromZero);

        // Marginal relief: tax+surcharge must not exceed taxAtThreshold + (income - threshold)
        decimal taxAtThreshold = ComputeSlabTax(slab.IncomeFrom, taxSlabs);
        decimal reliefLimit = taxAtThreshold + (income - slab.IncomeFrom);
        if (tax + rawSurcharge > reliefLimit)
            return Math.Max(0m, Math.Round(reliefLimit - tax, 2, MidpointRounding.AwayFromZero));

        return rawSurcharge;
    }
}
