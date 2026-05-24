using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Payslips;

public record DownloadPayslipQuery(Guid PayslipId) : IRequest<(Stream Stream, string FileName)>;

public sealed class DownloadPayslipHandler(
    IPayslipRepository payslipRepo,
    IEmployeeRepository employeeRepo,
    IFileStorageService fileStorage)
    : IRequestHandler<DownloadPayslipQuery, (Stream Stream, string FileName)>
{
    public async Task<(Stream Stream, string FileName)> Handle(DownloadPayslipQuery req, CancellationToken ct)
    {
        var payslip = await payslipRepo.GetByIdAsync(req.PayslipId, ct)
            ?? throw new NotFoundException($"Payslip {req.PayslipId} not found.");

        if (!payslip.IsPublished)
            throw new InvalidOperationException("Payslip is not yet published.");

        var employee = await employeeRepo.GetByIdAsync(payslip.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {payslip.EmployeeId} not found.");

        Stream stream = await fileStorage.GetAsync(payslip.PdfStorageKey, ct);
        string fileName = $"Payslip_{employee.EmployeeCode}_{payslip.GeneratedAt:yyyy-MM}.pdf";
        return (stream, fileName);
    }
}
