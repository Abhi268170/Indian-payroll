using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class SetSalaryComponentActiveHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<SetSalaryComponentActiveCommand>
{
    public async Task Handle(SetSalaryComponentActiveCommand req, CancellationToken ct)
    {
        SalaryComponent? component = await repo.GetByIdAsync(req.Id, ct);
        if (component is null || component.TenantId != tenantContext.TenantId)
            throw new InvalidOperationException("Salary component not found.");

        if (!req.IsActive)
        {
            bool referenced = await repo.IsReferencedByTemplateAsync(req.Id, ct);
            if (referenced)
                throw new InvalidOperationException("Cannot deactivate a component that is used in an active salary structure template.");
        }

        component.SetActive(req.IsActive);
        await uow.SaveChangesAsync(ct);
    }
}
