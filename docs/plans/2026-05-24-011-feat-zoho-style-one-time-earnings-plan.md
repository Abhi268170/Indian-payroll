# Zoho-Style One-Time Earnings, Deductions & Statutory-Aware Recompute

**Date:** 2026-05-24
**Owner:** TBD
**Status:** Draft
**Related:**
- `2026-05-22-008-fix-salary-component-flags-plan.md`
- `2026-05-23-009-feat-payroll-import-export-plan.md`
- `2026-05-24-010-feat-background-jobs-import-initiation-plan.md`

---

## 1. Problem Statement

Today, one-time earnings, deductions, and reimbursements added to a payroll run mutate `PayrunEmployee.GrossPay` / `NetPay` directly and skip the payroll engine. Consequences:

1. **Tax non-compliance** — a taxable bonus does not trigger TDS recomputation. Annual projected income is wrong; year-end Form 16 mismatches.
2. **PF / ESI / PT silently wrong** — components that should affect statutory bases are ignored.
3. **Run-level totals drift** — four different handlers compute `PayrollCost` with different formulas. Each mutation may corrupt totals.
4. **Schema-level recompute infidelity** — `PayrunComponentBreakdown` stores `ConsiderForEpf` only; engine cannot reconstruct full component flags during re-run, so `SetLopCommand.RecomputeEmployee` hardcodes `IsTaxable: true` and drops `ConsiderForEsi`. Non-taxable allowances become taxable after any LOP edit.
5. **Operator UX is ambiguous** — “add earning” means anything from “bonus subject to all statutory” to “net-pay adjustment”. Code picks a model silently.

## 2. Goal

Mirror Zoho Payroll’s component-driven model:

- Admin pre-configures one-time components at **Settings → Salary Components** (e.g. “Diwali Bonus”, “Loan Recovery”) with statutory flags (`IsTaxable`, `ConsiderForEpf`, `ConsiderForEsi`, …).
- Operator at **Payroll Run → Employee Drawer → Add Earning / Deduction** picks the pre-configured component and types an amount.
- System reruns the payroll engine with the augmented component list. All statutory calculations (TDS annualized, PF base bumped, ESI threshold checked, PT slab re-looked-up) update automatically.
- Single source of truth: one `RecomputeEmployee` helper used by `InitiatePayrollRun`, `SetLop`, `Add/RemoveOneTimeEarning`, `BulkImportOneTimeEarnings`, `BulkImportReimbursements`.
- Single source of truth: one `CalculatePayrollCost` helper used by every handler that mutates run totals.

Non-goals:

- Old tax regime (deferred).
- Off-cycle / one-time payout *runs* (separate Zoho feature; covered in future plan).
- TDS spread strategies (e.g. lumpsum vs spread). v1 keeps engine’s uniform annualization.

## 3. Scope

In scope:

- Schema: `SalaryComponent.IsOneTime`, `PayrunComponentBreakdown.IsTaxable`, `PayrunComponentBreakdown.ConsiderForEsi`.
- Domain: `RecomputeEmployee` extracted into `IPayrollRecomputeService` with full-fidelity flag restoration.
- Application:
  - `AddOneTimeEarningCommand` / `RemoveOneTimeEarningCommand` reroute through engine.
  - `BulkImportOneTimeEarningsCommand` / `BulkImportReimbursementsCommand` reroute through engine.
  - `SetLopCommand.RecomputeEmployee` replaced by shared helper (fixes hardcoded `IsTaxable:true` + dropped `ConsiderForEsi`).
  - `IPayrollCostCalculator` extracted; used by initiation, LOP, all imports.
- Backfill: existing components matching `EarningType in (Bonus, Commission, LeaveEncashment, Arrears)` flipped to `IsOneTime = true`.
- API: new query `GET /salary-components/one-time?category=earning|deduction` for operator dropdown.
- Frontend:
  - `AddEarningModal` / `AddDeductionModal` gain `IsOneTime` checkbox.
  - `VariableInputsPanel` adds **Add Earning** and **Add Deduction** buttons + modal.
  - Reimbursement modal stays as separate flow (Zoho parity).
