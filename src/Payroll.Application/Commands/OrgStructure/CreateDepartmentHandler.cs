using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

internal sealed class CreateDepartmentHandler(
    IDepartmentRepository departments,
    ITenantContext tenantContext,
    IUnitOfWork uow) : IRequestHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        Department dept = Department.Create(request.Name, tenantContext.TenantId, request.ActorId, request.Code);
        await departments.AddAsync(dept, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return dept.Id;
    }
}
