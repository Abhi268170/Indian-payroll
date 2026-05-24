using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class SalaryStructureComponentConfiguration : IEntityTypeConfiguration<SalaryStructureComponent>
{
    public void Configure(EntityTypeBuilder<SalaryStructureComponent> builder)
    {
        builder.ToTable("salary_structure_components");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TemplateId).IsRequired();
        builder.Property(c => c.ComponentId).IsRequired();
        builder.Property(c => c.FormulaType).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.FixedAmount).HasColumnType("numeric(18,4)");
        builder.Property(c => c.Percentage).HasColumnType("numeric(7,4)");
        builder.Property(c => c.DisplayOrder).IsRequired();

        builder.HasOne(c => c.Component)
               .WithMany()
               .HasForeignKey(c => c.ComponentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(c => c.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(c => new { c.TemplateId, c.ComponentId }).IsUnique();
    }
}
