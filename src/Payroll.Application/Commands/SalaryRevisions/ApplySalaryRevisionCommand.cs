using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryRevisions;

public record ApplySalaryRevisionCommand(Guid RevisionId, Guid ActorId) : IRequest;

internal sealed class ApplySalaryRevisionCommandValidator : AbstractValidator<ApplySalaryRevisionCommand>
{
    public ApplySalaryRevisionCommandValidator()
    {
        RuleFor(x => x.RevisionId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

// Applies a pending revision: closes the current salary structure the day before the
// revision's effective month and opens a new backdated structure reproduced from the
// revision's CTC + template + override snapshot. Does NOT alter employee statutory
// flags — a revision changes compensation, not PF/ESI/PT/LWF eligibility.
public sealed class ApplySalaryRevisionHandler(
    ISalaryRevisionRepository revisionRepo,
    IEmployeeSalaryStructureRepository salaryStructureRepo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<ApplySalaryRevisionCommand>
{
    public async Task Handle(ApplySalaryRevisionCommand req, CancellationToken ct)
    {
        SalaryRevision revision = await revisionRepo.GetByIdWithOverridesAsync(req.RevisionId, ct)
            ?? throw new NotFoundException($"Salary revision {req.RevisionId} not found.");

        if (revision.Status != SalaryRevisionStatus.Pending)
            throw new InvalidOperationException("Salary revision has already been applied.");

        var effectiveFrom = new DateOnly(revision.EffectiveFromYear, revision.EffectiveFromMonth, 1);

        EmployeeSalaryStructure? existing = await salaryStructureRepo.GetActiveAsync(revision.EmployeeId, ct);
        if (existing is not null)
        {
            existing.Close(effectiveFrom.AddDays(-1), req.ActorId);
            salaryStructureRepo.Update(existing);
        }

        EmployeeSalaryStructure structure = EmployeeSalaryStructure.Create(
            revision.EmployeeId,
            tenantContext.TenantId,
            revision.SalaryStructureTemplateId,
            revision.NewAnnualCTC,
            effectiveFrom,
            req.ActorId);

        foreach (SalaryRevisionComponentOverride o in revision.ComponentOverrides)
        {
            structure.AddOverride(EmployeeSalaryComponentOverride.Create(
                structure.Id, o.SalaryComponentId, o.FormulaType, o.Percentage, o.FixedAmount, req.ActorId));
        }

        await salaryStructureRepo.AddAsync(structure, ct);

        revision.Apply(structure.Id, req.ActorId);
        revisionRepo.Update(revision);

        await uow.SaveChangesAsync(ct);
    }
}
