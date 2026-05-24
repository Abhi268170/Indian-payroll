using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class LwfStateConfigConfiguration : IEntityTypeConfiguration<LwfStateConfig>
{
    public void Configure(EntityTypeBuilder<LwfStateConfig> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StateCode).IsRequired().HasMaxLength(10);
        builder.Property(s => s.Frequency).IsRequired().HasMaxLength(20);
        builder.Property(s => s.EmployeeAmount).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.EmployerAmount).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.EmployeeRate).HasColumnType("numeric(7,4)");
        builder.Property(s => s.EmployerRate).HasColumnType("numeric(7,4)");
        builder.Property(s => s.RateCapEmployee).HasColumnType("numeric(18,4)");
        builder.Property(s => s.RateCapEmployer).HasColumnType("numeric(18,4)");
        builder.Property(s => s.WageThreshold).HasColumnType("numeric(18,4)");

        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.StateCode, s.EffectiveDate }).IsUnique();
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
