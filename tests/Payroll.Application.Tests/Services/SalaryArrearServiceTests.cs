using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Payroll.Application.Services;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;
using Payroll.Engine.Inputs;
using Xunit;
using DomainPayrollRun = Payroll.Domain.Entities.PayrollRun;

namespace Payroll.Application.Tests.Services;

// WI-018 step 4 — orchestration shell around the pure arrear calculator.
public class SalaryArrearServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid TemplateId = Guid.NewGuid();

    private static void SetId<T>(T entity, Guid id)
    {
        PropertyInfo prop = typeof(T).GetProperty("Id") ?? typeof(T).BaseType!.GetProperty("Id")!;
        prop.SetValue(entity, id);
    }

    private static void SetComponent(SalaryStructureComponent slot, SalaryComponent component) =>
        typeof(SalaryStructureComponent).GetProperty(nameof(SalaryStructureComponent.Component))!
            .SetValue(slot, component);

    private static StatutoryConfig NoCtcDeductionsConfig() => new(
        NewRegimeSlabs: [], SurchargeSlabs: [], StandardDeduction: 75_000m,
        Rebate87ALimit: 700_000m, Rebate87AAmount: 25_000m, CessRate: 0.04m,
        PFWageCap: 15_000m, EPFEmployeeRate: 0.12m, EPSEmployerRate: 0.0833m, EPSCap: 1_250m,
        EpfRestrictEmployerWage: true, EpfConsiderSalaryOnLop: true, EpfProRateRestrictedPfWage: false,
        ESIWageLimit: 21_000m, ESIPWDWageLimit: 25_000m, ESIEmployeeRate: 0.0075m, ESIEmployerRate: 0.0325m,
        PTSlabs: [], LWFStates: [], PFEnabled: true, ESIEnabled: true, PTEnabled: false,
        EpfIncludeEmployerInCtc: false, GratuityIncludedInCtc: false);

    private static (SalaryComponent basic, SalaryComponent hra, SalaryComponent residual) MakeComponents()
    {
        SalaryComponent basic = SalaryComponent.CreateEarning(
            "Basic Salary", "Basic Salary", "BASICSALARY", EarningType.Basic, PayType.Monthly,
            ComponentFormulaType.PercentOfCTC, null, 37.5m, true, true, EpfInclusionRule.Always,
            true, true, true, TenantId, ActorId);
        SalaryComponent hra = SalaryComponent.CreateEarning(
            "HRA", "HRA", "HRA", EarningType.HouseRentAllowance, PayType.Monthly,
            ComponentFormulaType.PercentOfBasic, null, 40m, true, false, EpfInclusionRule.Always,
            false, true, true, TenantId, ActorId);
        SalaryComponent residual = SalaryComponent.CreateSystemFixedAllowance(TenantId, ActorId);
        return (basic, hra, residual);
    }

    private static SalaryStructureTemplate MakeTemplate()
    {
        var (basic, hra, residual) = MakeComponents();
        SalaryStructureComponent sb = SalaryStructureComponent.Create(TemplateId, basic.Id, ComponentFormulaType.PercentOfCTC, null, 37.5m, 1);
        SalaryStructureComponent sh = SalaryStructureComponent.Create(TemplateId, hra.Id, ComponentFormulaType.PercentOfBasic, null, 40m, 2);
        SalaryStructureComponent sr = SalaryStructureComponent.Create(TemplateId, residual.Id, ComponentFormulaType.ResidualCTC, null, null, 3);
        SetComponent(sb, basic);
        SetComponent(sh, hra);
        SetComponent(sr, residual);
        SalaryStructureTemplate t = SalaryStructureTemplate.Create("Test", null, TenantId, ActorId);
        SetId(t, TemplateId);
        t.SetComponents([sb, sh, sr]);
        return t;
    }

    // Old run (CTC 12,00,000, no LOP): monthly gross 1,00,000 → Basic 37500, HRA 15000, Residual 47500.
    private static IReadOnlyList<PayrunComponentBreakdown> OldBreakdowns(Guid runId)
    {
        PayrunComponentBreakdown Row(string code, decimal amt) => PayrunComponentBreakdown.Create(
            runId, EmployeeId, TenantId, Guid.NewGuid(), code, code, amt, amt,
            isOneTimeEarning: false, isTaxable: true);
        return [Row("BASICSALARY", 37500m), Row("HRA", 15000m), Row("FIXED_ALLOWANCE", 47500m)];
    }

    private sealed class Mocks
    {
        public ISalaryRevisionRepository RevisionRepo = Substitute.For<ISalaryRevisionRepository>();
        public IPayrollRunRepository RunRepo = Substitute.For<IPayrollRunRepository>();
        public IPayrunEmployeeRepository PayrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        public IPayrunComponentBreakdownRepository BreakdownRepo = Substitute.For<IPayrunComponentBreakdownRepository>();
        public ISalaryStructureTemplateRepository TemplateRepo = Substitute.For<ISalaryStructureTemplateRepository>();
        public ISalaryComponentRepository ComponentRepo = Substitute.For<ISalaryComponentRepository>();

        public SalaryArrearService Build() =>
            new(RevisionRepo, RunRepo, PayrunEmpRepo, BreakdownRepo, TemplateRepo, ComponentRepo);
    }

    private static SalaryRevision MakeRevision(decimal newCtc)
    {
        SalaryRevision r = SalaryRevision.Create(
            EmployeeId, TenantId, previousAnnualCTC: 1_200_000m, newAnnualCTC: newCtc,
            effectiveFromMonth: 3, effectiveFromYear: 2026,
            payoutMonth: 5, payoutYear: 2026,
            salaryStructureTemplateId: TemplateId, notes: null, createdBy: ActorId);
        SetId(r, Guid.NewGuid());
        return r;
    }

    [Fact]
    public async Task ComputeForRevision_OneFinalisedMonth_OneMissing_ArrearsAndExclusion()
    {
        var m = new Mocks();
        SalaryRevision revision = MakeRevision(1_320_000m); // 10% hike
        m.RevisionRepo.GetByIdWithOverridesAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        m.TemplateRepo.GetByIdWithComponentsAsync(TemplateId, Arg.Any<CancellationToken>()).Returns(MakeTemplate());

        // March 2026 — finalised run with frozen config
        var run = DomainPayrollRun.Create(
            tenantId: TenantId, payPeriod: new PayPeriod(2026, 3), type: PayrollRunType.Regular,
            payDay: new DateOnly(2026, 3, 31),
            statutoryConfigSnapshot: JsonSerializer.Serialize(NoCtcDeductionsConfig()),
            employeeCount: 1, createdBy: ActorId);
        SetId(run, Guid.NewGuid());
        m.RunRepo.GetFinalisedRegularRunForPeriodAsync(2026, 3, Arg.Any<CancellationToken>()).Returns(run);
        m.RunRepo.GetFinalisedRegularRunForPeriodAsync(2026, 4, Arg.Any<CancellationToken>()).Returns((DomainPayrollRun?)null);

        m.PayrunEmpRepo.GetByRunAndEmployeeAsync(run.Id, EmployeeId, Arg.Any<CancellationToken>())
            .Returns(PayrunEmployee.Create(run.Id, EmployeeId, TenantId, baseDays: 30, ActorId));
        m.BreakdownRepo.GetByRunAndEmployeeAsync(run.Id, EmployeeId, Arg.Any<CancellationToken>())
            .Returns(OldBreakdowns(run.Id));

        SalaryArrearResult result = await m.Build().ComputeForRevisionAsync(revision.Id, CancellationToken.None);

        // Monthly gross rose 1,00,000 → 1,10,000 = 10,000 arrear for the one finalised month.
        result.TotalArrear.Should().Be(10_000m);
        result.Lines.Sum(l => l.Amount).Should().Be(10_000m);
        result.ExcludedMonths.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Year = 2026, Month = 4 },
                o => o.ExcludingMissingMembers());
    }
}
