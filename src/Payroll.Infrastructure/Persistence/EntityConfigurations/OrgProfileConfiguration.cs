using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class OrgProfileConfiguration : IEntityTypeConfiguration<OrgProfile>
{
    public void Configure(EntityTypeBuilder<OrgProfile> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.LegalName).HasMaxLength(200);
        builder.Property(o => o.Pan).HasMaxLength(10);
        builder.Property(o => o.Gstin).HasMaxLength(15);
        builder.Property(o => o.Website).HasMaxLength(500);
        builder.Property(o => o.Industry).HasMaxLength(150);
        builder.Property(o => o.AddressLine1).HasMaxLength(250);
        builder.Property(o => o.AddressLine2).HasMaxLength(250);
        builder.Property(o => o.City).HasMaxLength(100);
        builder.Property(o => o.State).HasConversion<string>().HasMaxLength(100);
        builder.Property(o => o.PinCode).HasMaxLength(6);
        builder.Property(o => o.LogoObjectKey).HasMaxLength(500);

        builder.Property(o => o.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(o => o.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(o => o.DeletedAt).HasColumnType("timestamptz");

        builder.HasOne<WorkLocation>()
            .WithMany()
            .HasForeignKey(o => o.FilingAddressWorkLocationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
