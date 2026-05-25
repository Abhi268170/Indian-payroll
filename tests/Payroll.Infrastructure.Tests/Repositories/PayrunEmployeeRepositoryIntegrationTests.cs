using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.ValueObjects;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Persistence.Repositories;
using Payroll.Infrastructure.Tests.Fixtures;
using Xunit;

namespace Payroll.Infrastructure.Tests.Repositories;

// Integration coverage for the new code paths introduced by the TDS-correctness
// fix (audit findings #1, #2, #3, #5) + the FnF LWF lookback (#4). Exercises real
// PostgreSQL + real EF Core + real migrations — proves the engine-row wiring
// survives end-to-end, which mocks in Application.Tests cannot.
public sealed class PayrunEmployeeRepositoryIntegrationTests : IClassFixture<PostgresTenantFixture>
{
    private readonly PostgresTenantFixture _fixture;
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public PayrunEmployeeRepositoryIntegrationTests(PostgresTenantFixture fixture)
    {
        _fixture = fixture;
    }

    // ── #1 + persistence: TaxableGrossPay round-trips through EF + migration ─

    [Fact]
    public async Task TaxableGrossPay_RoundTripsThroughEf()
    {
        Guid runId = await SeedRunAsync(2024, 4, PayrollRunStatus.Approved);
        Guid employeeId = Guid.NewGuid();

        await using (PayrollDbContext db = _fixture.NewContext())
        {
            PayrunEmployee row = PayrunEmployee.Create(runId, employeeId, PostgresTenantFixture.TenantId, 31, Actor);
            row.UpdateComputedAmounts(
                grossPay: 100_000m,
                taxableGrossPay: 70_000m,
                netPay: 88_000m,
                taxesAmount: 6_000m,
                benefitsAmount: 1_800m,
                reimbursementsAmount: 0m,
                employeePf: 1_800m,
                employerPf: 1_800m,
                employeeEsi: 0m,
                employerEsi: 0m,
                ptAmount: 200m,
                tdsAmount: 5_800m,
                lwfEmployeeAmount: 20m,
                lwfEmployerAmount: 40m,
                gratuityAmount: 0m,
                epsAmount: 0m,
                monthlyCTC: 100_000m,
                actorId: Actor);
            db.PayrunEmployees.Add(row);
            await db.SaveChangesAsync();
        }

        await using PayrollDbContext readDb = _fixture.NewContext();
        PayrunEmployee? reloaded = await readDb.PayrunEmployees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        reloaded.Should().NotBeNull();
        reloaded!.GrossPay.Should().Be(100_000m);
        reloaded.TaxableGrossPay.Should().Be(70_000m);
    }

    // ── #1: GetCurrentEmployerYtdAsync returns YtdTaxableGross separately ────

    [Fact]
    public async Task GetCurrentEmployerYtdAsync_ReturnsTaxableGross_DistinctFromGross()
    {
        Guid employeeId = Guid.NewGuid();
        // Two approved runs in FY 2024-25 with the same gross but different taxable
        // portions (some non-taxable HRA included in gross only).
        await SeedPayrunEmployeeAsync(2024, 5, employeeId, gross: 100_000m, taxable: 70_000m, tds: 5_000m);
        await SeedPayrunEmployeeAsync(2024, 6, employeeId, gross: 100_000m, taxable: 70_000m, tds: 5_000m);

        await using PayrollDbContext db = _fixture.NewContext();
        PayrunEmployeeRepository repo = new(db);
        Dictionary<Guid, (decimal YtdGross, decimal YtdTaxableGross, decimal YtdTds)> ytd =
            await repo.GetCurrentEmployerYtdAsync([employeeId], fiscalYear: 2024);

        ytd.Should().ContainKey(employeeId);
        (decimal YtdGross, decimal YtdTaxableGross, decimal YtdTds) row = ytd[employeeId];
        row.YtdGross.Should().Be(200_000m);
        row.YtdTaxableGross.Should().Be(140_000m);
        row.YtdTds.Should().Be(10_000m);
    }

    // ── #4: HasLwfDeductedInPeriodAsync detects approved row with LWF > 0 ────

    [Fact]
    public async Task HasLwfDeductedInPeriodAsync_ReturnsTrue_WhenApprovedRunHasLwf()
    {
        Guid employeeId = Guid.NewGuid();
        await SeedPayrunEmployeeAsync(2024, 7, employeeId, gross: 25_000m, taxable: 25_000m, tds: 0m,
            lwfEmployee: 20m, lwfEmployer: 40m);

        await using PayrollDbContext db = _fixture.NewContext();
        PayrunEmployeeRepository repo = new(db);

        bool found = await repo.HasLwfDeductedInPeriodAsync(employeeId, year: 2024, firstMonth: 4, lastMonth: 9);
        found.Should().BeTrue();
    }

