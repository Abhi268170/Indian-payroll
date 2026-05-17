using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record DeleteRecordedPaymentCommand(Guid RunId, Guid ActorId) : IRequest;

public sealed class DeleteRecordedPaymentCommandValidator : AbstractValidator<DeleteRecordedPaymentCommand>
{
    public DeleteRecordedPaymentCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class DeleteRecordedPaymentHandler(
    IPayrollRunRepository runRepo,
    IPayslipRepository payslipRepo,
    IPayrollRunAuditLogRepository auditLogRepo,
    IUnitOfWork uow)
    : IRequestHandler<DeleteRecordedPaymentCommand>
{
    public async Task Handle(DeleteRecordedPaymentCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Paid)
            throw new InvalidOperationException("Only a Paid payroll run can have its payment deleted.");

        run.DeletePayment(req.ActorId);
        runRepo.Update(run);

        // Unpublish payslips — employees should not see payslips if payment is reversed
        var payslips = await payslipRepo.GetByRunIdAsync(req.RunId, ct);
        foreach (var payslip in payslips.Where(p => p.IsPublished))
        {
            payslip.Unpublish(req.ActorId);
            payslipRepo.Update(payslip);
        }

        var auditEntry = PayrollRunAuditLog.Create(
            req.RunId, run.TenantId, PayrollRunStatus.Paid, PayrollRunStatus.Approved, req.ActorId,
            "Payment record deleted");
        await auditLogRepo.AddAsync(auditEntry, ct);

        await uow.SaveChangesAsync(ct);
    }
}
