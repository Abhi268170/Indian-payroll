using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

internal sealed class CreateCostCentreHandler(
    ICostCentreRepository costCentres,
    ITenantContext tenantContext,
    IUnitOfWork uow) : IRequestHandler<CreateCostCentreCommand, Guid>
{
    public async Task<Guid> Handle(CreateCostCentreCommand request, CancellationToken cancellationToken)
    {
        CostCentre costCentre = CostCentre.Create(request.Name, tenantContext.TenantId, request.ActorId, request.Code);
        await costCentres.AddAsync(costCentre, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return costCentre.Id;
    }
}
