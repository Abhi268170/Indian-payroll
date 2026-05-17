using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record RejectApprovalCommand(Guid RunId, string Reason, Guid ActorId) : IRequest;

public sealed class RejectApprovalCommandValidator : AbstractValidator<RejectApprovalCommand>
{
    public RejectApprovalCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Rejection reason is required.");
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class RejectApprovalHandler(
    IPayrollRunRepository runRepo,
    IPayrollRunAuditLogRepository auditLogRepo,
    IUnitOfWork uow)
    : IRequestHandler<RejectApprovalCommand>
{
    public async Task Handle(RejectApprovalCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Approved)
            throw new InvalidOperationException("Only an Approved payroll run can have its approval rejected.");

        run.RejectApproval(req.Reason, req.ActorId);
        runRepo.Update(run);

        var auditEntry = PayrollRunAuditLog.Create(
            req.RunId, run.TenantId, PayrollRunStatus.Approved, PayrollRunStatus.Draft, req.ActorId, req.Reason);
        await auditLogRepo.AddAsync(auditEntry, ct);

        await uow.SaveChangesAsync(ct);
    }
}
