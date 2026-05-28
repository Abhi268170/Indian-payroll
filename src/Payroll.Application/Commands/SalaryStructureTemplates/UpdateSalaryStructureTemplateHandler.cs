using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using MediatR;

namespace Payroll.Application.Commands.SalaryStructureTemplates;

public sealed class UpdateSalaryStructureTemplateHandler(
    ISalaryStructureTemplateRepository templateRepo,
    ISalaryComponentRepository componentRepo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<UpdateSalaryStructureTemplateCommand>
{
    public async Task Handle(UpdateSalaryStructureTemplateCommand req, CancellationToken ct)
    {
        SalaryStructureTemplate? template = await templateRepo.GetByIdAsync(req.Id, ct);
        if (template is null || template.TenantId != tenantContext.TenantId)
            throw new InvalidOperationException("Salary structure template not found.");

        template.Update(req.Name, req.Description);
        template.UpdateStatutoryDefaults(req.EpfEnabled, req.EsiEnabled, req.PtEnabled, req.LwfEnabled);

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

        if (req.EpfEnabled && !hasPfEligibleWage)
            throw new InvalidOperationException(
                "EPF is enabled but no component on this template has 'Consider for EPF' set. Enable it on at least one earning component, or disable EPF on the template.");

        await templateRepo.ReplaceComponentsAsync(req.Id, slots, ct);
        await uow.SaveChangesAsync(ct);
    }
}
