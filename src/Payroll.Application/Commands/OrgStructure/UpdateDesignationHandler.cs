using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class UpdateDesignationHandler(IDesignationRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateDesignationCommand>
{
    public async Task Handle(UpdateDesignationCommand request, CancellationToken cancellationToken)
    {
        var designation = await repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Designation not found");
        designation.Update(request.Name, request.ActorId);
        repo.Update(designation);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
