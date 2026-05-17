using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryStructureTemplates;

public sealed class SetSalaryStructureTemplateActiveHandler(
    ISalaryStructureTemplateRepository templateRepo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<SetSalaryStructureTemplateActiveCommand>
{
    public async Task Handle(SetSalaryStructureTemplateActiveCommand req, CancellationToken ct)
    {
        SalaryStructureTemplate? template = await templateRepo.GetByIdAsync(req.Id, ct);
        if (template is null || template.TenantId != tenantContext.TenantId)
            throw new InvalidOperationException("Salary structure template not found.");

        template.SetActive(req.IsActive);
        await uow.SaveChangesAsync(ct);
    }
}
