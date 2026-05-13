using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class SalaryComponentConfiguration : IEntityTypeConfiguration<SalaryComponent>
{
    public void Configure(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.FormulaType).IsRequired().HasConversion<string>();
        builder.Property(s => s.FixedAmount).HasColumnType("numeric(18,4)");
        builder.Property(s => s.Percentage).HasColumnType("numeric(7,4)");
        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnType("timestamptz");
        builder.HasIndex(s => new { s.TenantId, s.Code }).IsUnique();
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
