using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Infrastructure.Persistence.EntityConfigurations;

namespace Payroll.Infrastructure.Persistence;

// Platform-level tables (tenant registry, Identity users/roles, OpenIddict stores, Data Protection keys).
// Always operates in the "public" schema — never crosses into tenant schemas.
public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        base.OnModelCreating(modelBuilder);
        // Apply only platform-level configs — not PayrollDbContext entity configs
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());
    }
}
