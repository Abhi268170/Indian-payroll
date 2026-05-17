using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmployeeSalaryComponentOverrideConfiguration : IEntityTypeConfiguration<EmployeeSalaryComponentOverride>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryComponentOverride> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeSalaryStructureId).IsRequired();
        builder.Property(e => e.SalaryComponentId).IsRequired();
        builder.Property(e => e.FormulaType).IsRequired().HasConversion<string>();
        builder.Property(e => e.Percentage).HasColumnType("numeric(7,4)");
        builder.Property(e => e.FixedAmount).HasColumnType("numeric(18,4)");
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.EmployeeSalaryStructureId, e.SalaryComponentId }).IsUnique();
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
