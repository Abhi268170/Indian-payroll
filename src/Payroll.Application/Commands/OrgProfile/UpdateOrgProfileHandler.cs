using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using OrgProfileEntity = Payroll.Domain.Entities.OrgProfile;

namespace Payroll.Application.Commands.OrgProfile;

public sealed class UpdateOrgProfileHandler(IOrgProfileRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateOrgProfileCommand>
{
    public async Task Handle(UpdateOrgProfileCommand request, CancellationToken cancellationToken)
    {
        IndianState? state = request.State is not null
            ? Enum.Parse<IndianState>(request.State)
            : null;

        OrgProfileEntity? profile = await repo.GetAsync(cancellationToken);

        if (profile is null)
        {
            profile = OrgProfileEntity.Create(request.CompanyName, request.ActorId);
            await repo.AddAsync(profile, cancellationToken);
        }

        profile.Update(
            request.CompanyName,
            request.LegalName,
            request.Pan,
            request.Gstin,
            request.Website,
            request.Industry,
            request.IncorporationDate,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            state,
            request.PinCode,
            request.FilingAddressWorkLocationId,
            request.ActorId);

        await uow.SaveChangesAsync(cancellationToken);
    }
}
