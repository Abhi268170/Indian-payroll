using FluentAssertions;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using System.Reflection;
using Xunit;

namespace Payroll.Application.Tests.PayrollRun;

public class BuildComponentInputsTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    // Reflection helper — Component nav property is set by EF in production; set manually in tests.
    private static void SetComponent(SalaryStructureComponent slot, SalaryComponent component)
    {
        PropertyInfo prop = typeof(SalaryStructureComponent)
            .GetProperty(nameof(SalaryStructureComponent.Component))!;
        prop.SetValue(slot, component);
    }

    private static SalaryComponent MakeBasic() =>
        SalaryComponent.CreateEarning(
            "Basic Salary", "Basic Salary", "BASICSALARY",
            EarningType.Basic, PayType.Monthly,
            ComponentFormulaType.PercentOfCTC,
            fixedAmount: null, percentage: 37.5m,
            isTaxable: true, considerForEpf: true,
            EpfInclusionRule.Always,
            considerForEsi: true, calculateOnProRata: true,
            showInPayslip: true,
            TenantId, ActorId);

    private static SalaryComponent MakeHra() =>
        SalaryComponent.CreateEarning(
            "HRA", "HRA", "HRA",
            EarningType.HouseRentAllowance, PayType.Monthly,
            ComponentFormulaType.PercentOfBasic,
            fixedAmount: null, percentage: 40m,
            isTaxable: true, considerForEpf: false,
            EpfInclusionRule.Always,
            considerForEsi: false, calculateOnProRata: true,
            showInPayslip: true,
            TenantId, ActorId);

    private static SalaryComponent MakeResidual() =>
        SalaryComponent.CreateSystemFixedAllowance(TenantId, ActorId);

    private static (SalaryStructureTemplate, EmployeeSalaryStructure) BuildFixture(decimal annualCtc)
    {
        SalaryComponent basic = MakeBasic();
        SalaryComponent hra = MakeHra();
        SalaryComponent residual = MakeResidual();

        Guid templateId = Guid.NewGuid();

        SalaryStructureComponent slotBasic = SalaryStructureComponent.Create(
            templateId, basic.Id, ComponentFormulaType.PercentOfCTC, null, 37.5m, 1);
        SalaryStructureComponent slotHra = SalaryStructureComponent.Create(
            templateId, hra.Id, ComponentFormulaType.PercentOfBasic, null, 40m, 2);
        SalaryStructureComponent slotResidual = SalaryStructureComponent.Create(
            templateId, residual.Id, ComponentFormulaType.ResidualCTC, null, null, 3);

        SetComponent(slotBasic, basic);
        SetComponent(slotHra, hra);
        SetComponent(slotResidual, residual);

        SalaryStructureTemplate template = SalaryStructureTemplate.Create("Test", null, TenantId, ActorId);
        template.SetComponents([slotBasic, slotHra, slotResidual]);

        EmployeeSalaryStructure structure = EmployeeSalaryStructure.Create(
            Guid.NewGuid(), TenantId, templateId, annualCtc,
            new DateOnly(2025, 1, 1), ActorId);

        return (template, structure);
    }

    [Fact]
    public void Basic_PercentOfCTC_IsCorrect()
    {
        var (template, structure) = BuildFixture(1_200_000m); // 12L CTC
        var inputs = InitiatePayrollRunHandler.BuildComponentInputs(structure, template, new System.Collections.Generic.Dictionary<System.Guid, Payroll.Domain.Entities.SalaryComponent>());

        decimal basicMonthly = 1_200_000m * 37.5m / 100m / 12m; // 37,500
        inputs.Single(i => i.Code == "BASICSALARY").Amount.Should().Be(basicMonthly);
    }

    [Fact]
    public void HRA_PercentOfBasic_UsesBasicNotZero()
    {
        var (template, structure) = BuildFixture(1_200_000m);
        var inputs = InitiatePayrollRunHandler.BuildComponentInputs(structure, template, new System.Collections.Generic.Dictionary<System.Guid, Payroll.Domain.Entities.SalaryComponent>());

        decimal basicMonthly = 1_200_000m * 37.5m / 100m / 12m; // 37,500
        decimal expectedHra = Math.Round(basicMonthly * 40m / 100m, 2, MidpointRounding.AwayFromZero); // 15,000

        inputs.Single(i => i.Code == "HRA").Amount.Should().Be(expectedHra);
    }

    [Fact]
    public void Residual_AbsorbsRemainder()
    {
        var (template, structure) = BuildFixture(1_200_000m);
        var inputs = InitiatePayrollRunHandler.BuildComponentInputs(structure, template, new System.Collections.Generic.Dictionary<System.Guid, Payroll.Domain.Entities.SalaryComponent>());

        decimal monthlyGross = 1_200_000m / 12m;    // 100,000
        decimal basicMonthly = monthlyGross * 37.5m / 100m; // 37,500
        decimal hraMonthly = basicMonthly * 40m / 100m;     // 15,000
        decimal expectedResidual = Math.Round(monthlyGross - basicMonthly - hraMonthly, 2, MidpointRounding.AwayFromZero); // 47,500

        inputs.Single(i => i.Code == "FIXED_ALLOWANCE").Amount.Should().Be(expectedResidual);
    }

    [Fact]
    public void GrossTotal_EqualsMonthlyCtc()
    {
        var (template, structure) = BuildFixture(1_200_000m);
        var inputs = InitiatePayrollRunHandler.BuildComponentInputs(structure, template, new System.Collections.Generic.Dictionary<System.Guid, Payroll.Domain.Entities.SalaryComponent>());

        decimal monthlyGross = 1_200_000m / 12m;
        inputs.Sum(i => i.Amount).Should().Be(monthlyGross);
    }

    [Fact]
    public void NullTemplate_ReturnsEmpty()
    {
        EmployeeSalaryStructure structure = EmployeeSalaryStructure.Create(
            Guid.NewGuid(), TenantId, null, 1_200_000m,
            new DateOnly(2025, 1, 1), ActorId);

        var inputs = InitiatePayrollRunHandler.BuildComponentInputs(structure, null, new System.Collections.Generic.Dictionary<System.Guid, Payroll.Domain.Entities.SalaryComponent>());

        inputs.Should().BeEmpty();
    }
}
