using MediatR;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Platform;

public record GetTenantQuery(Guid Id) : IRequest<TenantDto>, IAllowWithoutTenant;

public sealed class GetTenantHandler(
    ITenantRepository tenants,
    IUserService userService)
    : IRequestHandler<GetTenantQuery, TenantDto>
{
    public async Task<TenantDto> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        Tenant? tenant = await tenants.GetByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
            throw new NotFoundException($"Tenant '{request.Id}' not found.");

        string? adminEmail = await userService.GetOrgAdminEmailAsync(tenant.Id, cancellationToken);

        return new TenantDto(tenant.Id, tenant.DisplayName, tenant.Slug, tenant.IsActive, tenant.CreatedAt)
        {
            Schema = tenant.Schema,
            AdminEmail = adminEmail,
        };
    }
}
