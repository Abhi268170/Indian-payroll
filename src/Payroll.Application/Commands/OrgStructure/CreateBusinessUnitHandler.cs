using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class CreateBusinessUnitHandler(IBusinessUnitRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateBusinessUnitCommand, Guid>
{
    public async Task<Guid> Handle(CreateBusinessUnitCommand request, CancellationToken cancellationToken)
    {
        BusinessUnit businessUnit = BusinessUnit.Create(request.Name, request.Description, request.ActorId);
        await repo.AddAsync(businessUnit, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return businessUnit.Id;
    }
}
