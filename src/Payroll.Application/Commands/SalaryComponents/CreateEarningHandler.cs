using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class CreateEarningHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<CreateEarningCommand, Guid>
{
    public async Task<Guid> Handle(CreateEarningCommand req, CancellationToken ct)
    {
        EarningType earningType = Enum.Parse<EarningType>(req.EarningType);
        PayType payType = Enum.Parse<PayType>(req.PayType);
        ComponentFormulaType formulaType = Enum.Parse<ComponentFormulaType>(req.FormulaType);
        EpfInclusionRule epfRule = req.ConsiderForEpf
            ? Enum.Parse<EpfInclusionRule>(req.EpfInclusionRule)
            : EpfInclusionRule.Always;

        string code = string.IsNullOrEmpty(req.Code)
            ? GenerateCode(req.Name)
            : req.Code.ToUpperInvariant();

        bool codeExists = await repo.ExistsCodeAsync(tenantContext.TenantId, code, excludeId: null, ct);
        if (codeExists)
            throw new InvalidOperationException($"A salary component with code '{code}' already exists.");

        SalaryComponent component = SalaryComponent.CreateEarning(
            req.Name, req.NameInPayslip, code,
            earningType, payType, formulaType,
            req.FixedAmount, req.Percentage,
            req.IsTaxable, req.ConsiderForEpf, epfRule,
            req.ConsiderForEsi, req.CalculateOnProRata, req.ShowInPayslip,
            tenantContext.TenantId, req.ActorId);

        await repo.AddAsync(component, ct);
        await uow.SaveChangesAsync(ct);
        return component.Id;
    }

    private static string GenerateCode(string name) =>
        new(name.ToUpperInvariant()
                .Where(c => char.IsLetterOrDigit(c) || c == '_')
                .Take(50)
                .ToArray());
}