- Audit: row inserted in `AuditLog` per add/remove (entity = `PayrunComponentBreakdown`).
- Tests: engine fixtures, handler tests, validators, E2E happy paths.

Out of scope (this plan):

- Variable Pay component flow (separate Zoho concept).
- Multi-component one-time payout *run* (covered by future “Off Cycle Payroll Run” plan).
- TDS worksheet UI rework (separate fix exists; this plan keeps worksheet refresh as a side effect of engine recompute).
- Frontend RHF / Zod migration (codebase uses `useState` — match existing pattern).

## 4. Architecture Decisions

### 4.1 Why a component-driven model (not freeform amount + tags)

Each one-time entry inherits all statutory flags from its parent `SalaryComponent`. Operator cannot mis-tag at runtime. This matches Zoho’s mental model and lets all calculation paths converge on one engine call.

### 4.2 `IsOneTime` flag vs `PayType.FlatAmount`

`PayType.FlatAmount` already exists but means “absolute number, not a formula” — orthogonal concern (a *monthly* fixed-amount allowance is also FlatAmount). Adding a separate `IsOneTime: bool` is correct because:

- It controls *eligibility* in the run-time dropdown (filter `IsOneTime = true`).
- It excludes the component from the salary structure builder (a one-time bonus must never become part of recurring salary).
- It coexists with `PayType.FlatAmount`: a one-time bonus is typically (`IsOneTime: true`, `PayType: FlatAmount`, `FormulaType: Fixed`).

### 4.3 Why store `IsTaxable` / `ConsiderForEsi` on the breakdown row

The component definition can change after the run is created (admin edits component flags mid-quarter). The payroll run must be **deterministic** — re-running the engine on the same run must produce the same result. Storing flags inline on the breakdown row freezes them at entry time and matches CLAUDE.md’s “Reprocessing a payroll run must produce identical output given identical inputs.”

### 4.4 Engine call site — recompute the whole employee

When an operator adds a ₹10k bonus, naïvely we could “delta” the changes. We do not. Instead we rebuild the full `Components` list (existing breakdowns minus deletions plus new entries) and call `PayrollEngine.Compute([empInput], runInput, config)`. This is the same path `SetLopCommand` already uses (line 162) and the same path will handle TDS YTD aggregation correctly.

Performance: **per-employee engine cost has not been benchmarked yet**. Plan §10 Phase 2 includes a perf gate: bench `PayrollRecomputeService.RecomputeEmployeeAsync` on a representative 1-employee scenario against local Postgres. Required gate: ≤ 20 ms p95 per employee. If bench exceeds 20 ms, increase Hangfire chunk size cap and surface progress per chunk. For interactive add the cost is one call and irrelevant.

### 4.5 Run-total recalculation strategy

After each mutation:

```
totals = activeEmployees.Aggregate(CalculatePayrollCost)
run.UpdateFinancialSummary(totals)
```

One helper. No copy-paste formulas. Tested independently.

### 4.6 Reimbursement stays separate

Reimbursement has different defaults (`IsTaxable: false`, no PF/ESI/PT) and is a separate workflow in Zoho (uses a “Report Number” instead of a component code). Folding it into one-time earnings would force a component dropdown where none exists. Keep its dedicated command; only refactor it to use the same `IPayrollRecomputeService` so totals stay consistent.

### 4.7 Migration safety

`IsOneTime` is added with `default false` for all existing rows (correct: no existing component should be one-time until backfilled).
Breakdown columns `IsTaxable`, `ConsiderForEsi` default to `true` and `false` respectively (matches today’s implicit assumption that all recurring components are taxable; ESI false is conservative — it will be recomputed on next mutation if wrong). Backfill script populates correct values from each row’s linked `SalaryComponent`.

## 5. Data Model Changes

### 5.1 `SalaryComponent`

Add column:

```csharp
// Domain/Entities/SalaryComponent.cs
public bool IsOneTime { get; private set; }
```

Update factories:

- `CreateEarning(..., bool isOneTime = false)` — adds optional param at end.
- `CreateDeduction(..., bool isOneTime = false)` — same.

EF config (`SalaryComponentConfiguration.cs`):

```csharp
builder.Property(s => s.IsOneTime).IsRequired().HasDefaultValue(false);
builder.HasIndex(s => new { s.TenantId, s.IsOneTime, s.Category, s.IsActive });
```

