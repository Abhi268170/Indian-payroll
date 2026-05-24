# Employee Exit + Full and Final Settlement (FnF)

**Date:** 2026-05-24
**Owner:** TBD
**Status:** Draft
**Related BA audit:** `docs/ba-audit/exit/01-initiate-exit-step1.md`, `02-fnf-settlement-payroll.md`, `03-fnf-runs-relationship.md`
**Related plans:** `2026-05-24-011-feat-zoho-style-one-time-earnings-plan.md` (FnF reuses one-time component infra)

---

## 1. Problem Statement

We have no flow for employees leaving the organisation. The skeleton (`EmployeeExit` domain entity, `employee_exits` table, `Employee.MarkExited()` / `RevertExit()`) was created in May 2026 but never wired. As of today:

- Admin cannot mark an employee as resigned/terminated through the UI or any API.
- Exited employees are silently included in regular pay runs (because the filter is `Status == Active`, but nothing sets `Status = Exited`).
- There is no Full and Final settlement payslip — no Bonus / Leave Encashment / Gratuity / Notice Pay path, no separate run type, no personal-email dispatch path.
- No PayrollRunType for FnF; the engine has no FnF-aware orchestration; payslip PDF has no FnF variant.

This plan implements the Zoho-mirrored exit workflow end-to-end so an admin can: initiate an exit on any active employee, choose between bulk-aligned or custom-dated settlement, review and edit the FnF payroll run, approve and pay, and have the final payslip mailed to the employee's personal address.

## 2. Goal

Mirror the Zoho Payroll exit workflow:

1. From an employee's overview page, an admin clicks **Initiate Exit Process** in the kebab menu.
2. Step 1: short form captures Last Working Day, Reason, Settlement Mode (regular schedule vs custom date), optional Personal Email + Notes.
3. On submit, the system:
   - Persists an `EmployeeExit` record.
   - Marks the employee with a "Will be resigned on {LWD}" badge but keeps `EmployeeStatus = Active` until LWD passes.
   - Removes the employee from future regular pay runs whose period covers or exceeds LWD.
   - Creates (or appends to) the appropriate FnF payroll run.
4. Step 2: admin opens the FnF run (single-employee Final Settlement Payroll, or shared Bulk Final Settlement Payroll), edits earnings/deductions/LOP, approves.
5. On approval + payment, FnF payslip generates with an FnF-aware layout and emails to the employee's personal address.

