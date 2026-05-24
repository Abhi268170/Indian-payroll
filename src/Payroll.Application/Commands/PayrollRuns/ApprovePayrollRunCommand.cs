using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Application.Queries.PayrollRuns;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record ApprovePayrollRunCommand(Guid RunId, Guid ActorId) : IRequest;

public sealed class ApprovePayrollRunHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrollRunAuditLogRepository auditLogRepo,
    IPayrollRecomputeService recomputeService,
    IPayrollCostCalculator costCalculator,
    IUnitOfWork uow,
    ISender sender,
    IPayrollJobDispatcher jobDispatcher)
    : IRequestHandler<ApprovePayrollRunCommand>
{
    public async Task Handle(ApprovePayrollRunCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Only a Draft payroll run can be approved.");

        // Guard: no hard blocks
        var pending = await sender.Send(new GetPendingTasksQuery(req.RunId), ct);
        if (pending.HasAnyHardBlocks)
            throw new PayrollRunHasBlockingTasksException(pending.HardBlocks.Count);

        var payrunEmployees = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);
        var activeEmployees = payrunEmployees.Where(pe => pe.Status == PayrunEmployeeStatus.Active).ToList();

        // Rebuild TDS worksheets through the shared recompute service so the
        // approved snapshot uses canonical engine output + the operator's TDS
        // override (if any). Service deletes-and-adds the worksheet per employee
        // so the prior draft state is replaced cleanly.
        foreach (var pe in activeEmployees)
        {
            await recomputeService.RecomputeEmployeeAsync(req.RunId, pe.EmployeeId, ct);
        }

        var snapshot = costCalculator.Calculate(activeEmployees);
        run.UpdateFinancialSummary(
            payrollCost: snapshot.PayrollCost,
            totalNetPay: snapshot.TotalNet,
            totalEmployerPf: snapshot.TotalEmployerPf,
            totalEmployerEsi: snapshot.TotalEmployerEsi,
            totalTds: snapshot.TotalTds,
            totalPt: snapshot.TotalPt,
            employeeCount: snapshot.EmployeeCount,
            actorId: req.ActorId);

        run.Approve(req.ActorId);
        runRepo.Update(run);

        var auditEntry = PayrollRunAuditLog.Create(
            req.RunId, run.TenantId, PayrollRunStatus.Draft, PayrollRunStatus.Approved, req.ActorId, null);
        await auditLogRepo.AddAsync(auditEntry, ct);

        await uow.SaveChangesAsync(ct);

        jobDispatcher.EnqueueGeneratePayslips(req.RunId, run.TenantId);
    }
}
