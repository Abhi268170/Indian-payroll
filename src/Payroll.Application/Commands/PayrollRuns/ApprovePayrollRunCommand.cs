using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Application.Queries.PayrollRuns;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine.Outputs;

namespace Payroll.Application.Commands.PayrollRuns;

public record ApprovePayrollRunCommand(Guid RunId, Guid ActorId) : IRequest;

public sealed class ApprovePayrollRunHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrollRunAuditLogRepository auditLogRepo,
    IPayrollRecomputeService recomputeService,
    IPayrollFnfOrchestrator fnfOrchestrator,
    ITdsWorksheetRepository tdsWorksheetRepo,
    IEmployeeExitRepository exitRepo,
    IPayrollCostCalculator costCalculator,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IFileStorageService fileStorage,
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

        bool isFnf = run.Type == PayrollRunType.FinalSettlement
                  || run.Type == PayrollRunType.BulkFinalSettlement;

        // Lock in canonical engine output + operator TDS override.
        // FnF runs use the FnF orchestrator (MonthsRemainingInFY=1, gratuity
        // as flat component, LWF half-year dedup). Regular runs use the
        // shared recompute service which also upserts TDS worksheets.
        foreach (var pe in activeEmployees)
        {
            if (isFnf)
            {
                FnfEngineResult fnf = await fnfOrchestrator.ComputeAsync(req.RunId, pe.EmployeeId, ct);
                PayrollFnfOrchestrator.ApplyToPayrunEmployee(pe, fnf, req.ActorId);
                payrunEmployeeRepo.Update(pe);

                await tdsWorksheetRepo.DeleteByRunAndEmployeeAsync(req.RunId, pe.EmployeeId, ct);
                await tdsWorksheetRepo.AddAsync(
                    PayrollFnfOrchestrator.BuildWorksheet(run, pe, fnf, req.ActorId), ct);

                // WI-09: mark exit Completed so re-hired employees can exit again.
                EmployeeExit? exit = await exitRepo.GetActiveByEmployeeAsync(pe.EmployeeId, ct);
                if (exit != null)
                {
                    exit.MarkCompleted(req.ActorId);
                    exitRepo.Update(exit);
                }
            }
            else
            {
                // Apply the result — recompute rewrites worksheets/breakdowns with
                // live YTD; discarding it let paid amounts diverge from the audit
                // worksheet whenever YTD shifted after the last draft edit.
                Services.RecomputeResult recompute =
                    await recomputeService.RecomputeEmployeeAsync(req.RunId, pe.EmployeeId, ct);
                Services.RecomputeResultApplier.Apply(pe, recompute, req.ActorId);
                payrunEmployeeRepo.Update(pe);
            }
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

        // Audit invariant: variable inputs (LOP, overrides, one-time entries,
        // skip decisions) are frozen as an immutable artifact at approval.
        string artifactKey = await WriteVariableInputsArtifactAsync(run, payrunEmployees, ct);
        run.SetVariableInputsFileKey(artifactKey, req.ActorId);

        run.Approve(req.ActorId);
        runRepo.Update(run);

        var auditEntry = PayrollRunAuditLog.Create(
            req.RunId, run.TenantId, PayrollRunStatus.Draft, PayrollRunStatus.Approved, req.ActorId, null);
        await auditLogRepo.AddAsync(auditEntry, ct);

        await uow.SaveChangesAsync(ct);

        // WI-18: FnF settlements are emailed automatically (often the work email is
        // already deactivated, so timely delivery to personal email matters). Regular
        // runs keep generate-only — payslip emailing there stays a separate action.
        if (isFnf)
            jobDispatcher.EnqueueGeneratePayslipsThenNotify(req.RunId, run.TenantId);
        else
            jobDispatcher.EnqueueGeneratePayslips(req.RunId, run.TenantId);
    }

    private async Task<string> WriteVariableInputsArtifactAsync(
        PayrollRun run, IReadOnlyList<PayrunEmployee> payrunEmployees, CancellationToken ct)
    {
        var breakdowns = await breakdownRepo.GetByRunIdAsync(run.Id, ct);
        var oneTimeRows = breakdowns
            .Where(b => b.IsOneTimeEarning)
            .Select(b => new
            {
                b.EmployeeId,
                b.ComponentCode,
                b.ComponentName,
                Amount = b.FullAmount,
            })
            .ToList();

        var payload = new
        {
            RunId = run.Id,
            run.PayPeriod.Year,
            run.PayPeriod.Month,
            RunType = run.Type.ToString(),
            Employees = payrunEmployees.Select(pe => new
            {
                pe.EmployeeId,
                Status = pe.Status.ToString(),
                pe.SkipReason,
                pe.LopDays,
                pe.VpfPercent,
                pe.TdsOverrideAmount,
                pe.TdsOverrideReason,
            }).ToList(),
            OneTimeEntries = oneTimeRows,
        };

        string key = $"payroll-runs/{run.TenantId}/{run.Id}/variable-inputs.json";
        byte[] bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload);
        using MemoryStream stream = new(bytes);
        await fileStorage.UploadAsync(key, stream, "application/json", ct);
        return key;
    }
}
