using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class PriorEmployerYtdConfiguration : IEntityTypeConfiguration<PriorEmployerYtd>
{
    public void Configure(EntityTypeBuilder<PriorEmployerYtd> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.FinancialYear).IsRequired();
        builder.Property(e => e.EmployerName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.GrossSalary).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(e => e.StandardDeductionClaimed).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(e => e.ProfessionalTaxPaid).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(e => e.TdsDeducted).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(e => e.OtherIncome).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        // One YTD record per employee per financial year
        builder.HasIndex(e => new { e.EmployeeId, e.FinancialYear }).IsUnique()
            .HasFilter("is_deleted = false");
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
