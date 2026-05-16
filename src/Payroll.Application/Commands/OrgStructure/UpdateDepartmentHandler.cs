using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class UpdateDepartmentHandler(IDepartmentRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateDepartmentCommand>
{
    public async Task Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Department not found");
        department.Update(request.Name, request.Code, request.Description, request.ActorId);
        repo.Update(department);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
