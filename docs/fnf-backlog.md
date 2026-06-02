# FnF Full & Final Settlement — Complete Ordered Backlog

Ordered by: dependency → compliance severity → operational impact.
Every item includes a smoke test or verification step.

---

## PHASE 1 — Foundation (nothing else works without these)

---

### WI-01 [BUG · CRITICAL] Seed recurring salary components into FnF PayrunEmployee on exit initiation

**What's wrong:**
`InitiateExitCommand` creates a `PayrunEmployee` with no `PayrunComponentBreakdown` rows and `MonthlyCTC = 0`.
`UpdateFnfRunCommand.cs:94` contains a comment "Phase 4 initiation will populate those" — Phase 4 was never built.
The FnF orchestrator reads breakdowns to build engine inputs. Empty breakdowns → engine computes ₹0 gross, ₹0 EPF, ₹0 ESI, ₹0 PT, ₹0 TDS, ₹0 net pay for every FnF run.

**Files:** `InitiateExitCommand.cs:118-148`, `UpdateFnfRunCommand.cs:94`, `PayrollFnfOrchestrator.cs:54`

**Fix:** In `InitiateExitCommand.Handle()`, after creating `PayrunEmployee`, load the employee's active `EmployeeSalaryStructure` + `SalaryStructureTemplate`, resolve component amounts using the same logic in `InitiatePayrollRunCommand` (methods `ComputeComponents` / `ResolveAmounts`), and create `PayrunComponentBreakdown` rows. Also set `MonthlyCTC = salaryStructure.AnnualCTC / 12` on the `PayrunEmployee`.

**Smoke test:**
1. Initiate exit for any active employee with a salary structure assigned.
2. Open the FnF run → employee breakdown.
3. Verify: "Monthly CTC" shows correct value, Earnings table lists BASICSALARY/HRA/allowances with non-zero amounts, Gross Pay > ₹0.
4. Backend: query `PayrunComponentBreakdown` for the FnF `payroll_run_id` + `employee_id` — rows must exist for each salary component.

---

### WI-02 [BUG · CRITICAL] Validate active salary structure exists before allowing exit initiation

**What's wrong:**
`InitiateExitCommandValidator` does not check whether the employee has an active `EmployeeSalaryStructure`. An employee with no salary structure silently creates a ₹0 FnF run (compounded by WI-01).

**File:** `InitiateExitCommand.cs:27-48`

**Fix:** In the handler (not validator — requires DB access), load `salaryStructureRepo.GetActiveByEmployeeAsync(req.EmployeeId)`. If null, throw `DomainException("Employee has no active salary structure. Assign one before initiating exit.")`.

**Smoke test:**
1. Remove the salary structure from an employee.
2. Attempt to initiate exit → must receive HTTP 422 with the above message.
3. Re-assign salary structure → exit initiation must succeed.

---

## PHASE 2 — Approval correctness (FnF approval is broken independently of Phase 1)

---

### WI-03 [BUG · CRITICAL] Approval uses regular recompute service — FnF-specific engine parameters lost

**What's wrong:**
`ApprovePayrollRunCommand:47` calls `recomputeService.RecomputeEmployeeAsync()` for all run types including `FinalSettlement` and `BulkFinalSettlement`. The regular recompute service does not apply:
- `MonthsRemainingInFY = 1` (FnF closes TDS for the year)
- `GratuityEnabled = false` (gratuity injected as flat breakdown)
- LWF half-year dedup
- Proration to LastWorkingDay

The approved snapshot will differ from what HR reviewed in draft.

**File:** `ApprovePayrollRunCommand.cs:14-20, 47`

**Fix:** In `ApprovePayrollRunHandler`, inject `IPayrollFnfOrchestrator` alongside `IPayrollRecomputeService`. Branch on `run.Type`:
```csharp
if (run.Type == PayrollRunType.FinalSettlement || run.Type == PayrollRunType.BulkFinalSettlement)
    await fnfOrchestrator.ComputeAsync(req.RunId, pe.EmployeeId, ct);
else
    await recomputeService.RecomputeEmployeeAsync(req.RunId, pe.EmployeeId, ct);
```
Then persist the updated `PayrunEmployee` amounts from orchestrator result.

**Smoke test:**
1. Add FnF components for an employee (e.g., ₹50,000 leave encashment).
2. Note draft Net Pay, TDS, Gross.
3. Approve the run.
4. Fetch the generated payslip → figures must match draft exactly.
5. Check `TdsWorksheet` rows for the FnF employee exist post-approval (covered further in WI-04).

