using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.EmployeeId).IsRequired();
        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.DocumentType).IsRequired().HasMaxLength(100);
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(300);
        builder.Property(d => d.StorageKey).IsRequired().HasMaxLength(1000);
        builder.Property(d => d.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(d => d.CreatedBy).IsRequired();
        builder.HasIndex(d => d.EmployeeId);
    }
}
