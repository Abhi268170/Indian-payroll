namespace Payroll.Application.Interfaces;

public sealed record ExportFileResult(string FileName, string ContentType, byte[] Data);

public interface IPayrollExportService
{
    Task<ExportFileResult> ExportEmployeePayRunDetailsAsync(Guid runId, string format, CancellationToken ct = default);
}