---

### WI-04 [BUG · CRITICAL] TdsWorksheet not created for FnF runs — Form 16 data absent

**What's wrong:**
`UpdateFnfRunCommand` has no `ITdsWorksheetRepository` dependency and creates no `TdsWorksheet` rows. The FnF recompute triggered at approval (after WI-03 fix) must also persist the TDS worksheet.

**File:** `UpdateFnfRunCommand.cs` (no `ITdsWorksheetRepository`), `ApprovePayrollRunCommand.cs`

**Fix:** After the FnF orchestrator computes in both `UpdateFnfRunCommand` and `ApprovePayrollRunCommand`, write a `TdsWorksheet` row (same structure as regular run) using `result.TDS` and annualised figures. Delete any prior worksheet for the same run+employee before writing.

**Smoke test:**
1. Complete FnF for an employee and approve run.
2. Query `tds_worksheets` table for `payroll_run_id` = FnF run ID → at least one row must exist with non-null `annual_taxable_income`, `monthly_tds`.
3. Verify figures match approved payslip TDS amount.

---

## PHASE 3 — Compliance correctness

---

### WI-05 [BUG · HIGH] FnF YTD excludes EmployeeFyOpening — TDS closure understated for pre-system joiners

**What's wrong:**
`PayrollFnfOrchestrator.LoadCurrentYtdAsync` only queries `payrunEmpRepo.GetCurrentEmployerYtdAsync`.
Regular run (`InitiatePayrollRunCommand:150-159`) merges `EmployeeFyOpening` records into YTD before engine input.
For employees whose early FY months were entered as opening balances, FnF TDS will annualise on incomplete YTD → under-deduction.

**File:** `PayrollFnfOrchestrator.cs:88, 151-158`

**Fix:** Inject `IEmployeeFyOpeningRepository` into `PayrollFnfOrchestrator`. After `LoadCurrentYtdAsync`, load opening for the employee's fiscal year and add `opening.TaxableGross` to `ytdTaxableGross`, `opening.TdsDeducted` to `ytdTds`.

**Smoke test:**
1. Create an employee with an FY opening balance (e.g., ₹3,00,000 taxable, ₹9,000 TDS).
2. Run one regular payroll month for them.
3. Initiate exit → compute FnF.
4. Verify TDS worksheet shows `annual_taxable_income` = opening taxable + run YTD taxable + FnF month taxable.

---

### WI-06 [BUG · HIGH] Leave encashment fully taxable — Section 10(10AA) ₹25L exemption missing

**What's wrong:**
`UpdateFnfRunCommand.cs:103` always adds leave encashment as `isTaxable: true`. Under Section 10(10AA), private-sector employees are exempt up to ₹25,00,000 lifetime. Entire amount is taxed — over-deduction on every FnF with leave encashment.

**File:** `UpdateFnfRunCommand.cs:103`

**Fix:** Apply same pattern as gratuity — split into exempt (up to limit from DB config) and taxable portions, create `FNF_LEAVE_ENCASHMENT_EXEMPT` (isTaxable: false) and `FNF_LEAVE_ENCASHMENT_TAXABLE` (isTaxable: true) breakdown rows. Exemption limit must come from DB config (do NOT hardcode ₹25L — see WI-19).

**Smoke test:**
1. Enter ₹10,00,000 leave encashment for an exiting employee (below ₹25L limit).
2. Verify TDS computation treats full ₹10L as non-taxable.
3. Enter ₹30,00,000 leave encashment (above limit).
4. Verify only ₹5L (excess over ₹25L) is taxable.
5. Check breakdown rows: `FNF_LEAVE_ENCASHMENT_EXEMPT` + `FNF_LEAVE_ENCASHMENT_TAXABLE` both exist.

---

### WI-07 [BUG · HIGH] BulkFnF proration uses pay-date month, not LWD month — cross-month exits get wrong salary

**What's wrong:**
Bulk FnF `PayPeriod` is set to the pay date's month. Orchestrator derives `periodStart` from `run.PayPeriod`.
When LWD is in a different month than pay date (e.g., LWD = March 20, pay date = May 5), `exit.LastWorkingDay < periodStart` → `workedDays = DaysInMonth(May) = 31`. Employee receives full May salary instead of 20/31 of March salary.

**File:** `PayrollFnfOrchestrator.cs:72-75`, `InitiateExitCommand.cs:160`

