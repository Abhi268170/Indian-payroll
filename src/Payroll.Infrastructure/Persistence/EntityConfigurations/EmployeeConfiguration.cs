using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.EncryptedPAN).IsRequired();
        builder.Property(e => e.EncryptedAadhaar);
        builder.Property(e => e.EncryptedBankAccount);
        builder.Property(e => e.EncryptedIFSC);
        builder.Property(e => e.UAN).HasMaxLength(12);
        builder.Property(e => e.ESICIPNumber).HasMaxLength(17);
        builder.Property(e => e.Gender).IsRequired().HasConversion<string>();
        builder.Property(e => e.EmploymentType).IsRequired().HasConversion<string>();
        builder.Property(e => e.Status).IsRequired().HasConversion<string>();
        builder.Property(e => e.WorkState).IsRequired().HasConversion<string>();
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.DepartmentId).IsRequired();
        builder.Property(e => e.DesignationId).IsRequired();

        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.TenantId, e.EmployeeCode }).IsUnique();
        builder.HasIndex(e => e.TenantId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
