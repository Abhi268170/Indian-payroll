using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmployeeFyOpeningConfiguration : IEntityTypeConfiguration<EmployeeFyOpening>
{
    public void Configure(EntityTypeBuilder<EmployeeFyOpening> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.FiscalYear).IsRequired();
        builder.Property(e => e.MonthsCount).IsRequired();
        builder.Property(e => e.GrossSalary).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(e => e.TdsDeducted).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(e => e.PfDeducted).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.EmployeeId, e.FiscalYear }).IsUnique()
            .HasFilter("is_deleted = false");
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