**Fix:** The proration period must always be the LWD month, not the run PayPeriod month. Two options:
- Store the LWD-month `PayPeriod` per `PayrunEmployee` (clean, requires schema change).
- Derive from `exit.LastWorkingDay` directly: `periodStart = new DateOnly(exit.LastWorkingDay.Year, exit.LastWorkingDay.Month, 1)`.
Also, `salaryDivisor` must use LWD month's calendar days, not pay-date month.

**Smoke test:**
1. Exit employee with LWD = last day of previous month, choose bulk FnF (next regular pay date = current month).
2. Verify prorated salary = full month (since LWD was last day of month).
3. Exit employee with LWD = 15th of previous month, bulk FnF in current month.
4. Verify prorated salary ≈ 15/prev_month_days × monthly gross.

---

### WI-08 [BUG · HIGH] Same-month joiner-and-exit gets full-month salary — joining date not considered for proration

**What's wrong:**
Orchestrator `workedDays` counts from `periodStart` (1st of month) to LWD. But if the employee joined mid-month in the same month they exit, the engine's proration denominator is the full month and no LOP correction accounts for the joining date. Employee gets paid for days before they joined.

**File:** `PayrollFnfOrchestrator.cs:72-75`

**Fix:** `workedDays` = `exit.LastWorkingDay.DayNumber - max(periodStart, employee.DateOfJoining).DayNumber + 1`.

**Smoke test:**
1. Create employee with `DateOfJoining = June 10`.
2. Initiate exit with LWD = June 20 (same month).
3. FnF `workedDays` must be 11 (June 10–20 inclusive).
4. Gross pay must ≈ 11/30 × monthly gross (not 20/30).

---

## PHASE 4 — Data integrity and edge cases

---

### WI-09 [BUG · HIGH] EmployeeExitRepository.GetActiveByEmployeeAsync has no status filter — blocks rehire exit

**What's wrong:**
`db.EmployeeExits.FirstOrDefaultAsync(e => e.EmployeeId == employeeId)` returns any historical exit record.
`EmployeeExit` has no `Status` or `IsCompleted` field. A previously terminated and re-hired employee can never be exited again — handler throws "An exit is already in progress."

**File:** `EmployeeExitRepository.cs:12-13`, `EmployeeExit.cs` (no status field)

**Fix:**
1. Add `ExitStatus` enum (`InProgress`, `Completed`, `Reverted`) and `Status` property to `EmployeeExit`.
2. Set `Status = Completed` when FnF run is approved/paid.
3. Set `Status = Reverted` on cancel exit (WI-11).
4. `GetActiveByEmployeeAsync` filters by `Status == InProgress`.
5. Write migration for the new column.

**Smoke test:**
1. Exit an employee → approve FnF → verify `ExitStatus = Completed` in DB.
2. Rehire the same employee (set `Status = Active`, clear `DateOfLeaving`).
3. Initiate exit again → must succeed without error.

---

### WI-10 [BUG · HIGH] Approve with ₹0 recurring components silently produces invalid payslip

**What's wrong:**
`GetPendingTasksQuery` only hard-blocks on onboarding-incomplete skipped employees and soft-warns on missing PAN. No check that active FnF employees have non-zero recurring breakdowns (i.e., salary was seeded). A ₹0 FnF can be approved, generating a legally invalid settlement document.

**File:** `GetPendingTasksQuery.cs`, `ApprovePayrollRunCommand.cs:35`

**Fix:** In `GetPendingTasksHandler`, for FnF runs, check each active `PayrunEmployee`: if `GrossPay == 0` and no recurring component breakdowns exist → hard block with message "Salary components not configured for [name]. Save FnF details before approving."

**Smoke test:**
1. Initiate exit (salary seeded after WI-01, but before that: use an employee with no structure).
2. Attempt to approve FnF run → must receive hard block error.
3. After salary seeding + FnF save: approval must succeed.

---

### WI-11 [BUG · MEDIUM] BulkFnF SetEmployeeCount increment never calls runRepo.Update — count lost

**What's wrong:**
When appending an employee to an existing bulk FnF run, `fnfRun.SetEmployeeCount(+1)` mutates the entity but `runRepo.Update(fnfRun)` is never called. EF change tracking may or may not detect this mutation depending on tracking state. In repository pattern (explicit Update), the increment is not guaranteed to persist.

**File:** `InitiateExitCommand.cs:143`

**Fix:** Add `runRepo.Update(fnfRun)` after `fnfRun.SetEmployeeCount(...)` for the bulk path. Also add a similar call when a single FnF run is created.

