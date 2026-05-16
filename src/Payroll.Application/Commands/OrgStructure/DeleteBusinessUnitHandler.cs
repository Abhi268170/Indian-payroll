using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class DeleteBusinessUnitHandler(IBusinessUnitRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteBusinessUnitCommand>
{
    public async Task Handle(DeleteBusinessUnitCommand request, CancellationToken cancellationToken)
    {
        var businessUnit = await repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Business Unit not found");
        repo.Remove(businessUnit);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
