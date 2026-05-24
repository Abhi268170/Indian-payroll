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
            req.Name, req.Description, tenantContext.TenantId, req.ActorId);

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

        template.SetComponents(slots);
        await templateRepo.AddAsync(template, ct);
        await uow.SaveChangesAsync(ct);
        return template.Id;
    }
}