**Smoke test:**
1. Exit two employees into the same bulk FnF run (same pay date, regular schedule mode).
2. After second exit initiation: query `payroll_runs.employee_count` → must be 2.
3. UI FnF run header must show "Employees: 2".

---

### WI-12 [BUG · MEDIUM] MarkExitedOnLwdJob AutomaticRetry=0 — transient failure leaves employee as Active

**What's wrong:**
`[AutomaticRetry(Attempts = 0)]` means any exception permanently fails the job. Employee status badge remains `Active` in the UI even after LWD passes, until the next day's successful run.

**File:** `MarkExitedOnLwdJob.cs:14`

**Fix:** Change to `[AutomaticRetry(Attempts = 3)]` with default exponential backoff. This is a daily idempotent sweep — retries are safe.

**STATUS: DONE.** Attribute changed 0 → 3. Guarded by a reflection test
(`MarkExitedOnLwdJobAttributeTests`) asserting `Attempts == 3`.

**Verification note:** the retry-on-transient-failure behavior itself is NOT
smoke-observable — there is no clean way to inject a transient DB failure mid-run
to watch Hangfire retry. The flip-to-Exited job logic is pre-existing and was not
modified by this change (attribute-only edit). The Hangfire dashboard trigger is
gated behind the SuperAdmin policy via cookie auth, so a bearer-token curl trigger
returns 403 — manual dashboard trigger remains the operator path.

---

### WI-13 [BUG · MEDIUM] GratuityAmount on PayrunEmployee always 0 for FnF — ~~cost reports undercount gratuity disbursed~~

**STATUS: WONTFIX (misdiagnosis — the proposed fix would regress three consumers).**

**Original claim:** `GratuityAmount` is 0 for FnF rows so cost reports undercount the gratuity disbursed.

**Why it's wrong:** FnF gratuity is written as `FNF_GRATUITY_EXEMPT` / `FNF_GRATUITY_TAXABLE`
breakdown rows with `isOneTimeEarning:true`. The orchestrator maps these into engine
components and `GrossCalculator` sums every component into `grossWage` (one-time ⇒ not
prorated ⇒ full amount). **Gratuity is therefore already in `GrossPay`** — and `NetPay`,
since the employee receives it.

`payrollCost = totalGross + employer extras`, and gratuity is in `totalGross`, so it is
already counted exactly once. Setting `GratuityAmount = req.Gratuity` would double-count it in:
- `PayrollCostCalculator.cs:40` — payroll cost inflated by the gratuity amount.
- `PayslipPdfGenerator.cs:248` — `employerPfInCtc = MonthlyCTC − GrossPay − GratuityAmount`
  subtracts gratuity that is already inside GrossPay, corrupting the CTC breakdown.
- `PayrollDetailsExportService.cs:164` — employer cost inflated.

**Semantic note:** for *regular* runs `GratuityAmount` is the engine *accrual*
(`GratuityEnabled=true`) — a future-liability provision NOT in gross, correctly added to
cost. An exiting employee has no future accrual; their gratuity is disbursed *through* gross.
So `GratuityAmount = 0` for FnF is correct, not a gap.

**Verified empirically:** on a clean FnF run, `UpdateFnfRun` with `gratuity=500000` raised
`gross_pay` and `payroll_cost` each by ₹5,00,000 while `gratuity_amount` stayed 0 — confirming
gratuity is already fully counted via gross.

**Action:** none. Do not touch `PayrollCostCalculator` / `PayslipPdfGenerator` /
`PayrollDetailsExportService`. Current behavior is correct.

---

## PHASE 5 — Missing flows

---

### WI-14 [GAP · HIGH] Cancel/revert exit — Application command, API endpoint, and UI all missing

**What's missing:**
`Employee.RevertExit()` exists in domain (resets Status=Active, clears DateOfLeaving). No `CancelExitCommand`, no API endpoint (`DELETE /api/v1/employees/{id}/exit`), no UI action.

**Fix:**
1. Add `CancelExitCommand(EmployeeId, ActorId)` handler:
   - Throw if no `InProgress` exit exists (after WI-09).
   - Call `employee.RevertExit(actorId)`.
   - Delete or soft-delete the `EmployeeExit` row (set `Status = Reverted`).
   - Remove the `PayrunEmployee` from the FnF run.
   - If bulk FnF run: decrement `EmployeeCount`; if count drops to 0 delete the run.
   - If single FnF run: delete the run.
   - Re-add the employee to any open draft regular run covering the original LWD month.
