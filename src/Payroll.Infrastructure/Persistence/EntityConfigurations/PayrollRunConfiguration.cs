using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.Status).IsRequired().HasConversion<string>();
        builder.Property(p => p.Type).IsRequired().HasConversion<string>();
        builder.Property(p => p.FailureReason).HasMaxLength(2000);

        // Financial summary
        builder.Property(p => p.PayrollCost).HasColumnType("numeric(18,2)");
        builder.Property(p => p.TotalNetPay).HasColumnType("numeric(18,2)");
        builder.Property(p => p.TotalEmployerPf).HasColumnType("numeric(18,2)");
        builder.Property(p => p.TotalEmployerEsi).HasColumnType("numeric(18,2)");
        builder.Property(p => p.TotalTds).HasColumnType("numeric(18,2)");
        builder.Property(p => p.TotalPt).HasColumnType("numeric(18,2)");

        // Approval
        builder.Property(p => p.ApprovedAt).HasColumnType("timestamptz");
        builder.Property(p => p.ApprovalRejectionReason).HasMaxLength(2000);

        // Payment
        builder.Property(p => p.PaidAt).HasColumnType("timestamptz");
        builder.Property(p => p.PaymentMode).HasMaxLength(50);
        builder.Property(p => p.PaymentReference).HasMaxLength(500);
        builder.Property(p => p.BankAdviceFileKey).HasMaxLength(1000);

        // Statutory config snapshot
        builder.Property(p => p.StatutoryConfigSnapshot).HasColumnType("text");
        builder.Property(p => p.VariableInputsFileKey).HasMaxLength(1000);

        // Audit timestamps
        builder.Property(p => p.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(p => p.DeletedAt).HasColumnType("timestamptz");

        builder.OwnsOne(p => p.PayPeriod, pp =>
        {
            pp.Property(x => x.Year).HasColumnName("pay_period_year").IsRequired();
            pp.Property(x => x.Month).HasColumnName("pay_period_month").IsRequired();
        });

        builder.HasIndex(p => new { p.TenantId, p.Status });

        // Unique constraint on (tenant, period) excluding deleted rows is enforced via raw SQL
        // migration 20260519110000_AddUniquePayrollRunPeriodConstraint — not modelled here
        // because EF Core cannot index OwnsOne columns directly.

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
