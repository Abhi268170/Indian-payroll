using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class ProfessionalTaxSlabConfiguration : IEntityTypeConfiguration<ProfessionalTaxSlab>
{
    public void Configure(EntityTypeBuilder<ProfessionalTaxSlab> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StateCode).IsRequired().HasMaxLength(10);
        builder.Property(s => s.Frequency).IsRequired().HasMaxLength(20);
        builder.Property(s => s.DeductionMonthsCsv).HasMaxLength(30);
        builder.Property(s => s.Gender).HasMaxLength(10);
        builder.Property(s => s.MinGross).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.MaxGross).HasColumnType("numeric(18,4)");
        builder.Property(s => s.PtAmount).HasColumnType("numeric(18,4)").IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.StateCode, s.EffectiveDate });
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
