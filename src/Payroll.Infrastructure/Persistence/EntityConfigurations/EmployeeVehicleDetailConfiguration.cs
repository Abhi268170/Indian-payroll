using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmployeeVehicleDetailConfiguration : IEntityTypeConfiguration<EmployeeVehicleDetail>
{
    public void Configure(EntityTypeBuilder<EmployeeVehicleDetail> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        // One vehicle record per employee
        builder.HasIndex(e => e.EmployeeId).IsUnique()
            .HasFilter("is_deleted = false");
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
