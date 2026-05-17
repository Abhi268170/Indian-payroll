using Payroll.Domain.Entities;
using Payroll.Engine.Inputs;

namespace Payroll.Application.Services;

/// <summary>
/// Builds engine StatutoryConfig from persisted domain entities.
/// All PF/ESI rates are statutory — EPF Act 1952 and ESI Act 1948 mandates.
/// </summary>
public static class StatutoryConfigBuilder
{
    private const decimal PfWageCap = 15_000m;
    private const decimal EpfEmployeeRate = 0.12m;
    private const decimal EpfEmployerRate = 0.0367m;
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
            .Select(s => new PTSlab(s.StateCode, s.MinGross, s.MaxGross, s.PtAmount, s.EffectiveDate))
            .ToList();

        decimal? lwfEmployee = null;
        decimal? lwfEmployer = null;
        if (lwfConfigs.Count > 0)
        {
            lwfEmployee = lwfConfigs.Sum(l => l.EmployeeAmount);
            lwfEmployer = lwfConfigs.Sum(l => l.EmployerAmount);
        }

        return new StatutoryConfig(
            NewRegimeSlabs: newRegimeSlabs,
            SurchargeSlabs: surchargeConfig,
            StandardDeduction: taxConfig?.StandardDeduction ?? 75_000m,
            Rebate87ALimit: taxConfig?.Rebate87ALimit ?? 700_000m,
            Rebate87AAmount: taxConfig?.Rebate87AAmount ?? 25_000m,
            CessRate: 0.04m,
            PFWageCap: PfWageCap,
            EPFEmployeeRate: EpfEmployeeRate,
            EPFEmployerRate: EpfEmployerRate,
            EPSEmployerRate: EpsEmployerRate,
            EPSCap: EpsCap,
            EDLIEmployerRate: EdliEmployerRate,
            EDLICap: EdliCap,
            EPFAdminRate: EpfAdminRate,
            EPFAdminMinimum: EpfAdminMinimum,
            ESIWageLimit: EsiWageLimit,
            ESIPWDWageLimit: EsiPwdWageLimit,
            ESIEmployeeRate: EsiEmployeeRate,
            ESIEmployerRate: EsiEmployerRate,
            PTSlabs: ptSlabInputs,
            LWFEmployeeAmount: lwfEmployee,
            LWFEmployerAmount: lwfEmployer,
            PFEnabled: orgConfig.EpfEnabled,
            ESIEnabled: orgConfig.EsiEnabled,
            PTEnabled: true,
            LWFEnabled: lwfConfigs.Count > 0
        );
    }
}
