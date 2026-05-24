using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;
using OrgProfileEntity = Payroll.Domain.Entities.OrgProfile;

namespace Payroll.Application.Commands.OrgProfile;

public record DeleteOrgLogoCommand(Guid ActorId) : IRequest;

public sealed class DeleteOrgLogoHandler(
    IOrgProfileRepository repo,
    IUnitOfWork uow,
    IFileStorageService storage)
    : IRequestHandler<DeleteOrgLogoCommand>
{
    public async Task Handle(DeleteOrgLogoCommand request, CancellationToken cancellationToken)
    {
        OrgProfileEntity? profile = await repo.GetAsync(cancellationToken);
        if (profile?.LogoObjectKey is null)
            return;

        await storage.DeleteAsync(profile.LogoObjectKey, cancellationToken);
        profile.ClearLogo(request.ActorId);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