2. Add `DELETE /api/v1/employees/{id}/exit` controller endpoint.
3. Add "Cancel Exit" button in the UI on the employee detail page (visible when `DateOfLeaving` is set and FnF not yet approved).

**Smoke test:**
1. Initiate exit for an employee.
2. Verify employee appears in FnF run.
3. Cancel exit via API `DELETE /api/v1/employees/{id}/exit`.
4. Verify: employee `Status = Active`, `DateOfLeaving = null`, FnF `PayrunEmployee` row deleted.
5. If bulk run had only this employee: verify bulk FnF run deleted.
6. Verify employee re-appears in any open draft regular run for that period.

---

### WI-15 [GAP · HIGH] Gratuity 5-year tenure eligibility check missing

**What's missing:**
`UpdateFnfRunCommand` accepts any non-zero `Gratuity` without checking if the employee has ≥ 4 years 240 days (effectively 5 years) of continuous service as required by Section 4, Payment of Gratuity Act 1972.

**Fix:**
1. In `UpdateFnfRunHandler`, load `employee.DateOfJoining` and compute tenure as of `exit.LastWorkingDay`.
2. If `req.Gratuity > 0` and tenure < 4 years 240 days: throw `DomainException("Employee has not completed 5 years of service (4y 240d). Gratuity is not payable.")`.
3. Optionally: soft warning if employer wants to pay ex-gratia (with explicit override flag in the command).

**Smoke test:**
1. Exit employee with < 5 years tenure. Enter ₹1,00,000 gratuity → must receive error.
2. Exit employee with ≥ 5 years tenure. Enter ₹1,00,000 gratuity → must save successfully.
3. Verify correct tenure boundary: employee with exactly 4y 240d → eligible.

---

### WI-16 [GAP · HIGH] Onboarding completeness check not enforced at FnF settlement approval

**What's missing:**
`InitiatePayrollRunCommand` skips employees with missing `DateOfBirth`, `FathersName`, or `EncryptedBankAccount`. No equivalent guard in FnF path — an employee with no bank account can reach an approved FnF run, causing payment failure after the fact.

**Fix:** In `GetPendingTasksHandler`, for FnF runs add hard block if active `PayrunEmployee`'s employee is missing `EncryptedBankAccount` or `DateOfBirth`. Message: "Bank account not configured for [name]. Payment will fail without it."

**Smoke test:**
1. Exit an employee who has no bank account on file.
2. Attempt to approve FnF → must receive hard block.
3. Add bank account → approval must succeed.

---

### WI-17 [GAP · HIGH] Benefit-category breakdown rows not written for FnF — payslip employer-benefits section empty

**What's missing:**
`InitiatePayrollRunCommand:364-397` creates `IsBenefit=true` breakdown rows (health insurance, NPS employer match) so payslips render the employer-benefits section. `UpdateFnfRunCommand` does not create these rows.

**Fix:** As part of WI-01 salary seeding, include benefit-category components from the salary structure template using the same `IsBenefit` flag logic as regular run initiation.

**Smoke test:**
1. Ensure employee salary structure includes a benefit component (e.g., health insurance).
2. After WI-01: verify `PayrunComponentBreakdown` for FnF includes a row with `is_benefit = true` for that component.
3. Generated FnF payslip must show employer-benefits section with correct amounts.

---

### WI-18 [GAP · HIGH] SendPayslipNotificationJob is a stub — FnF payslip email never sent

**What's missing:**
`SendPayslipNotificationJob.Execute()` returns `Task.CompletedTask` immediately. `SendPayslipEmailCommand` handler is fully implemented and correctly prefers `PersonalEmail` for FnF employees. The job just never calls it.

**File:** `SendPayslipNotificationJob.cs:6`

**Fix:** Implement `SendPayslipNotificationJob.Execute(Guid payrollRunId)`:
1. Load all published payslips for the run.
2. For each, dispatch `SendPayslipEmailCommand(payrollRunId, employeeId)` via MediatR.
3. For FnF runs: use custom subject "Full & Final Settlement — [Company Name]" (see WI-23).

**Smoke test:**
1. Approve FnF run for employee with a personal email set.
2. Job fires (trigger manually in Hangfire dashboard).
3. MailHog UI (`localhost:8025`) must show an email to the personal email address with the FnF payslip attached.
4. Verify email subject and PDF filename are FnF-specific (after WI-23).

---

