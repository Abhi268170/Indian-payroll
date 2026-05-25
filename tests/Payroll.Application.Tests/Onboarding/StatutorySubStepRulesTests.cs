using FluentAssertions;
using NSubstitute;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.Onboarding;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Xunit;

namespace Payroll.Application.Tests.Onboarding;

// Covers the 9-case test matrix from plan §9 for the statutory sub-step
// derivation. Mocks repos to feed exact inputs and asserts each sub-step's
// (Complete, Hint) outcome.
public class StatutorySubStepRulesTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private static WorkLocation MakeLocation(IndianState state) =>
        WorkLocation.Create($"Loc-{state}", state, null, null, null, null, ActorId);

    private static StatutoryOrgConfig MakeConfig(
        bool epfEnabled, string? epfCode,
        bool esiEnabled, string? esiCode)
    {
        var c = StatutoryOrgConfig.CreateDefault(TenantId, ActorId);
        c.ConfigureEpf(epfEnabled, epfCode, "RestrictedWage12", "RestrictedWage12",
            includeEmployerInCtc: true, overrideAtEmployeeLevel: false,
            proRateRestrictedPfWage: false, considerSalaryOnLop: true, updatedBy: ActorId);
        c.ConfigureEsi(esiEnabled, esiCode, notifiedArea: true, updatedBy: ActorId);
        return c;
    }

    private static ProfessionalTaxSlab MakeSlab(string state) =>
        ProfessionalTaxSlab.Create(state, new DateOnly(2024, 4, 1), "Monthly", null,
            minGross: 0m, maxGross: 999999m, ptAmount: 200m, isFebruarySurcharge: false, createdBy: ActorId);

    private static LwfStateConfig MakeLwfConfig(string state) =>
        LwfStateConfig.Create(state, new DateOnly(2024, 4, 1),
            employeeAmount: 12m, employerAmount: 36m,
            isPercentageBased: false, employeeRate: null, employerRate: null,
            rateCapEmployee: null, rateCapEmployer: null,
            frequency: "Monthly", deductionMonth: 12, depositDueDay: 15,
            wageThreshold: null, createdBy: ActorId);

    private static GetOnboardingStatusHandler BuildHandler(
        StatutoryOrgConfig? statutory,
        IReadOnlyList<WorkLocation> locations,
        Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>> ptByState,
        IReadOnlyList<LwfStateConfig> lwfConfigs)
    {
        var orgProfileRepo = Substitute.For<IOrgProfileRepository>();
        orgProfileRepo.GetAsync(Arg.Any<CancellationToken>()).Returns((OrgProfile?)null);

        var payScheduleRepo = Substitute.For<IPayScheduleRepository>();
        payScheduleRepo.GetAsync(Arg.Any<CancellationToken>()).Returns((Payroll.Domain.Entities.PaySchedule?)null);

        var statutoryRepo = Substitute.For<IStatutoryConfigRepository>();
        statutoryRepo.GetByTenantAsync(Arg.Any<CancellationToken>()).Returns(statutory);
        statutoryRepo.GetPtSlabsAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(c => ptByState.TryGetValue(c.Arg<string>(), out var slabs)
                ? slabs
                : (IReadOnlyList<ProfessionalTaxSlab>)Array.Empty<ProfessionalTaxSlab>());
        statutoryRepo.GetPtRegistrationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PtStateRegistration?)null);
        statutoryRepo.GetLwfConfigsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(lwfConfigs);

        var workLocationRepo = Substitute.For<IWorkLocationRepository>();
        workLocationRepo.ListAsync(Arg.Any<CancellationToken>()).Returns(locations);

        var deptRepo = Substitute.For<IDepartmentRepository>();
        deptRepo.ListAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<Department>)Array.Empty<Department>());
        var desigRepo = Substitute.For<IDesignationRepository>();
        desigRepo.ListAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<Designation>)Array.Empty<Designation>());
        var templateRepo = Substitute.For<ISalaryStructureTemplateRepository>();
        templateRepo.ListByTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<SalaryStructureTemplate>());
        var employeeRepo = Substitute.For<IEmployeeRepository>();
        employeeRepo.ListAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<Employee>)Array.Empty<Employee>());

        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(TenantId);

        return new GetOnboardingStatusHandler(
            orgProfileRepo, payScheduleRepo, statutoryRepo,
            workLocationRepo, deptRepo, desigRepo, templateRepo, employeeRepo,
            tenant);
    }

    private static async Task<List<OnboardingSubStepDto>> RunAsync(GetOnboardingStatusHandler h)
    {
        var status = await h.Handle(new GetOnboardingStatusQuery(), CancellationToken.None);
        var statutory = status.Steps.First(s => s.Id == "statutory");
        return statutory.SubSteps?.ToList() ?? new List<OnboardingSubStepDto>();
    }

    [Fact]
    public async Task Kerala_only_WithSeededPtAndLwf_RequiresPtRegistration()
    {
        var pt = new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>
        {
            ["KL"] = new List<ProfessionalTaxSlab> { MakeSlab("KL") },
        };
        var handler = BuildHandler(
            MakeConfig(true, "KL/KOC/0001/000", true, "ESI-1"),
            new[] { MakeLocation(IndianState.Kerala) },
            pt,
            new[] { MakeLwfConfig("KL") });

        var subs = await RunAsync(handler);
        subs.First(s => s.Id == "pt").Complete.Should().BeFalse();
        subs.First(s => s.Id == "pt").Hint.Should().Contain("Kerala");
        subs.First(s => s.Id == "lwf").Complete.Should().BeTrue();
    }

    [Fact]
    public async Task Karnataka_only_SameRulesAsKerala()
    {
        var pt = new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>
        {
            ["KA"] = new List<ProfessionalTaxSlab> { MakeSlab("KA") },
        };
        var handler = BuildHandler(
            MakeConfig(true, "EPF-KA", true, "ESI-KA"),
            new[] { MakeLocation(IndianState.Karnataka) },
            pt,
            new[] { MakeLwfConfig("KA") });

        var subs = await RunAsync(handler);
        subs.First(s => s.Id == "pt").Complete.Should().BeFalse();
        subs.First(s => s.Id == "pt").Hint.Should().Contain("Karnataka");
    }

    [Fact]
    public async Task MixedStates_OneIncomplete_KeepsParentIncomplete()
    {
        var pt = new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>
        {
            ["KL"] = new List<ProfessionalTaxSlab> { MakeSlab("KL") },
            ["MH"] = new List<ProfessionalTaxSlab> { MakeSlab("MH") },
        };
        var handler = BuildHandler(
            MakeConfig(true, "EPF", true, "ESI"),
            new[] { MakeLocation(IndianState.Kerala), MakeLocation(IndianState.Maharashtra) },
            pt,
            new[] { MakeLwfConfig("KL"), MakeLwfConfig("MH") });

        var subs = await RunAsync(handler);
        var pt2 = subs.First(s => s.Id == "pt");
        pt2.Complete.Should().BeFalse();
        // Either state can appear first depending on enum ordering; both must be listed.
        pt2.Hint.Should().Contain("Kerala").And.Contain("Maharashtra");
    }

    [Fact]
    public async Task StateWithoutPtSeed_AutoCompletesPt()
    {
        var pt = new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>();  // empty — no PT for any state
        var handler = BuildHandler(
            MakeConfig(true, "EPF", true, "ESI"),
            new[] { MakeLocation(IndianState.Goa) },
            pt,
            new[] { MakeLwfConfig("GA") });

        var subs = await RunAsync(handler);
        subs.First(s => s.Id == "pt").Complete.Should().BeTrue();
    }

    [Fact]
    public async Task StateNotInLwfApplicableSet_AutoCompletesLwf()
    {
        // Tamil Nadu is NOT in StatutoryReference.LwfApplicableStates today, so absence
        // of a config row for TN is "LWF does not apply", not drift.
        var handler = BuildHandler(
            MakeConfig(true, "EPF", true, "ESI"),
            new[] { MakeLocation(IndianState.TamilNadu) },
            new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>(),
            Array.Empty<LwfStateConfig>());

        var subs = await RunAsync(handler);
        subs.First(s => s.Id == "lwf").Complete.Should().BeTrue();
    }

    [Fact]
    public async Task StateInLwfApplicableSet_MissingConfig_FlagsDrift()
    {
        // Kerala IS on the LWF list (StatutoryReference) — if the seeded config row
        // is missing from this tenant (data drift, manual delete, provisioner bug),
        // the sub-step must be Incomplete with a per-state hint. Without this
        // regression case the dead-code path silently passes.
        var handler = BuildHandler(
            MakeConfig(true, "EPF", true, "ESI"),
            new[] { MakeLocation(IndianState.Kerala) },
            new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>(),
            Array.Empty<LwfStateConfig>());  // KL is regulated but config missing

        var subs = await RunAsync(handler);
        var lwf = subs.First(s => s.Id == "lwf");
        lwf.Complete.Should().BeFalse();
        lwf.Hint.Should().Contain("Kerala");
    }

    [Fact]
    public async Task MixedStates_OneLwfDrift_FlagsOnlyThatState()
    {
        // KA seeded; KL regulated but missing → only KL appears in the hint.
        var handler = BuildHandler(
            MakeConfig(true, "EPF", true, "ESI"),
            new[] { MakeLocation(IndianState.Karnataka), MakeLocation(IndianState.Kerala) },
            new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>(),
            new[] { MakeLwfConfig("KA") });

        var subs = await RunAsync(handler);
        var lwf = subs.First(s => s.Id == "lwf");
        lwf.Complete.Should().BeFalse();
        lwf.Hint.Should().Contain("Kerala").And.NotContain("Karnataka");
    }

    [Fact]
    public async Task EpfEnabled_WithoutEstablishmentCode_Incomplete()
    {
        var handler = BuildHandler(
            MakeConfig(true, null, true, "ESI"),
            new[] { MakeLocation(IndianState.Karnataka) },
            new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>(),
            Array.Empty<LwfStateConfig>());

        var subs = await RunAsync(handler);
        var epf = subs.First(s => s.Id == "epf");
        epf.Complete.Should().BeFalse();
        epf.Hint.Should().Contain("establishment code");
    }

    [Fact]
    public async Task EpfEnabled_WithEstablishmentCode_Complete()
    {
        var handler = BuildHandler(
            MakeConfig(true, "MH/MUM/0000123/000", true, "ESI"),
            new[] { MakeLocation(IndianState.Karnataka) },
            new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>(),
            Array.Empty<LwfStateConfig>());

        var subs = await RunAsync(handler);
        subs.First(s => s.Id == "epf").Complete.Should().BeTrue();
    }

    [Fact]
    public async Task EpfDisabled_OptOut_Complete()
    {
        var handler = BuildHandler(
            MakeConfig(false, null, true, "ESI"),
            new[] { MakeLocation(IndianState.Karnataka) },
            new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>(),
            Array.Empty<LwfStateConfig>());

        var subs = await RunAsync(handler);
        subs.First(s => s.Id == "epf").Complete.Should().BeTrue();
    }

    [Fact]
    public async Task EsiSameMatrix_DisabledIsComplete_EnabledNeedsCode()
    {
        var disabled = BuildHandler(
            MakeConfig(true, "EPF", false, null),
            new[] { MakeLocation(IndianState.Karnataka) },
            new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>(),
            Array.Empty<LwfStateConfig>());
        var enabledNoCode = BuildHandler(
            MakeConfig(true, "EPF", true, null),
            new[] { MakeLocation(IndianState.Karnataka) },
            new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>(),
            Array.Empty<LwfStateConfig>());
        var enabledWithCode = BuildHandler(
            MakeConfig(true, "EPF", true, "ESI-1"),
            new[] { MakeLocation(IndianState.Karnataka) },
            new Dictionary<string, IReadOnlyList<ProfessionalTaxSlab>>(),
            Array.Empty<LwfStateConfig>());

        (await RunAsync(disabled)).First(s => s.Id == "esi").Complete.Should().BeTrue();
        (await RunAsync(enabledNoCode)).First(s => s.Id == "esi").Complete.Should().BeFalse();
        (await RunAsync(enabledWithCode)).First(s => s.Id == "esi").Complete.Should().BeTrue();
    }
}
