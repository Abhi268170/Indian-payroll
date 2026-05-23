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
    decimal NetPayWithReimbursement);
