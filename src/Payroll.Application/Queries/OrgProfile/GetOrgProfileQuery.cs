using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;
using OrgProfileEntity = Payroll.Domain.Entities.OrgProfile;

namespace Payroll.Application.Queries.OrgProfile;

public record GetOrgProfileQuery : IRequest<OrgProfileDto>;

public sealed class GetOrgProfileHandler(IOrgProfileRepository repo)
    : IRequestHandler<GetOrgProfileQuery, OrgProfileDto>
{
    public async Task<OrgProfileDto> Handle(GetOrgProfileQuery request, CancellationToken cancellationToken)
    {
        OrgProfileEntity? profile = await repo.GetAsync(cancellationToken);

        if (profile is null)
            return new OrgProfileDto(
                string.Empty, null, null, null, null, null, null,
                null, null, null, null, null, null, false);

        return new OrgProfileDto(
            profile.CompanyName,
            profile.LegalName,
            profile.Pan,
            profile.Gstin,
            profile.Website,
            profile.Industry,
            profile.IncorporationDate,
            profile.AddressLine1,
            profile.AddressLine2,
            profile.City,
            profile.State?.ToString(),
            profile.PinCode,
            profile.FilingAddressWorkLocationId,
            profile.LogoObjectKey is not null);
    }
}
