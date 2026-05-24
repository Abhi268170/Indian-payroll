using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class WorkLocationConfiguration : IEntityTypeConfiguration<WorkLocation>
{
    public void Configure(EntityTypeBuilder<WorkLocation> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(w => w.State)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(w => w.AddressLine1).HasMaxLength(250);
        builder.Property(w => w.AddressLine2).HasMaxLength(250);
        builder.Property(w => w.City).HasMaxLength(250);
        builder.Property(w => w.PinCode).HasMaxLength(6);
        builder.Property(w => w.PtRegistrationNumber).HasMaxLength(50);

        builder.Property(w => w.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(w => w.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(w => w.DeletedAt).HasColumnType("timestamptz");

        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}
