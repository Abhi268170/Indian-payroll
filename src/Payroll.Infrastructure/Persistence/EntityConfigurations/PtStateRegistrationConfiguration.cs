using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class PtStateRegistrationConfiguration : IEntityTypeConfiguration<PtStateRegistration>
{
    public void Configure(EntityTypeBuilder<PtStateRegistration> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.StateCode).IsRequired().HasMaxLength(10);
        builder.Property(r => r.RegistrationNumber).IsRequired().HasMaxLength(100);

        builder.Property(r => r.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(r => r.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(r => r.StateCode).IsUnique();
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
