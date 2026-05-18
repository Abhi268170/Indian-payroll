using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Statutory;

public sealed record GetPtRegistrationsQuery : IRequest<IReadOnlyList<PtRegistrationDto>>;

internal sealed class GetPtRegistrationsHandler(IStatutoryConfigRepository repo)
    : IRequestHandler<GetPtRegistrationsQuery, IReadOnlyList<PtRegistrationDto>>
{
    public async Task<IReadOnlyList<PtRegistrationDto>> Handle(GetPtRegistrationsQuery _, CancellationToken ct)
    {
        var registrations = await repo.GetPtRegistrationsAsync(ct);
        return registrations
            .Select(r => new PtRegistrationDto(r.StateCode, r.RegistrationNumber))
            .ToList();
    }
}
