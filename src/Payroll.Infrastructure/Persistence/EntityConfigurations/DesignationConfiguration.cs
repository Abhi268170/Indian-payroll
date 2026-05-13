using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class DesignationConfiguration : IEntityTypeConfiguration<Designation>
{
    public void Configure(EntityTypeBuilder<Designation> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(d => d.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(d => d.DeletedAt).HasColumnType("timestamptz");
        builder.HasIndex(d => d.TenantId);
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
