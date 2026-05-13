using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Payroll.Infrastructure.Persistence;

// Used by dotnet-ef at design time only (migrations add/remove/script).
internal sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PlatformDbContext> options = new();
        options.UseNpgsql("Host=localhost;Database=payroll_design;Username=postgres")
               .UseSnakeCaseNamingConvention()
               .UseOpenIddict();
        return new PlatformDbContext(options.Options);
    }
}
