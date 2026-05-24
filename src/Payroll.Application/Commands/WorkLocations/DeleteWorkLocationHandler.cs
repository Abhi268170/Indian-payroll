using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.WorkLocations;

public sealed class DeleteWorkLocationHandler(IWorkLocationRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteWorkLocationCommand, Unit>
{
    public async Task<Unit> Handle(DeleteWorkLocationCommand request, CancellationToken cancellationToken)
    {
        WorkLocation? location = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (location is null)
            throw new NotFoundException($"WorkLocation '{request.Id}' not found.");

        int employeeCount = await repo.GetEmployeeCountAsync(request.Id, cancellationToken);
        if (employeeCount > 0)
            throw new DomainException("Cannot delete a work location with assigned employees.");

        location.SoftDelete(request.ActorId);
        repo.Update(location);
        await uow.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
