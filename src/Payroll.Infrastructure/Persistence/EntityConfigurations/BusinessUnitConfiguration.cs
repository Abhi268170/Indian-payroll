using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class BusinessUnitConfiguration : IEntityTypeConfiguration<BusinessUnit>
{
    public void Configure(EntityTypeBuilder<BusinessUnit> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(150);
        builder.Property(b => b.Description).HasMaxLength(500);
        builder.Property(b => b.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(b => b.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(b => b.DeletedAt).HasColumnType("timestamptz");
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
