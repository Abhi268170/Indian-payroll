using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.WorkLocations;

public sealed class CreateWorkLocationHandler(IWorkLocationRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateWorkLocationCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkLocationCommand request, CancellationToken cancellationToken)
    {
        IndianState state = Enum.Parse<IndianState>(request.State);

        WorkLocation workLocation = WorkLocation.Create(
            request.Name,
            state,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.PinCode,
            request.ActorId);

        await repo.AddAsync(workLocation, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return workLocation.Id;
    }
}
