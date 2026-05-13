using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

// Used by dotnet-ef at design time only (migrations add/remove/script).
// Injects a stub ITenantContext so OnModelCreating does not throw when schema is unresolved.
// Does NOT register TenantModelCacheKeyFactory — model caching is irrelevant for migration generation.
internal sealed class PayrollDbContextFactory : IDesignTimeDbContextFactory<PayrollDbContext>
{
    public PayrollDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PayrollDbContext> options = new();
        options.UseNpgsql("Host=localhost;Database=payroll_design;Username=postgres")
               .UseSnakeCaseNamingConvention();
        return new PayrollDbContext(options.Options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public string Schema => "migrations_placeholder";
        public string Slug => "design-time";
        public bool IsResolved => true;

        public void SetTenant(TenantInfo tenant) { }
    }
}
