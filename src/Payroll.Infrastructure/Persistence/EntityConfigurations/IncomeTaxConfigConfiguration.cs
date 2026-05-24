using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class IncomeTaxConfigConfiguration : IEntityTypeConfiguration<IncomeTaxConfig>
{
    public void Configure(EntityTypeBuilder<IncomeTaxConfig> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.FiscalYear, s.Regime }).IsUnique();

        builder.Property(s => s.FiscalYear).IsRequired().HasMaxLength(10);
        builder.Property(s => s.Regime).IsRequired().HasMaxLength(20);
        builder.Property(s => s.StandardDeduction).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.Rebate87ALimit).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.Rebate87AAmount).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.EmployerStatutoryCap).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.NpsEmployerMaxRate).HasColumnType("numeric(7,4)").IsRequired();
        builder.Property(s => s.CessRate).HasColumnType("numeric(7,4)").IsRequired();
        builder.Property(s => s.PfWageCap).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.EpfEmployeeRate).HasColumnType("numeric(7,4)").IsRequired();
        builder.Property(s => s.EpsEmployerRate).HasColumnType("numeric(7,4)").IsRequired();
        builder.Property(s => s.EpsCap).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.EsiWageLimit).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.EsiPwdWageLimit).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.EsiEmployeeRate).HasColumnType("numeric(7,4)").IsRequired();
        builder.Property(s => s.EsiEmployerRate).HasColumnType("numeric(7,4)").IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnType("timestamptz");

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
