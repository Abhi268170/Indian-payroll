using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class IncomeTaxSlabConfiguration : IEntityTypeConfiguration<IncomeTaxSlab>
{
    public void Configure(EntityTypeBuilder<IncomeTaxSlab> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.FiscalYear).IsRequired().HasMaxLength(10);
        builder.Property(s => s.Regime).IsRequired().HasMaxLength(20);
        builder.Property(s => s.BracketMin).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.BracketMax).HasColumnType("numeric(18,4)");
        builder.Property(s => s.Rate).HasColumnType("numeric(7,4)").IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.FiscalYear, s.Regime, s.BracketMin });
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
