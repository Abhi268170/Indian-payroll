using Payroll.Domain.Entities;
using Payroll.Engine.Inputs;

namespace Payroll.Application.Services;

// Builds engine StatutoryConfig from persisted domain entities.
// PF rates are statutory (EPF Act 1952). ESI rates are statutory (ESI Act 1948).
public static class StatutoryConfigBuilder
{
    private const decimal PfWageCap = 15_000m;
    private const decimal EpfEmployeeRate = 0.12m;
    private const decimal EpsEmployerRate = 0.0833m;
    private const decimal EpsCap = 1_250m;
    private const decimal EdliEmployerRate = 0.005m;
    private const decimal EdliCap = 75m;
    private const decimal EpfAdminRate = 0.005m;
    private const decimal EpfAdminMinimum = 500m;
    private const decimal EsiWageLimit = 21_000m;
    private const decimal EsiPwdWageLimit = 25_000m;
    private const decimal EsiEmployeeRate = 0.0075m;
    private const decimal EsiEmployerRate = 0.0325m;

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
            CessRate: 0.04m,
            PFWageCap: PfWageCap,
            EPFEmployeeRate: EpfEmployeeRate,
            EPSEmployerRate: EpsEmployerRate,
            EPSCap: EpsCap,
            EDLIEmployerRate: EdliEmployerRate,
            EDLICap: EdliCap,
            EPFAdminRate: EpfAdminRate,
            EPFAdminMinimum: EpfAdminMinimum,
            EpfRestrictEmployerWage: orgConfig.EpfEmployerContributionRate == "RestrictedWage12",
            EpfConsiderSalaryOnLop: orgConfig.EpfConsiderSalaryOnLop,
            EpfProRateRestrictedPfWage: orgConfig.EpfProRateRestrictedPfWage,
            ESIWageLimit: EsiWageLimit,
            ESIPWDWageLimit: EsiPwdWageLimit,
            ESIEmployeeRate: EsiEmployeeRate,
            ESIEmployerRate: EsiEmployerRate,
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
