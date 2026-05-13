using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Platform.Tenants;

internal sealed class SuspendTenantHandler(
    ITenantRepository repository,
    IPlatformUnitOfWork unitOfWork) : IRequestHandler<SuspendTenantCommand>
{
    public async Task Handle(SuspendTenantCommand command, CancellationToken cancellationToken)
    {
        Tenant? tenant = await repository.GetByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            throw new NotFoundException($"Tenant '{command.TenantId}' not found.");

        tenant.Suspend();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
