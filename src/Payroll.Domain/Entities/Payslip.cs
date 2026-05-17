using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class Payslip : AuditableEntity
{
    private Payslip() { }

    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid TenantId { get; private set; }
    public string PdfStorageKey { get; private set; } = default!;
    public DateTimeOffset GeneratedAt { get; private set; }
    public bool IsPublished { get; private set; }
    public decimal NetPay { get; private set; }
    public string? NetPayInWords { get; private set; }

    // JSON column — per-component YTD amounts for the fiscal year, denormalized for PDF rendering
    public string? YtdDataJson { get; private set; }

    public static Payslip Create(
        Guid payrollRunId,
        Guid employeeId,
        Guid tenantId,
        string pdfStorageKey,
        decimal netPay,
        string? netPayInWords,
        string? ytdDataJson,
        Guid createdBy) =>
        new()
        {
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            TenantId = tenantId,
            PdfStorageKey = pdfStorageKey,
            GeneratedAt = DateTimeOffset.UtcNow,
            IsPublished = false,
            NetPay = netPay,
            NetPayInWords = netPayInWords,
            YtdDataJson = ytdDataJson,
            CreatedBy = createdBy
        };

    public void Publish(Guid actorId)
    {
        IsPublished = true;
        SetUpdated(actorId);
    }
}
