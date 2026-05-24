using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class PayrunEmployeeConfiguration : IEntityTypeConfiguration<PayrunEmployee>
{
    public void Configure(EntityTypeBuilder<PayrunEmployee> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PayrollRunId).IsRequired();
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Status).IsRequired().HasConversion<string>();

        builder.Property(e => e.GrossPay).HasColumnType("numeric(18,2)");
        builder.Property(e => e.NetPay).HasColumnType("numeric(18,2)");
        builder.Property(e => e.TaxesAmount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.BenefitsAmount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.ReimbursementsAmount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.EmployeePf).HasColumnType("numeric(18,2)");
        builder.Property(e => e.EmployerPf).HasColumnType("numeric(18,2)");
        builder.Property(e => e.EmployeeEsi).HasColumnType("numeric(18,2)");
        builder.Property(e => e.EmployerEsi).HasColumnType("numeric(18,2)");
        builder.Property(e => e.PtAmount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.TdsAmount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.LwfEmployeeAmount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.LwfEmployerAmount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.GratuityAmount).HasColumnType("numeric(18,4)");
        builder.Property(e => e.EpsAmount).HasColumnType("numeric(18,4)");
        builder.Property(e => e.MonthlyCTC).HasColumnType("numeric(18,2)");
        builder.Property(e => e.TdsOverrideAmount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.TdsOverrideReason).HasMaxLength(2000);
        builder.Property(e => e.SkipReason).HasMaxLength(2000);
        builder.Property(e => e.PaymentModeOverride).HasConversion<string>();
        builder.Property(e => e.EmployeeExitId);

        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.PayrollRunId, e.EmployeeId }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.EmployeeId });
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
