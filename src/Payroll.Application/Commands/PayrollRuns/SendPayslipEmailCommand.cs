using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record SendPayslipEmailCommand(Guid PayrollRunId, Guid EmployeeId) : IRequest;

public sealed class SendPayslipEmailHandler(
    IPayslipRepository payslipRepo,
    IEmployeeRepository employeeRepo,
    IFileStorageService fileStorage,
    IEmailService emailService)
    : IRequestHandler<SendPayslipEmailCommand>
{
    public async Task Handle(SendPayslipEmailCommand req, CancellationToken ct)
    {
        var payslip = await payslipRepo.GetByRunAndEmployeeAsync(req.PayrollRunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Payslip not found for employee {req.EmployeeId} in run {req.PayrollRunId}.");

        if (!payslip.IsPublished)
            throw new InvalidOperationException("Payslip must be published before it can be sent.");

        var employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        string recipientEmail = string.IsNullOrEmpty(employee.PersonalEmail)
            ? employee.WorkEmail
            : employee.PersonalEmail;

        using Stream pdfStream = await fileStorage.GetAsync(payslip.PdfStorageKey, ct);
        using MemoryStream ms = new();
        await pdfStream.CopyToAsync(ms, ct);
        byte[] pdfBytes = ms.ToArray();

        string fileName = $"Payslip_{employee.EmployeeCode}_{payslip.GeneratedAt:yyyy-MM}.pdf";
        string body = $"<p>Dear {employee.FullName},</p>" +
                      $"<p>Please find your payslip attached for {payslip.GeneratedAt:MMMM yyyy}.</p>" +
                      "<p>Regards,<br/>HR Team</p>";

        await emailService.SendWithAttachmentAsync(
            recipientEmail,
            $"Payslip for {payslip.GeneratedAt:MMMM yyyy}",
            body,
            pdfBytes,
            fileName,
            "application/pdf",
            ct);
    }
}
