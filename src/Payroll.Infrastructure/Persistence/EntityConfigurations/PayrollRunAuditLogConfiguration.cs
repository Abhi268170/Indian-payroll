using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class PayrollRunAuditLogConfiguration : IEntityTypeConfiguration<PayrollRunAuditLog>
{
    public void Configure(EntityTypeBuilder<PayrollRunAuditLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PayrollRunId).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.FromStatus).IsRequired().HasConversion<string>();
        builder.Property(e => e.ToStatus).IsRequired().HasConversion<string>();
        builder.Property(e => e.ActorUserId).IsRequired();
        builder.Property(e => e.Timestamp).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(2000);

        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.PayrollRunId, e.Timestamp });
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
