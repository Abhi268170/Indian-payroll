using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class IncomeTaxSurchargeSlabConfiguration : IEntityTypeConfiguration<IncomeTaxSurchargeSlab>
{
    public void Configure(EntityTypeBuilder<IncomeTaxSurchargeSlab> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.FiscalYear).IsRequired().HasMaxLength(10);
        builder.Property(s => s.Regime).IsRequired().HasMaxLength(20);
        builder.Property(s => s.IncomeFrom).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.IncomeTo).HasColumnType("numeric(18,4)");
        builder.Property(s => s.SurchargeRate).HasColumnType("numeric(7,4)").IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.FiscalYear, s.Regime });
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
