using MediatR;
using Payroll.Application.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record ExportTdsBreakupQuery(Guid RunId, string Format) : IRequest<ExportFileResult>;

public sealed class ExportTdsBreakupHandler(ITdsBreakupExportService exportService)
    : IRequestHandler<ExportTdsBreakupQuery, ExportFileResult>
{
    public Task<ExportFileResult> Handle(ExportTdsBreakupQuery req, CancellationToken ct) =>
        exportService.ExportAsync(req.RunId, req.Format, ct);
}
