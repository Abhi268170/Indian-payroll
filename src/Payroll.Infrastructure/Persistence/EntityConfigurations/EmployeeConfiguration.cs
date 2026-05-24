using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        // Basic
        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.MiddleName).HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.WorkEmail).IsRequired().HasMaxLength(255);
        builder.Property(e => e.MobileNumber).HasMaxLength(15);
        builder.Property(e => e.Gender).IsRequired().HasConversion<string>();
        builder.Property(e => e.EmploymentType).IsRequired().HasConversion<string>();
        builder.Property(e => e.Status).IsRequired().HasConversion<string>();
        builder.Property(e => e.PaymentMode).IsRequired().HasConversion<string>();
        builder.Property(e => e.AccountType).HasConversion<string>();
        builder.Property(e => e.DifferentlyAbledType).IsRequired().HasConversion<string>();
        builder.Property(e => e.ResidentialState).HasConversion<string>();

        // Org structure
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.DepartmentId).IsRequired();
        builder.Property(e => e.DesignationId).IsRequired();
        builder.Property(e => e.WorkLocationId).IsRequired();

        // Personal
        builder.Property(e => e.FathersName).HasMaxLength(200);
        builder.Property(e => e.PersonalEmail).HasMaxLength(255);
        builder.Property(e => e.AddressLine1).HasMaxLength(500);
        builder.Property(e => e.AddressLine2).HasMaxLength(500);
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.PinCode).HasMaxLength(6);

        // Statutory
        builder.Property(e => e.UAN).HasMaxLength(12);
        builder.Property(e => e.ESICIPNumber).HasMaxLength(17);

        // Encrypted sensitive fields (AES-256 ciphertext)
        builder.Property(e => e.EncryptedPAN);
        builder.Property(e => e.EncryptedAadhaar);
        builder.Property(e => e.EncryptedBankAccount);
        builder.Property(e => e.EncryptedIFSC);

        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.TenantId, e.EmployeeCode }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.WorkEmail }).IsUnique();
        builder.HasIndex(e => e.TenantId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
