using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmployeeExitConfiguration : IEntityTypeConfiguration<EmployeeExit>
{
    public void Configure(EntityTypeBuilder<EmployeeExit> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.Reason).IsRequired().HasConversion<string>();
        builder.Property(e => e.SettlementMode).IsRequired().HasConversion<string>();
        builder.Property(e => e.PersonalEmail).HasMaxLength(255);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        // One active exit record per employee
        builder.HasIndex(e => e.EmployeeId).IsUnique()
            .HasFilter("is_deleted = false");
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