Migration `20260524100000_AddIsOneTimeToSalaryComponent`:

- `Up`: AddColumn `is_one_time bool NOT NULL DEFAULT false`; AddIndex.
- `Up` follow-up SQL backfill:

```sql
UPDATE salary_components
SET is_one_time = true
WHERE category = 'Earning'
  AND earning_type IN ('Bonus', 'Commission', 'LeaveEncashment', 'ArrearsEarning');
```

- `Down`: DropIndex; DropColumn.

### 5.2 `PayrunComponentBreakdown`

Add columns (all four flags needed to fully restore `SalaryComponentInput` from breakdown without component re-lookup — determinism requirement per §4.3):

```csharp
public bool IsTaxable { get; private set; }
public bool ConsiderForEsi { get; private set; }
public bool CalculateOnProRata { get; private set; }
public EpfInclusionRule EpfInclusionRule { get; private set; }
```

Without `CalculateOnProRata`, engine cannot tell whether a component must shrink under LOP. Without `EpfInclusionRule`, components with `OnlyWhenPfWageBelowLimit` cannot be correctly excluded from PF base after recompute. Both gaps surface only after edits, silently mis-stating statutory totals — same class of bug as the existing hardcoded `IsTaxable:true`.

Update `Create()` factory signature to accept all four. Update `PayrunComponentBreakdownConfiguration` to add three bool columns + one string-converted enum column (`varchar(40)`).

Migration `20260524110000_AddFullStatutoryFlagsToBreakdown`:

- `Up`: 
  - AddColumn `is_taxable bool NOT NULL DEFAULT true`
  - AddColumn `consider_for_esi bool NOT NULL DEFAULT false`
  - AddColumn `calculate_on_pro_rata bool NOT NULL DEFAULT true`
  - AddColumn `epf_inclusion_rule varchar(40) NOT NULL DEFAULT 'Always'`
- `Up` backfill (raw SQL):

```sql
UPDATE payrun_component_breakdowns b
SET is_taxable           = COALESCE(c.is_taxable, true),
    consider_for_esi     = COALESCE(c.consider_for_esi, false),
    calculate_on_pro_rata = COALESCE(c.calculate_on_pro_rata, true),
    epf_inclusion_rule   = COALESCE(c.epf_inclusion_rule, 'Always')
FROM salary_components c
WHERE b.salary_component_id = c.id;
-- Reimbursement rows (salary_component_id IS NULL): leave defaults.
-- They are filtered out before engine call (see §6.3) so flag values are inert.
```

- `Down`: DropColumn all four.

### 5.3 `EarningType` enum

No change. Already contains Bonus, Commission, LeaveEncashment, ArrearsEarning, NotInList, Other.

### 5.4 Audit table

No new entity. Use existing `AuditLog`:

```csharp
audit.Action = "AddOneTimeEarning" | "RemoveOneTimeEarning" | "BulkImportEarnings" | …;
audit.EntityType = "PayrunComponentBreakdown";
audit.EntityId = breakdown.Id;
audit.NewValue = JsonSerializer.Serialize(new { ComponentId, Amount, RunId, EmployeeId });
```

## 6. Domain & Application Changes

### 6.1 New shared service — `IPayrollRecomputeService`

Location: `src/Payroll.Application/Services/IPayrollRecomputeService.cs` (interface) + `PayrollRecomputeService.cs` (impl).

Contract:

```csharp
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
    TdsWorksheet RefreshedWorksheet);
```

Implementation rules:

1. **Load**: run, employee, statutory config (cached), all breakdowns where `IsDeleted = false`, prior-employer YTD opening, current employer YTD (gross + TDS) aggregated from earlier-month finalised runs.
2. **Partition** breakdowns:
   - **Reimbursement rows** (`SalaryComponentId IS NULL` OR `ComponentCode = 'REIMBURSEMENT'`) → excluded from engine input. Their sum becomes `RecomputeResult.ReimbursementsAmount` — added by caller to `NetPay` *after* engine returns. Matches Zoho: reimbursement is paid in net but not part of gross / not statutory.
   - **All other rows** (recurring + one-time earnings + one-time deductions) → mapped to `SalaryComponentInput`.
