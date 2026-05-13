using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.Action).IsRequired().HasMaxLength(200);
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(200);
        builder.Property(a => a.EntityId).IsRequired();
        builder.Property(a => a.PerformedBy).IsRequired();
        builder.Property(a => a.OccurredAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.OldValue).HasColumnType("text");
        builder.Property(a => a.NewValue).HasColumnType("text");
        builder.HasIndex(a => new { a.TenantId, a.OccurredAt });
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
