using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Statutory;

public sealed record GetPtSlabsQuery(string StateCode, DateOnly AsOf) : IRequest<IReadOnlyList<PtSlabDto>>;

internal sealed class GetPtSlabsHandler(IStatutoryConfigRepository repo)
    : IRequestHandler<GetPtSlabsQuery, IReadOnlyList<PtSlabDto>>
{
    public async Task<IReadOnlyList<PtSlabDto>> Handle(GetPtSlabsQuery request, CancellationToken ct)
    {
        IReadOnlyList<ProfessionalTaxSlab> slabs = await repo.GetPtSlabsAsync(request.StateCode, request.AsOf, ct);
        return slabs.Select(s => new PtSlabDto(
            s.StateCode, s.EffectiveDate, s.Frequency, s.Gender,
            s.MinGross, s.MaxGross, s.PtAmount, s.IsFebruarySurcharge)).ToList();
    }
}
