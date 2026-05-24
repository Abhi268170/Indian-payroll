using FluentValidation;
using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record RecordPaymentCommand(
    Guid RunId,
    DateOnly PaymentDate,
    string PaymentMode,
    string? Reference,
    bool NotifyEmployees,
    Guid ActorId) : IRequest;

public sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.PaymentMode).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class RecordPaymentHandler(
    IPayrollRunRepository runRepo,
    IPayrollRunAuditLogRepository auditLogRepo,
    IPayScheduleRepository payScheduleRepo,
    IPayrollJobDispatcher jobDispatcher,
    IPayrunEmployeeRepository payrunEmpRepo,
    IEmployeeRepository employeeRepo,
    IUnitOfWork uow)
    : IRequestHandler<RecordPaymentCommand>
{
    public async Task Handle(RecordPaymentCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Approved)
            throw new InvalidOperationException("Only an Approved payroll run can record payment.");

        if (!Enum.TryParse<PaymentMode>(req.PaymentMode, ignoreCase: true, out PaymentMode mode))
            throw new DomainException($"Invalid payment mode: {req.PaymentMode}.");

        run.RecordPayment(req.PaymentDate, mode.ToString(), req.Reference, req.ActorId);
        runRepo.Update(run);

        var paySchedule = await payScheduleRepo.GetAsync(ct);
        if (paySchedule is not null && !paySchedule.IsLockedAfterPayrun)
        {
            paySchedule.LockAfterPayrun();
            payScheduleRepo.Update(paySchedule);
        }

        var auditEntry = PayrollRunAuditLog.Create(
            req.RunId, run.TenantId, PayrollRunStatus.Approved, PayrollRunStatus.Paid, req.ActorId, null);
        await auditLogRepo.AddAsync(auditEntry, ct);

        // FnF settlement payment also flips each linked employee to Exited.
        // Iterates all PayrunEmployees because Bulk runs can carry many exits
        // on the same pay date — must not hardcode for single-employee.
        if (run.Type == PayrollRunType.FinalSettlement || run.Type == PayrollRunType.BulkFinalSettlement)
        {
            var rows = await payrunEmpRepo.GetByRunIdAsync(req.RunId, ct);
            foreach (var pe in rows.Where(r => r.EmployeeExitId != null))
            {
                var emp = await employeeRepo.GetByIdAsync(pe.EmployeeId, ct);
                if (emp != null && emp.Status == EmployeeStatus.Active && emp.DateOfLeaving != null)
                    emp.MarkExited(emp.DateOfLeaving.Value, req.ActorId);
            }
        }

        await uow.SaveChangesAsync(ct);

        if (req.NotifyEmployees)
            jobDispatcher.EnqueueGeneratePayslipsThenNotify(req.RunId, run.TenantId);
        else
            jobDispatcher.EnqueueGeneratePayslips(req.RunId, run.TenantId);
    }
}
