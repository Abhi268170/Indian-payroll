using FluentAssertions;
using NSubstitute;
using Payroll.Application.Commands.SalaryStructureTemplates;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Xunit;

namespace Payroll.Application.Tests.SalaryStructureTemplates;

// Validator for the new template-level statutory toggle: when EpfEnabled is on,
// at least one component in the template must have ConsiderForEpf = true.
// Otherwise the engine would silently compute zero PF for every employee on
// this template — the configuration mistake should fail at template save, not
// surface as missing PF deductions a month later.
public class CreateSalaryStructureTemplateHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid PfEligibleId = Guid.NewGuid();
    private static readonly Guid NonPfId = Guid.NewGuid();
    private static readonly Guid ResidualId = Guid.NewGuid();

    private static (CreateSalaryStructureTemplateHandler Handler,
        ISalaryStructureTemplateRepository TemplateRepo) MakeHandler(
            bool pfEligibleComponentExists)
    {
        ITenantContext tenantCtx = Substitute.For<ITenantContext>();
        tenantCtx.TenantId.Returns(TenantId);

        SalaryComponent pfEligible = SalaryComponent.CreateEarning(
            name: "Basic", nameInPayslip: "Basic", code: "BASIC",
            earningType: EarningType.Basic, payType: PayType.Monthly,
            formulaType: ComponentFormulaType.PercentOfCTC,
            fixedAmount: null, percentage: 50m, isTaxable: true,
            considerForEpf: true, epfInclusionRule: EpfInclusionRule.Always,
            considerForEsi: true, calculateOnProRata: true, showInPayslip: true,
            tenantId: TenantId, createdBy: Actor);
        typeof(SalaryComponent).GetProperty("Id")!.SetValue(pfEligible, PfEligibleId);

        SalaryComponent nonPf = SalaryComponent.CreateEarning(
            name: "HRA", nameInPayslip: "HRA", code: "HRA",
            earningType: EarningType.HouseRentAllowance, payType: PayType.Monthly,
            formulaType: ComponentFormulaType.PercentOfBasic,
            fixedAmount: null, percentage: 40m, isTaxable: false,
            considerForEpf: false, epfInclusionRule: EpfInclusionRule.Always,
            considerForEsi: true, calculateOnProRata: true, showInPayslip: true,
            tenantId: TenantId, createdBy: Actor);
        typeof(SalaryComponent).GetProperty("Id")!.SetValue(nonPf, NonPfId);

        SalaryComponent residual = SalaryComponent.CreateSystemFixedAllowance(TenantId, Actor);
        typeof(SalaryComponent).GetProperty("Id")!.SetValue(residual, ResidualId);

        ISalaryComponentRepository componentRepo = Substitute.For<ISalaryComponentRepository>();
        componentRepo.GetByIdAsync(PfEligibleId, Arg.Any<CancellationToken>()).Returns(pfEligibleComponentExists ? pfEligible : nonPf);
        componentRepo.GetByIdAsync(NonPfId, Arg.Any<CancellationToken>()).Returns(nonPf);
        componentRepo.GetByIdAsync(ResidualId, Arg.Any<CancellationToken>()).Returns(residual);

        ISalaryStructureTemplateRepository templateRepo = Substitute.For<ISalaryStructureTemplateRepository>();
        IUnitOfWork uow = Substitute.For<IUnitOfWork>();

        return (new CreateSalaryStructureTemplateHandler(templateRepo, componentRepo, tenantCtx, uow), templateRepo);
    }

    private static CreateSalaryStructureTemplateCommand MakeCommand(
        Guid firstComponentId, bool epfEnabled = true) =>
        new(
            Name: "Standard",
            Description: null,
            Components: [
                new TemplateComponentInput(firstComponentId, ComponentFormulaType.PercentOfCTC.ToString(), null, 50m, 0),
                new TemplateComponentInput(NonPfId, ComponentFormulaType.PercentOfBasic.ToString(), null, 40m, 1),
                new TemplateComponentInput(ResidualId, ComponentFormulaType.ResidualCTC.ToString(), null, null, 2),
            ],
            ActorId: Actor,
            EpfEnabled: epfEnabled,
            EsiEnabled: true,
            PtEnabled: true,
            LwfEnabled: true);

    [Fact]
    public async Task CreateTemplate_EpfEnabledWithPfEligibleComponent_Succeeds()
    {
        (CreateSalaryStructureTemplateHandler handler, ISalaryStructureTemplateRepository templateRepo) =
            MakeHandler(pfEligibleComponentExists: true);

        Guid id = await handler.Handle(MakeCommand(PfEligibleId, epfEnabled: true), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await templateRepo.Received(1).AddAsync(
            Arg.Is<SalaryStructureTemplate>(t => t.EpfEnabled && t.Name == "Standard"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTemplate_EpfEnabledButNoPfEligibleComponent_Throws()
    {
        (CreateSalaryStructureTemplateHandler handler, _) =
            MakeHandler(pfEligibleComponentExists: false);

        Func<Task> act = () => handler.Handle(MakeCommand(NonPfId, epfEnabled: true), CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("EPF is enabled but no component on this template has 'Consider for EPF' set.*");
    }

    [Fact]
    public async Task CreateTemplate_EpfDisabledAndNoPfEligibleComponent_Succeeds()
    {
        (CreateSalaryStructureTemplateHandler handler, ISalaryStructureTemplateRepository templateRepo) =
            MakeHandler(pfEligibleComponentExists: false);

        await handler.Handle(MakeCommand(NonPfId, epfEnabled: false), CancellationToken.None);

        await templateRepo.Received(1).AddAsync(
            Arg.Is<SalaryStructureTemplate>(t => !t.EpfEnabled),
            Arg.Any<CancellationToken>());
    }
}
