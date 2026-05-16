using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;
using OrgProfileEntity = Payroll.Domain.Entities.OrgProfile;

namespace Payroll.Application.Queries.OrgProfile;

public record GetOrgLogoStreamQuery : IRequest<Stream>;

public sealed class GetOrgLogoStreamHandler(IOrgProfileRepository repo, IFileStorageService storage)
    : IRequestHandler<GetOrgLogoStreamQuery, Stream>
{
    public async Task<Stream> Handle(GetOrgLogoStreamQuery request, CancellationToken cancellationToken)
    {
        OrgProfileEntity? profile = await repo.GetAsync(cancellationToken);
        if (profile?.LogoObjectKey is null)
            throw new NotFoundException("Logo not found.");

        return await storage.GetAsync(profile.LogoObjectKey, cancellationToken);
    }
}
