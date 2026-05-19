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
                ParseDeductionMonths(s.DeductionMonthsCsv)))
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
            StandardDeduction: taxConfig?.StandardDeduction ?? 75_000m,
            Rebate87ALimit: taxConfig?.Rebate87ALimit ?? 700_000m,
            Rebate87AAmount: taxConfig?.Rebate87AAmount ?? 25_000m,
            CessRate: taxConfig?.CessRate ?? 0.04m,
            PFWageCap: taxConfig?.PfWageCap ?? 15_000m,
            EPFEmployeeRate: taxConfig?.EpfEmployeeRate ?? 0.12m,
            EPSEmployerRate: taxConfig?.EpsEmployerRate ?? 0.0833m,
            EPSCap: taxConfig?.EpsCap ?? 1_250m,
            EDLIEmployerRate: taxConfig?.EdliEmployerRate ?? 0.005m,
            EDLICap: taxConfig?.EdliCap ?? 75m,
            EPFAdminRate: taxConfig?.EpfAdminRate ?? 0.005m,
            EPFAdminMinimum: taxConfig?.EpfAdminMinimum ?? 500m,
            EpfRestrictEmployerWage: orgConfig.EpfEmployerContributionRate == "RestrictedWage12",
            EpfConsiderSalaryOnLop: orgConfig.EpfConsiderSalaryOnLop,
            EpfProRateRestrictedPfWage: orgConfig.EpfProRateRestrictedPfWage,
            ESIWageLimit: taxConfig?.EsiWageLimit ?? 21_000m,
            ESIPWDWageLimit: taxConfig?.EsiPwdWageLimit ?? 25_000m,
            ESIEmployeeRate: taxConfig?.EsiEmployeeRate ?? 0.0075m,
            ESIEmployerRate: taxConfig?.EsiEmployerRate ?? 0.0325m,
            PTSlabs: ptSlabInputs,
            LWFStates: lwfStates,
            PFEnabled: orgConfig.EpfEnabled,
            ESIEnabled: orgConfig.EsiEnabled,
            PTEnabled: true
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
