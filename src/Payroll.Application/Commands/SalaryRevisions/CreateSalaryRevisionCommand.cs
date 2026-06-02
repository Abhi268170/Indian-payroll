using FluentValidation;
using MediatR;
using Payroll.Application.Commands.Employees;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryRevisions;

public record CreateSalaryRevisionCommand(
    Guid EmployeeId,
    decimal NewAnnualCTC,
    int EffectiveFromMonth,
    int EffectiveFromYear,
    int PayoutMonth,
    int PayoutYear,
    Guid? SalaryStructureTemplateId,
    IReadOnlyList<ComponentOverrideInput> Overrides,
    string? Notes,
    Guid ActorId) : IRequest<Guid>;

internal sealed class CreateSalaryRevisionCommandValidator : AbstractValidator<CreateSalaryRevisionCommand>
{
    public CreateSalaryRevisionCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.NewAnnualCTC).GreaterThan(0m).WithMessage("New annual CTC must be greater than zero.");
        RuleFor(x => x.EffectiveFromMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.PayoutMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.EffectiveFromYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.PayoutYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x)
            .Must(x => (x.EffectiveFromYear * 12 + x.EffectiveFromMonth) <= (x.PayoutYear * 12 + x.PayoutMonth))
            .WithMessage("Effective-from month must be on or before the payout month.");
        // Per-component arrears need a reproducible structure: a template, an override
        // snapshot, or both. A revision with neither cannot resolve component amounts.
        RuleFor(x => x)
            .Must(x => x.SalaryStructureTemplateId is not null || x.Overrides.Count > 0)
            .WithMessage("A salary revision must reference a template or supply component overrides.");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class CreateSalaryRevisionHandler(
    IEmployeeRepository employeeRepo,
    IEmployeeSalaryStructureRepository salaryStructureRepo,
    ISalaryRevisionRepository revisionRepo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<CreateSalaryRevisionCommand, Guid>
{
    public async Task<Guid> Handle(CreateSalaryRevisionCommand req, CancellationToken ct)
    {
        Employee employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        EmployeeSalaryStructure active = await salaryStructureRepo.GetActiveAsync(employee.Id, ct)
            ?? throw new DomainException("Employee has no active salary structure to revise.");

        SalaryRevision revision = SalaryRevision.Create(
            employee.Id,
            tenantContext.TenantId,
            active.AnnualCTC,
            req.NewAnnualCTC,
            req.EffectiveFromMonth,
            req.EffectiveFromYear,
            req.PayoutMonth,
            req.PayoutYear,
            req.SalaryStructureTemplateId,
            req.Notes,
            req.ActorId);

        foreach (ComponentOverrideInput o in req.Overrides)
        {
            if (!Enum.TryParse<ComponentFormulaType>(o.FormulaType, out ComponentFormulaType formulaType))
                continue;
            revision.AddOverride(SalaryRevisionComponentOverride.Create(
                revision.Id, o.SalaryComponentId, formulaType, o.Percentage, o.FixedAmount, req.ActorId));
        }

        await revisionRepo.AddAsync(revision, ct);
        await uow.SaveChangesAsync(ct);
        return revision.Id;
    }
}
