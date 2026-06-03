using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.SalaryRevisions;

public sealed record SalaryRevisionSummaryDto(
    Guid Id,
    string Status,
    decimal PreviousAnnualCTC,
    decimal NewAnnualCTC,
    int EffectiveFromMonth,
    int EffectiveFromYear,
    int PayoutMonth,
    int PayoutYear,
    bool ArrearsPaid,
    string? Notes,
    DateTimeOffset CreatedAt);

public record ListSalaryRevisionsQuery(Guid EmployeeId) : IRequest<IReadOnlyList<SalaryRevisionSummaryDto>>;

internal sealed class ListSalaryRevisionsHandler(ISalaryRevisionRepository revisionRepo)
    : IRequestHandler<ListSalaryRevisionsQuery, IReadOnlyList<SalaryRevisionSummaryDto>>
{
    public async Task<IReadOnlyList<SalaryRevisionSummaryDto>> Handle(
        ListSalaryRevisionsQuery req, CancellationToken ct)
    {
        IReadOnlyList<SalaryRevision> revisions = await revisionRepo.GetByEmployeeAsync(req.EmployeeId, ct);
        return revisions
            .Select(r => new SalaryRevisionSummaryDto(
                r.Id,
                r.Status.ToString(),
                r.PreviousAnnualCTC,
                r.NewAnnualCTC,
                r.EffectiveFromMonth,
                r.EffectiveFromYear,
                r.PayoutMonth,
                r.PayoutYear,
                r.ArrearPaidRunId is not null,
                r.Notes,
                r.CreatedAt))
            .ToList();
    }
}
