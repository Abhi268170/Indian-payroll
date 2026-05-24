using MediatR;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Platform;

public record ListTenantsQuery : IRequest<IReadOnlyList<TenantDto>>, IAllowWithoutTenant;

public sealed class ListTenantsHandler(ITenantRepository tenants)
    : IRequestHandler<ListTenantsQuery, IReadOnlyList<TenantDto>>
{
    public async Task<IReadOnlyList<TenantDto>> Handle(
        ListTenantsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Tenant> all = await tenants.ListAllAsync(cancellationToken);
        return all.Select(t => new TenantDto(t.Id, t.DisplayName, t.Slug, t.IsActive, t.CreatedAt))
                  .ToList();
    }
}
