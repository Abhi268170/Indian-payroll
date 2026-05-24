using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

public sealed class DeleteDesignationHandler(IDesignationRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteDesignationCommand>
{
    public async Task Handle(DeleteDesignationCommand request, CancellationToken cancellationToken)
    {
        var designation = await repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Designation not found");
        repo.Remove(designation);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