3. **Map** each non-reimbursement breakdown using stored flags:
   ```csharp
   new SalaryComponentInput(
       ComponentId: b.SalaryComponentId ?? Guid.Empty,
       Code: b.ComponentCode,
       Amount: b.FullAmount,
       IsTaxable: b.IsTaxable,
       ConsiderForEpf: b.ConsiderForEpf && EpfRuleMatches(b.EpfInclusionRule, ...),
       ConsiderForEsi: b.ConsiderForEsi,
       CalculateOnProRata: b.CalculateOnProRata,
       IsFlat: false,
       ShowInPayslip: b.ShowInPayslip)
   ```
   One-time entry creation already sets `CalculateOnProRata = false` per §6.3 — engine then keeps full amount under LOP.
4. **Compute**: `PayrollEngine.Compute([empInput], runInput, config)[0]`.
5. **Refresh `TdsWorksheet`** as a side effect using engine output + correct YTD (fixes prior YTD=0 bug in `SetLopCommand:140`):
   ```csharp
   var worksheet = TdsWorksheet.FromEngine(engine.TDS, currentYtdTds, priorYtdTds, ...);
   await tdsWorksheetRepo.UpsertAsync(runId, employeeId, worksheet, ct);
   ```
6. Return `RecomputeResult`. **Caller** persists `PayrunEmployee.UpdateComputedAmounts(...)` (uses `engine.NetPay + ReimbursementsAmount` for the final net) and recomputes run totals via `IPayrollCostCalculator`.

Why service writes worksheet: every mutation that touches the engine must keep the worksheet consistent. Putting the write at one site eliminates per-handler drift (prior audit found `AddOneTimeEarningCommand` skipped it entirely; `SetLopCommand` did it with YTD=0).

### 6.2 New shared helper — `IPayrollCostCalculator`

Location: `src/Payroll.Application/Services/IPayrollCostCalculator.cs`.

```csharp
public interface IPayrollCostCalculator
{
    PayrollCostSnapshot Calculate(IReadOnlyList<PayrunEmployee> employees);
}

public record PayrollCostSnapshot(
    decimal TotalGross,
    decimal TotalNet,
    decimal TotalEmployerPf,
    decimal TotalEmployerEps,
    decimal TotalEmployerEsi,
    decimal TotalLwfEmployer,
    decimal TotalGratuity,
    decimal TotalPayrollCost);
```

Formula matches today’s **initiation** version (6 components). Every handler that mutates totals calls `Calculate` and assigns to `run.UpdateFinancialSummary(snapshot)`.

### 6.3 Command refactors

#### `AddOneTimeEarningCommand`

New handler logic:

1. Validate run is `Draft`, employee not `Skipped`.
2. Load `SalaryComponent` by `req.ComponentId` — require `IsOneTime = true`, `IsActive = true`, `TenantId` matches.
3. Create `PayrunComponentBreakdown` with **all** flags copied from component (`IsTaxable`, `ConsiderForEpf`, `ConsiderForEsi`, `ShowInPayslip`) and `IsOneTimeEarning = true`, `CalculateOnProRata = false`.
4. Persist breakdown.
5. Call `IPayrollRecomputeService.RecomputeEmployeeAsync(runId, employeeId)`.
6. Persist `PayrunEmployee.UpdateComputedAmounts(...)` from engine result.
7. Recompute run totals via `IPayrollCostCalculator`.
8. Write `AuditLog` entry.
9. Return breakdown id.

#### `RemoveOneTimeEarningCommand`

- Mark breakdown soft-deleted.
- Call `RecomputeEmployeeAsync` (engine now sees breakdowns sans removed row).
- Persist + recompute totals + audit.

#### `BulkImportOneTimeEarningsCommand`

- Parse CSV once (existing logic).
- Per row: validate component is `IsOneTime = true`. Reject otherwise into `errors[]`.
- Insert all breakdowns in one `SaveChangesAsync` per chunk.
- Group by `EmployeeId`; call `RecomputeEmployeeAsync` per affected employee.
- One `IPayrollCostCalculator.Calculate` at the end of the chunk.

