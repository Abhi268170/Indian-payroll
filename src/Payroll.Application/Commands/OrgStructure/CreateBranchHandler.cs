using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

internal sealed class CreateBranchHandler(
    IBranchRepository branches,
    ITenantContext tenantContext,
    IUnitOfWork uow) : IRequestHandler<CreateBranchCommand, Guid>
{
    public async Task<Guid> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        Branch branch = Branch.Create(request.Name, request.State, tenantContext.TenantId, request.ActorId);
        await branches.AddAsync(branch, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return branch.Id;
    }
}
