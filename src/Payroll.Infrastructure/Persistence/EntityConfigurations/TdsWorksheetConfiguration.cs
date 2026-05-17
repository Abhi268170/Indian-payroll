using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class TdsWorksheetConfiguration : IEntityTypeConfiguration<TdsWorksheet>
{
    public void Configure(EntityTypeBuilder<TdsWorksheet> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PayrollRunId).IsRequired();
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.FiscalYear).IsRequired();
        builder.Property(e => e.TaxRegime).IsRequired().HasMaxLength(20);

        builder.Property(e => e.AnnualProjectedIncome).HasColumnType("numeric(18,2)");
        builder.Property(e => e.StandardDeduction).HasColumnType("numeric(18,2)");
        builder.Property(e => e.TaxableIncome).HasColumnType("numeric(18,2)");
        builder.Property(e => e.TaxBeforeRebate).HasColumnType("numeric(18,2)");
        builder.Property(e => e.Rebate87A).HasColumnType("numeric(18,2)");
        builder.Property(e => e.Surcharge).HasColumnType("numeric(18,2)");
        builder.Property(e => e.Cess).HasColumnType("numeric(18,2)");
        builder.Property(e => e.AnnualTaxLiability).HasColumnType("numeric(18,2)");
        builder.Property(e => e.YtdTdsDeducted).HasColumnType("numeric(18,2)");
        builder.Property(e => e.TdsThisMonth).HasColumnType("numeric(18,2)");

        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.PayrollRunId, e.EmployeeId }).IsUnique();
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
