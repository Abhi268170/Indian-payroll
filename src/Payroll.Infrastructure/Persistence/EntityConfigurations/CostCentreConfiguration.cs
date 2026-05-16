using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class CostCentreConfiguration : IEntityTypeConfiguration<CostCentre>
{
    public void Configure(EntityTypeBuilder<CostCentre> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Code).HasMaxLength(20);
        builder.Property(c => c.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(c => c.DeletedAt).HasColumnType("timestamptz");
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