#### `BulkImportReimbursementsCommand`

- Persist breakdowns as today (`SalaryComponentId = null`, `ComponentCode = "REIMBURSEMENT"`). New flag columns receive their defaults (`IsTaxable=true, ConsiderForEsi=false, …`) — values are inert because the row is filtered out before engine input per §6.1 step 2.
- Call `RecomputeEmployeeAsync` per affected employee. Service returns `RecomputeResult.ReimbursementsAmount = sum(reimbursement breakdowns)`. Caller sets `PayrunEmployee.NetPay = engine.NetPay + reimbursementsAmount`. `GrossPay = engine.Gross.GrossWage` (excludes reimbursement). `ReimbursementsAmount = sum`.
- Totals via `IPayrollCostCalculator`. Reimbursement does **not** flow into employer payroll cost.

#### `SetLopCommand`

- Drop its private `RecomputeEmployee`. Call `IPayrollRecomputeService` instead.
- Fixes `IsTaxable:true` hardcode bug + dropped `ConsiderForEsi` bug as side effect.
- Totals via `IPayrollCostCalculator`.

#### `InitiatePayrollRunCommand`

- Use `IPayrollCostCalculator.Calculate` at line 356 (replace inline 6-component sum). Identical numeric result — pure refactor.
- (Performance batching covered in separate plan `010-feat-background-jobs-import-initiation`; do not bundle.)

### 6.4 Validators (FluentValidation)

`AddOneTimeEarningCommandValidator`:

```csharp
RuleFor(x => x.RunId).NotEmpty();
RuleFor(x => x.EmployeeId).NotEmpty();
RuleFor(x => x.ComponentId).NotEmpty();
RuleFor(x => x.Amount).GreaterThan(0m).LessThanOrEqualTo(10_000_000m); // ₹1 cr sanity cap
```

`BulkImportOneTimeEarningsCommandValidator`:

```csharp
RuleFor(x => x.RunId).NotEmpty();
RuleFor(x => x.CsvContent).NotEmpty();
```

CSV row-level validation stays inside the handler (per-row error reporting model).

### 6.5 Queries

New query: `GetOneTimeComponentsQuery(Guid tenantId, ComponentCategory category)`.

Returns `List<SalaryComponentSummaryDto>` filtered by `IsOneTime = true`, `IsActive = true`, `Category = category`. Cached for 5 min via existing `IDistributedCache` registration (key `tenant:{id}:one-time-components:{category}`). Cache invalidated by `CreateEarningCommand`, `UpdateComponentCommand`, etc. (existing patterns).

## 7. API Surface

### 7.1 New endpoints

| Method | Route                                                                                  | Handler                                |
| ------ | -------------------------------------------------------------------------------------- | -------------------------------------- |
| POST   | `/api/v1/payroll-runs/{runId}/employees/{employeeId}/deductions`                       | `AddOneTimeDeductionCommand`           |
| GET    | `/api/v1/salary-components/one-time?category=earning`                                  | `GetOneTimeComponentsQuery(Earning)`   |
| GET    | `/api/v1/salary-components/one-time?category=deduction`                                | `GetOneTimeComponentsQuery(Deduction)` |

### 7.2 Existing endpoints (refactor, no contract change)

| Method | Route                                                                  | Change                                |
| ------ | ---------------------------------------------------------------------- | ------------------------------------- |
| POST   | `/api/v1/payroll-runs/{runId}/employees/{eid}/earnings`                | Now reroutes through engine recompute |
| DELETE | `/api/v1/payroll-runs/{runId}/employees/{eid}/earnings/{breakdownId}`  | Same                                  |
| POST   | `/api/v1/payroll-runs/{runId}/import/earnings`                         | Same                                  |
| POST   | `/api/v1/payroll-runs/{runId}/import/reimbursements`                   | Same                                  |

### 7.3 DTO updates

`SalaryComponentSummaryDto` add field:

```typescript
isOneTime: boolean;
```

`ComponentBreakdownDto` add fields:

```typescript
isTaxable: boolean;
considerForEsi: boolean;
```

`AddOneTimeEarningRequest` — no change (componentId + amount).

## 8. Frontend Changes

### 8.1 Settings → Salary Components

