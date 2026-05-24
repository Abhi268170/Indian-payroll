using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPayslipPdfQuery(Guid PayrollRunId, Guid EmployeeId) : IRequest<(Stream Stream, string FileName)>;

public sealed class GetPayslipPdfHandler(
    IPayslipRepository payslipRepo,
    IEmployeeRepository employeeRepo,
    IFileStorageService fileStorage)
    : IRequestHandler<GetPayslipPdfQuery, (Stream Stream, string FileName)>
{
    public async Task<(Stream Stream, string FileName)> Handle(GetPayslipPdfQuery req, CancellationToken ct)
    {
        var payslip = await payslipRepo.GetByRunAndEmployeeAsync(req.PayrollRunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Payslip not found for employee {req.EmployeeId}.");

        if (!payslip.IsPublished)
            throw new InvalidOperationException("Payslip is not yet published.");

        var employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        Stream stream = await fileStorage.GetAsync(payslip.PdfStorageKey, ct);
        string fileName = $"Payslip_{employee.EmployeeCode}_{payslip.GeneratedAt:yyyy-MM}.pdf";
        return (stream, fileName);
    }
}