## PHASE 6 — API completeness

---

### WI-19 [GAP · MEDIUM] Gratuity ₹20L exemption cap hardcoded — violates no-magic-numbers rule

**What's missing:**
`UpdateFnfRunCommand.cs:68`: `private const decimal GratuityExemptionLimit = 2_000_000m` is a hardcoded statutory value. Per CLAUDE.md: "all statutory limits must come from DB config tables."

**Fix:**
1. Add a `StatutoryLimits` or extend the existing statutory config table with `gratuity_exemption_limit` and `leave_encashment_exemption_limit` columns.
2. Load from DB in `UpdateFnfRunHandler` via `IStatutoryConfigRepository`.
3. Remove the `const`.

**Smoke test:**
1. Verify DB has the statutory limits row with ₹20,00,000 for gratuity, ₹25,00,000 for leave encashment.
2. Enter ₹25,00,000 gratuity → ₹20L exempt, ₹5L taxable.
3. Update DB value to ₹30,00,000 without redeployment.
4. Re-run: ₹25L fully exempt.

---

### WI-20 [GAP · MEDIUM] GetPayrollRunSummary doesn't surface FnF-specific metadata

**What's missing:**
`GET /api/v1/payroll-runs/{id}` returns generic fields only. No `LastWorkingDay`, `ExitReason`, `SettlementMode`, `SettlementDate` for FnF runs. Frontend must make a separate call or join client-side.

**Fix:** Extend `PayrollRunSummaryDto` with nullable FnF fields. Populate them in the query handler by joining `EmployeeExit` when `run.Type` is FinalSettlement or BulkFinalSettlement.

**Smoke test:**
1. `GET /api/v1/payroll-runs/{fnf-run-id}` → response must include `lastWorkingDay`, `exitReason`, `settlementMode` fields with correct values.
2. Same endpoint for a regular run → FnF fields must be null/absent.

---

### WI-21 [GAP · MEDIUM] No dedicated FnF settlement summary endpoint

**What's missing:**
No endpoint returns a categorized FnF settlement summary (prorated salary, gratuity, leave encashment, notice pay, FnF deductions, statutory deductions, net payable). Frontend must reconstruct from raw breakdown rows.

**Fix:** Add `GET /api/v1/payroll-runs/{id}/fnf-summary/{employeeId}` returning:
```json
{
  "lastWorkingDay": "2026-06-15",
  "workedDays": 15,
  "proratedSalary": 42000,
  "earnings": [...],
  "deductions": [...],
  "statutoryDeductions": { "epf": ..., "esi": ..., "pt": ..., "tds": ... },
  "netSettlement": 98500
}
```

**Smoke test:**
1. Complete FnF save with leave encashment + gratuity.
2. Call `GET /api/v1/payroll-runs/{id}/fnf-summary/{employeeId}`.
3. Verify all components appear in correct categories with correct taxability.
4. Net settlement must match `PayrunEmployee.NetPay + Reimbursements`.

---

### WI-22 [GAP · MEDIUM] No FnF preview/compute-before-save endpoint — what-if analysis impossible

**What's missing:**
The only way to see FnF computed amounts is to call `PUT /fnf-settlement` (which saves and recomputes). HR cannot model different gratuity/leave encashment amounts without creating intermediate draft states.

**Fix:** Add `POST /api/v1/payroll-runs/{id}/fnf-preview/{employeeId}` accepting the same body as `UpdateFnfRunCommand` but not persisting anything. Runs the orchestrator in memory and returns computed breakdown.

**Smoke test:**
1. Call preview endpoint with various gratuity amounts.
2. Verify returned TDS and net pay change correctly.
3. Verify DB has no new/modified breakdown rows after preview call.

---

## PHASE 7 — UI completeness

---

### WI-23 [GAP · MEDIUM] FnF payslip email subject/filename generic — indistinguishable from regular payslips

**What's missing:**
`SendPayslipEmailCommand` subject: `"Payslip for {MMMM yyyy}"`, filename: `"Payslip_{code}_{yyyy-MM}.pdf"` for all run types.

**File:** `SendPayslipEmailCommand.cs:37-48`

**Fix:** Pass `PayrollRunType` into the command. When `FinalSettlement` or `BulkFinalSettlement`:
- Subject: `"Full & Final Settlement — {MMMM yyyy}"`
- Filename: `"FnF_Settlement_{code}_{yyyy-MM}.pdf"`
- Email body: reference exit date and settlement amount.

