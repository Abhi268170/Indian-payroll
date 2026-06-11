using Payroll.Engine.Outputs;

namespace Payroll.Application.Services;

// Single entry point for re-running the payroll engine for one employee inside
// a Draft run. Used by SetLop, AddOneTimeEarning/Deduction, Remove, and the
// bulk import handlers so statutory totals stay in sync no matter how the run
// is mutated.
//
// The service:
//   1. Loads the run, employee, breakdowns, statutory config, and YTDs.
//   2. Maps breakdowns to engine SalaryComponentInput using flags frozen on
//      each breakdown row (deterministic across config edits).
//   3. Filters reimbursement rows out of engine input — they are paid in net
//      but excluded from gross / statutory, per Zoho parity.
//   4. Calls the engine.
//   5. Refreshes the TdsWorksheet with correct YTD (fixes the old YTD=0 bug
//      that SetLop used to write).
//   6. Updates the breakdown rows' prorated amounts to match engine output.
//
// Caller is responsible for:
//   - Persisting PayrunEmployee via UpdateComputedAmounts(..., result, ...,
//     reimbursementsAmount).
//   - Recomputing run totals via IPayrollCostCalculator.
//   - Committing the UnitOfWork.
public interface IPayrollRecomputeService
{
    Task<RecomputeResult> RecomputeEmployeeAsync(
        Guid runId,
        Guid employeeId,
        CancellationToken ct = default);
}

public sealed record RecomputeResult(
    PayrollResult Engine,
    decimal ReimbursementsAmount,
    decimal DeductionsAmount,
    decimal NetPayWithAdjustments);

// Applies a recompute result to the stored PayrunEmployee amounts — the same
// mapping every mutation handler uses, so what gets PAID always matches the
// worksheet/breakdowns the recompute just rewrote.
public static class RecomputeResultApplier
{
    public static void Apply(Domain.Entities.PayrunEmployee payrunEmp, RecomputeResult recompute, Guid actorId)
    {
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
            actorId: actorId,
            vpfAmount: result.PF.VPFContribution,
            edliAmount: result.PF.EdliCharge,
            adminChargesAmount: result.PF.AdminCharge);
    }
}
