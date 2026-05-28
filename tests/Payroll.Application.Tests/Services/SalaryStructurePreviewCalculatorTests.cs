using FluentAssertions;
using Payroll.Application.Services;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Xunit;

namespace Payroll.Application.Tests.Services;

// Locks down the residual + employer-statutory math used in the salary structure
// builder, employee hire wizard, and employee salary view. These three places
// previously had their own copies; this calculator is now the only source of
// truth and any drift back to per-page math should break a test here.
public class SalaryStructurePreviewCalculatorTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid BasicId = Guid.NewGuid();
    private static readonly Guid HraId = Guid.NewGuid();
    private static readonly Guid SpecialId = Guid.NewGuid();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static SalaryStructurePreviewCalculator.Inputs MakeInputs(
        decimal annualCtc,
        IReadOnlyList<SalaryStructureComponent> templateComponents,
        bool epfEnabled = true,
        bool gratuityEnabled = true,
        bool epfIncludeEmployerInCtc = true,
        bool gratuityIncludedInCtc = true,
        string? workStateCode = null,
        bool ptEnabled = true,
        bool lwfEnabled = true,
        bool esiEnabled = true,
        bool isPwd = false,
        IReadOnlyList<SalaryStructurePreviewCalculator.BenefitInput>? benefits = null)
    {
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
        org.ConfigureEsi(enabled: true, establishmentCode: null, notifiedArea: true, updatedBy: Guid.Empty);
        org.ConfigureGratuity(includedInCtc: gratuityIncludedInCtc, updatedBy: Guid.Empty);

        // Build engine StatutoryConfig from the same defaults the production
        // StatutoryConfigBuilder uses, just without PT/LWF unless caller passes
        // workStateCode. Tests for residual + employer EPF don't need slabs.
        Payroll.Engine.Inputs.StatutoryConfig engineConfig = Payroll.Application.Services.StatutoryConfigBuilder.Build(
            org, taxConfig: null, taxSlabs: [], surchargeSlabs: [], ptSlabs: [], lwfConfigs: []);

        return new SalaryStructurePreviewCalculator.Inputs(
            AnnualCtc: annualCtc,
            TemplateComponents: templateComponents,
            Overrides: new Dictionary<Guid, EmployeeSalaryComponentOverride>(),
            AddedComponents: [],
            Benefits: benefits ?? [],
            EmployeeFlags: new SalaryStructurePreviewCalculator.EmployeeStatutoryFlags(
                EpfEnabled: epfEnabled,
                EsiEnabled: esiEnabled,
                PtEnabled: ptEnabled,
                LwfEnabled: lwfEnabled,
                GratuityEnabled: gratuityEnabled,
                IsPwd: isPwd),
            EngineConfig: engineConfig,
            WorkStateCode: workStateCode,
            Year: 2025,
            Month: 5);
    }

    // Build a template with: Basic (PF-eligible) + HRA (non-PF) + Special-Allowance-as-residual.
    private static IReadOnlyList<SalaryStructureComponent> SimpleTemplate()
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

        SalaryStructureTemplate template = SalaryStructureTemplate.Create(
            name: "Standard", description: null, tenantId: TenantId, createdBy: Guid.Empty);

        SalaryStructureComponent basicSlot = SalaryStructureComponent.Create(
            template.Id, BasicId, ComponentFormulaType.PercentOfCTC, null, 50m, 0);
        SalaryStructureComponent hraSlot = SalaryStructureComponent.Create(
            template.Id, HraId, ComponentFormulaType.PercentOfBasic, null, 40m, 1);
        SalaryStructureComponent specialSlot = SalaryStructureComponent.Create(
            template.Id, SpecialId, ComponentFormulaType.ResidualCTC, null, null, 2);

        typeof(SalaryStructureComponent).GetProperty("Component")!.SetValue(basicSlot, basic);
        typeof(SalaryStructureComponent).GetProperty("Component")!.SetValue(hraSlot, hra);
        typeof(SalaryStructureComponent).GetProperty("Component")!.SetValue(specialSlot, special);

        template.SetComponents([basicSlot, hraSlot, specialSlot]);
        return template.Components.ToList();
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public void Residual_SubtractsEmployerEpfAndGratuity_WhenAllFlagsOn()
    {
        // CTC = ₹6L → basic = 50% = ₹25k/mo
        // pfWage = basic only = ₹25k → cap at ₹15k → employer EPF = ₹15,000 × 0.12 = ₹1,800/mo
        // gratuity = ₹25,000 × 15 / 26 / 12 = ₹1,201.92/mo
        // HRA = 40% of basic = ₹10k/mo
        // monthlyGross = ₹50k; nonResidualMonthly = ₹35k
        // residual = 50000 - 35000 - 1800 - 1201.92 = ₹11,998.08
        SalaryStructurePreviewCalculator.Output output = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate()));

        SalaryStructurePreviewCalculator.PreviewRow residual = output.Rows.First(r => r.IsResidual);
        residual.MonthlyAmount.Should().Be(11_998.08m);

        output.EmployerContributions.Should().HaveCount(2);
        output.EmployerContributions.First(e => e.Code == "EPF_EMPLOYER").MonthlyAmount.Should().Be(1_800m);
        output.EmployerContributions.First(e => e.Code == "GRATUITY_ACCRUAL").MonthlyAmount.Should().Be(1_201.92m);
    }

    [Fact]
    public void Residual_GrowsByExactlyEmployerEpf_WhenEmployeeEpfOff()
    {
        SalaryStructurePreviewCalculator.Output withEpf = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate()));
        SalaryStructurePreviewCalculator.Output noEpf = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate(), epfEnabled: false));

        decimal withEpfResidual = withEpf.Rows.First(r => r.IsResidual).MonthlyAmount;
        decimal noEpfResidual = noEpf.Rows.First(r => r.IsResidual).MonthlyAmount;

        (noEpfResidual - withEpfResidual).Should().Be(1_800m);
        noEpf.EmployerContributions.Should().NotContain(e => e.Code == "EPF_EMPLOYER");
    }

    [Fact]
    public void Residual_GrowsWhen_OrgExcludesEmployerEpfFromCtc()
    {
        SalaryStructurePreviewCalculator.Output noCtcEpf = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate(),
                epfIncludeEmployerInCtc: false));

        noCtcEpf.EmployerContributions.Should().NotContain(e => e.Code == "EPF_EMPLOYER");
        noCtcEpf.Rows.First(r => r.IsResidual).MonthlyAmount.Should().Be(13_798.08m);  // +1800
    }

    [Fact]
    public void Residual_GrowsWhen_GratuityNotInCtc()
    {
        SalaryStructurePreviewCalculator.Output noGratCtc = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate(),
                gratuityIncludedInCtc: false));

        noGratCtc.EmployerContributions.Should().NotContain(e => e.Code == "GRATUITY_ACCRUAL");
        noGratCtc.Rows.First(r => r.IsResidual).MonthlyAmount.Should().Be(13_200m);  // 50k - 35k - 1800
    }

    [Fact]
    public void Residual_GrowsWhen_EmployeeGratuityOff()
    {
        SalaryStructurePreviewCalculator.Output noGrat = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate(),
                gratuityEnabled: false));

        noGrat.EmployerContributions.Should().NotContain(e => e.Code == "GRATUITY_ACCRUAL");
        noGrat.Rows.First(r => r.IsResidual).MonthlyAmount.Should().Be(13_200m);
    }

    [Fact]
    public void PfWageAboveCap_EmployerEpfStaysCapped()
    {
        // CTC ₹24L → basic 50% = ₹1L/mo → pfWage cap ₹15k → employer EPF = ₹1,800
        SalaryStructurePreviewCalculator.Output output = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 24_00_000m, templateComponents: SimpleTemplate()));

        output.EmployerContributions.First(e => e.Code == "EPF_EMPLOYER").MonthlyAmount.Should().Be(1_800m);
    }

    [Fact]
    public void Residual_ClampedAtZero_WhenStructureOverAllocates()
    {
        // Single-component template with Basic at 110% of CTC over-allocates → residual should be 0.
        SalaryComponent basic = SalaryComponent.CreateEarning(
            name: "Basic", nameInPayslip: "Basic", code: "BASIC",
            earningType: EarningType.Basic, payType: PayType.Monthly,
            formulaType: ComponentFormulaType.PercentOfCTC,
            fixedAmount: null, percentage: 110m, isTaxable: true,
            considerForEpf: true, epfInclusionRule: EpfInclusionRule.Always,
            considerForEsi: true, calculateOnProRata: true, showInPayslip: true,
            tenantId: TenantId, createdBy: Guid.Empty);
        typeof(SalaryComponent).GetProperty("Id")!.SetValue(basic, BasicId);
        SalaryComponent special = SalaryComponent.CreateSystemFixedAllowance(TenantId, Guid.Empty);
        typeof(SalaryComponent).GetProperty("Id")!.SetValue(special, SpecialId);

        SalaryStructureTemplate template = SalaryStructureTemplate.Create(
            name: "Over", description: null, tenantId: TenantId, createdBy: Guid.Empty);
        SalaryStructureComponent basicSlot = SalaryStructureComponent.Create(
            template.Id, BasicId, ComponentFormulaType.PercentOfCTC, null, 110m, 0);
        SalaryStructureComponent specialSlot = SalaryStructureComponent.Create(
            template.Id, SpecialId, ComponentFormulaType.ResidualCTC, null, null, 1);
        typeof(SalaryStructureComponent).GetProperty("Component")!.SetValue(basicSlot, basic);
        typeof(SalaryStructureComponent).GetProperty("Component")!.SetValue(specialSlot, special);
        template.SetComponents([basicSlot, specialSlot]);

        SalaryStructurePreviewCalculator.Output output = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: template.Components.ToList()));

        output.Rows.First(r => r.IsResidual).MonthlyAmount.Should().Be(0m);
    }

    // ── PR4: employee deductions, net pay, benefits ──────────────────────────

    [Fact]
    public void EmployeeEpf_SubtractsFromNet_WhenFlagsOn()
    {
        // CTC ₹6L → basic ₹25k → PF wage capped at ₹15k → employee EPF = ₹1,800
        SalaryStructurePreviewCalculator.Output output = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate()));

        output.EmployeeDeductions.Should().ContainSingle(d => d.Code == "EPF_EMPLOYEE");
        output.EmployeeDeductions.First(d => d.Code == "EPF_EMPLOYEE").MonthlyAmount.Should().Be(1_800m);
    }

    [Fact]
    public void EmployeeEpf_Zero_WhenEmployeeOptedOut()
    {
        SalaryStructurePreviewCalculator.Output output = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate(), epfEnabled: false));

        output.EmployeeDeductions.Should().NotContain(d => d.Code == "EPF_EMPLOYEE");
    }

    [Fact]
    public void EmployeeEsi_AppliesAtWageBelowLimit()
    {
        // Drop CTC so monthly ESI wage stays below the ₹21k cap. Basic ₹10k →
        // ESI wage = HRA+Basic = ₹14k (HRA considerForEsi=true in fixture). ESI
        // employee 0.75% = ₹105.
        SalaryStructurePreviewCalculator.Output output = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 2_40_000m, templateComponents: SimpleTemplate()));

        output.EmployeeDeductions.Should().Contain(d => d.Code == "ESI_EMPLOYEE");
        output.EmployeeDeductions.First(d => d.Code == "ESI_EMPLOYEE").MonthlyAmount.Should().Be(105m);
    }

    [Fact]
    public void EmployeeEsi_Zero_AboveLimit()
    {
        // ESI wage at default ₹6L CTC is way above ₹21k → exempt.
        SalaryStructurePreviewCalculator.Output output = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate()));

        output.EmployeeDeductions.Should().NotContain(d => d.Code == "ESI_EMPLOYEE");
    }

    [Fact]
    public void Pt_SkippedWhenWorkStateCodeNull()
    {
        SalaryStructurePreviewCalculator.Output output = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate(), workStateCode: null));

        output.EmployeeDeductions.Should().NotContain(d => d.Code == "PT_EMPLOYEE");
    }

    [Fact]
    public void NetPay_EqualsGrossMinusAllEmployeeDeductions()
    {
        SalaryStructurePreviewCalculator.Output output = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate()));

        decimal totalDeductions = output.EmployeeDeductions.Sum(d => d.MonthlyAmount);
        output.NetPayMonthly.Should().Be(50_000m - totalDeductions);  // grossMonthly - deductions
    }

    [Fact]
    public void Benefits_RenderedSeparately_ButSubtractFromResidual()
    {
        // Benefits operator added (health insurance, NPS, etc.) are employer-funded
        // costs inside CTC. They should shrink the residual just like employer EPF
        // does — otherwise the operator allocates ₹50k/yr of benefits and the
        // residual still shows the full ₹X without accounting for them.
        SalaryStructurePreviewCalculator.BenefitInput insurance = new(
            Guid.NewGuid(), "HEALTH", "Health Insurance", AnnualAmount: 48_000m);

        SalaryStructurePreviewCalculator.Output withBenefit = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate(),
                benefits: new[] { insurance }));
        SalaryStructurePreviewCalculator.Output without = SalaryStructurePreviewCalculator.Compute(
            MakeInputs(annualCtc: 6_00_000m, templateComponents: SimpleTemplate()));

        // Benefit rendered in its own section
        withBenefit.Benefits.Should().HaveCount(1);
        withBenefit.Benefits.First().MonthlyAmount.Should().Be(4_000m);
        withBenefit.Benefits.First().AnnualAmount.Should().Be(48_000m);

        // Residual drops by exactly the benefit monthly cost
        decimal residualWith = withBenefit.Rows.First(r => r.IsResidual).MonthlyAmount;
        decimal residualWithout = without.Rows.First(r => r.IsResidual).MonthlyAmount;
        (residualWithout - residualWith).Should().Be(4_000m);
    }
}