**Smoke test:**
1. Approve FnF run, trigger notification.
2. MailHog: subject must contain "Full & Final Settlement", attachment filename must start with `FnF_Settlement_`.

---

### WI-24 [GAP · MEDIUM] FnF employee table missing Last Working Day and Exit Reason columns

**What's missing:**
Both single and bulk FnF employee tables show only name, gross, deductions, taxes, net pay. No LWD, no exit reason. Bulk FnF with multiple employees forces HR to open each row to verify the right people are included.

**File:** Frontend FnF settlement page employee table

**Fix:** Add `lastWorkingDay` and `exitReason` columns to the FnF employee table. Source from `EmployeeExitDto` joined by `employeeExitId` on each `PayrunEmployee`.

**Smoke test:**
1. Add two employees to a bulk FnF run with different LWDs and exit reasons.
2. FnF run page employee table must show both columns with correct values per row without expanding any row.

---

### WI-25 [GAP · MEDIUM] Itemized recurring salary not shown in FnF employee breakdown UI

**What's missing:**
After WI-01 seeds recurring components, the UI breakdown accordion shows them in the Earnings table. Verify this renders correctly — no separate story needed if WI-01 is done, but must be explicitly validated.

**Smoke test (after WI-01):**
1. Open FnF run → expand Nithin Nair row.
2. Earnings table must list: Basic Salary, HRA, each allowance with prorated amounts.
3. Gross Pay must equal sum of all earnings rows.
4. "Monthly CTC" footer must show correct CTC.

---

### WI-26 [GAP · MEDIUM] Settlement date not editable after exit initiation

**What's missing:**
FnF run shows `PayDay` as a read-only header. Settlement dates often change after notice is served. No edit path exists short of cancelling and re-initiating exit.

**Fix:** Add `PATCH /api/v1/payroll-runs/{id}/settlement-date` endpoint (for Draft FnF runs only). Add a small edit control next to the PayDay stat on the FnF run page.

**Smoke test:**
1. Initiate exit with settlement date = June 30.
2. Edit settlement date to July 15 via API/UI.
3. Verify `payroll_runs.pay_day` updated in DB.
4. Attempt to edit settlement date on an Approved run → must receive 422.

---

### WI-27 [GAP · MEDIUM] CustomDate FnF payslip shows wrong period — LWD month ≠ payment month

**What's missing:**
`CreateFinalSettlement` uses `PayPeriod(LWD.Year, LWD.Month)` even when the settlement date may be months later. Payslip period label says "April" for a June payment, confusing employees and auditors.

**File:** `InitiateExitCommand.cs:123`

**Fix:** Either:
- Add a `description` field to `PayrollRun` for FnF (e.g., "Final Settlement for the period ending 30 April 2026").
- Or use `SettlementDate.Month` for `PayPeriod` but store LWD separately as the proration reference (requires orchestrator to read `exit.LastWorkingDay` for proration, not `run.PayPeriod` — aligns with WI-07 fix).

**Smoke test:**
1. Initiate exit with LWD = April 30, custom settlement date = June 30.
2. Approve FnF → open payslip PDF.
3. Payslip must clearly indicate "Final Settlement" with either the correct period label or a descriptive header — must not say "Pay Period: April 2026" ambiguously when payment date is June 2026.

---

## PHASE 8 — Low priority / infrastructure

---

### WI-28 [GAP · LOW] Domain events missing on exit initiation — notification and audit hooks absent

**What's missing:**
`Employee.ScheduleExit()` and `EmployeeExit.Create()` raise no `IDomainEvent`. No hook for exit confirmation email to employee, no audit trail entry beyond DB timestamps, no trigger for document workflows.

**Fix:** Add `ExitInitiatedDomainEvent(employeeId, lastWorkingDay, reason)`. Register a handler to: (1) log audit entry, (2) optionally queue exit confirmation email to personal email.

**Smoke test:**
1. Initiate exit.
2. Verify audit log table has an `ExitInitiated` event row with correct employeeId, timestamp, actorId.

---

### WI-29 [GAP · LOW] YTD snapshot not locked at exit initiation — FnF recomputes can shift if upstream runs change state later

**What's missing:**
`PayrollFnfOrchestrator.LoadCurrentYtdAsync` queries approved runs dynamically. If a prior-month run is approved after exit initiation, YTD shifts and FnF recompute results change. Two recomputes of the same FnF at different points produce different TDS.

**Fix:** At exit initiation time, snapshot the current YTD (gross, taxable, TDS) per employee and store on `EmployeeExit`. Orchestrator reads the snapshot instead of querying dynamically.

