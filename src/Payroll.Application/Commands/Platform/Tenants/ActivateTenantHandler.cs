using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Platform.Tenants;

internal sealed class ActivateTenantHandler(
    ITenantRepository repository,
    IPlatformUnitOfWork unitOfWork,
    ITenantCacheService cache) : IRequestHandler<ActivateTenantCommand>
{
    public async Task Handle(ActivateTenantCommand command, CancellationToken cancellationToken)
    {
        Tenant? tenant = await repository.GetByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            throw new NotFoundException($"Tenant '{command.TenantId}' not found.");

        tenant.Activate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.EvictAsync(tenant.Slug, cancellationToken);
    }
}
