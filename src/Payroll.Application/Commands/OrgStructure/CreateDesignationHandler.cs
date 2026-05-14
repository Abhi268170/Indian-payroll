using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

internal sealed class CreateDesignationHandler(
    IDesignationRepository designations,
    ITenantContext tenantContext,
    IUnitOfWork uow) : IRequestHandler<CreateDesignationCommand, Guid>
{
    public async Task<Guid> Handle(CreateDesignationCommand request, CancellationToken cancellationToken)
    {
        Designation designation = Designation.Create(request.Name, tenantContext.TenantId, request.ActorId);
        await designations.AddAsync(designation, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return designation.Id;
    }
}
