using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.WorkLocations;

public sealed class ToggleWorkLocationActiveHandler(IWorkLocationRepository repo, IUnitOfWork uow)
    : IRequestHandler<ToggleWorkLocationActiveCommand, Unit>
{
    public async Task<Unit> Handle(ToggleWorkLocationActiveCommand request, CancellationToken cancellationToken)
    {
        WorkLocation? location = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (location is null)
            throw new NotFoundException($"WorkLocation '{request.Id}' not found.");

        if (request.Activate)
            location.Activate(request.ActorId);
        else
            location.Deactivate(request.ActorId);

        repo.Update(location);
        await uow.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
