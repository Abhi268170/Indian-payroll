using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class CreateDeductionHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<CreateDeductionCommand, Guid>
{
    public async Task<Guid> Handle(CreateDeductionCommand req, CancellationToken ct)
    {
        DeductionFrequency frequency = Enum.Parse<DeductionFrequency>(req.DeductionFrequency);
        string code = string.IsNullOrEmpty(req.Code)
            ? new string(req.Name.ToUpperInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_').Take(50).ToArray())
            : req.Code.ToUpperInvariant();

        bool codeExists = await repo.ExistsCodeAsync(tenantContext.TenantId, code, excludeId: null, ct);
        if (codeExists)
            throw new InvalidOperationException($"A salary component with code '{code}' already exists.");

        SalaryComponent component = SalaryComponent.CreateDeduction(
            req.Name, req.NameInPayslip, code, frequency,
            tenantContext.TenantId, req.ActorId);

        await repo.AddAsync(component, ct);
        await uow.SaveChangesAsync(ct);
        return component.Id;
    }
}
