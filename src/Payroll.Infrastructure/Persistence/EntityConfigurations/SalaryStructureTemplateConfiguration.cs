using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class SalaryStructureTemplateConfiguration : IEntityTypeConfiguration<SalaryStructureTemplate>
{
    public void Configure(EntityTypeBuilder<SalaryStructureTemplate> builder)
    {
        builder.ToTable("salary_structure_templates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.EpfEnabled).IsRequired().HasDefaultValue(true);
        builder.Property(t => t.EsiEnabled).IsRequired().HasDefaultValue(true);
        builder.Property(t => t.PtEnabled).IsRequired().HasDefaultValue(true);
        builder.Property(t => t.LwfEnabled).IsRequired().HasDefaultValue(true);

        builder.HasMany(t => t.Components)
               .WithOne()
               .HasForeignKey(c => c.TemplateId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(t => t.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(t => t.TenantId);
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
