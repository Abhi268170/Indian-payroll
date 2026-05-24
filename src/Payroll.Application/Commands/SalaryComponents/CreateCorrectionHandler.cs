using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class CreateCorrectionHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<CreateCorrectionCommand, Guid>
{
    public async Task<Guid> Handle(CreateCorrectionCommand req, CancellationToken ct)
    {
        SalaryComponent? parent = await repo.GetByIdAsync(req.ForCorrectionOfComponentId, ct);
        if (parent is null
            || parent.TenantId != tenantContext.TenantId
            || parent.Category != ComponentCategory.Earning
            || !parent.IsActive)
        {
            throw new InvalidOperationException("Parent component must be an active earning in the same organisation.");
        }

        string code = string.IsNullOrEmpty(req.Code)
            ? new string(req.CorrectionName.ToUpperInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_').Take(50).ToArray())
            : req.Code.ToUpperInvariant();

        bool codeExists = await repo.ExistsCodeAsync(tenantContext.TenantId, code, excludeId: null, ct);
        if (codeExists)
            throw new InvalidOperationException($"A salary component with code '{code}' already exists.");

        SalaryComponent component = SalaryComponent.CreateCorrection(
            req.CorrectionName, code, parent,
            tenantContext.TenantId, req.ActorId);

        await repo.AddAsync(component, ct);
        await uow.SaveChangesAsync(ct);
        return component.Id;
    }
}
