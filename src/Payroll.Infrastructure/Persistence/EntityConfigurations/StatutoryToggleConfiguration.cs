using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class StatutoryToggleConfiguration : IEntityTypeConfiguration<StatutoryToggle>
{
    public void Configure(EntityTypeBuilder<StatutoryToggle> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.Module).IsRequired().HasConversion<string>();
        builder.Property(s => s.IsEnabled).IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnType("timestamptz");
        builder.HasIndex(s => new { s.TenantId, s.Module }).IsUnique();
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
