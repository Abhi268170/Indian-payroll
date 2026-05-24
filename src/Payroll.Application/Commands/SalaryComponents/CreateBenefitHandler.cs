using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class CreateBenefitHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<CreateBenefitCommand, Guid>
{
    public async Task<Guid> Handle(CreateBenefitCommand req, CancellationToken ct)
    {
        BenefitType benefitType = Enum.Parse<BenefitType>(req.BenefitType);
        string code = string.IsNullOrEmpty(req.Code)
            ? new string(req.Name.ToUpperInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_').Take(50).ToArray())
            : req.Code.ToUpperInvariant();

        bool codeExists = await repo.ExistsCodeAsync(tenantContext.TenantId, code, excludeId: null, ct);
        if (codeExists)
            throw new InvalidOperationException($"A salary component with code '{code}' already exists.");

        // VPF and NPS are one-per-tenant benefit types
        if (benefitType is BenefitType.VPF or BenefitType.NPS)
        {
            bool typeExists = await repo.HasActiveBenefitTypeAsync(tenantContext.TenantId, benefitType, excludeId: null, ct);
            if (typeExists)
                throw new InvalidOperationException($"A {benefitType} benefit component already exists for this organisation.");
        }

        SalaryComponent component = SalaryComponent.CreateBenefit(
            req.Name, req.NameInPayslip, code, benefitType,
            req.BenefitPercentage, req.IsApplicableToAllEmployees, req.IsNpsGovernmentSector,
            tenantContext.TenantId, req.ActorId);

        await repo.AddAsync(component, ct);
        await uow.SaveChangesAsync(ct);
        return component.Id;
    }
}
