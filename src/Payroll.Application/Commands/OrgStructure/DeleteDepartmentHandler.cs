using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class DeleteDepartmentHandler(IDepartmentRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteDepartmentCommand>
{
    public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Department not found");
        repo.Remove(department);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
