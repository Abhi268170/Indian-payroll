using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record SendPayslipEmailCommand(Guid PayrollRunId, Guid EmployeeId) : IRequest;

public sealed class SendPayslipEmailHandler(
    IPayslipRepository payslipRepo,
    IPayrollRunRepository runRepo,
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

        var run = await runRepo.GetByIdAsync(req.PayrollRunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.PayrollRunId} not found.");

        // FnF settlements may be sent after the work email is deactivated, so the
        // command already prefers PersonalEmail when present (set during exit).
        string recipientEmail = string.IsNullOrEmpty(employee.PersonalEmail)
            ? employee.WorkEmail
            : employee.PersonalEmail;

        using Stream pdfStream = await fileStorage.GetAsync(payslip.PdfStorageKey, ct);
        using MemoryStream ms = new();
        await pdfStream.CopyToAsync(ms, ct);
        byte[] pdfBytes = ms.ToArray();

        // WI-23: FnF settlements get a distinct subject/filename so recipients
        // (and audit retrieval) can tell them apart from monthly payslips.
        bool isFnf = run.Type == PayrollRunType.FinalSettlement
                  || run.Type == PayrollRunType.BulkFinalSettlement;

        string period = $"{payslip.GeneratedAt:MMMM yyyy}";
        string subject, fileName, body;
        if (isFnf)
        {
            subject = $"Full & Final Settlement — {period}";
            fileName = $"FnF_Settlement_{employee.EmployeeCode}_{payslip.GeneratedAt:yyyy-MM}.pdf";
            body = $"<p>Dear {employee.FullName},</p>" +
                   "<p>Please find attached your Full & Final Settlement statement following your exit. " +
                   "It reflects your final pay, statutory deductions and any settlement components.</p>" +
                   "<p>Regards,<br/>HR Team</p>";
        }
        else
        {
            subject = $"Payslip for {period}";
            fileName = $"Payslip_{employee.EmployeeCode}_{payslip.GeneratedAt:yyyy-MM}.pdf";
            body = $"<p>Dear {employee.FullName},</p>" +
                   $"<p>Please find your payslip attached for {period}.</p>" +
                   "<p>Regards,<br/>HR Team</p>";
        }

        await emailService.SendWithAttachmentAsync(
            recipientEmail, subject, body, pdfBytes, fileName, "application/pdf", ct);
    }
}