    [Fact]
    public async Task HasLwfDeductedInPeriodAsync_ReturnsFalse_WhenLwfZero()
    {
        Guid employeeId = Guid.NewGuid();
        await SeedPayrunEmployeeAsync(2024, 8, employeeId, gross: 25_000m, taxable: 25_000m, tds: 0m,
            lwfEmployee: 0m, lwfEmployer: 0m);

        await using PayrollDbContext db = _fixture.NewContext();
        PayrunEmployeeRepository repo = new(db);

        bool found = await repo.HasLwfDeductedInPeriodAsync(employeeId, year: 2024, firstMonth: 4, lastMonth: 9);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task HasLwfDeductedInPeriodAsync_IgnoresDraftRuns()
    {
        Guid employeeId = Guid.NewGuid();
        // Draft run with LWF > 0 must NOT count — only Approved/Paid runs are part of YTD.
        await SeedPayrunEmployeeAsync(2024, 9, employeeId, gross: 25_000m, taxable: 25_000m, tds: 0m,
            lwfEmployee: 20m, lwfEmployer: 40m, runStatus: PayrollRunStatus.Draft);

        await using PayrollDbContext db = _fixture.NewContext();
        PayrunEmployeeRepository repo = new(db);

        bool found = await repo.HasLwfDeductedInPeriodAsync(employeeId, year: 2024, firstMonth: 4, lastMonth: 9);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task HasLwfDeductedInPeriodAsync_RespectsMonthWindow()
    {
        Guid employeeId = Guid.NewGuid();
        // LWF deducted in Oct 2024 (month 10, H2); H1 lookup (Apr..Sep) must not find it.
        await SeedPayrunEmployeeAsync(2024, 10, employeeId, gross: 25_000m, taxable: 25_000m, tds: 0m,
            lwfEmployee: 20m, lwfEmployer: 40m);

        await using PayrollDbContext db = _fixture.NewContext();
        PayrunEmployeeRepository repo = new(db);

        bool foundOutOfRange = await repo.HasLwfDeductedInPeriodAsync(employeeId, year: 2024, firstMonth: 4, lastMonth: 9);
        foundOutOfRange.Should().BeFalse();

        bool foundInRange = await repo.HasLwfDeductedInPeriodAsync(employeeId, year: 2024, firstMonth: 10, lastMonth: 12);
        foundInRange.Should().BeTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> SeedRunAsync(int year, int month, PayrollRunStatus status)
    {
        await using PayrollDbContext db = _fixture.NewContext();
        PayrollRun run = PayrollRun.Create(
            tenantId: PostgresTenantFixture.TenantId,
            payPeriod: new PayPeriod(year, month),
            type: PayrollRunType.Regular,
            payDay: new DateOnly(year, month, 25),
            statutoryConfigSnapshot: null,
            employeeCount: 1,
            createdBy: Actor);
        if (status == PayrollRunStatus.Approved || status == PayrollRunStatus.Paid)
            run.Approve(Actor);
        if (status == PayrollRunStatus.Paid)
            run.RecordPayment(new DateOnly(year, month, 25), "BankTransfer", null, Actor);
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync();
        return run.Id;
    }

    private async Task SeedPayrunEmployeeAsync(
        int year, int month, Guid employeeId,
        decimal gross, decimal taxable, decimal tds,
        decimal lwfEmployee = 0m, decimal lwfEmployer = 0m,
        PayrollRunStatus runStatus = PayrollRunStatus.Approved)
    {
        Guid runId = await SeedRunAsync(year, month, runStatus);

        await using PayrollDbContext db = _fixture.NewContext();
        PayrunEmployee row = PayrunEmployee.Create(runId, employeeId, PostgresTenantFixture.TenantId, 30, Actor);
        row.UpdateComputedAmounts(
            grossPay: gross, taxableGrossPay: taxable, netPay: gross - tds,
            taxesAmount: tds, benefitsAmount: 0m, reimbursementsAmount: 0m,
            employeePf: 0m, employerPf: 0m, employeeEsi: 0m, employerEsi: 0m,
            ptAmount: 0m, tdsAmount: tds,
            lwfEmployeeAmount: lwfEmployee, lwfEmployerAmount: lwfEmployer,
            gratuityAmount: 0m, epsAmount: 0m, monthlyCTC: gross, actorId: Actor);
        db.PayrunEmployees.Add(row);
        await db.SaveChangesAsync();
    }
}
