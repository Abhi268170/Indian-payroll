using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;
using Payroll.Domain.ValueObjects;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.Status).IsRequired().HasConversion<string>();
        builder.Property(p => p.StartedAt).HasColumnType("timestamptz");
        builder.Property(p => p.CompletedAt).HasColumnType("timestamptz");
        builder.Property(p => p.FailureReason).HasMaxLength(2000);
        builder.Property(p => p.UnlockReason).HasMaxLength(2000);
        builder.Property(p => p.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(p => p.DeletedAt).HasColumnType("timestamptz");

        builder.OwnsOne(p => p.PayPeriod, pp =>
        {
            pp.Property(x => x.Year).HasColumnName("pay_period_year").IsRequired();
            pp.Property(x => x.Month).HasColumnName("pay_period_month").IsRequired();
        });

        builder.HasIndex(p => new { p.TenantId, p.Status });
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
