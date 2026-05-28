namespace Payroll.Application.Interfaces;

public interface ITdsBreakupExportService
{
    Task<ExportFileResult> ExportAsync(Guid runId, string format, CancellationToken ct = default);
}
