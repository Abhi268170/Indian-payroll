using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Behaviours;

internal sealed class TenantValidationBehaviour<TRequest, TResponse>(ITenantContext tenantContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved && request is not IAllowWithoutTenant)
            throw new InvalidOperationException(
                $"Tenant context not resolved for {typeof(TRequest).Name}. " +
                "Tenant-scoped commands require a resolved ITenantContext.");

        return next();
    }
}