`AddEarningModal.tsx`:

- Add checkbox `IsOneTime` to the “behavior” block (after `ShowInPayslip`).
- When checked, disable `formulaType` selector (force `Fixed`), disable `payType` to `FlatAmount`, hide `calculateOnProRata` (always false).
- Tooltip: “One-time components are not part of recurring salary; they appear in the ‘Add Earning’ dropdown inside a payroll run.”

`AddDeductionModal.tsx`:

- Add same `IsOneTime` checkbox.
- Hide `deductionFrequency` when checked (one-time deductions are always single-shot).

`EditComponentModal.tsx`:

- `IsOneTime` is **immutable once `IsAssociatedWithEmployee = true`** — same pattern as other locked flags. Otherwise editable.

`SalaryComponentsPage.tsx`:

- Component list adds “One-time” badge for `isOneTime: true` rows.
- Optional filter chip: All / Recurring / One-time.

### 8.2 Payroll Run → Variable Inputs drawer

`VariableInputsPanel.tsx`:

- Above existing one-time earnings list, render **“+ Add Earning”** button.
- New modal `AddOneTimeEarningModal.tsx`:
  - Component dropdown (uses new `GET /salary-components/one-time?category=earning`).
  - Amount input (number, formatted with `formatINR()` on blur display).
  - Submit → `POST /payroll-runs/.../earnings` body `{ componentId, amount }`.
  - On success: invalidate `['variable-inputs', runId, employeeId]` and `['run-employees', runId]`.
- Symmetric **“+ Add Deduction”** button + modal calling `POST /payroll-runs/.../deductions`.
- Display now shows: component name + “(One-time)” badge + impact preview chips (“Taxable”, “PF”, “ESI”) read from new DTO fields.

Reimbursement section unchanged (already has its own button per separate flow).

### 8.3 Status gating

All new add buttons hidden when `payrollRun.status !== 'Draft'`. Matches existing gate in `EmployeeSummaryTable.tsx`.

## 9. Testing Plan

### 9.1 Engine tests (`tests/Payroll.Engine.Tests`)

Already cover statutory math. Add fixtures that exercise:

- One non-taxable component → `TaxableGrossWage` excludes it, TDS unchanged.
- One bonus component (`IsTaxable: true, ConsiderForEpf: false, ConsiderForEsi: false`) → `AnnualProjectedTaxableGross` includes it × monthsRemaining, TDS bumps.
- One bonus pushing PT slab → `PTResult.Amount` increments to next slab (verify in standard Monthly PT state, e.g. Karnataka).
- One bonus pushing ESI gross above ₹21k → ESI auto-exempt for the month.
- One non-prorated bonus + LOP=10 days → bonus full amount preserved while prorated components shrink.
- **PT half-year-split states (Tamil Nadu, West Bengal)**: confirm expected behavior of a one-time bonus added mid half-year. Today engine annualizes via `gross × HalfYearTotalMonths` for slab lookup — a ₹50 k bonus could push slab. Compare to Zoho audit (`docs/ba-audit/statutory-components/`) to decide whether to:
  - **(a)** treat bonus identically (Zoho parity unknown — likely incorrect; bonus is single-shot, not recurring half-year baseline), OR
  - **(b)** exclude one-time earnings from PT half-year annualization base (mark `IsFlat = true` on `SalaryComponentInput` for one-time rows so PT splits on recurring baseline only).
  - **Resolution required before writing the fixture.** Owner verifies against Zoho before merging Phase 1.

### 9.2 Handler tests (`tests/Payroll.Application.Tests`)

`AddOneTimeEarningCommandHandlerTests`:

- Happy path: bonus increases `GrossPay`, `TdsAmount`, `EmployeePf` (if applicable). Asserts engine called once.
- Component not one-time → rejects with `ValidationException`.
- Run not Draft → rejects.
- Tenant mismatch → rejects.

`BulkImportOneTimeEarningsCommandHandlerTests`:

- CSV with two valid + one invalid rows → two breakdowns persisted, error[] has one entry.
- All rows for same employee → engine recompute called once (not per-row).

`RemoveOneTimeEarningCommandHandlerTests`:

- Removes breakdown, recompute restores pre-add values exactly.

