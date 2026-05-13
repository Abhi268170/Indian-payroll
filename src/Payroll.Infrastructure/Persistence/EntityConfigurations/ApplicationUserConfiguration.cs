using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.TenantId)
            .IsRequired(false);

        builder.Property(u => u.EmployeeId)
            .IsRequired(false);

        builder.Property(u => u.IsSuperAdmin)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
