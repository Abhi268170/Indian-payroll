using FluentAssertions;
using MediatR;
using NSubstitute;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Application.Interfaces;
using Payroll.Application.Queries.PayrollRuns;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Application.DTOs;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;
using Xunit;

using DomainPayrollRun = Payroll.Domain.Entities.PayrollRun;

namespace Payroll.Application.Tests.PayrollRun;

/// <summary>
/// Tests for WI-03 (approval uses FnF orchestrator) and WI-04 (TDS worksheet).
/// </summary>
public class ApproveFnfRunTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    // ─── WI-03: approval branches correctly ─────────────────────────────────

    [Fact]
    public async Task Approve_FnfRun_CallsFnfOrchestrator_NotRecomputeService()
    {
        var (handler, deps) = BuildHandler(PayrollRunType.BulkFinalSettlement);

        await handler.Handle(new ApprovePayrollRunCommand(RunId, ActorId), CancellationToken.None);

        await deps.FnfOrchestrator.Received(1).ComputeAsync(RunId, EmployeeId, Arg.Any<CancellationToken>());
        await deps.RecomputeService.DidNotReceive().RecomputeEmployeeAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_FinalSettlementRun_CallsFnfOrchestrator_NotRecomputeService()
    {
        var (handler, deps) = BuildHandler(PayrollRunType.FinalSettlement);

        await handler.Handle(new ApprovePayrollRunCommand(RunId, ActorId), CancellationToken.None);

        await deps.FnfOrchestrator.Received(1).ComputeAsync(RunId, EmployeeId, Arg.Any<CancellationToken>());
        await deps.RecomputeService.DidNotReceive().RecomputeEmployeeAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_RegularRun_CallsRecomputeService_NotFnfOrchestrator()
    {
        var (handler, deps) = BuildHandler(PayrollRunType.Regular);

        await handler.Handle(new ApprovePayrollRunCommand(RunId, ActorId), CancellationToken.None);

        await deps.RecomputeService.Received(1).RecomputeEmployeeAsync(
            RunId, EmployeeId, Arg.Any<CancellationToken>());
        await deps.FnfOrchestrator.DidNotReceive().ComputeAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ─── WI-04: TDS worksheet written ────────────────────────────────────────

    [Fact]
    public async Task Approve_FnfRun_WritesTdsWorksheet()
    {
        var (handler, deps) = BuildHandler(PayrollRunType.BulkFinalSettlement);

        await handler.Handle(new ApprovePayrollRunCommand(RunId, ActorId), CancellationToken.None);

        await deps.TdsWorksheetRepo.Received(1).DeleteByRunAndEmployeeAsync(
            RunId, EmployeeId, Arg.Any<CancellationToken>());
        await deps.TdsWorksheetRepo.Received(1).AddAsync(
            Arg.Any<TdsWorksheet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_RegularRun_DoesNotWriteTdsWorksheetDirectly()
    {
        // Regular path uses recomputeService which handles its own worksheet.
        var (handler, deps) = BuildHandler(PayrollRunType.Regular);

        await handler.Handle(new ApprovePayrollRunCommand(RunId, ActorId), CancellationToken.None);

        await deps.TdsWorksheetRepo.DidNotReceive().AddAsync(
            Arg.Any<TdsWorksheet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_FnfRun_UpdatesPayrunEmployeeAmounts()
    {
        var (handler, deps) = BuildHandler(PayrollRunType.BulkFinalSettlement);

        await handler.Handle(new ApprovePayrollRunCommand(RunId, ActorId), CancellationToken.None);

        // PayrunEmployee must be updated via repo after ApplyToPayrunEmployee.
        deps.PayrunEmployeeRepo.Received().Update(Arg.Any<PayrunEmployee>());
    }

    // ─── PayrollFnfOrchestrator.BuildWorksheet — remainingMonthsInFy=1 ───────

    [Fact]
    public void BuildWorksheet_RemainingMonthsInFy_IsAlwaysOne()
    {
        // Regardless of when in the FY the exit happens, FnF worksheet must
        // show remainingMonthsInFy=1 because engine ran with MonthsRemainingInFY=1.
        DomainPayrollRun run = MakeRun(PayrollRunType.BulkFinalSettlement);
        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        FnfEngineResult fnf = MakeFnfResult();

        TdsWorksheet ws = PayrollFnfOrchestrator.BuildWorksheet(run, pe, fnf, ActorId);

        ws.RemainingMonthsInFy.Should().Be(1,
            "FnF TDS worksheet must always use remainingMonthsInFy=1 — engine ran with MonthsRemainingInFY=1");
    }

    [Fact]
    public void BuildWorksheet_TdsThisMonth_UsesOverrideWhenSet()
    {
        DomainPayrollRun run = MakeRun(PayrollRunType.BulkFinalSettlement);
        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        pe.SetTdsOverride(99_999m, "test override", ActorId);
        FnfEngineResult fnf = MakeFnfResult(engineMonthlyTds: 5_000m);

        TdsWorksheet ws = PayrollFnfOrchestrator.BuildWorksheet(run, pe, fnf, ActorId);

        ws.TdsThisMonth.Should().Be(99_999m,
            "TDS override must take precedence over engine TDS in the worksheet");
    }

    [Fact]
    public void BuildWorksheet_TdsThisMonth_UsesEngineWhenNoOverride()
    {
        DomainPayrollRun run = MakeRun(PayrollRunType.BulkFinalSettlement);
        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        FnfEngineResult fnf = MakeFnfResult(engineMonthlyTds: 12_500m);

        TdsWorksheet ws = PayrollFnfOrchestrator.BuildWorksheet(run, pe, fnf, ActorId);

        ws.TdsThisMonth.Should().Be(12_500m);
    }

    [Fact]
    public void BuildWorksheet_YtdTdsDeducted_MatchesFnfResult()
    {
        DomainPayrollRun run = MakeRun(PayrollRunType.BulkFinalSettlement);
        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        FnfEngineResult fnf = MakeFnfResult(ytdTdsDeducted: 67_500m);

        TdsWorksheet ws = PayrollFnfOrchestrator.BuildWorksheet(run, pe, fnf, ActorId);

        ws.YtdTdsDeducted.Should().Be(67_500m);
    }

    // ─── PayrollFnfOrchestrator.ApplyToPayrunEmployee ────────────────────────

    [Fact]
    public void ApplyToPayrunEmployee_NetPay_UsesNetPayWithAdjustments()
    {
        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        FnfEngineResult fnf = MakeFnfResult(netPayWithAdj: 88_000m);

        PayrollFnfOrchestrator.ApplyToPayrunEmployee(pe, fnf, ActorId);

        pe.NetPay.Should().Be(88_000m);
    }

    [Fact]
    public void ApplyToPayrunEmployee_TdsAmount_UsesOverrideWhenSet()
    {
        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        pe.SetTdsOverride(5_000m, "override", ActorId);
        FnfEngineResult fnf = MakeFnfResult(engineMonthlyTds: 12_000m);

        PayrollFnfOrchestrator.ApplyToPayrunEmployee(pe, fnf, ActorId);

        pe.TdsAmount.Should().Be(5_000m, "override must take precedence over engine TDS");
    }

    [Fact]
    public void ApplyToPayrunEmployee_TdsAmount_UsesEngineWhenNoOverride()
    {
        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        FnfEngineResult fnf = MakeFnfResult(engineMonthlyTds: 12_000m);

        PayrollFnfOrchestrator.ApplyToPayrunEmployee(pe, fnf, ActorId);

        pe.TdsAmount.Should().Be(12_000m);
    }

    [Fact]
    public void ApplyToPayrunEmployee_MonthlyCTC_Preserved()
    {
        // MonthlyCTC was set at seeding time (WI-01) and must not be zeroed by Apply.
        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        pe.SetMonthlyCTC(108_333m, ActorId);
        FnfEngineResult fnf = MakeFnfResult();

        PayrollFnfOrchestrator.ApplyToPayrunEmployee(pe, fnf, ActorId);

        pe.MonthlyCTC.Should().Be(108_333m, "ApplyToPayrunEmployee must not overwrite MonthlyCTC set during seeding");
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private sealed record Deps(
        IPayrollFnfOrchestrator FnfOrchestrator,
        IPayrollRecomputeService RecomputeService,
        ITdsWorksheetRepository TdsWorksheetRepo,
        IPayrunEmployeeRepository PayrunEmployeeRepo);

    private (ApprovePayrollRunHandler Handler, Deps Deps) BuildHandler(PayrollRunType runType)
    {
        var runRepo = Substitute.For<IPayrollRunRepository>();
        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        var auditLogRepo = Substitute.For<IPayrollRunAuditLogRepository>();
        var recomputeService = Substitute.For<IPayrollRecomputeService>();
        var fnfOrchestrator = Substitute.For<IPayrollFnfOrchestrator>();
        var tdsWorksheetRepo = Substitute.For<ITdsWorksheetRepository>();
        var costCalculator = Substitute.For<IPayrollCostCalculator>();
        var uow = Substitute.For<IUnitOfWork>();
        var sender = Substitute.For<ISender>();
        var jobDispatcher = Substitute.For<IPayrollJobDispatcher>();

        DomainPayrollRun run = MakeRun(runType);
        runRepo.GetByIdAsync(RunId, Arg.Any<CancellationToken>()).Returns(run);

        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        pe.SetMonthlyCTC(108_333m, ActorId);

        payrunEmpRepo.GetByRunIdAsync(RunId, Arg.Any<CancellationToken>())
            .Returns(new List<PayrunEmployee> { pe });

        sender.Send(Arg.Any<GetPendingTasksQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PendingTasksDto([], []));

        fnfOrchestrator.ComputeAsync(RunId, EmployeeId, Arg.Any<CancellationToken>())
            .Returns(MakeFnfResult());

        recomputeService.RecomputeEmployeeAsync(RunId, EmployeeId, Arg.Any<CancellationToken>())
            .Returns(new RecomputeResult(
                Engine: MakeFnfResult().Engine,
                ReimbursementsAmount: 0m,
                DeductionsAmount: 0m,
                NetPayWithAdjustments: 85_000m));

        costCalculator.Calculate(Arg.Any<IReadOnlyList<PayrunEmployee>>())
            .Returns(new PayrollCostSnapshot(
                TotalGross: 100_000m,
                TotalNet: 85_000m,
                TotalEmployerPf: 1_800m,
                TotalEmployerEps: 1_250m,
                TotalEmployerEsi: 3_250m,
                TotalLwfEmployer: 0m,
                TotalGratuity: 0m,
                TotalTds: 15_000m,
                TotalPt: 200m,
                EmployeeCount: 1,
                PayrollCost: 105_000m));

        var exitRepo = Substitute.For<IEmployeeExitRepository>();
        exitRepo.GetActiveByEmployeeAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Payroll.Domain.Entities.EmployeeExit?)null);

        var salaryRevisionRepo = Substitute.For<ISalaryRevisionRepository>();
        var breakdownRepo = Substitute.For<IPayrunComponentBreakdownRepository>();
        var handler = new ApprovePayrollRunHandler(
            runRepo, payrunEmpRepo, auditLogRepo, recomputeService,
            fnfOrchestrator, tdsWorksheetRepo, exitRepo,
            costCalculator, salaryRevisionRepo, breakdownRepo, uow, sender, jobDispatcher);

        return (handler, new Deps(fnfOrchestrator, recomputeService, tdsWorksheetRepo, payrunEmpRepo));
    }

    private static DomainPayrollRun MakeRun(PayrollRunType runType)
    {
        string snapshot = System.Text.Json.JsonSerializer.Serialize(MinimalStatutoryConfig());
        return runType switch
        {
            PayrollRunType.BulkFinalSettlement => DomainPayrollRun.CreateBulkFinalSettlement(
                tenantId: TenantId,
                payPeriod: new Domain.ValueObjects.PayPeriod(2026, 6),
                payDay: new DateOnly(2026, 6, 30),
                statutoryConfigSnapshot: snapshot,
                createdBy: ActorId),
            PayrollRunType.FinalSettlement => DomainPayrollRun.CreateFinalSettlement(
                tenantId: TenantId,
                payPeriod: new Domain.ValueObjects.PayPeriod(2026, 6),
                payDay: new DateOnly(2026, 6, 30),
                employeeExitId: Guid.NewGuid(),
                statutoryConfigSnapshot: snapshot,
                createdBy: ActorId),
            _ => DomainPayrollRun.Create(
                tenantId: TenantId,
                payPeriod: new Domain.ValueObjects.PayPeriod(2026, 4),
                type: PayrollRunType.Regular,
                payDay: new DateOnly(2026, 4, 30),
                statutoryConfigSnapshot: snapshot,
                employeeCount: 1,
                createdBy: ActorId),
        };
    }

    private static FnfEngineResult MakeFnfResult(
        decimal netPayWithAdj = 85_000m,
        decimal engineMonthlyTds = 15_000m,
        decimal ytdTdsDeducted = 30_000m)
    {
        var engine = new PayrollResult(
            EmployeeId: EmployeeId,
            Gross: new GrossResult(
                GrossWage: 100_000m,
                PFWage: 15_000m,
                FullPFWage: 15_000m,
                AnnualProjectedGross: 1_200_000m,
                LOPDeduction: 0m,
                ArrearAmount: 0m,
                ComponentBreakdown: [],
                TaxableGrossWage: 95_000m,
                AnnualProjectedTaxableGross: 1_140_000m,
                ESIWage: 0m),
            PF: new PFResult(
                EmployeeContribution: 1_800m,
                VPFContribution: 0m,
                EPFEmployerContribution: 550m,
                EPSEmployerContribution: 1_250m,
                IsExempt: false),
            ESI: new ESIResult(
                EmployeeContribution: 750m,
                EmployerContribution: 3_250m,
                IsExempt: false),
            PT: new PTResult(Amount: 200m, IsExempt: false),
            LWF: new LWFResult(EmployeeAmount: 0m, EmployerAmount: 0m, IsExempt: true),
            TDS: new TDSResult(
                MonthlyTDS: engineMonthlyTds,
                AnnualProjectedTax: engineMonthlyTds,
                TaxableIncome: 900_000m,
                TaxBeforeRebate: engineMonthlyTds,
                Surcharge: 0m,
                Cess: 0m,
                HasPanOverride: false,
                Rebate87AApplied: false),
            Gratuity: new GratuityResult(MonthlyAccrual: 0m, IsExempt: true),
            NetPay: netPayWithAdj);

        return new FnfEngineResult(
            Engine: engine,
            ReimbursementsAmount: 0m,
            NetPayWithAdjustments: netPayWithAdj,
            YtdTdsDeducted: ytdTdsDeducted,
            StaticConfig: MinimalStatutoryConfig(),
            LwdFiscalYear: 2025);
    }

    private static StatutoryConfig MinimalStatutoryConfig() => new(
        NewRegimeSlabs: [],
        SurchargeSlabs: [],
        StandardDeduction: 75_000m,
        Rebate87ALimit: 700_000m,
        Rebate87AAmount: 25_000m,
        CessRate: 0.04m,
        PFWageCap: 15_000m,
        EPFEmployeeRate: 0.12m,
        EPSEmployerRate: 0.0833m,
        EPSCap: 1_250m,
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
        EpfIncludeEmployerInCtc: false,
        GratuityIncludedInCtc: false);
}