`SetLopCommandHandlerTests` (existing + new):

- New test: component with `IsTaxable: false` (e.g. medical reimbursement) → after LOP, remains non-taxable (today’s bug regression test).
- New test: component with `ConsiderForEsi: true` → ESI base shrinks proportional to LOP.

`PayrollCostCalculatorTests`:

- 6-component formula matches initiation expectations.
- Empty employee list → all zeros.

### 9.3 Integration tests (`tests/Payroll.Infrastructure.Tests` w/ Testcontainers)

- Migration `Up` then `Down` round-trips cleanly.
- Backfill SQL populates `IsTaxable` / `ConsiderForEsi` from joined component.

### 9.4 Frontend tests (`web/src/__tests__`)

- Vitest test for `AddOneTimeEarningModal`: component dropdown renders, submit calls correct endpoint with payload.
- Vitest test for `VariableInputsPanel`: invalidation keys fire on add success.

### 9.5 E2E (`e2e/`)

Playwright scenario:

1. Login admin → Settings → create component “Diwali Bonus” (Earning, IsOneTime=true, IsTaxable=true).
2. Navigate to Draft payroll run → click employee → Add Earning → pick “Diwali Bonus” → enter ₹10,000 → save.
3. Assert: in drawer, `GrossPay` increased by ₹10,000 and `TdsAmount` increased by > 0 (exact amount asserted via stored test CTC fixture).
4. Approve run → assert payslip PDF shows Diwali Bonus line.

## 10. Rollout & Sequencing

Phases keep main branch deployable after each merge.

**Phase 1 — Cost-summary consistency fix (PR 1)**
Schema migration for `SalaryComponent.IsOneTime` + four breakdown flag columns. Backfill SQL. `IPayrollCostCalculator` extracted and switched in `Initiate`, `SetLop`, `BulkImportOneTimeEarnings`, `BulkImportReimbursements`. **This is a behavior change**: `PayrollCost` on existing Draft runs will jump on first edit because they previously used the 3-component formula and now use the canonical 6-component formula (gross + employerPf + EPS + employerEsi + employerLwf + gratuity). Document in CHANGELOG + post a Slack-grade ops note: "Draft runs created before PR 1 will see `PayrollCost` recompute to true value on next edit. Finalised runs are immutable per project rule and unaffected."
*Verify: parameterised test — same employee set produces identical `PayrollCost` snapshot from all 4 call sites.*

**Phase 2 — Recompute service (PR 2)**
`IPayrollRecomputeService` extracted from `SetLopCommand.RecomputeEmployee`. `SetLopCommand` now calls it. `RecomputeEmployee` bugs (`IsTaxable:true`, dropped `ConsiderForEsi`) fixed by reading from breakdown columns. Add tests for non-taxable + ESI scenarios.
*Verify: LOP regression tests pass; existing TDS worksheet values match pre-refactor for taxable-only fixtures.*

**Phase 3 — Reroute commands (PR 3)**
`AddOneTimeEarningCommand`, `RemoveOneTimeEarningCommand`, `BulkImportOneTimeEarningsCommand`, `BulkImportReimbursementsCommand` now call `IPayrollRecomputeService`. Existing endpoints behave correctly even before UI changes.
*Verify: handler tests pass; one E2E run created via API exhibits correct TDS bump on bonus.*

**Phase 4 — Settings UI (PR 4)**
`IsOneTime` checkbox in `AddEarningModal` / `AddDeductionModal` / `EditComponentModal`. Backfill flag visible in component list. `GET /salary-components/one-time` endpoint live.
*Verify: admin can create one-time bonus; appears in API filter.*

**Phase 5 — Payroll Run UI (PR 5)**
Add-Earning + Add-Deduction modals in `VariableInputsPanel`. Status gating. Impact-preview chips.
*Verify: full operator flow demoable end-to-end.*

**Phase 6 — Hardening (PR 6)**
E2E test added. Audit log entries verified. Coverage thresholds raised for `Payroll.Application` to 80 % (CLAUDE.md target).

Each PR ships behind no feature flag — schema additive, behavior fixes already shippable per-phase.

## 11. Risks & Mitigations

