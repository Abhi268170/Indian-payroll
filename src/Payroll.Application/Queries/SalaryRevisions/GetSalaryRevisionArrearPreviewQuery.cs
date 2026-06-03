using MediatR;
using Payroll.Application.Services;

namespace Payroll.Application.Queries.SalaryRevisions;

public sealed record ArrearLineDto(string Code, string Name, decimal Amount, bool IsTaxable);

public sealed record ArrearExcludedMonthDto(int Year, int Month, string Reason);

public sealed record SalaryRevisionArrearPreviewDto(
    IReadOnlyList<ArrearLineDto> Lines,
    decimal TotalArrear,
    decimal TotalTaxableArrear,
    IReadOnlyList<ArrearExcludedMonthDto> ExcludedMonths);

// WI-018 step 5 — preview the arrears a revision would pay, without persisting. Wraps
// ISalaryArrearService so the revise UI can show per-month/per-component arrears and the
// excluded-month warnings before the operator commits.
public record GetSalaryRevisionArrearPreviewQuery(Guid RevisionId)
    : IRequest<SalaryRevisionArrearPreviewDto>;

internal sealed class GetSalaryRevisionArrearPreviewHandler(ISalaryArrearService arrearService)
    : IRequestHandler<GetSalaryRevisionArrearPreviewQuery, SalaryRevisionArrearPreviewDto>
{
    public async Task<SalaryRevisionArrearPreviewDto> Handle(
        GetSalaryRevisionArrearPreviewQuery req, CancellationToken ct)
    {
        SalaryArrearResult result = await arrearService.ComputeForRevisionAsync(req.RevisionId, ct);

        return new SalaryRevisionArrearPreviewDto(
            Lines: result.Lines
                .Select(l => new ArrearLineDto(l.Code, l.Name, l.Amount, l.IsTaxable))
                .ToList(),
            TotalArrear: result.TotalArrear,
            TotalTaxableArrear: result.TotalTaxableArrear,
            ExcludedMonths: result.ExcludedMonths
                .Select(e => new ArrearExcludedMonthDto(e.Year, e.Month, e.Reason))
                .ToList());
    }
}
