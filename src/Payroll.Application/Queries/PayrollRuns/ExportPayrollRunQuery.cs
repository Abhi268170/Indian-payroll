using MediatR;
using Payroll.Application.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record ExportPayrollRunQuery(Guid RunId, string Format) : IRequest<ExportFileResult>;

public sealed class ExportPayrollRunHandler(IPayrollExportService exportService)
    : IRequestHandler<ExportPayrollRunQuery, ExportFileResult>
{
    public Task<ExportFileResult> Handle(ExportPayrollRunQuery req, CancellationToken ct) =>
        exportService.ExportEmployeePayRunDetailsAsync(req.RunId, req.Format, ct);
}
