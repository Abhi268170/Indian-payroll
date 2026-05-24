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

        List<SalaryStructureComponent> slots = [];
        foreach (TemplateComponentInput input in req.Components)
        {
            SalaryComponent? component = await componentRepo.GetByIdAsync(input.ComponentId, ct);
            if (component is null || component.TenantId != tenantContext.TenantId || !component.IsActive)
                throw new InvalidOperationException($"Component {input.ComponentId} is not available.");

            ComponentFormulaType formulaType = Enum.Parse<ComponentFormulaType>(input.FormulaType);
            slots.Add(SalaryStructureComponent.Create(
                template.Id, component.Id,
                formulaType, input.FixedAmount, input.Percentage,
                input.DisplayOrder));
        }

        await templateRepo.ReplaceComponentsAsync(req.Id, slots, ct);
        await uow.SaveChangesAsync(ct);
    }
}
