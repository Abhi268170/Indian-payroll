using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class CreateCostCentreHandler(ICostCentreRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateCostCentreCommand, Guid>
{
    public async Task<Guid> Handle(CreateCostCentreCommand request, CancellationToken cancellationToken)
    {
        CostCentre costCentre = CostCentre.Create(request.Name, request.Code, request.ActorId);
        await repo.AddAsync(costCentre, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return costCentre.Id;
    }
}
