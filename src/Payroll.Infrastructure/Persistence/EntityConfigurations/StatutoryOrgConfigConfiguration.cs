using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class StatutoryOrgConfigConfiguration : IEntityTypeConfiguration<StatutoryOrgConfig>
{
    public void Configure(EntityTypeBuilder<StatutoryOrgConfig> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.TenantId).IsUnique();

        builder.Property(s => s.EpfEstablishmentCode).HasMaxLength(30);
        builder.Property(s => s.EpfEmployeeContributionRate).IsRequired().HasMaxLength(40);
        builder.Property(s => s.EpfEmployerContributionRate).IsRequired().HasMaxLength(40);
        builder.Property(s => s.EsiEstablishmentCode).HasMaxLength(30);
        builder.Property(s => s.BonusRate).HasColumnType("numeric(5,4)").IsRequired().HasDefaultValue(0.0833m);
        builder.Property(s => s.BonusMode).IsRequired().HasMaxLength(20).HasDefaultValue("Yearly");

        builder.Property(s => s.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnType("timestamptz");

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
