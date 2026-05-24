using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class CreateDepartmentHandler(IDepartmentRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        Department department = Department.Create(request.Name, request.Code, request.Description, request.ActorId);
        await repo.AddAsync(department, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return department.Id;
    }
}