Non-goals (deferred):
- Form-16 generation (separate feature; v1 ships without).
- Off-cycle one-time payout runs (separate Zoho concept).
- Past-month LOP adjustments inside FnF — capture the UI but defer engine math to v2.
- Resettlement (retro pay) runs — separate plan.
- Auto-tenure-based gratuity payout — out for v1; admin types the gratuity lump-sum manually (Zoho's behavior in the trial we audited).

## 3. Scope

**In scope:**

- Domain: extend `EmployeeExit` if needed, add tenure helper on `Employee`.
- Application: 6 commands (`InitiateExit`, `UpdateExit`, `CancelExit`, `InitiateFnfRun`, `UpdateFnfRun`, `SetLwdOnExit`) + 2 queries (`GetEmployeeExit`, `ListPendingFnfRuns`).
- Engine adapter: orchestrate FnF inputs (force `MonthsRemainingInFY=1`, inject FnF flat components, half-yearly LWF skip when already deducted).
- API: 6 routes under `/employees/{id}/exit` and `/payroll-runs/...`.
- DB: 2 migrations — add new `PayrollRunType` enum values, add link col from `PayrollRun` to `EmployeeExit`.
- Frontend: kebab menu item, Step 1 exit form page, Step 2 FnF settlement page, Pay Runs list filter chips, "Will be resigned" badge, status-tab gating.
- Payslip: FnF-aware QuestPDF layout variant; existing `SendPayslipEmailCommand` already routes to `PersonalEmail` first (no change).
- Audit: every exit lifecycle event written to `AuditLog`; FnF run transitions to `PayrollRunAuditLog`.
- Tests: handler unit tests, engine FnF fixtures, frontend Vitest stubs.

**Out of scope (deferred):**

- Form-16 PDF.
- Past-month LOP adjustment math.
- Auto gratuity computation from tenure.
- Full PF / EPS final settlement (employee handles via EPFO Form 19; we don't generate Form 19 in v1).
- Resettlement / Arrear pay runs.
- Bulk import for exit (one-by-one only in v1).

## 4. Architectural Decisions

### 4.1 Status semantics: when does `Employee.Status` flip to `Exited`?

Options considered:
- **(a)** Flip immediately on initiate. Clean, but breaks "Will be resigned" UI (employee should still appear in Active list until LWD passes per Zoho behavior).
- **(b)** Keep `Active`; rely on the dedicated `Employee.DateOfLeaving` column + a query-level filter (`DateOfLeaving IS NULL OR DateOfLeaving >= period.EndDate`) to exclude from regular runs.
- **(c)** Flip via a scheduled Hangfire job at midnight UTC on the day after LWD.
- **(d)** Flip immediately when the FnF run is paid (`RecordPaymentCommand`).

**Decision: (b)** for the regular-run filter (no race; idempotent) + **(c)** + **(d)** for the status flip. (b) is authoritative for payroll inclusion; (c) and (d) are cosmetic and complementary — whichever fires first wins, the other is a no-op (idempotent `if (status == Active) status = Exited`). `RevertExit` cancels all three states.

**Important: Bulk run RecordPayment iterates.** When `RecordPaymentCommand` runs on a `BulkFinalSettlement` payroll run, it must loop over every `PayrunEmployee` and flip each linked employee individually. Do not hardcode for single-employee — the Bulk path is the common case once multiple exits cluster on the same pay date.

### 4.2 PayrollRunType extension

Add two values: `FinalSettlement` (1 employee, custom date) and `BulkFinalSettlement` (N employees, aligned to next regular pay date).

Why two enum values not one with a "bulk?" flag: existing audit + reporting + filter-chips logic switches on `Type`; adding a flag forces every consumer to branch. Two enum values keep call sites compile-time exhaustive.

### 4.3 Idempotency and pay-date selection for `BulkFinalSettlement` runs

**Pay-date rule:** the FnF run's pay date = the **first regular pay date ≥ LWD** (computed from the active `PaySchedule`). Picking "next regular pay date" is wrong when LWD is in the future: e.g. LWD = Aug 15 with monthly schedule paying on the last day → if "next" is Jul 31 we would pay FnF while the employee is still employed. Correct answer is Aug 31.

**Idempotency:** a second exit choosing `RegularSchedule` whose pay date resolves to the same value must **append** a `PayrunEmployee` row to the existing Draft Bulk run, not spawn a second. Implementation:

```csharp
DateOnly targetPayDate = paySchedule.FirstRegularPayDateOnOrAfter(exit.LastWorkingDay);
var existingBulk = await runRepo.FindDraftBulkFnfByPayDateAsync(targetPayDate, ct);
if (existingBulk is null)
    existingBulk = await CreateBulkFnfRun(targetPayDate, ct);
await AddPayrunEmployeeToRun(existingBulk, employeeId, ct);
```

If the matched Bulk run is already Approved/Paid, the new exit creates a fresh Bulk run for the **next** eligible pay date (frontend surfaces a banner: "Bulk settlement for {targetPayDate} is already approved. This exit has been scheduled for {nextPayDate}.").

### 4.4 Regular-run filter + open-Draft-run reconciliation

**(a) Filter for newly-initiated regular runs.** `InitiatePayrollRunHandler` line 84 currently:
```csharp
var activeEmployees = employees.Where(e => e.Status == EmployeeStatus.Active).ToList();
```
Change to:
```csharp
var eligible = employees.Where(e =>
    e.Status == EmployeeStatus.Active
    && (e.DateOfLeaving == null || e.DateOfLeaving >= period.EndDate));
```
This handles three cases atomically: still-employed (Status=Active, DateOfLeaving=null), exiting later (Active + LWD in future, still included until that period), already-left (Status=Exited, excluded). Same filter pattern applied in `BulkImportLopCommand`, `ReEvaluateSkippedCommand`, etc.

**(b) Reconciliation of existing Draft regular runs.** If an admin initiated July's regular run on Jul 1 and a July-period exit (LWD=Jul 20) is initiated on Jul 5, the July regular Draft already contains the exiting employee. Without reconciliation we double-pay: regular run pays for July + FnF run pays for July. `InitiateExitCommand` must scan every Draft regular run whose `PayPeriod` covers a date ≤ LWD and call `RemovePayrunEmployeeFromRunCommand(runId, employeeId, "Removed: exit initiated, paid via FnF")`. Implementation order in the handler:

```csharp
// 1. Validate (tax-deductor, no dup exit, ...)
// 2. Persist EmployeeExit
// 3. Reconcile pre-existing Draft regular runs
var draftRuns = await runRepo.FindDraftRegularRunsCoveringDateAsync(exit.LastWorkingDay, ct);
foreach (var run in draftRuns)
{
    var pe = await payrunEmpRepo.FindAsync(run.Id, exit.EmployeeId, ct);
    if (pe is not null) {
        await mediator.Send(new RemovePayrunEmployeeFromRunCommand(run.Id, exit.EmployeeId,
            $"Exit initiated on {DateTime.UtcNow:yyyy-MM-dd}, paid via Final Settlement run"), ct);
    }
}
// 4. Create or append FnF run (§4.3)
```

(If an admin already approved a July regular run that included the exiting employee, the system rejects the exit with: "Cannot initiate exit — pay run for {period} is already approved and includes this employee. Reject the run or wait for the next cycle.")

### 4.5 Tax Deductor gate

Before creating an `EmployeeExit`, the handler checks: is this employee the org's Tax Deductor? Tax deductor identity lives on the org's tax configuration record (per the Zoho audit it sits under Settings → Taxes; in our codebase it is stored on the `tax_details` table read via `IStatutoryConfigRepository` — Phase 1 of this work must add `Task<Guid?> GetTaxDeductorEmployeeIdAsync(CancellationToken ct)` and wire it through). If the returned id matches the exiting employee, reject:

```csharp
Guid? taxDeductorId = await statutoryRepo.GetTaxDeductorEmployeeIdAsync(ct);
if (taxDeductorId == employeeId)
    throw new DomainException("Cannot initiate exit: this employee is the organisation's Tax Deductor. Reassign in Settings → Taxes first.");
```

Frontend mirrors the Zoho modal text verbatim.

### 4.6 FnF engine orchestration

Engine has no FnF-aware flag (Phase 6 audit confirmed). FnF handler builds inputs as follows:

| Engine input | FnF value |
| --- | --- |
| `PayrollRunInput.MonthsRemainingInFY` | **1** (forces full-year TDS closure as lump sum) |
| `EmployeeInput.LOPDays` | LOP entered on FnF screen |
| `EmployeeInput.WorkingDaysInMonth` | Days from period start to LWD (not full month) |
| `EmployeeInput.Components` | Recurring components prorated to LWD + FnF flat components (Bonus, Commission, Leave Encashment, Gratuity, Notice Pay) appended with `IsFlat = true` |
| `EmployeeInput.GratuityEnabled` | `false` (engine's monthly accrual is wrong for FnF; we inject lump sum as an earning component instead) |

Half-yearly LWF: check if already deducted in the current half-year (April–September or October–March); if yes, pass `LWFEnabled = false` on this employee for the FnF run.

**Gratuity tax exemption (Sec 10(10) / Payment of Gratuity Act):** Gratuity received on exit is **exempt up to ₹20,00,000 lifetime cap** per Section 10(10)(ii). The default `SalaryComponent` flag for an Earning is `IsTaxable = true`, so naively injecting gratuity with the default would over-deduct TDS on the FnF month. The orchestrator therefore:

1. Reads the gratuity amount typed by admin into the Step 2 form.
2. Reads the employee's lifetime gratuity already received (sum of `EmployeeFyOpening.PriorGratuityReceived` + any prior FnF runs in this system) — for v1 we read only `EmployeeFyOpening` (Phase 1 of this plan adds the column if not present) and assume zero prior-FnF gratuity inside our system.
3. Splits into two synthetic engine components:
   - `GRATUITY_EXEMPT` (`IsTaxable = false`) = min(typed amount, ₹20,00,000 − lifetime received)
   - `GRATUITY_TAXABLE` (`IsTaxable = true`) = typed amount − exempt portion (only when typed amount exceeds the remaining exemption headroom)
4. Stores the split on `payrun_component_breakdown` rows so the payslip + Form-16 (when added) can present the exempt vs taxable portion explicitly.

The orchestrator never calls the engine's `GratuityCalculator` — that path remains for monthly accrual on regular runs only. For v1 the lifetime-received figure is best-effort (admin must enter it as part of mid-year onboarding via `EmployeeFyOpening`); §15 risks lists this as a documented limitation.

### 4.7 PersonalEmail dispatch

`SendPayslipEmailCommand` already prefers `PersonalEmail` over `WorkEmail`. For FnF, set `employee.PersonalEmail = exit.PersonalEmail` (if exit.PersonalEmail was provided in Step 1) before the payslip email job fires. Alternative — pass an explicit override into the job. Going with the simpler approach: the `InitiateExitCommand` writes the exit's PersonalEmail back to `Employee.PersonalEmail`; existing dispatch route Just Works.

### 4.8 Payslip layout

Add `IsFinalSettlement bool` and `Tenure { years, months }` to `PayslipData`. `PayslipPdfGenerator.Generate(...)` branches on the flag to:
- Title: "Final Settlement Payslip" instead of "Payslip"
- Show exit details block (LWD, Reason, Settlement Date)
- Section for FnF-specific earnings (Bonus, Commission, Leave Encashment, Gratuity, Notice Pay) — labeled and grouped distinctly from regular earnings
- Footer: any text from `EmployeeExit.Notes` (Zoho rule per audit: notes are printed)

## 5. Data Model Changes

### 5.1 `PayrollRunType` enum (Domain/Enums)

```csharp
public enum PayrollRunType
{
    Regular,
    OffCycle,
    OneTimePayout,
    Resettlement,
    FinalSettlement,       // NEW: single-employee, custom date
    BulkFinalSettlement,   // NEW: multi-employee, regular pay date
}
```

Migration: `AddFinalSettlementRunTypes` — no schema change (column is `varchar`, values added as enum); just a code-side change. EF Core will start storing the new strings automatically.

### 5.2 `PayrollRun.EmployeeExitId` (nullable Guid FK)

For FinalSettlement runs, links the single-employee FnF back to its exit record. For Bulk runs, stays null (linkage is via `PayrunEmployee` → `EmployeeExit`).

Migration `20260524130000_AddEmployeeExitIdToPayrollRun`:
- `Up`: AddColumn `employee_exit_id uuid NULL`, AddForeignKey to `employee_exits.id` (`ON DELETE RESTRICT`).
- `Down`: drop FK + column.

### 5.3 `PayrunEmployee.EmployeeExitId` (nullable Guid FK)

For BulkFinalSettlement: each PayrunEmployee row carries the originating exit. For regular runs, stays null.

Migration `20260524130001_AddEmployeeExitIdToPayrunEmployee` — same pattern.

### 5.4 `EmployeeExit` — already exists; one addition

Add column `FnfPayrollRunId uuid NULL` — points back to the FnF run that settles this exit. Useful for "open FnF for this exit" navigation. Migration `20260524130002_AddFnfPayrollRunIdToEmployeeExit`.

### 5.5 No new entities needed

`termination_deductions` and `past_lop_adjustments` from the audit doc map onto existing infra:
- One-time deductions reuse the Phase 011 work (one-time `SalaryComponent` with `IsOneTime=true`, `Category=Deduction`) and store as `PayrunComponentBreakdown` rows.
- Past-month LOP adjustment math is deferred; we capture the UI but don't ship the engine recompute for v1.

## 6. Domain Layer

### 6.1 `Employee.TenureAt(DateOnly date)` returns `Tenure` value object

The Payment of Gratuity Act rounds **6 or more months up to a full year** for eligibility math, so months matter — `int years` alone is wrong. Define a value object:

```csharp
public readonly record struct Tenure(int Years, int Months)
{
    public int YearsForGratuity =>
        Months >= 6 ? Years + 1 : Years;

    public override string ToString() => $"{Years}y {Months}m";
}

// Employee.cs
public Tenure TenureAt(DateOnly date)
{
    if (DateOfJoining == default) return new Tenure(0, 0);
    int years = date.Year - DateOfJoining.Year;
    int months = date.Month - DateOfJoining.Month;
    if (date.Day < DateOfJoining.Day) months--;
    if (months < 0) { years--; months += 12; }
    if (years < 0) return new Tenure(0, 0);
    return new Tenure(years, months);
}
```

Used by frontend ("12y 4m" badge), FnF orchestration (gratuity eligibility), and the FnF payslip layout (§11).

### 6.2 `EmployeeExit` — no breaking changes, only column add

Already has `Update(...)` method covering field edits. New `LinkFnfRun(Guid runId, Guid actor)` method to set `FnfPayrollRunId`.

### 6.3 `PayrollRun.CreateFinalSettlement(...)` factory

```csharp
public static PayrollRun CreateFinalSettlement(
    Guid tenantId,
    PayPeriod payPeriod,
    DateOnly payDay,
    Guid employeeExitId,
    string statutoryConfigSnapshot,
    Guid createdBy) => new()
    {
        TenantId = tenantId,
        PayPeriod = payPeriod,
        Type = PayrollRunType.FinalSettlement,
        Status = PayrollRunStatus.Draft,
        PayDay = payDay,
        EmployeeExitId = employeeExitId,
        StatutoryConfigSnapshot = statutoryConfigSnapshot,
        EmployeeCount = 1,
        CreatedBy = createdBy
    };
```

Plus `CreateBulkFinalSettlement(...)` (omits `EmployeeExitId`, accepts initial count 0 — grows via append).

## 7. Application Layer (Commands & Queries)

### 7.1 Commands

| Command | Purpose | Returns |
| --- | --- | --- |
| `InitiateExitCommand(employeeId, lwd, reason, mode, settlementDate?, personalEmail?, notes?)` | Validates (active employee, not tax-deductor, no existing exit, LWD ≥ today), creates `EmployeeExit`, creates or appends to FnF run, links FnF run to exit, audit-logs. | `EmployeeExitDto` |
| `UpdateExitCommand(exitId, lwd, reason, mode, settlementDate?, personalEmail?, notes?)` | Mutates fields; if mode/LWD/SettlementDate changes affect FnF run alignment, moves the PayrunEmployee row to the right run. | `EmployeeExitDto` |
| `CancelExitCommand(exitId, reason)` | Soft-deletes exit; removes PayrunEmployee from FnF run; if FnF run becomes empty (single-employee Final, or last row in Bulk), soft-deletes run too. Reverts `Employee.DateOfLeaving` to null. Audit-logs. | void |
| `UpdateFnfRunCommand(runId, lopDays, bonus, commission, leaveEncash, gratuity, hasNoticePay, noticePayDirection, noticePayAmount, payslipNotes, deductions[])` | Step 2 form save; updates FnF run draft fields, reruns engine (FnF orchestration), persists totals. | `FnfRunDetailDto` |
| `ApproveFnfRunCommand(runId, actorId)` | Same as `ApprovePayrollRunCommand` but for FnF type; on success enqueues `GenerateFnfPayslipJob`. | void |
| `RecordFnfPaymentCommand(runId, ...)` | Same flow as `RecordPaymentCommand`; on success transitions `Employee.Status = Exited` immediately (no waiting for LWD job since FnF settled = exit complete). | void |

### 7.2 Queries

| Query | Returns |
| --- | --- |
| `GetEmployeeExitQuery(employeeId)` | `EmployeeExitDetailDto?` (null when no active exit) — for the "Continue Exit" affordance on already-resigning employees. |
| `ListPendingFnfRunsQuery()` | `[FnfRunSummaryDto]` for Pay Runs page filter chips. |

### 7.3 Validators (FluentValidation)

`InitiateExitCommandValidator`:
- `EmployeeId` not empty.
- `LastWorkingDay` ≥ today and ≤ today + 5 years (sanity bound).
- `Reason` valid enum value.
- `Mode == CustomDate` ⇒ `SettlementDate` required and ≥ `LastWorkingDay` and ≤ `LastWorkingDay + 90 days` (sanity).
- `PersonalEmail` valid email format if provided.
- `Notes` ≤ 2000 chars.

### 7.4 Scheduled job — `MarkExitedOnLwdJob`

Hangfire recurring job, runs once a day at 01:00 UTC per tenant. SQL:
```sql
UPDATE employees
SET status = 'Exited', updated_at = NOW()
WHERE status = 'Active'
  AND date_of_leaving IS NOT NULL
  AND date_of_leaving < CURRENT_DATE;
```
Plus audit-log row per flipped employee. Idempotent.

## 8. API Surface

| Method | Route | Command/Query |
| --- | --- | --- |
| POST | `/api/v1/employees/{id}/exit` | `InitiateExitCommand` |
| GET | `/api/v1/employees/{id}/exit` | `GetEmployeeExitQuery` |
| PUT | `/api/v1/employees/{id}/exit` | `UpdateExitCommand` |
| DELETE | `/api/v1/employees/{id}/exit` | `CancelExitCommand` |
| GET | `/api/v1/payroll-runs/fnf-pending` | `ListPendingFnfRunsQuery` |
| PUT | `/api/v1/payroll-runs/{id}/fnf-settlement` | `UpdateFnfRunCommand` |
| POST | `/api/v1/payroll-runs/{id}/approve` | reuses `ApprovePayrollRunCommand` (handler branches on `Type`) |
| POST | `/api/v1/payroll-runs/{id}/record-payment` | reuses `RecordPaymentCommand` (handler branches on `Type` to flip Employee.Status) |

Existing `/api/v1/payroll-runs` GET grows: returns FnF rows alongside regular, distinguishable by `type` field.

## 9. Engine Orchestration

`PayrollFnfOrchestrator` (new service in Application layer) takes a `PayrollRun (FnF type)` + its `PayrunEmployee` + their `EmployeeExit` and builds engine inputs:

```csharp
public sealed class PayrollFnfOrchestrator(
    IEmployeeRepository employeeRepo,
    IPayScheduleRepository payScheduleRepo,
    IEmployeeSalaryStructureRepository structureRepo,
    ...)
{
    public async Task<FnfEngineResult> ComputeAsync(Guid fnfRunId, Guid employeeId, CancellationToken ct)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId, ct);
        var exit = await exitRepo.GetByEmployeeAsync(employeeId, ct);
        var workingDaysToLwd = ComputeWorkingDaysToLwd(payPeriod, exit.LastWorkingDay, paySchedule);

        var recurringComponents = BuildProratedRecurringComponents(...);  // existing
        var fnfFlatComponents = BuildFnfFlatComponents(fnfRunDraft);      // Bonus, Commission, Leave Encash, Gratuity, Notice Pay — IsFlat = true
        var components = recurringComponents.Concat(fnfFlatComponents).ToList();

        var lwfAlreadyDeducted = await IsLwfAlreadyDeductedThisHalfYearAsync(employeeId, payPeriod, ct);

        var empInput = new EmployeeInput(
            ...,
            Components: components,
            LOPDays: fnfRunDraft.LopDays,
            WorkingDaysInMonth: workingDaysToLwd,
            GratuityEnabled: false,           // we inject lump sum manually as an Earning component
            ...
        );

        var runInput = new PayrollRunInput(
            ...,
            MonthsRemainingInFY: 1,           // force full-year TDS closure
        );

        var result = PayrollEngine.Compute([empInput], runInput, staticConfig)[0];

        if (lwfAlreadyDeducted)
            result = result with { LWF = new LWFResult(0m, 0m, IsExempt: true) };

        return MapToFnfResult(result, fnfRunDraft);
    }
}
```

Reuses the `IPayrollRecomputeService` from Phase 6 conceptually but for FnF context — likely a new sibling service rather than overloading the recurring one (different inputs, different orchestration semantics).

## 10. Frontend

### 10.1 Components

| File (new) | Purpose |
| --- | --- |
| `web/src/pages/employees/ExitInitiationPage.tsx` | Step 1 form (LWD, Reason, Mode radio, Settlement Date, Personal Email, Notes). Route `/employees/{id}/exit/initiate`. |
| `web/src/pages/payroll/FnfSettlementPage.tsx` | Step 2 FnF form (Attendance, Earnings, Deductions, Notice Pay, Notes). Route `/pay-runs/fnf/{runId}`. |
| `web/src/pages/employees/components/InitiateExitMenuItem.tsx` | Kebab-menu item rendered conditionally (gates: active employee, not tax-deductor, no existing exit). |
| `web/src/pages/employees/components/WillBeResignedBadge.tsx` | Orange badge "Will be resigned on dd/MM/yyyy" + click → "Continue Exit" action. |
| `web/src/pages/payroll/components/FnfFilterChips.tsx` | Three chips on Pay Runs list: All Pending / Final Settlement Payroll / Bulk Termination Payroll. |

### 10.2 Modifications

- `EmployeeDetailPage.tsx`: kebab gains "Initiate Exit Process" item (uses validation query to decide whether to render or render-disabled with tooltip explaining why).
- `EmployeesPage.tsx`: row badge logic — pre-LWD shows "Will be resigned"; post-LWD shows existing "Exited".
- `PayRunsPage.tsx`: filter chips + card layouts for the two FnF run types (matches Zoho visual we captured).

### 10.3 Status-tab gating

Employees page existing "Active / Inactive / Exited" tabs continue to work. The "Will be resigned" badge appears under the Active tab. Once `MarkExitedOnLwdJob` flips the status (or `RecordFnfPaymentCommand` flips it immediately), the employee moves to the Exited tab automatically.

## 11. Payslip PDF (FnF Variant)

`PayslipPdfGenerator.Generate(PayslipData data)` branches:

```csharp
if (data.IsFinalSettlement)
{
    // Title: "Final Settlement Payslip"
    // Add ExitDetails block: LWD, Reason, Settlement Date, Tenure
    // Group Earnings: Regular | Final Settlement (Bonus/Commission/Leave Encash/Gratuity/Notice Pay)
    // Show Notes from EmployeeExit.Notes
}
else
{
    // existing layout
}
```

Storage path stays the same pattern (`payslips/{tenantId}/{payrollRunId}/{employeeId}.pdf`).

## 12. Audit

| Event | Log target | Action string |
| --- | --- | --- |
| Exit initiated | `AuditLog` (EntityType=EmployeeExit, EntityId=exit.Id) | `"exit.initiated"` |
| Exit updated | `AuditLog` | `"exit.updated"` |
| Exit cancelled | `AuditLog` | `"exit.cancelled"` |
| FnF run created | `PayrollRunAuditLog` (None → Draft) | n/a (Create call already audits) |
| FnF run approved | `PayrollRunAuditLog` (Draft → Approved) | reuses existing |
| FnF payment recorded | `PayrollRunAuditLog` (Approved → Paid) + Employee.Status flip audit row | reuses existing + new |
| Auto-flip on LWD passing | `AuditLog` (EntityType=Employee) | `"employee.auto_exited_on_lwd"` |

## 13. Testing

### 13.1 Engine fixtures (`tests/Payroll.Engine.Tests`)

- FnF with `MonthsRemainingInFY=1` closes annual TDS in one month (assert exact rupee against hand-computed slab math for a Bogus employee).
- Half-yearly LWF skip — pass `LWFEnabled=false` → `LWF.EmployeeAmount == 0`.
- One-time `IsFlat=true` gratuity component flows into `Gross.GrossWage` and TDS without proration.

### 13.2 Handler tests (`tests/Payroll.Application.Tests`)

- `InitiateExitCommandHandlerTests`: tax-deductor blocked, duplicate-exit blocked, valid → creates exit + FnF run + audit row.
- `InitiateExitCommandHandlerTests` (bulk path): second exit on same pay date appends to existing Bulk run, doesn't spawn a second.
- `CancelExitCommandHandlerTests`: removes PayrunEmployee; empty Bulk run gets soft-deleted; Final Settlement (single) always soft-deleted.
- `UpdateFnfRunCommandHandlerTests`: changing LOP triggers engine recompute; FnF flat components persist; cost calculator picks up the new totals (reuses Phase 011 calculator).

### 13.3 Integration (`tests/Payroll.Infrastructure.Tests` with Testcontainers)

- Migrations `Up`/`Down` round-trip.
- Unique-active-exit constraint enforces 1 row per employee.
- Auto-flip job updates the right employees only.

### 13.4 Frontend (`web/src/__tests__`)

- `ExitInitiationPage` form: required-field guards, settlement-date show/hide on radio change, valid submit posts correct payload.
- `FnfSettlementPage`: dirty-tracking, save-draft vs approve flows, totals update on field change.

### 13.5 E2E (`e2e/`)

Playwright scenario (matches Zoho audit walkthrough):
1. Login, navigate to employee, click Initiate Exit Process.
2. Block path: try with tax-deductor — assert modal text.
3. Reassign tax deductor in Settings → Taxes.
4. Retry; fill Step 1 with custom date; assert FnF run appears in Pay Runs list with the chosen date.
5. Open FnF run; type Bonus + Leave Encashment; assert Net Pay > 0 and TDS reflects full-year closure.
6. Approve + record payment; assert employee moves to Exited tab.

## 14. Rollout & Sequencing

Six PRs, each independently shippable.

**Phase 1 — Domain + migrations + filter fix (PR 1)**
- Add `PayrollRunType.FinalSettlement` + `BulkFinalSettlement` (no migration needed; varchar column).
- Add `PayrollRun.EmployeeExitId`, `PayrunEmployee.EmployeeExitId`, `EmployeeExit.FnfPayrollRunId` columns.
- Add `Employee.TenureYearsAt(date)` method.
- Patch the regular-run filter in `InitiatePayrollRunHandler` + `BulkImportLopCommand` to honor `DateOfLeaving`.
- Add `MarkExitedOnLwdJob` (recurring Hangfire).
- *Verify:* existing tests stay green; new filter tested on employees with `DateOfLeaving` in past / future / null.

**Phase 2 — InitiateExit command + API + tax-deductor gate (PR 2)**
- Add `IEmployeeExitRepository`.
- `InitiateExitCommand` + validator + handler.
- `IOrgStatutoryConfigRepository.IsTaxDeductorAsync(employeeId)` — extends existing repo.
- API: POST `/employees/{id}/exit`.
- Audit-log integration.
- *Verify:* via E2E and handler tests; no UI yet, drive via API.

**Phase 3 — FnF orchestrator + UpdateFnfRunCommand + ApproveFnfRun flow (PR 3)**
- `PayrollFnfOrchestrator` service.
- `UpdateFnfRunCommand` handler.
- `ApprovePayrollRunCommand` branches on `Type` for FnF post-approval actions.
- `RecordPaymentCommand` flips `Employee.Status = Exited` when paying off an FnF run.
- *Verify:* engine fixtures + handler tests.

**Phase 4 — Payslip FnF layout + email dispatch (PR 4)**
- Extend `PayslipData` with `IsFinalSettlement`, `ExitDetails`, `Tenure`.
- `PayslipPdfGenerator` FnF branch.
- `GenerateFnfPayslipJob` (parallel to existing `GeneratePayslipsJob`).
- `ApproveFnfRunCommand` enqueues this job on success.
- *Verify:* compare rendered PDF against a fixture; smoke-test email dispatch in MailHog.

**Phase 5 — Frontend Step 1 + Step 2 + kebab + badges (PR 5)**
- Add kebab item; gate visibility.
- Add `ExitInitiationPage` + route.
- Add `FnfSettlementPage` + route.
- Add `WillBeResignedBadge`.
- Add Pay Runs filter chips for FnF.
- *Verify:* Vitest + Playwright E2E.

**Phase 6 — Hardening + edge cases (PR 6)**
- CancelExit + UpdateExit endpoints.
- "Continue Exit" affordance from employee row when an exit already exists.
- "Past LOP" UI scaffolding (form only; engine recompute deferred to v2 — show coming-soon disabled state).
- Coverage threshold raise; CHANGELOG; deployment note.

## 15. Risks & Mitigations

| Risk | Likelihood | Mitigation |
| --- | --- | --- |
| Engine surcharge / cess scaled to full year when employee exits mid-FY → over-deduction of TDS in the FnF month | Medium | Add an "actual months worked" override in `TDSCalculator` v2; for v1, accept the engine's output (over-deducted amount squares out at year-end self-assessment for employee). Document in user-facing help. |
| Half-yearly LWF check requires reading prior pay runs — slow if naive | Low | Add `IsLwfAlreadyDeductedAsync(employeeId, halfYear)` repo method backed by a single indexed query on `payrun_employees.lwf_employee_amount > 0` joined with `payroll_runs.pay_period`. |
| Race between two admins initiating exit on the same employee | Low | Unique index on `(employee_id) WHERE is_deleted = false` already enforces 1-exit-per-employee at DB. Second initiate gets a DB-level conflict; handler converts to friendly error. |
| Bulk run already approved when a new exit chooses RegularSchedule | Medium | Handler detects, creates a fresh Bulk run for the *following* pay date, surfaces a frontend banner: "{N} employee(s) added to settlement payroll for {next next pay date} because the upcoming one is already approved." |
| `Employee.PersonalEmail` overwrite if admin already had one set | Low | Handler merges: never overwrites an existing `Employee.PersonalEmail`; only sets it when blank. Step 1 form pre-fills from `Employee.PersonalEmail` if any. |
| FnF UI exit pencil click loses unsaved Step 2 edits (Zoho behavior) | Low | Mirror Zoho: pencil click warns "Unsaved FnF changes will be discarded; continue?". |
| `MarkExitedOnLwdJob` race vs FnF payment that flips status earlier | Low | Both paths use `if (status == Active) status = Exited` — idempotent. |
| `EmployeeFyOpening.PriorGratuityReceived` missing for v1 employees | Medium | Phase 1 adds the column with `NULL` default; FnF gratuity treats `NULL` as `0` (full ₹20L exemption available) and surfaces a tooltip "Lifetime gratuity received elsewhere is assumed 0. Update employee FY opening if otherwise." Operator can fix mid-flight. |
| Surcharge / cess scaled to full year when LWD is mid-FY → over-deduction in FnF month | Medium | Documented behavior for v1; employee squares out at year-end self-assessment. v2 task: add `ActualMonthsWorked` override on `TDSCalculator` to scale surcharge proportionally. |

## 16. Open Decisions (tagged by blocking phase)

| # | Decision | Blocks Phase | Recommendation |
| --- | --- | --- | --- |
| 1 | What pay-period is the FnF run keyed to when LWD is mid-month? Containing month vs settlement-date month. | **Phase 1** (filter + orchestrator both consume this) | Containing month — accounting-policy review needed before merge. |
| 2 | Gratuity auto-calc from tenure vs manual admin input? | **Phase 3** (FnF orchestrator math) | Match Zoho — manual input. Auto-calc deferred to v2. |
| 3 | FnF display location — sibling card vs nested under parent regular run? | **Phase 5** (frontend only) | Sibling card (matches Zoho). |
| 4 | Form-16 generation timeline — ship FnF v1 without and add later? | Product decision, **no code gate** | Defer — FnF v1 ships without; flag to product before any tax filing season. |
| 5 | Pre-existing Approved regular run that included exiting employee — block exit, or auto-reject and let admin re-initiate run? | **Phase 2** (InitiateExitCommand handler) | Block exit with friendly error per §4.4; admin must reject the approval first. |
| 6 | Lifetime gratuity received tracking — read only from `EmployeeFyOpening.PriorGratuityReceived` for v1, or also from prior in-system FnF runs? | **Phase 3** | v1 reads only `EmployeeFyOpening` (first-FY exits will be accurate; second-FY exits assume admin updates opening). Flag in user-facing help. |

## 17. Acceptance Criteria

- Admin can click Initiate Exit Process from the kebab on any Active, non-tax-deductor employee with a complete profile.
- Step 1 form rejects: tax-deductor employee, missing required field, LWD in past beyond grace, SettlementDate < LWD.
- On Step 1 submit: `EmployeeExit` row created; employee shows "Will be resigned on dd/MM/yyyy" badge; FnF run (Final or Bulk) created or appended.
- Same-day second exit choosing RegularSchedule → same Bulk run grows (count column reflects new total).
- Initiating regular monthly pay run after an exit excludes the exiting employee for any period at/after their LWD.
- Step 2 FnF form persists Bonus/Commission/Leave Encash/Gratuity/Notice Pay/Adhoc Deductions; engine recompute yields net pay = engine output + reimbursements - deductions per Phase 011 contract.
- Approving the FnF run generates an FnF-variant payslip PDF, emails to `EmployeeExit.PersonalEmail` (or `Employee.PersonalEmail` fallback), and (on RecordPayment) flips `Employee.Status` to `Exited`.
- Audit log row exists for every state transition.
- Cancelling an exit: removes PayrunEmployee from the FnF run; if Bulk becomes empty, soft-deletes the Bulk run; reverts `Employee.DateOfLeaving` to null.
- All CI gates green: build, tests (Application + Engine + Infrastructure with Testcontainers), Vitest, Playwright E2E happy path.
