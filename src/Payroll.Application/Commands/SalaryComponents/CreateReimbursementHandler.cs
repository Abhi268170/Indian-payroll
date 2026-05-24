using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class CreateReimbursementHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<CreateReimbursementCommand, Guid>
{
    public async Task<Guid> Handle(CreateReimbursementCommand req, CancellationToken ct)
    {
        ReimbursementType type = Enum.Parse<ReimbursementType>(req.ReimbursementType);
        UnclaimedReimbursementHandling handling = Enum.Parse<UnclaimedReimbursementHandling>(req.UnclaimedHandling);
        string code = string.IsNullOrEmpty(req.Code)
            ? new string(req.Name.ToUpperInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_').Take(50).ToArray())
            : req.Code.ToUpperInvariant();

        bool codeExists = await repo.ExistsCodeAsync(tenantContext.TenantId, code, excludeId: null, ct);
        if (codeExists)
            throw new InvalidOperationException($"A salary component with code '{code}' already exists.");

        SalaryComponent component = SalaryComponent.CreateReimbursement(
            req.Name, req.NameInPayslip, code, type, req.Amount, handling,
            tenantContext.TenantId, req.ActorId);

        await repo.AddAsync(component, ct);
        await uow.SaveChangesAsync(ct);
        return component.Id;
    }
}
