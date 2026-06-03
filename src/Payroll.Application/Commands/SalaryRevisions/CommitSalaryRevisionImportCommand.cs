using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryRevisions;

public record CommitSalaryRevisionImportCommand(Stream File, bool OverwriteExisting, Guid ActorId)
    : IRequest<SalaryRevisionImportCommitResult>;

public sealed record SalaryRevisionImportCommitResult(
    int Created,
    int Skipped,
    IReadOnlyList<SalaryRevisionImportRowError> Errors);

public sealed class CommitSalaryRevisionImportHandler(
    ISalaryRevisionImportParser parser,
    IEmployeeRepository employeeRepo,
    ISalaryStructureTemplateRepository templateRepo,
    IEmployeeSalaryStructureRepository salaryStructureRepo,
    ISalaryRevisionRepository revisionRepo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<CommitSalaryRevisionImportCommand, SalaryRevisionImportCommitResult>
{
    public async Task<SalaryRevisionImportCommitResult> Handle(
        CommitSalaryRevisionImportCommand req, CancellationToken ct)
    {
        IReadOnlyList<SalaryRevisionImportRow> rows = parser.Parse(req.File);
        if (rows.Count > SalaryRevisionImportProcessor.MaxRows)
            throw new DomainException($"Import file exceeds the {SalaryRevisionImportProcessor.MaxRows}-row limit.");

        SalaryRevisionImportContext ctx = await SalaryRevisionImportContext.LoadAsync(
            rows, employeeRepo, templateRepo, salaryStructureRepo, revisionRepo, tenantContext, ct);

        (List<ResolvedRevisionRow> valid, List<SalaryRevisionImportRow> skipped, List<SalaryRevisionImportRowError> errors) = SalaryRevisionImportProcessor.Process(
            rows, ctx.EmployeesByCode, ctx.TemplatesByName, ctx.ActiveCtcByEmployee,
            ctx.ExistingPayoutsByEmployee, req.OverwriteExisting);

        // Cache existing revisions per employee so overwrite soft-deletes the prior
        // pending/applied revision for the same payout period before re-creating.
        Dictionary<Guid, IReadOnlyList<SalaryRevision>> existingByEmployee = new Dictionary<Guid, IReadOnlyList<SalaryRevision>>();

        foreach (ResolvedRevisionRow r in valid)
        {
            if (r.IsDuplicate)
            {
                if (!existingByEmployee.TryGetValue(r.EmployeeId, out IReadOnlyList<SalaryRevision>? existing))
                {
                    existing = await revisionRepo.GetByEmployeeAsync(r.EmployeeId, ct);
                    existingByEmployee[r.EmployeeId] = existing;
                }
                foreach (SalaryRevision dup in existing.Where(x =>
                    x.PayoutMonth == r.PayoutMonth && x.PayoutYear == r.PayoutYear && !x.IsDeleted))
                {
                    dup.SoftDelete(req.ActorId);
                    revisionRepo.Update(dup);
                }
            }

            SalaryRevision revision = SalaryRevision.Create(
                r.EmployeeId,
                tenantContext.TenantId,
                r.PreviousAnnualCTC,
                r.NewAnnualCTC,
                r.EffectiveFromMonth,
                r.EffectiveFromYear,
                r.PayoutMonth,
                r.PayoutYear,
                r.TemplateId,
                r.Notes,
                req.ActorId);
            await revisionRepo.AddAsync(revision, ct);
        }

        await uow.SaveChangesAsync(ct);
        return new SalaryRevisionImportCommitResult(valid.Count, skipped.Count, errors);
    }
}