**Smoke test:**
1. Initiate exit with current YTD = ₹3,00,000 taxable.
2. Approve a prior-month regular run that increases YTD to ₹3,50,000.
3. FnF recompute should use the locked ₹3,00,000 (not the updated ₹3,50,000).

---

### WI-30 [GAP · LOW] UpdateFnfRun zero-value semantics (zero = remove component) undocumented in API contract

**What's missing:**
Sending `gratuity: 0` silently removes existing `FNF_GRATUITY` rows. Not documented. Integration partners or future frontend developers may expect zero to write a zero-value row rather than delete.

**Fix:** Add an API description comment / OpenAPI annotation on the `UpdateFnfRun` endpoint: "A zero value for any FnF component removes that component from the settlement. To retain a zero-value component explicitly, this endpoint currently does not support it."

**Smoke test:**
1. Save FnF with ₹5,00,000 gratuity.
2. Resave with `gratuity: 0`.
3. Query `payrun_component_breakdowns` — `FNF_GRATUITY_EXEMPT` and `FNF_GRATUITY_TAXABLE` rows must be deleted.
4. FnF UI must show ₹0 gratuity (no row).

---

### WI-31 [GAP · LOW] No domain events on exit for relieving letter / experience letter generation

**What's missing:**
Dependent on WI-28. Once `ExitInitiatedDomainEvent` is raised, a handler can trigger document generation (experience letter, relieving letter) to be stored in MinIO and linked to the employee.

**Fix:** Implement after WI-28. Add a document generation job triggered by `ExitInitiatedDomainEvent`. Template-based PDF generation using employee name, designation, tenure, last working day.

**Smoke test:**
1. Initiate exit.
2. Check MinIO bucket for a generated document for the employee.
3. Document must be retrievable via `GET /api/v1/employees/{id}/documents`.

---

### WI-32 [GAP · LOW] Form 16 and Form 24Q generation absent — TDS return filing not implemented

**What's missing:**
No Form 16, Form 24Q, or TRACES integration exists. FnF TDS sweeps are computed and stored (after WI-04) but never exported into statutory returns. This affects all employees, not just FnF exits.

**Note:** Deferred — mark all related code with `// DEFERRED: form16-form24q` per CLAUDE.md convention.

**Smoke test (when implemented):**
1. Complete at least one FnF run and several regular runs in a fiscal year.
2. Generate Form 24Q for the quarter → must include FnF TDS deduction in the deductee annexure.
3. Generate Form 16 for the exiting employee → Part A must show TDS from FnF month, Part B must show full income breakdown including FnF components.

---

## Implementation Order Summary

| Priority | WI | Dependency |
|----------|-----|------------|
| 1 | WI-01 | None — must go first |
| 2 | WI-02 | Before WI-01 can be tested cleanly |
| 3 | WI-03 | Needs WI-01 to produce non-zero results |
| 4 | WI-04 | Needs WI-03 |
| 5 | WI-05 | Independent |
| 6 | WI-06 | Independent (needs WI-19 for DB config) |
| 7 | WI-07 | Independent |
| 8 | WI-08 | Independent |
| 9 | WI-09 | Needed before WI-14 (cancel exit) |
| 10 | WI-10 | Needs WI-01 (to have something to guard against) |
| 11 | WI-11 | Independent |
| 12 | WI-12 | Independent |
| 13 | WI-13 | Needs WI-01 |
| 14 | WI-14 | Needs WI-09 |
| 15 | WI-15 | Needs WI-09's `ExitStatus` model |
| 16 | WI-16 | Independent |
| 17 | WI-17 | Needs WI-01 |
| 18 | WI-18 | Needs WI-23 for correct subject |
| 19 | WI-19 | Needed before WI-06 |
| 20 | WI-20 | Independent |
| 21 | WI-21 | Needs WI-01 for meaningful data |
| 22 | WI-22 | Needs WI-01 |
| 23 | WI-23 | Needs WI-18 |
| 24 | WI-24 | Independent |
| 25 | WI-25 | Needs WI-01 |
| 26 | WI-26 | Independent |
| 27 | WI-27 | Needs WI-07 (share proration ref fix) |
| 28 | WI-28 | Independent |
| 29 | WI-29 | Needs WI-28 |
| 30 | WI-30 | Independent |
| 31 | WI-31 | Needs WI-28 |
| 32 | WI-32 | Needs WI-04 |
