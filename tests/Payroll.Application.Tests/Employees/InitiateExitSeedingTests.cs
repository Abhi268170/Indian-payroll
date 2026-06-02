using FluentAssertions;
using NSubstitute;
using Payroll.Application.Commands.Employees;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine.Inputs;
using System.Reflection;
using Xunit;

using DomainPayrollRun = Payroll.Domain.Entities.PayrollRun;
using DomainPaySchedule = Payroll.Domain.Entities.PaySchedule;

namespace Payroll.Application.Tests.Employees;

/// <summary>
/// Tests for WI-01 (salary component seeding) and WI-02 (salary structure validation)
/// in InitiateExitCommand.
/// </summary>
public class InitiateExitSeedingTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid DeptId = Guid.NewGuid();
    private static readonly Guid DesigId = Guid.NewGuid();
    private static readonly Guid WorkLocId = Guid.NewGuid();

    // ─── helpers ────────────────────────────────────────────────────────────

    private static void SetNavProp<TEntity, TProp>(TEntity entity, string propName, TProp value)
    {
        PropertyInfo prop = typeof(TEntity).GetProperty(propName)!;
        prop.SetValue(entity, value);
    }

    private static StatutoryConfig MinimalConfig() => new(
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

    private static SalaryComponent MakeBasic() =>
        SalaryComponent.CreateEarning(
            "Basic Salary", "Basic Salary", "BASICSALARY",
            EarningType.Basic, PayType.Monthly,
            ComponentFormulaType.PercentOfCTC,
            fixedAmount: null, percentage: 40m,
            isTaxable: true, considerForEpf: true,
            EpfInclusionRule.Always,
            considerForEsi: true, calculateOnProRata: true,
            showInPayslip: true, TenantId, ActorId);

    private static SalaryComponent MakeHra() =>
        SalaryComponent.CreateEarning(
            "HRA", "HRA", "HRA",
            EarningType.HouseRentAllowance, PayType.Monthly,
            ComponentFormulaType.PercentOfBasic,
            fixedAmount: null, percentage: 40m,
            isTaxable: true, considerForEpf: false,
            EpfInclusionRule.Always,
            considerForEsi: false, calculateOnProRata: true,
            showInPayslip: true, TenantId, ActorId);

    private static SalaryComponent MakeResidual() =>
        SalaryComponent.CreateSystemFixedAllowance(TenantId, ActorId);

    private static (SalaryStructureTemplate Template, EmployeeSalaryStructure Structure) BuildFixture(decimal annualCtc)
    {
        SalaryComponent basic = MakeBasic();
        SalaryComponent hra = MakeHra();
        SalaryComponent residual = MakeResidual();

        Guid templateId = Guid.NewGuid();

        SalaryStructureComponent slotBasic = SalaryStructureComponent.Create(
            templateId, basic.Id, ComponentFormulaType.PercentOfCTC, null, 40m, 1);
        SalaryStructureComponent slotHra = SalaryStructureComponent.Create(
            templateId, hra.Id, ComponentFormulaType.PercentOfBasic, null, 40m, 2);
        SalaryStructureComponent slotResidual = SalaryStructureComponent.Create(
            templateId, residual.Id, ComponentFormulaType.ResidualCTC, null, null, 3);

        SetNavProp(slotBasic, nameof(SalaryStructureComponent.Component), basic);
        SetNavProp(slotHra, nameof(SalaryStructureComponent.Component), hra);
        SetNavProp(slotResidual, nameof(SalaryStructureComponent.Component), residual);

        SalaryStructureTemplate template = SalaryStructureTemplate.Create("Test", null, TenantId, ActorId);
        template.SetComponents([slotBasic, slotHra, slotResidual]);

        EmployeeSalaryStructure structure = EmployeeSalaryStructure.Create(
            EmployeeId, TenantId, templateId, annualCtc,
            new DateOnly(2025, 1, 1), ActorId);

        return (template, structure);
    }

    // ─── BuildComponentInputs flag fidelity ─────────────────────────────────

    [Fact]
    public void BasicSalary_ConsiderForEpf_True_IsPreserved()
    {
        var (template, structure) = BuildFixture(1_200_000m);
        IReadOnlyList<SalaryComponentInput> inputs =
            InitiatePayrollRunHandler.BuildComponentInputs(structure, template, [], MinimalConfig());

        inputs.Single(i => i.Code == "BASICSALARY").ConsiderForEpf.Should().BeTrue();
    }

    [Fact]
    public void Hra_ConsiderForEpf_False_IsPreserved()
    {
        var (template, structure) = BuildFixture(1_200_000m);
        IReadOnlyList<SalaryComponentInput> inputs =
            InitiatePayrollRunHandler.BuildComponentInputs(structure, template, [], MinimalConfig());

        inputs.Single(i => i.Code == "HRA").ConsiderForEpf.Should().BeFalse();
    }

    [Fact]
    public void BasicSalary_ConsiderForEsi_True_IsPreserved()
    {
        var (template, structure) = BuildFixture(1_200_000m);
        IReadOnlyList<SalaryComponentInput> inputs =
            InitiatePayrollRunHandler.BuildComponentInputs(structure, template, [], MinimalConfig());

        inputs.Single(i => i.Code == "BASICSALARY").ConsiderForEsi.Should().BeTrue();
    }

    [Fact]
    public void Hra_ConsiderForEsi_False_IsPreserved()
    {
        var (template, structure) = BuildFixture(1_200_000m);
        IReadOnlyList<SalaryComponentInput> inputs =
            InitiatePayrollRunHandler.BuildComponentInputs(structure, template, [], MinimalConfig());

        inputs.Single(i => i.Code == "HRA").ConsiderForEsi.Should().BeFalse();
    }

    [Fact]
    public void AllComponents_IsTaxable_IsPreserved()
    {
        var (template, structure) = BuildFixture(1_200_000m);
        IReadOnlyList<SalaryComponentInput> inputs =
            InitiatePayrollRunHandler.BuildComponentInputs(structure, template, [], MinimalConfig());

        inputs.Should().OnlyContain(i => i.IsTaxable);
    }

    [Fact]
    public void AllComponents_CalculateOnProRata_IsPreserved()
    {
        var (template, structure) = BuildFixture(1_200_000m);
        IReadOnlyList<SalaryComponentInput> inputs =
            InitiatePayrollRunHandler.BuildComponentInputs(structure, template, [], MinimalConfig());

        inputs.Should().OnlyContain(i => i.CalculateOnProRata);
    }

    // ─── Amounts ────────────────────────────────────────────────────────────

    [Fact]
    public void BasicSalary_Amount_IsPercentOfCtc()
    {
        decimal annualCtc = 1_200_000m;
        var (template, structure) = BuildFixture(annualCtc);
        IReadOnlyList<SalaryComponentInput> inputs =
            InitiatePayrollRunHandler.BuildComponentInputs(structure, template, [], MinimalConfig());

        decimal expected = Math.Round(annualCtc * 40m / 100m / 12m, 2, MidpointRounding.AwayFromZero);
        inputs.Single(i => i.Code == "BASICSALARY").Amount.Should().Be(expected);
    }

    [Fact]
    public void TotalComponents_SumToMonthlyGross()
    {
        decimal annualCtc = 1_200_000m;
        var (template, structure) = BuildFixture(annualCtc);
        IReadOnlyList<SalaryComponentInput> inputs =
            InitiatePayrollRunHandler.BuildComponentInputs(structure, template, [], MinimalConfig());

        // No employer CTC deductions in MinimalConfig → gross = CTC/12
        inputs.Sum(i => i.Amount).Should().Be(annualCtc / 12m);
    }

    // ─── Benefit exclusion ───────────────────────────────────────────────────

    [Fact]
    public void BenefitComponents_AreExcludedFromComponentInputs()
    {
        SalaryComponent benefit = SalaryComponent.CreateBenefit(
            "Health Insurance", "Health Insurance", "HEALTH_INS",
            BenefitType.OtherNonTaxable,
            benefitPercentage: null,
            isApplicableToAllEmployees: true,
            isNpsGovernmentSector: null,
            TenantId, ActorId);

        var (template, structure) = BuildFixture(1_200_000m);

        EmployeeSalaryComponentOverride benefitOverride = EmployeeSalaryComponentOverride.Create(
            structure.Id, benefit.Id,
            ComponentFormulaType.Fixed, percentage: null, fixedAmount: 2_000m, createdBy: ActorId);
        structure.AddOverride(benefitOverride);

        Dictionary<Guid, SalaryComponent> addedDetails = new() { [benefit.Id] = benefit };

        IReadOnlyList<SalaryComponentInput> inputs =
            InitiatePayrollRunHandler.BuildComponentInputs(structure, template, addedDetails, MinimalConfig());

        inputs.Should().NotContain(i => i.Code == "HEALTH_INS",
            "benefit-category components must be excluded from engine inputs");
    }

    // ─── SetMonthlyCTC on PayrunEmployee ────────────────────────────────────

    [Fact]
    public void SetMonthlyCTC_SetsCorrectValue()
    {
        PayrunEmployee pe = PayrunEmployee.Create(Guid.NewGuid(), EmployeeId, TenantId, 30, ActorId);
        pe.SetMonthlyCTC(100_000m, ActorId);
        pe.MonthlyCTC.Should().Be(100_000m);
    }

    [Fact]
    public void SetMonthlyCTC_FromAnnualCtc_IsExact()
    {
        PayrunEmployee pe = PayrunEmployee.Create(Guid.NewGuid(), EmployeeId, TenantId, 30, ActorId);
        pe.SetMonthlyCTC(1_200_000m / 12m, ActorId);
        pe.MonthlyCTC.Should().Be(100_000m);
    }

    // ─── WI-02: salary structure validation ─────────────────────────────────

    [Fact]
    public async Task Handle_NoActiveSalaryStructure_ThrowsDomainException()
    {
        var (handler, _, _, _) = BuildHandlerWithStubs(salaryStructure: null);

        Func<Task> act = () => handler.Handle(MakeCmd(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*no active salary structure*");
    }

    // ─── WI-01: breakdown seeding ────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithSalaryStructure_SeedsThreeRecurringBreakdowns()
    {
        var (addedBreakdowns, _) = await RunHandlerCapture();

        addedBreakdowns.Should().HaveCount(3,
            "Basic, HRA, and residual allowance must be seeded as recurring components");
        addedBreakdowns.Should().Contain(b => b.ComponentCode == "BASICSALARY");
        addedBreakdowns.Should().Contain(b => b.ComponentCode == "HRA");
        addedBreakdowns.Should().Contain(b => b.ComponentCode == "FIXED_ALLOWANCE");
    }

    [Fact]
    public async Task Handle_WithSalaryStructure_Basic_EpfFlagTrue()
    {
        var (addedBreakdowns, _) = await RunHandlerCapture();

        addedBreakdowns.Single(b => b.ComponentCode == "BASICSALARY")
            .ConsiderForEpf.Should().BeTrue(
                "EPF flag must be copied faithfully from SalaryComponentInput, not defaulted");
    }

    [Fact]
    public async Task Handle_WithSalaryStructure_Hra_EpfFlagFalse()
    {
        var (addedBreakdowns, _) = await RunHandlerCapture();

        addedBreakdowns.Single(b => b.ComponentCode == "HRA")
            .ConsiderForEpf.Should().BeFalse(
                "HRA is not EPF-eligible and flag must not be defaulted to false accidentally");
    }

    [Fact]
    public async Task Handle_WithSalaryStructure_Basic_EsiFlagTrue()
    {
        var (addedBreakdowns, _) = await RunHandlerCapture();

        addedBreakdowns.Single(b => b.ComponentCode == "BASICSALARY")
            .ConsiderForEsi.Should().BeTrue(
                "ESI flag must be copied faithfully — default false would silently exclude basic from ESI");
    }

    [Fact]
    public async Task Handle_WithSalaryStructure_Hra_EsiFlagFalse()
    {
        var (addedBreakdowns, _) = await RunHandlerCapture();

        addedBreakdowns.Single(b => b.ComponentCode == "HRA")
            .ConsiderForEsi.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithSalaryStructure_AllBreakdowns_AreRecurring()
    {
        var (addedBreakdowns, _) = await RunHandlerCapture();

        addedBreakdowns.Should().OnlyContain(b => !b.IsOneTimeEarning,
            "recurring salary components must not be flagged as one-time FnF earnings");
    }

    [Fact]
    public async Task Handle_WithSalaryStructure_BasicAmount_IsNonZero()
    {
        var (addedBreakdowns, _) = await RunHandlerCapture();

        addedBreakdowns.Single(b => b.ComponentCode == "BASICSALARY")
            .FullAmount.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task Handle_WithSalaryStructure_SumOfComponents_IsPositive()
    {
        // Exact amount math is covered by BuildComponentInputsTests (unit).
        // Here we assert seeding wrote actual non-zero amounts, not defaults.
        var (addedBreakdowns, _) = await RunHandlerCapture();

        addedBreakdowns.Sum(b => b.FullAmount).Should().BeGreaterThan(0m,
            "salary components must be seeded with actual computed amounts, not zero");
    }

    [Fact]
    public async Task Handle_WithSalaryStructure_MonthlyCTC_SetOnPayrunEmployee()
    {
        decimal annualCtc = 1_200_000m;
        var (_, capturedPEs) = await RunHandlerCapture(annualCtc);

        capturedPEs.Should().ContainSingle().Which
            .MonthlyCTC.Should().Be(annualCtc / 12m);
    }

    // ─── WI-11: bulk FnF employee count persisted on append ──────────────────

    [Fact]
    public async Task Handle_AppendToExistingBulkRun_CallsRunRepoUpdate_WithIncrementedCount()
    {
        var (template, structure) = BuildFixture(1_200_000m);
        var (handler, _, _, runRepo) =
            BuildHandlerWithStubs(salaryStructure: structure);

        // An existing bulk FnF run with one employee already appended.
        DomainPayrollRun existingBulk = DomainPayrollRun.CreateBulkFinalSettlement(
            tenantId: TenantId,
            payPeriod: new Payroll.Domain.ValueObjects.PayPeriod(2026, 6),
            payDay: new DateOnly(2026, 6, 30),
            statutoryConfigSnapshot: "{}",
            createdBy: ActorId);
        existingBulk.SetEmployeeCount(1, ActorId);

        runRepo.FindDraftBulkFnfByPayDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(existingBulk);

        DomainPayrollRun? updatedRun = null;
        runRepo.When(r => r.Update(Arg.Any<DomainPayrollRun>()))
            .Do(ci => updatedRun = ci.Arg<DomainPayrollRun>());

        await handler.Handle(MakeCmd(), CancellationToken.None);

        // WI-11: the increment must be persisted via an explicit Update call.
        runRepo.Received().Update(Arg.Is<DomainPayrollRun>(r => r.Id == existingBulk.Id));
        updatedRun.Should().NotBeNull();
        updatedRun!.EmployeeCount.Should().Be(2,
            "appending a second employee must increment the bulk run count to 2");
    }

    // ─── Internal helpers ────────────────────────────────────────────────────

    private static InitiateExitCommand MakeCmd() => new(
        EmployeeId,
        LastWorkingDay: new DateOnly(2026, 6, 30),
        Reason: ExitReason.ResignedByEmployee,
        SettlementMode: ExitSettlementMode.RegularSchedule,
        SettlementDate: null,
        PersonalEmail: null,
        Notes: null,
        ActorId: ActorId);

    private static (InitiateExitHandler Handler,
                    IPayrunComponentBreakdownRepository BreakdownRepo,
                    IPayrunEmployeeRepository PayrunEmpRepo,
                    IPayrollRunRepository RunRepo)
        BuildHandlerWithStubs(EmployeeSalaryStructure? salaryStructure = null, decimal annualCtc = 1_200_000m)
    {
        var employeeRepo = Substitute.For<IEmployeeRepository>();
        var exitRepo = Substitute.For<IEmployeeExitRepository>();
        var orgProfileRepo = Substitute.For<IOrgProfileRepository>();
        var runRepo = Substitute.For<IPayrollRunRepository>();
        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        var payScheduleRepo = Substitute.For<IPayScheduleRepository>();
        var statutoryRepo = Substitute.For<IStatutoryConfigRepository>();
        var workLocationRepo = Substitute.For<IWorkLocationRepository>();
        var salaryStructureRepo = Substitute.For<IEmployeeSalaryStructureRepository>();
        var templateRepo = Substitute.For<ISalaryStructureTemplateRepository>();
        var salaryComponentRepo = Substitute.For<ISalaryComponentRepository>();
        var breakdownRepo = Substitute.For<IPayrunComponentBreakdownRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var uow = Substitute.For<IUnitOfWork>();

        tenantContext.TenantId.Returns(TenantId);

        Employee employee = Employee.CreateStep1(
            firstName: "Test", middleName: null, lastName: "Employee",
            employeeCode: "EMP001", workEmail: "test@acme.com",
            mobileNumber: null, gender: Gender.Male,
            dateOfJoining: new DateOnly(2020, 1, 1),
            employmentType: EmploymentType.FullTime,
            isDirector: false, enablePortalAccess: false,
            tenantId: TenantId,
            departmentId: DeptId, designationId: DesigId, workLocationId: WorkLocId,
            businessUnitId: null,
            dateOfBirth: new DateOnly(1990, 1, 1),
            createdBy: ActorId);

        // Set EmployeeId via reflection (private setter in AuditableEntity/base)
        PropertyInfo? idProp = typeof(Employee).GetProperty("Id")
            ?? typeof(Employee).BaseType?.GetProperty("Id");
        idProp?.SetValue(employee, EmployeeId);

        employeeRepo.GetByIdAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(employee);
        exitRepo.GetActiveByEmployeeAsync(EmployeeId, Arg.Any<CancellationToken>())
            .Returns((EmployeeExit?)null);
        orgProfileRepo.GetAsync(Arg.Any<CancellationToken>()).Returns((OrgProfile?)null);

        if (salaryStructure is null)
        {
            salaryStructureRepo.GetActiveWithOverridesAsync(EmployeeId, Arg.Any<CancellationToken>())
                .Returns((EmployeeSalaryStructure?)null);
        }
        else
        {
            salaryStructureRepo.GetActiveWithOverridesAsync(EmployeeId, Arg.Any<CancellationToken>())
                .Returns(salaryStructure);

            if (salaryStructure.SalaryStructureTemplateId.HasValue)
            {
                var (template, _) = BuildFixture(annualCtc);
                templateRepo.GetByIdWithComponentsAsync(
                    salaryStructure.SalaryStructureTemplateId.Value, Arg.Any<CancellationToken>())
                    .Returns(template);
            }

            salaryComponentRepo.GetByIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
                .Returns(new List<SalaryComponent>());
        }

        DomainPaySchedule paySchedule = DomainPaySchedule.Create(
            workWeekDays: WorkWeekDay.Monday | WorkWeekDay.Tuesday | WorkWeekDay.Wednesday
                        | WorkWeekDay.Thursday | WorkWeekDay.Friday,
            salaryCalculationMethod: SalaryCalculationMethod.ActualDays,
            fixedWorkingDaysPerMonth: null,
            payDateType: PayDateType.LastDay,
            payDateDay: null,
            firstPayPeriodMonth: null,
            firstPayPeriodYear: null,
            createdBy: ActorId);
        payScheduleRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(paySchedule);

        // Statutory stubs
        StatutoryOrgConfig orgConfig = MakeOrgConfig();
        statutoryRepo.GetByTenantAsync(Arg.Any<CancellationToken>()).Returns(orgConfig);
        statutoryRepo.GetIncomeTaxConfigAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IncomeTaxConfig?)null);
        statutoryRepo.GetIncomeTaxSlabsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<IncomeTaxSlab>());
        statutoryRepo.GetSurchargeSlabsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<IncomeTaxSurchargeSlab>());
        statutoryRepo.GetPtSlabsAsync(
            Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProfessionalTaxSlab>());
        statutoryRepo.GetLwfConfigsAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LwfStateConfig>());

        workLocationRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WorkLocation?)null);

        // Run repo — no existing bulk FnF run
        runRepo.FindDraftBulkFnfByPayDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((DomainPayrollRun?)null);
        runRepo.FindDraftRegularRunsCoveringDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<DomainPayrollRun>());

        var handler = new InitiateExitHandler(
            employeeRepo, exitRepo, orgProfileRepo, runRepo, payrunEmpRepo,
            payScheduleRepo, statutoryRepo, workLocationRepo,
            salaryStructureRepo, templateRepo, salaryComponentRepo, breakdownRepo,
            tenantContext, uow);

        return (handler, breakdownRepo, payrunEmpRepo, runRepo);
    }

    private async Task<(List<PayrunComponentBreakdown> Breakdowns, List<PayrunEmployee> PayrunEmps)>
        RunHandlerCapture(decimal annualCtc = 1_200_000m)
    {
        var (template, structure) = BuildFixture(annualCtc);

        var (handler, breakdownRepo, payrunEmpRepo, _) =
            BuildHandlerWithStubs(salaryStructure: structure, annualCtc: annualCtc);

        List<PayrunComponentBreakdown> addedBreakdowns = [];
        List<PayrunEmployee> capturedPEs = [];

        // Discard to suppress CS4014 — NSubstitute Arg.Do setup, not an actual call
        _ = breakdownRepo.AddAsync(
            Arg.Do<PayrunComponentBreakdown>(b => addedBreakdowns.Add(b)),
            Arg.Any<CancellationToken>());
        _ = payrunEmpRepo.AddAsync(
            Arg.Do<PayrunEmployee>(pe => capturedPEs.Add(pe)),
            Arg.Any<CancellationToken>());

        await handler.Handle(MakeCmd(), CancellationToken.None);

        return (addedBreakdowns, capturedPEs);
    }

    private static StatutoryOrgConfig MakeOrgConfig() =>
        StatutoryOrgConfig.CreateDefault(TenantId, ActorId);
}
