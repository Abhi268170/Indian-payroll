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

        // Section 206AA: 20% flat if PAN not furnished, overrides all slab logic
        if (!hasPan)
        {
            decimal flatAnnualTax = Math.Round(totalProjected * 0.20m, 2, MidpointRounding.AwayFromZero);
            decimal flatMonthly = monthsRemainingInFY > 0
                ? Math.Round(flatAnnualTax / monthsRemainingInFY, 2, MidpointRounding.AwayFromZero)
                : 0m;
            return new TDSWorkingResult(
                MonthlyTDS: flatMonthly,
                AnnualProjectedTax: flatAnnualTax,
                TotalProjectedIncome: totalProjected,
                StandardDeduction: 0m,
                TaxableIncome: 0m,
                SlabBreakdown: Array.Empty<SlabTax>(),
                TaxBeforeRebate: 0m,
                Rebate87AApplied: false,
                Rebate87AAmount: 0m,
                TaxAfterRebate: 0m,
                SurchargeRate: null,
                RawSurcharge: 0m,
                MarginalReliefApplied: false,
                SurchargeAfterRelief: 0m,
                CessRate: config.CessRate,
                CessAmount: 0m,
                PriorEmployerTDS: priorEmployerYTDTDSDeducted,
                CurrentEmployerYTDTDS: currentEmployerYTDTDSDeducted,
                RemainingTaxForFY: flatAnnualTax,
                HasPanOverride: true,
                Pan206AAAnnual: flatAnnualTax,
                Pan206AAMonthly: flatMonthly);
        }

        decimal taxableIncome = totalProjected - config.StandardDeduction;
        if (taxableIncome <= 0m)
        {
            return new TDSWorkingResult(
                MonthlyTDS: 0m,
                AnnualProjectedTax: 0m,
                TotalProjectedIncome: totalProjected,
                StandardDeduction: config.StandardDeduction,
                TaxableIncome: 0m,
                SlabBreakdown: BuildEmptyBreakdown(config.NewRegimeSlabs),
                TaxBeforeRebate: 0m,
                Rebate87AApplied: false,
                Rebate87AAmount: 0m,
                TaxAfterRebate: 0m,
                SurchargeRate: null,
                RawSurcharge: 0m,
                MarginalReliefApplied: false,
                SurchargeAfterRelief: 0m,
                CessRate: config.CessRate,
                CessAmount: 0m,
                PriorEmployerTDS: priorEmployerYTDTDSDeducted,
                CurrentEmployerYTDTDS: currentEmployerYTDTDSDeducted,
                RemainingTaxForFY: 0m,
                HasPanOverride: false,
                Pan206AAAnnual: null,
                Pan206AAMonthly: null);
        }

        IReadOnlyList<SlabTax> breakdown = ComputeSlabBreakdown(taxableIncome, config.NewRegimeSlabs);
        decimal taxBeforeRebate = breakdown.Sum(s => s.Tax);

        decimal rebateAmount = 0m;
        bool rebateApplied = false;
        decimal taxAfterRebate = taxBeforeRebate;
        if (taxableIncome <= config.Rebate87ALimit)
        {
            rebateAmount = Math.Min(taxBeforeRebate, config.Rebate87AAmount);
            taxAfterRebate = taxBeforeRebate - rebateAmount;
            rebateApplied = true;
        }

        (decimal? surchargeRate, decimal rawSurcharge, bool reliefApplied, decimal surchargeFinal) =
            ComputeSurchargeVerbose(taxableIncome, taxAfterRebate, config.SurchargeSlabs, config.NewRegimeSlabs);

        decimal cess = Math.Round((taxAfterRebate + surchargeFinal) * config.CessRate, 2, MidpointRounding.AwayFromZero);
        decimal totalAnnualTax = taxAfterRebate + surchargeFinal + cess;

        decimal remainingTax = totalAnnualTax - currentEmployerYTDTDSDeducted - priorEmployerYTDTDSDeducted;
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
            HasPanOverride: false,
            Pan206AAAnnual: null,
            Pan206AAMonthly: null);
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
