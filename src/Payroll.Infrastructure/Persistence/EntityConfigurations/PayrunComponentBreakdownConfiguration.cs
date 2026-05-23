using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class PayrunComponentBreakdownConfiguration : IEntityTypeConfiguration<PayrunComponentBreakdown>
{
    public void Configure(EntityTypeBuilder<PayrunComponentBreakdown> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PayrollRunId).IsRequired();
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.ComponentCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ComponentName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.FullAmount).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(e => e.ProratedAmount).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(e => e.IsTaxable).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.ConsiderForEpf).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.ConsiderForEsi).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.CalculateOnProRata).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.EpfInclusionRule).IsRequired().HasConversion<string>().HasMaxLength(40).HasDefaultValue(Payroll.Domain.Enums.EpfInclusionRule.Always);
        builder.Property(e => e.ShowInPayslip).IsRequired().HasDefaultValue(true);

        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.PayrollRunId, e.EmployeeId });
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
