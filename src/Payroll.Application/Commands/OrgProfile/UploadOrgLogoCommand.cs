using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;
using OrgProfileEntity = Payroll.Domain.Entities.OrgProfile;

namespace Payroll.Application.Commands.OrgProfile;

public record UploadOrgLogoCommand(Stream FileStream, string ContentType, long SizeBytes, Guid ActorId) : IRequest;

public sealed class UploadOrgLogoHandler(
    IOrgProfileRepository repo,
    IUnitOfWork uow,
    IFileStorageService storage,
    ITenantContext tenant)
    : IRequestHandler<UploadOrgLogoCommand>
{
    private const long MaxSizeBytes = 2 * 1024 * 1024; // 2 MB

    public async Task Handle(UploadOrgLogoCommand request, CancellationToken cancellationToken)
    {
        if (request.SizeBytes > MaxSizeBytes)
            throw new DomainException("Logo must be under 2 MB.");

        if (request.ContentType is not ("image/png" or "image/jpeg"))
            throw new DomainException("Logo must be PNG or JPEG.");

        OrgProfileEntity? profile = await repo.GetAsync(cancellationToken);
        if (profile is null)
            throw new NotFoundException("Org profile not found. Save profile details first.");

        string objectKey = $"logos/{tenant.Schema}/{Guid.NewGuid()}";
        await storage.UploadAsync(objectKey, request.FileStream, request.ContentType, cancellationToken);

        profile.SetLogo(objectKey, request.ActorId);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
