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
        TDSWorkingResult v = ComputeVerbose(
            annualProjectedGross, priorEmployerYTDTaxableIncome,
            priorEmployerYTDTDSDeducted, currentEmployerYTDTDSDeducted,
            hasPan, config, monthsRemainingInFY);

        return new TDSResult(
            v.MonthlyTDS, v.AnnualProjectedTax,
            v.SurchargeAfterRelief, v.CessAmount,
            v.TaxableIncome, v.TaxBeforeRebate,
            v.Rebate87AApplied, v.HasPanOverride);
    }

    public static TDSWorkingResult ComputeVerbose(
        decimal annualProjectedGross,
        decimal priorEmployerYTDTaxableIncome,
        decimal priorEmployerYTDTDSDeducted,
        decimal currentEmployerYTDTDSDeducted,
        bool hasPan,
        StatutoryConfig config,
        int monthsRemainingInFY)
    {
        decimal totalProjected = annualProjectedGross + priorEmployerYTDTaxableIncome;
        decimal ytdCredit = currentEmployerYTDTDSDeducted + priorEmployerYTDTDSDeducted;

        decimal taxableIncome = Math.Max(0m, totalProjected - config.StandardDeduction);

        IReadOnlyList<SlabTax> breakdown = taxableIncome > 0m
            ? ComputeSlabBreakdown(taxableIncome, config.NewRegimeSlabs)
            : BuildEmptyBreakdown(config.NewRegimeSlabs);
        decimal taxBeforeRebate = breakdown.Sum(s => s.Tax);

        decimal rebateAmount = 0m;
        bool rebateApplied = false;
        bool marginalReliefApplied = false;
        decimal taxAfterRebate = taxBeforeRebate;
        if (taxableIncome > 0m && taxableIncome <= config.Rebate87ALimit)
        {
            rebateAmount = Math.Min(taxBeforeRebate, config.Rebate87AAmount);
            taxAfterRebate = taxBeforeRebate - rebateAmount;
            rebateApplied = true;
        }
        else if (taxableIncome > config.Rebate87ALimit && config.Rebate87ALimit > 0m)
        {
            // Section 87A marginal relief: just above the rebate limit, tax payable
            // cannot exceed the income in excess of the limit.
            decimal excessIncome = taxableIncome - config.Rebate87ALimit;
            if (taxBeforeRebate > excessIncome)
            {
                taxAfterRebate = excessIncome;
                marginalReliefApplied = true;
            }
        }

        (decimal? surchargeRate, decimal rawSurcharge, bool reliefApplied, decimal surchargeFinal) =
            ComputeSurchargeVerbose(taxableIncome, taxAfterRebate, config.SurchargeSlabs, config.NewRegimeSlabs);

        decimal cess = Math.Round((taxAfterRebate + surchargeFinal) * config.CessRate, 2, MidpointRounding.AwayFromZero);
        decimal totalAnnualTax = taxAfterRebate + surchargeFinal + cess;

        // Section 206AA: PAN not furnished → TDS at the HIGHER of the slab-computed
        // tax or the flat rate on total projected income (no cess on the flat path).
        decimal? pan206AAAnnual = null;
        decimal? pan206AAMonthly = null;
        bool panOverride = false;
        if (!hasPan && config.Pan206AARate > 0m)
        {
            decimal flatAnnual = Math.Round(totalProjected * config.Pan206AARate, 2, MidpointRounding.AwayFromZero);
            pan206AAAnnual = flatAnnual;
            decimal flatRemaining = Math.Max(0m, flatAnnual - ytdCredit);
            pan206AAMonthly = monthsRemainingInFY > 0
                ? Math.Round(flatRemaining / monthsRemainingInFY, 2, MidpointRounding.AwayFromZero)
                : 0m;
            if (flatAnnual > totalAnnualTax)
            {
                totalAnnualTax = flatAnnual;
                panOverride = true;
            }
        }

        decimal remainingTax = totalAnnualTax - ytdCredit;
        decimal monthlyTDS = monthsRemainingInFY > 0
            ? Math.Max(0m, Math.Round(remainingTax / monthsRemainingInFY, 2, MidpointRounding.AwayFromZero))
            : 0m;

        return new TDSWorkingResult(
            MonthlyTDS: monthlyTDS,
            AnnualProjectedTax: totalAnnualTax,
            TotalProjectedIncome: totalProjected,
            StandardDeduction: config.StandardDeduction,
            TaxableIncome: taxableIncome,
            SlabBreakdown: breakdown,
            TaxBeforeRebate: taxBeforeRebate,
            Rebate87AApplied: rebateApplied,
            Rebate87AAmount: rebateAmount,
            TaxAfterRebate: taxAfterRebate,
            SurchargeRate: surchargeRate,
            RawSurcharge: rawSurcharge,
            MarginalReliefApplied: reliefApplied,
            SurchargeAfterRelief: surchargeFinal,
            CessRate: config.CessRate,
            CessAmount: cess,
            PriorEmployerTDS: priorEmployerYTDTDSDeducted,
            CurrentEmployerYTDTDS: currentEmployerYTDTDSDeducted,
            RemainingTaxForFY: Math.Max(0m, remainingTax),
            HasPanOverride: panOverride,
            Pan206AAAnnual: pan206AAAnnual,
            Pan206AAMonthly: pan206AAMonthly,
            Rebate87AMarginalReliefApplied: marginalReliefApplied);
    }

    private static IReadOnlyList<SlabTax> ComputeSlabBreakdown(decimal income, IReadOnlyList<TaxSlab> slabs)
    {
        List<SlabTax> rows = new(slabs.Count);
        foreach (TaxSlab slab in slabs)
        {
            decimal slabIncome = 0m;
            decimal tax = 0m;
            if (income > slab.IncomeFrom)
            {
                decimal upper = slab.IncomeTo.HasValue ? Math.Min(income, slab.IncomeTo.Value) : income;
                slabIncome = upper - slab.IncomeFrom;
                tax = Math.Round(slabIncome * slab.Rate, 2, MidpointRounding.AwayFromZero);
            }
            rows.Add(new SlabTax(slab.IncomeFrom, slab.IncomeTo, slab.Rate, slabIncome, tax));
        }
        return rows;
    }

    private static IReadOnlyList<SlabTax> BuildEmptyBreakdown(IReadOnlyList<TaxSlab> slabs) =>
        slabs.Select(s => new SlabTax(s.IncomeFrom, s.IncomeTo, s.Rate, 0m, 0m)).ToList();

    private static (decimal? Rate, decimal Raw, bool ReliefApplied, decimal Final) ComputeSurchargeVerbose(
        decimal income,
        decimal tax,
        IReadOnlyList<SurchargeConfig> surchargeSlabs,
        IReadOnlyList<TaxSlab> taxSlabs)
    {
        SurchargeConfig? slab = surchargeSlabs
            .Where(s => income > s.IncomeFrom && (s.IncomeTo is null || income <= s.IncomeTo))
            .OrderByDescending(s => s.IncomeFrom)
            .FirstOrDefault();
        if (slab is null) return (null, 0m, false, 0m);

        decimal raw = Math.Round(tax * slab.Rate, 2, MidpointRounding.AwayFromZero);

        decimal taxAtThreshold = ComputeSlabTaxScalar(slab.IncomeFrom, taxSlabs);
        decimal reliefLimit = taxAtThreshold + (income - slab.IncomeFrom);
        if (tax + raw > reliefLimit)
        {
            decimal relieved = Math.Max(0m, Math.Round(reliefLimit - tax, 2, MidpointRounding.AwayFromZero));
            return (slab.Rate, raw, true, relieved);
        }
        return (slab.Rate, raw, false, raw);
    }

    private static decimal ComputeSlabTaxScalar(decimal income, IReadOnlyList<TaxSlab> slabs)
    {
        decimal tax = 0m;
        foreach (TaxSlab slab in slabs)
        {
            if (income <= slab.IncomeFrom) break;
            decimal upper = slab.IncomeTo.HasValue ? Math.Min(income, slab.IncomeTo.Value) : income;
            tax += Math.Round((upper - slab.IncomeFrom) * slab.Rate, 2, MidpointRounding.AwayFromZero);
        }
        return tax;
    }
}
