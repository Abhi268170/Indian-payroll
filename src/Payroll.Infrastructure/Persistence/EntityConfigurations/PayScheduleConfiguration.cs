using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class PayScheduleConfiguration : IEntityTypeConfiguration<PaySchedule>
{
    public void Configure(EntityTypeBuilder<PaySchedule> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.WorkWeekDays)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.SalaryCalculationMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.FixedWorkingDaysPerMonth);

        builder.Property(p => p.PayDateType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.PayDateDay);

        builder.Property(p => p.IsLockedAfterPayrun).IsRequired();

        builder.Property(p => p.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(p => p.DeletedAt).HasColumnType("timestamptz");

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
