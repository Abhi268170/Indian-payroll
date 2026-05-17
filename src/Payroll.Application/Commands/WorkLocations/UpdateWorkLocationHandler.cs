using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.WorkLocations;

public sealed class UpdateWorkLocationHandler(IWorkLocationRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateWorkLocationCommand, Unit>
{
    public async Task<Unit> Handle(UpdateWorkLocationCommand request, CancellationToken cancellationToken)
    {
        WorkLocation? location = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (location is null)
            throw new NotFoundException($"WorkLocation '{request.Id}' not found.");

        location.Update(
            request.Name,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.PinCode,
            request.PtRegistrationNumber,
            request.ActorId);

        repo.Update(location);
        await uow.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
