using Payroll.Domain.Entities;

namespace Payroll.Application.Tests.TestData;

// Seeded FY 2025-26 statutory fixtures. StatutoryConfigBuilder hard-fails on a
// missing IncomeTaxConfig or empty slabs, so handler tests must stub these.
public static class StatutoryTestFixtures
{
    public const string FyKey = "2025-26";

    public static IncomeTaxConfig IncomeTaxConfig2526() => IncomeTaxConfig.Create(
        FyKey, "New",
        standardDeduction: 75_000m, rebate87ALimit: 1_200_000m, rebate87AAmount: 60_000m,
        employerStatutoryCap: 750_000m, npsEmployerMaxRate: 0.14m,
        cessRate: 0.04m,
        pfWageCap: 15_000m, epfEmployeeRate: 0.12m, epsEmployerRate: 0.0833m, epsCap: 1_250m,
        esiWageLimit: 21_000m, esiPwdWageLimit: 25_000m, esiEmployeeRate: 0.0075m, esiEmployerRate: 0.0325m,
        createdBy: Guid.Empty,
        edliRate: 0.005m, edliWageCap: 15_000m, edliMaxAmount: 75m,
        epfAdminRate: 0.005m, pan206AARate: 0.20m);

    public static List<IncomeTaxSlab> Slabs2526() =>
    [
        IncomeTaxSlab.Create(FyKey, "New", 0m, 400_000m, 0m, Guid.Empty),
        IncomeTaxSlab.Create(FyKey, "New", 400_000m, 800_000m, 0.05m, Guid.Empty),
        IncomeTaxSlab.Create(FyKey, "New", 800_000m, 1_200_000m, 0.10m, Guid.Empty),
        IncomeTaxSlab.Create(FyKey, "New", 1_200_000m, 1_600_000m, 0.15m, Guid.Empty),
        IncomeTaxSlab.Create(FyKey, "New", 1_600_000m, 2_000_000m, 0.20m, Guid.Empty),
        IncomeTaxSlab.Create(FyKey, "New", 2_000_000m, 2_400_000m, 0.25m, Guid.Empty),
        IncomeTaxSlab.Create(FyKey, "New", 2_400_000m, null, 0.30m, Guid.Empty),
    ];
}
