using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class UpdateSalaryComponentHandler(
    ISalaryComponentRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<UpdateSalaryComponentCommand>
{
    public async Task Handle(UpdateSalaryComponentCommand req, CancellationToken ct)
    {
        SalaryComponent? component = await repo.GetByIdAsync(req.Id, ct);
        if (component is null || component.TenantId != tenantContext.TenantId)
            throw new InvalidOperationException("Salary component not found.");

        component.UpdateDisplayName(req.Name, req.NameInPayslip);

        switch (component.Category)
        {
            case ComponentCategory.Earning when !component.IsAssociatedWithEmployee:
                if (req.FormulaType is not null)
                {
                    ComponentFormulaType formulaType = Enum.Parse<ComponentFormulaType>(req.FormulaType);
                    EpfInclusionRule epfRule = req.ConsiderForEpf is true && req.EpfInclusionRule is not null
                        ? Enum.Parse<EpfInclusionRule>(req.EpfInclusionRule)
                        : EpfInclusionRule.Always;
                    component.UpdateEarningFormula(
                        formulaType, req.FixedAmount, req.Percentage,
                        req.IsTaxable ?? false,
                        req.ConsiderForEpf ?? false, epfRule,
                        req.ConsiderForEsi ?? false,
                        req.CalculateOnProRata ?? true,
                        req.ShowInPayslip ?? true);
                }
                break;

            case ComponentCategory.Earning when component.IsAssociatedWithEmployee
                && component.FormulaType == ComponentFormulaType.Fixed
                && req.FixedAmount.HasValue:
                component.UpdateFixedAmount(req.FixedAmount.Value);
                break;

            case ComponentCategory.Deduction when req.DeductionFrequency is not null:
                component.UpdateDeduction(Enum.Parse<DeductionFrequency>(req.DeductionFrequency));
                break;

            case ComponentCategory.Reimbursement when req.ReimbursementAmount.HasValue && req.UnclaimedHandling is not null:
                component.UpdateReimbursement(
                    req.ReimbursementAmount.Value,
                    Enum.Parse<UnclaimedReimbursementHandling>(req.UnclaimedHandling));
                break;

            case ComponentCategory.Benefit:
                component.UpdateBenefitNameInPayslip(req.NameInPayslip);
                break;
        }

        await uow.SaveChangesAsync(ct);
    }
}
