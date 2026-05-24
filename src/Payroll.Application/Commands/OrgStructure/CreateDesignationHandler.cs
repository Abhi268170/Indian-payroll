using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class CreateDesignationHandler(IDesignationRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateDesignationCommand, Guid>
{
    public async Task<Guid> Handle(CreateDesignationCommand request, CancellationToken cancellationToken)
    {
        Designation designation = Designation.Create(request.Name, request.ActorId);
        await repo.AddAsync(designation, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return designation.Id;
    }
}