| Risk                                                                                                                   | Likelihood | Mitigation                                                                                                                  |
| ---------------------------------------------------------------------------------------------------------------------- | ---------- | --------------------------------------------------------------------------------------------------------------------------- |
| Engine recompute on every one-time-earning add adds latency on bulk imports.                                           | Medium     | Per-employee recompute is ~5 ms; for 10 k employees with 1 entry each = ~50 s on cold path. Run via Hangfire (already wired). |
| Existing payroll runs’ totals shift when first reopened (engine now correctly reflects statutory truth).               | Medium     | Document as one-time correction. Finalised runs are immutable per CLAUDE.md — only Draft runs affected.                     |
| Backfill misclassifies a custom-named component as one-time.                                                           | Low        | Backfill keys on `EarningType` enum, not name. Admin can toggle `IsOneTime` later via Edit modal.                            |
| Operator confusion when bonus of ₹10 000 yields net < ₹10 000.                                                         | High       | UI shows preview before submit: Gross +₹10 000, TDS +₹X, PF +₹Y, Net +₹Z. Surface the breakdown so user is not surprised.   |
| Engine `IsTaxable` fix changes historical TDS computations on draft runs created before this PR.                       | Medium     | Acceptable — was a bug. Document in CHANGELOG. Drafts can be re-initiated.                                                  |
| `ConsiderForEsi` defaulting to `false` for pre-migration breakdowns misclassifies them.                                | Medium     | Backfill SQL joins on `salary_components.consider_for_esi`. Default `false` only used for orphan rows (reimbursements).     |
| Two engine recomputes racing on the same employee (concurrent operator clicks).                                        | Low        | Existing `IUnitOfWork` is request-scoped; row-level locking via `xmin` on `payrun_employees`. Add optimistic-concurrency token in PR 3. |

## 12. Resolved Decisions

1. **`CalculateOnProRata` on breakdown** — RESOLVED §5.2: yes, stored. `EpfInclusionRule` also stored.
2. **Reimbursement engine handling** — RESOLVED §6.1 step 2 + §6.3: filter reimbursement rows out of engine input; service returns `RecomputeResult.ReimbursementsAmount`; caller adds to `NetPay` post-engine; `GrossPay` excludes reimbursement.
3. **One-time deduction cap** — RESOLVED: no hard cap. Validator allows `Amount > 0 && <= 10_000_000`. UI surfaces warning chip "Net pay will go negative" if pre-recompute net < amount. Operator can still proceed (Zoho parity — allows it). Hard cap deferred to product decision.
4. **Audit log granularity** — RESOLVED: per breakdown. One `AuditLog` row per add / remove. Bulk import writes one summary row plus per-breakdown rows for forensic replay.

## 13. Remaining Verification Items (block Phase 1 merge)

- **PT half-year-split behavior with one-time bonus** — confirm Zoho parity before Phase 1 test fixture is written (§9.1). Owner: implementer.
- **Engine perf bench** — single-employee recompute against local Postgres. Required gate: ≤ 20 ms p95 (§4.4). Owner: implementer, run before Phase 2 merge.

## 14. Acceptance Criteria

- A taxable bonus added in Draft → `PayrunEmployee.TdsAmount` increases by an amount equal to `(bonusAmount × marginalSlabRate × monthsRemaining/monthsRemaining)` to within ₹1 (rounding).
- An EPF-applicable bonus added → `EmployeePf` and `EmployerPf` increase per the EPF base rules, capped at ₹15 k wage if `EpfRestrictEmployerWage = true`.
- A bonus pushing the employee’s monthly gross > ₹21 000 ESI cap → ESI auto-exempts for the month.
- Removing the same bonus restores all values to pre-add state byte-for-byte (deterministic recompute).
- `run.PayrollCost` calculated identically by `Initiate`, `SetLop`, `AddOneTimeEarning`, `BulkImport*` — verified by parameterised test.
- `SetLop` on a non-taxable allowance preserves `IsTaxable = false` (regression).
- Operator flow demoable end-to-end on a fresh dev environment in < 60 s.
- All CI gates green: build, test, coverage (Engine ≥ 95 %, Application ≥ 80 %), NetArchTest, ESLint, TypeScript, Vitest, Playwright.
