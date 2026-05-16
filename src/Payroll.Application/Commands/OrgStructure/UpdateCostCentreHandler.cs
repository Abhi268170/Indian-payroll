using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class UpdateCostCentreHandler(ICostCentreRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateCostCentreCommand>
{
    public async Task Handle(UpdateCostCentreCommand request, CancellationToken cancellationToken)
    {
        var costCentre = await repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Cost Centre not found");
        costCentre.Update(request.Name, request.Code, request.ActorId);
        repo.Update(costCentre);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
