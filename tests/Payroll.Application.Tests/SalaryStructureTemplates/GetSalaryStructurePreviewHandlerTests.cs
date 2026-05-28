using FluentAssertions;
using NSubstitute;
using Payroll.Application.Queries.SalaryStructureTemplates;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Xunit;

namespace Payroll.Application.Tests.SalaryStructureTemplates;

// Verifies the API preview endpoint matches the direct calculator on the
// same inputs. Catches drift between the in-memory entity construction the
// handler does and the per-employee path that loads real entities.
public class GetSalaryStructurePreviewHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid BasicId = Guid.NewGuid();
    private static readonly Guid HraId = Guid.NewGuid();
    private static readonly Guid SpecialId = Guid.NewGuid();

    private static (GetSalaryStructurePreviewHandler Handler, StatutoryOrgConfig OrgConfig) MakeHandler(
        bool epfIncludeEmployerInCtc = true,
        bool gratuityIncludedInCtc = true)
    {
        SalaryComponent basic = SalaryComponent.CreateEarning(
            name: "Basic", nameInPayslip: "Basic", code: "BASIC",
            earningType: EarningType.Basic, payType: PayType.Monthly,
            formulaType: ComponentFormulaType.PercentOfCTC,
            fixedAmount: null, percentage: 50m, isTaxable: true,
            considerForEpf: true, epfInclusionRule: EpfInclusionRule.Always,
            considerForEsi: true, calculateOnProRata: true, showInPayslip: true,
            tenantId: TenantId, createdBy: Guid.Empty);
        typeof(SalaryComponent).GetProperty("Id")!.SetValue(basic, BasicId);

        SalaryComponent hra = SalaryComponent.CreateEarning(
            name: "HRA", nameInPayslip: "HRA", code: "HRA",
            earningType: EarningType.HouseRentAllowance, payType: PayType.Monthly,
            formulaType: ComponentFormulaType.PercentOfBasic,
            fixedAmount: null, percentage: 40m, isTaxable: false,
            considerForEpf: false, epfInclusionRule: EpfInclusionRule.Always,
            considerForEsi: true, calculateOnProRata: true, showInPayslip: true,
            tenantId: TenantId, createdBy: Guid.Empty);
        typeof(SalaryComponent).GetProperty("Id")!.SetValue(hra, HraId);

        SalaryComponent special = SalaryComponent.CreateSystemFixedAllowance(TenantId, Guid.Empty);
        typeof(SalaryComponent).GetProperty("Id")!.SetValue(special, SpecialId);

        ISalaryComponentRepository componentRepo = Substitute.For<ISalaryComponentRepository>();
        componentRepo.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<SalaryComponent> { basic, hra, special });

        StatutoryOrgConfig org = StatutoryOrgConfig.CreateDefault(TenantId, Guid.Empty);
        org.ConfigureEpf(
            enabled: true, establishmentCode: null,
            employeeContributionRate: "ActualPfWage12",
            employerContributionRate: "RestrictedWage12",
            includeEmployerInCtc: epfIncludeEmployerInCtc,
            overrideAtEmployeeLevel: false,
            proRateRestrictedPfWage: false,
            considerSalaryOnLop: true,
            updatedBy: Guid.Empty);
        org.ConfigureGratuity(includedInCtc: gratuityIncludedInCtc, updatedBy: Guid.Empty);

        IStatutoryConfigRepository statutoryRepo = Substitute.For<IStatutoryConfigRepository>();
        statutoryRepo.GetByTenantAsync(Arg.Any<CancellationToken>()).Returns(org);
        // Handler builds engine config via StatutoryConfigBuilder which tolerates
        // null taxConfig + empty slabs. NSubstitute returns null for Task<X?> by
        // default, but stubbing the list-returning calls keeps the calculator from
        // tripping on `null` Task returns.
        statutoryRepo.GetIncomeTaxSlabsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<IncomeTaxSlab>());
        statutoryRepo.GetSurchargeSlabsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<IncomeTaxSurchargeSlab>());
        statutoryRepo.GetPtSlabsAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProfessionalTaxSlab>());
        statutoryRepo.GetLwfConfigsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LwfStateConfig>());

        ITenantContext tenantCtx = Substitute.For<ITenantContext>();
        tenantCtx.TenantId.Returns(TenantId);

        return (new GetSalaryStructurePreviewHandler(componentRepo, statutoryRepo, tenantCtx), org);
    }

    private static SalaryStructurePreviewQuery MakeQuery(
        decimal annualCtc = 6_00_000m,
        bool epfEnabled = true,
        bool gratuityEnabled = true) =>
        new(
            AnnualCtc: annualCtc,
            TemplateComponents: [
                new PreviewTemplateComponentInput(BasicId, "PercentOfCTC", null, 50m, 0),
                new PreviewTemplateComponentInput(HraId, "PercentOfBasic", null, 40m, 1),
                new PreviewTemplateComponentInput(SpecialId, "ResidualCTC", null, null, 2),
            ],
            Overrides: [],
            AddedComponents: [],
            Benefits: [],
            EmployeeFlags: new PreviewEmployeeFlagsInput(
                EpfEnabled: epfEnabled,
                GratuityEnabled: gratuityEnabled),
            WorkStateCode: null,
            Year: 2025,
            Month: 5);

    [Fact]
    public async Task Preview_MatchesDirectCalculator_AllFlagsOn()
    {
        (GetSalaryStructurePreviewHandler handler, _) = MakeHandler();

        SalaryStructurePreviewDto dto = await handler.Handle(MakeQuery(), CancellationToken.None);

        PreviewRowDto residual = dto.Rows.First(r => r.IsResidual);
        residual.MonthlyAmount.Should().Be(11_998.08m);  // same value the direct calculator test asserts
        dto.EmployerContributions.Should().HaveCount(2);
        dto.EmployerContributions.First(e => e.Code == "EPF_EMPLOYER").MonthlyAmount.Should().Be(1_800m);
        dto.EmployerContributions.First(e => e.Code == "GRATUITY_ACCRUAL").MonthlyAmount.Should().Be(1_201.92m);
    }

    [Fact]
    public async Task Preview_HonorsOrgEmployerEpfInCtcFlag()
    {
        (GetSalaryStructurePreviewHandler handler, _) = MakeHandler(epfIncludeEmployerInCtc: false);

        SalaryStructurePreviewDto dto = await handler.Handle(MakeQuery(), CancellationToken.None);

        dto.EmployerContributions.Should().NotContain(e => e.Code == "EPF_EMPLOYER");
        // Residual grows by exactly the employer-EPF amount that's no longer subtracted.
        dto.Rows.First(r => r.IsResidual).MonthlyAmount.Should().Be(13_798.08m);
    }

    [Fact]
    public async Task Preview_HonorsEmployeeEpfFlag()
    {
        (GetSalaryStructurePreviewHandler handler, _) = MakeHandler();

        SalaryStructurePreviewDto dto = await handler.Handle(MakeQuery(epfEnabled: false), CancellationToken.None);

        dto.EmployerContributions.Should().NotContain(e => e.Code == "EPF_EMPLOYER");
        dto.Rows.First(r => r.IsResidual).MonthlyAmount.Should().Be(13_798.08m);
    }
}
