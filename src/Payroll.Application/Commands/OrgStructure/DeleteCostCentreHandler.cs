using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class DeleteCostCentreHandler(ICostCentreRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteCostCentreCommand>
{
    public async Task Handle(DeleteCostCentreCommand request, CancellationToken cancellationToken)
    {
        var costCentre = await repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Cost Centre not found");
        repo.Remove(costCentre);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
