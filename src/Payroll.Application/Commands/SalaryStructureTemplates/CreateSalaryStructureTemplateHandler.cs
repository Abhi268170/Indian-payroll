using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryStructureTemplates;

public sealed class CreateSalaryStructureTemplateHandler(
    ISalaryStructureTemplateRepository templateRepo,
    ISalaryComponentRepository componentRepo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<CreateSalaryStructureTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreateSalaryStructureTemplateCommand req, CancellationToken ct)
    {
        SalaryStructureTemplate template = SalaryStructureTemplate.Create(
            req.Name, req.Description, tenantContext.TenantId, req.ActorId,
            epfEnabled: req.EpfEnabled, esiEnabled: req.EsiEnabled,
            ptEnabled: req.PtEnabled, lwfEnabled: req.LwfEnabled);

        List<SalaryStructureComponent> slots = [];
        bool hasPfEligibleWage = false;
        foreach (TemplateComponentInput input in req.Components)
        {
            SalaryComponent? component = await componentRepo.GetByIdAsync(input.ComponentId, ct);
            if (component is null || component.TenantId != tenantContext.TenantId || !component.IsActive)
                throw new InvalidOperationException($"Component {input.ComponentId} is not available.");

            if (component.ConsiderForEpf == true) hasPfEligibleWage = true;

            ComponentFormulaType formulaType = Enum.Parse<ComponentFormulaType>(input.FormulaType);
            slots.Add(SalaryStructureComponent.Create(
                template.Id, component.Id,
                formulaType, input.FixedAmount, input.Percentage,
                input.DisplayOrder));
        }

        // Catch the operator-mistake config where EPF is enabled at the template
        // level but no component contributes to the PF wage base. The engine
        // would silently compute zero PF for every employee on this template;
        // surface the mistake at template-save time instead.
        if (req.EpfEnabled && !hasPfEligibleWage)
            throw new InvalidOperationException(
                "EPF is enabled but no component on this template has 'Consider for EPF' set. Enable it on at least one earning component, or disable EPF on the template.");

        template.SetComponents(slots);
        await templateRepo.AddAsync(template, ct);
        await uow.SaveChangesAsync(ct);
        return template.Id;
    }
}
