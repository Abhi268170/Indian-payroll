using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class SalaryComponentConfiguration : IEntityTypeConfiguration<SalaryComponent>
{
    public void Configure(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.ToTable("salary_components");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameInPayslip).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Category).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.IsSystemComponent).IsRequired();
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.IsAssociatedWithEmployee).IsRequired();
        builder.Property(s => s.IsOneTime).IsRequired().HasDefaultValue(false);

        // Earning fields
        builder.Property(s => s.EarningType).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.PayType).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.FormulaType).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.FixedAmount).HasColumnType("numeric(18,4)");
        builder.Property(s => s.Percentage).HasColumnType("numeric(7,4)");
        builder.Property(s => s.EpfInclusionRule).HasConversion<string>().HasMaxLength(40);

        // Deduction fields
        builder.Property(s => s.DeductionFrequency).HasConversion<string>().HasMaxLength(30);

        // Reimbursement fields
        builder.Property(s => s.ReimbursementType).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.ReimbursementAmount).HasColumnType("numeric(18,4)");
        builder.Property(s => s.UnclaimedHandling).HasConversion<string>().HasMaxLength(30);

        // Benefit fields
        builder.Property(s => s.BenefitType).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.BenefitPercentage).HasColumnType("numeric(7,4)");

        // Correction self-reference
        builder.HasOne(s => s.ForCorrectionOfComponent)
               .WithMany()
               .HasForeignKey(s => s.ForCorrectionOfComponentId)
               .OnDelete(DeleteBehavior.Restrict);

        // Audit columns
        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.TenantId, s.Code }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.IsOneTime, s.Category, s.IsActive });
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
