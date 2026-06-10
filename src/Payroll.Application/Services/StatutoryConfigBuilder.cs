using Payroll.Domain.Entities;
using Payroll.Engine.Inputs;

namespace Payroll.Application.Services;

// Builds engine StatutoryConfig from persisted domain entities.
public static class StatutoryConfigBuilder
{
    public static StatutoryConfig Build(
        StatutoryOrgConfig orgConfig,
        IncomeTaxConfig? taxConfig,
        IReadOnlyList<IncomeTaxSlab> taxSlabs,
        IReadOnlyList<IncomeTaxSurchargeSlab> surchargeSlabs,
        IReadOnlyList<ProfessionalTaxSlab> ptSlabs,
        IReadOnlyList<LwfStateConfig> lwfConfigs)
    {
        // Hard fail. Computing payroll with fallback constants or empty slabs
        // silently produces ₹0 TDS — an unseeded fiscal year must be loud.
        if (taxConfig is null)
            throw new InvalidOperationException(
                "Income tax config not found for the requested fiscal year/regime. " +
                "Seed income_tax_configs before running payroll.");
        if (taxSlabs.Count == 0)
            throw new InvalidOperationException(
                $"No income tax slabs found for FY '{taxConfig.FiscalYear}' ({taxConfig.Regime} regime). " +
                "Seed income_tax_slabs before running payroll.");

        var newRegimeSlabs = taxSlabs
            .Select(s => new TaxSlab(s.BracketMin, s.BracketMax, s.Rate))
            .ToList();

        var surchargeConfig = surchargeSlabs
            .Select(s => new SurchargeConfig(s.IncomeFrom, s.IncomeTo, s.SurchargeRate))
            .ToList();

        var ptSlabInputs = ptSlabs
            .Select(s => new PTSlab(
                s.StateCode, s.MinGross, s.MaxGross, s.PtAmount, s.EffectiveDate,
                s.Frequency,
                ParseDeductionMonths(s.DeductionMonthsCsv),
                s.Gender,
                s.FebruaryAmount))
            .ToList();

        var lwfStates = lwfConfigs
            .Select(l => new LwfStateInput(
                l.StateCode,
                l.EmployeeAmount, l.EmployerAmount,
                l.IsPercentageBased,
                l.EmployeeRate, l.EmployerRate,
                l.RateCapEmployee, l.RateCapEmployer,
                l.Frequency, l.DeductionMonth,
                l.WageThreshold))
            .ToList();

        return new StatutoryConfig(
            NewRegimeSlabs: newRegimeSlabs,
            SurchargeSlabs: surchargeConfig,
            StandardDeduction: taxConfig.StandardDeduction,
            Rebate87ALimit: taxConfig.Rebate87ALimit,
            Rebate87AAmount: taxConfig.Rebate87AAmount,
            CessRate: taxConfig.CessRate,
            PFWageCap: taxConfig.PfWageCap,
            EPFEmployeeRate: taxConfig.EpfEmployeeRate,
            EPSEmployerRate: taxConfig.EpsEmployerRate,
            EPSCap: taxConfig.EpsCap,
            EpfRestrictEmployerWage: orgConfig.EpfEmployerContributionRate == "RestrictedWage12",
            EpfConsiderSalaryOnLop: orgConfig.EpfConsiderSalaryOnLop,
            EpfProRateRestrictedPfWage: orgConfig.EpfProRateRestrictedPfWage,
            ESIWageLimit: taxConfig.EsiWageLimit,
            ESIPWDWageLimit: taxConfig.EsiPwdWageLimit,
            ESIEmployeeRate: taxConfig.EsiEmployeeRate,
            ESIEmployerRate: taxConfig.EsiEmployerRate,
            PTSlabs: ptSlabInputs,
            LWFStates: lwfStates,
            PFEnabled: orgConfig.EpfEnabled,
            ESIEnabled: orgConfig.EsiEnabled,
            PTEnabled: true,
            EpfIncludeEmployerInCtc: orgConfig.EpfIncludeEmployerInCtc,
            GratuityIncludedInCtc: orgConfig.GratuityIncludedInCtc,
            EdliRate: taxConfig.EdliRate,
            EdliWageCap: taxConfig.EdliWageCap,
            EdliMaxAmount: taxConfig.EdliMaxAmount,
            EpfAdminRate: taxConfig.EpfAdminRate,
            Pan206AARate: taxConfig.Pan206AARate
        );
    }

    private static IReadOnlyList<int> ParseDeductionMonths(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',')
            .Select(s => int.TryParse(s.Trim(), out int m) ? m : 0)
            .Where(m => m is >= 1 and <= 12)
            .ToList();
    }
}
