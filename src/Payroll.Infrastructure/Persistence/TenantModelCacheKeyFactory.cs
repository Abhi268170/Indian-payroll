using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Payroll.Infrastructure.Persistence;

// Without this, EF Core compiles one model for all tenants and ignores HasDefaultSchema across requests.
// Uses PayrollDbContext.TenantSchema instead of constructor injection because this class lives in
// EF Core's internal service provider which cannot resolve application-scoped services.
internal sealed class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is PayrollDbContext payrollCtx
            ? (context.GetType(), payrollCtx.TenantSchema, designTime)
            : (context.GetType(), designTime);
}
