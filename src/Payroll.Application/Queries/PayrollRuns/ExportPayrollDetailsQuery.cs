using MediatR;
using Payroll.Application.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record ExportPayrollDetailsQuery(Guid RunId, string Format) : IRequest<ExportFileResult>;

public sealed class ExportPayrollDetailsHandler(IPayrollDetailsExportService exportService)
    : IRequestHandler<ExportPayrollDetailsQuery, ExportFileResult>
{
    public Task<ExportFileResult> Handle(ExportPayrollDetailsQuery req, CancellationToken ct) =>
        exportService.ExportAsync(req.RunId, req.Format, ct);
}
