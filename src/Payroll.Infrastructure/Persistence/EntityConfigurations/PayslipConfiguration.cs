using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Persistence.EntityConfigurations;

internal sealed class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PayrollRunId).IsRequired();
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.PdfStorageKey).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.NetPay).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(e => e.NetPayInWords).HasMaxLength(500);
        builder.Property(e => e.YtdDataJson).HasColumnType("text");
        builder.Property(e => e.GeneratedAt).HasColumnType("timestamptz").IsRequired();

        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.PayrollRunId, e.EmployeeId }).IsUnique();
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
