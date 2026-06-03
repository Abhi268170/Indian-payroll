using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine.Outputs;

namespace Payroll.Application.Commands.SalaryRevisions;

// WI-018 step 5 — inject salary-revision arrears into a Draft payout run.
//
// For every Applied, not-yet-paid revision whose payout month == the run's period,
// computes arrears (ISalaryArrearService) and writes per-component ARREAR_* breakdown
// rows for the employee, then recomputes them so the payout-month gross/PT/TDS pick up
// the arrears. Arrears ride the unified one-time engine path (IsOneTimeEarning = true),
// so they are taxed once, not projected ×MonthsRemainingInFY.
//
// Idempotent: existing ARREAR_* rows for an employee are cleared before rebuild, so it
// is safe to run on every initiate/recompute of the Draft run. Revisions are marked paid
// only when the run is finalised (see approval flow), not here.
public record InjectSalaryRevisionArrearsCommand(Guid RunId, Guid ActorId) : IRequest;

public sealed class InjectSalaryRevisionArrearsHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmpRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    ISalaryRevisionRepository revisionRepo,
    ISalaryArrearService arrearService,
    IPayrollRecomputeService recomputeService,
    IPayrollCostCalculator costCalculator,
    IUnitOfWork uow)
    : IRequestHandler<InjectSalaryRevisionArrearsCommand>
{
    public async Task Handle(InjectSalaryRevisionArrearsCommand req, CancellationToken ct)
    {
        PayrollRun run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Type != PayrollRunType.Regular)
            throw new InvalidOperationException("Arrears are injected into Regular runs only.");
        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Arrears can only be injected into a Draft run.");

        IReadOnlyList<SalaryRevision> revisions = await revisionRepo.GetAppliedUnpaidForPayoutAsync(
            run.PayPeriod.Year, run.PayPeriod.Month, ct);
        if (revisions.Count == 0) return;

        IEnumerable<IGrouping<Guid, SalaryRevision>> byEmployee = revisions.GroupBy(r => r.EmployeeId);
        List<Guid> affected = new List<Guid>();

        foreach (IGrouping<Guid, SalaryRevision> group in byEmployee)
        {
            PayrunEmployee? payrunEmp = await payrunEmpRepo.GetByRunAndEmployeeAsync(run.Id, group.Key, ct);
            if (payrunEmp is null || payrunEmp.Status != PayrunEmployeeStatus.Active)
                continue; // employee not actively in this run — nothing to inject onto

            // Accumulate arrears across all of this employee's revisions for the period.
            Dictionary<string, ArrearLine> linesByCode = new Dictionary<string, ArrearLine>(StringComparer.OrdinalIgnoreCase);
            foreach (SalaryRevision rev in group)
            {
                SalaryArrearResult res = await arrearService.ComputeForRevisionAsync(rev.Id, ct);
                foreach (ArrearLine line in res.Lines)
                {
                    linesByCode[line.Code] = linesByCode.TryGetValue(line.Code, out ArrearLine? existing)
                        ? existing with { Amount = existing.Amount + line.Amount }
                        : line;
                }
            }

            // Idempotent rebuild: drop prior ARREAR_* rows for this employee in this run.
            IReadOnlyList<PayrunComponentBreakdown> existingRows =
                await breakdownRepo.GetByRunAndEmployeeAsync(run.Id, group.Key, ct);
            foreach (PayrunComponentBreakdown b in existingRows)
            {
                if (b.ComponentCode.StartsWith("ARREAR_", StringComparison.OrdinalIgnoreCase))
                    breakdownRepo.Remove(b);
            }

            List<PayrunComponentBreakdown> rows = BuildArrearRows(
                run.Id, group.Key, run.TenantId, linesByCode.Values);
            if (rows.Count > 0)
                await breakdownRepo.AddRangeAsync(rows, ct);

            affected.Add(group.Key);
        }

        if (affected.Count == 0) return;

        await uow.SaveChangesAsync(ct); // persist row changes before recompute reads them

        foreach (Guid employeeId in affected)
        {
            PayrunEmployee payrunEmp = await payrunEmpRepo.GetByRunAndEmployeeAsync(run.Id, employeeId, ct)
                ?? throw new NotFoundException($"Employee {employeeId} not in run {run.Id}.");
            RecomputeResult recompute = await recomputeService.RecomputeEmployeeAsync(run.Id, employeeId, ct);
            PayrollResult result = recompute.Engine;

            payrunEmp.UpdateComputedAmounts(
                grossPay: result.Gross.GrossWage,
                taxableGrossPay: result.Gross.TaxableGrossWage,
                netPay: recompute.NetPayWithAdjustments,
                taxesAmount: result.TDS.MonthlyTDS + result.PT.Amount,
                benefitsAmount: result.PF.EPFEmployerContribution + result.ESI.EmployerContribution,
                reimbursementsAmount: recompute.ReimbursementsAmount,
                employeePf: result.PF.EmployeeContribution,
                employerPf: result.PF.EPFEmployerContribution,
                employeeEsi: result.ESI.EmployeeContribution,
                employerEsi: result.ESI.EmployerContribution,
                ptAmount: result.PT.Amount,
                tdsAmount: payrunEmp.TdsOverrideAmount ?? result.TDS.MonthlyTDS,
                lwfEmployeeAmount: result.LWF.EmployeeAmount,
                lwfEmployerAmount: result.LWF.EmployerAmount,
                gratuityAmount: result.Gratuity.MonthlyAccrual,
                epsAmount: result.PF.EPSEmployerContribution,
                monthlyCTC: payrunEmp.MonthlyCTC,
                actorId: req.ActorId);
            payrunEmpRepo.Update(payrunEmp);
        }

        IReadOnlyList<PayrunEmployee> allEmployees = await payrunEmpRepo.GetByRunIdAsync(run.Id, ct);
        List<PayrunEmployee> activeEmployees =
            allEmployees.Where(e => e.Status == PayrunEmployeeStatus.Active).ToList();
        PayrollCostSnapshot snapshot = costCalculator.Calculate(activeEmployees);
        run.UpdateFinancialSummary(
            payrollCost: snapshot.PayrollCost,
            totalNetPay: snapshot.TotalNet,
            totalEmployerPf: snapshot.TotalEmployerPf,
            totalEmployerEsi: snapshot.TotalEmployerEsi,
            totalTds: snapshot.TotalTds,
            totalPt: snapshot.TotalPt,
            employeeCount: snapshot.EmployeeCount,
            actorId: req.ActorId);
        runRepo.Update(run);

        await uow.SaveChangesAsync(ct);
    }

    // ARREAR_<CODE> rows ride the one-time path (taxed once). Real SalaryComponentId is
    // required so the row is not misrouted to the reimbursement bucket (null-id heuristic).
    internal static List<PayrunComponentBreakdown> BuildArrearRows(
        Guid runId, Guid employeeId, Guid tenantId, IEnumerable<ArrearLine> lines)
    {
        List<PayrunComponentBreakdown> rows = new List<PayrunComponentBreakdown>();
        foreach (ArrearLine line in lines)
        {
            if (line.Amount == 0m) continue;
            if (line.ComponentId == Guid.Empty)
                throw new DomainException(
                    $"Arrears for a fully-removed component ('{line.Code}') are not supported in v1. " +
                    "// DEFERRED: arrears-removed-component");

            rows.Add(PayrunComponentBreakdown.Create(
                payrollRunId: runId,
                employeeId: employeeId,
                tenantId: tenantId,
                salaryComponentId: line.ComponentId,
                componentCode: $"ARREAR_{line.Code}",
                componentName: $"Arrears - {line.Name}",
                fullAmount: line.Amount,
                proratedAmount: line.Amount,
                isOneTimeEarning: true,
                isTaxable: line.IsTaxable,
                considerForEpf: false,
                considerForEsi: false,
                calculateOnProRata: false,
                epfInclusionRule: EpfInclusionRule.Always,
                showInPayslip: true));
        }
        return rows;
    }
}
