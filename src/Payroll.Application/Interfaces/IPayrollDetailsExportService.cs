namespace Payroll.Application.Interfaces;

public interface IPayrollDetailsExportService
{
    Task<ExportFileResult> ExportAsync(Guid runId, string format, CancellationToken ct = default);
}
