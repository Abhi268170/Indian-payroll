using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.State).IsRequired().HasConversion<string>();
        builder.Property(b => b.TenantId).IsRequired();
        builder.Property(b => b.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(b => b.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(b => b.DeletedAt).HasColumnType("timestamptz");
        builder.HasIndex(b => b.TenantId);
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
