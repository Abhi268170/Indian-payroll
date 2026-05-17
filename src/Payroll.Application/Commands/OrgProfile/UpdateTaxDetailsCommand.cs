using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;
using OrgProfileEntity = Payroll.Domain.Entities.OrgProfile;

namespace Payroll.Application.Commands.OrgProfile;

public sealed record UpdateTaxDetailsCommand(
    string? Tan,
    string? AoAreaCode,
    string? AoType,
    string? AoRangeCode,
    string? AoNumber,
    string? DeductorType,
    string? DeductorName,
    string? DeductorFathersName,
    string? DeductorDesignation,
    Guid ActorId) : IRequest;

internal sealed class UpdateTaxDetailsHandler(IOrgProfileRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateTaxDetailsCommand>
{
    public async Task Handle(UpdateTaxDetailsCommand cmd, CancellationToken ct)
    {
        OrgProfileEntity? profile = await repo.GetAsync(ct);
        if (profile is null)
            throw new NotFoundException("Organisation profile not found.");

        profile.UpdateTaxDetails(
            cmd.Tan,
            cmd.AoAreaCode,
            cmd.AoType,
            cmd.AoRangeCode,
            cmd.AoNumber,
            cmd.DeductorType,
            cmd.DeductorName,
            cmd.DeductorFathersName,
            cmd.DeductorDesignation,
            cmd.ActorId);

        repo.Update(profile);
        await uow.SaveChangesAsync(ct);
    }
}
