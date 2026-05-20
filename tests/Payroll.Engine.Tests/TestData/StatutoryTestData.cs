using Payroll.Engine.Inputs;

namespace Payroll.Engine.Tests.TestData;

public static class StatutoryTestData
{
    // FY2026 new regime tax slabs — Finance Act 2025
    public static IReadOnlyList<TaxSlab> NewRegimeSlabs_FY2026() =>
    [
        new TaxSlab(0m,           300_000m,  0.00m),
        new TaxSlab(300_000m,     700_000m,  0.05m),
        new TaxSlab(700_000m,   1_000_000m,  0.10m),
        new TaxSlab(1_000_000m, 1_200_000m,  0.15m),
        new TaxSlab(1_200_000m, 1_500_000m,  0.20m),
        new TaxSlab(1_500_000m, null,         0.30m)
    ];

    public static StatutoryConfig DefaultConfig_FY2026() => new(
        NewRegimeSlabs: NewRegimeSlabs_FY2026(),
        SurchargeSlabs:
        [
            new SurchargeConfig(5_000_000m,  10_000_000m, 0.10m),
            new SurchargeConfig(10_000_000m, 20_000_000m, 0.15m),
            new SurchargeConfig(20_000_000m, null,         0.25m)
        ],
        StandardDeduction: 75_000m,
        Rebate87ALimit: 700_000m,
        Rebate87AAmount: 25_000m,
        CessRate: 0.04m,
        PFWageCap: 15_000m,
        EPFEmployeeRate: 0.12m,
        EPSEmployerRate: 0.0833m,
        EPSCap: 1_250m,
        EDLIEmployerRate: 0.005m,
        EDLICap: 75m,
        EPFAdminRate: 0.005m,
        EPFAdminMinimum: 500m,
        EpfRestrictEmployerWage: true,
        EpfConsiderSalaryOnLop: true,
        EpfProRateRestrictedPfWage: false,
        ESIWageLimit: 21_000m,
        ESIPWDWageLimit: 25_000m,
        ESIEmployeeRate: 0.0075m,
        ESIEmployerRate: 0.0325m,
        PTSlabs: [],
        LWFStates: [],
        PFEnabled: true,
        ESIEnabled: true,
        PTEnabled: false,
        EpfIncludeEmployerInCtc: true,
        EpfIncludeEdliInCtc: false,
        EpfIncludeAdminInCtc: false,
        GratuityIncludedInCtc: true
    );
}
