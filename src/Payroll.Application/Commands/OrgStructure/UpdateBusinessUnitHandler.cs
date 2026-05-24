using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class UpdateBusinessUnitHandler(IBusinessUnitRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateBusinessUnitCommand>
{
    public async Task Handle(UpdateBusinessUnitCommand request, CancellationToken cancellationToken)
    {
        var businessUnit = await repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Business Unit not found");
        businessUnit.Update(request.Name, request.Description, request.ActorId);
        repo.Update(businessUnit);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
