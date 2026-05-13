using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

// Without this, EF Core compiles one model for all tenants and ignores HasDefaultSchema across requests.
internal sealed class TenantModelCacheKeyFactory(ITenantContext tenantContext)
    : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), tenantContext.IsResolved ? tenantContext.Schema : "public", designTime);
}
