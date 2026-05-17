using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class SalaryRevisionConfiguration : IEntityTypeConfiguration<SalaryRevision>
{
    public void Configure(EntityTypeBuilder<SalaryRevision> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.PreviousAnnualCTC).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(e => e.NewAnnualCTC).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(e => e.Status).IsRequired().HasConversion<string>();
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.EmployeeId, e.Status });
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
