---
title: "feat: Payroll Run module — full lifecycle (Phase 1 & 2 core)"
type: feat
status: active
date: 2026-05-17
origin: docs/ba-audit/payroll-run-module.md
---

# feat: Payroll Run module — full lifecycle (Phase 1 & 2 core)

## Overview

Build the Payroll Run module end-to-end: period card → Draft (variable inputs) → Approved → Paid, plus payslip PDF generation and bank advice XLS. The engine calculators (TDS, PF, ESI, PT, LWF) already exist; the missing pieces are the run lifecycle domain model, persistence, variable-input commands, approval/payment state transitions, payslip generation, and the full frontend module.

Phase 3 (insights tab, bulk imports, off-cycle runs) and Phase 4 (statutory filings — ECR, Form 24Q, PT challan) are deferred.

---

## Problem Frame

The app can configure salary structures and create employees with assigned CTCs, but cannot compute or pay salaries. The Payroll Run module is the core value delivery mechanism — without it the product has no output. The Zoho Payroll audit (`docs/ba-audit/payroll-run-module.md`) defines the exact UX and business rules to replicate.

---

## Requirements Trace

- R1. Regular monthly pay runs are system-initiated (period card) — no manual period picker.
- R2. Draft state allows variable inputs: LOP days, one-time earnings (bonus/commission/leave encashment), TDS override (with mandatory reason), skip employee (with mandatory reason).
- R3. Approval requires all hard-block pending tasks to be resolved (employees with incomplete onboarding + no salary structure).
- R4. PAN-missing employees included with soft warning; TDS computed at 20% flat (§206AA).
- R5. Approval locks variable inputs, reimbursement claims, IT declarations, POI uploads.
- R6. Record Payment (Approved → Paid) stores payment date, mode, and reference; triggers payslip generation.
- R7. Payslips generated as PDF (QuestPDF), stored in MinIO, published to employee portal on payment.
- R8. Bank Advice XLS downloadable after approval (Standard Format + major bank formats).
- R9. Delete Recorded Payment reverts Paid → Approved (not Draft); full 5-step reversal supported.
- R10. LOP proration: per-component calendar-day formula — `prorated = fullAmount × (baseDays − lopDays) / baseDays`.
- R11. All monetary values: `decimal`. Zero exceptions.
- R12. Engine remains pure — all computation in `Payroll.Engine`, no I/O.

---

## Scope Boundaries

- Regular monthly pay runs only (no off-cycle, one-time payout, resettlement in this phase).
- No Insights tab (Overall Insights / Taxes & Deductions tab UI) — data computed and stored, tab deferred.
- No bulk CSV import of LOP / earnings / reimbursements.
- No statutory filings generation (ECR, Form 24Q, PT challan).
- No employee portal (payslip view for employees themselves).
- No salary revision or arrear runs.
- No Zoho Payments / direct bank transfer integration.
- Old tax regime: `// DEFERRED: old-regime` per project rules.

### Deferred to Follow-Up Work

- Overall Insights tab + Taxes & Deductions tab frontend: backend data is stored in this phase; UI in a follow-up.
- Bulk imports (LOP, earnings, reimbursements, deductions): endpoints and `ImportModal` component.
- Off-cycle and One Time Payout run creation flows.
- Resettlement / arrear runs (triggered by salary revision).
- Statutory filings: ECR, ESI return, Form 24Q, PT challan.
- `POST /api/v1/payroll-runs/{id}/reprocess` convenience command (single-step Paid → Draft).
- Mid-month joiner auto-LOP (differentiator feature) — deferred until leave module exists.

---

## Context & Research

### Relevant Code and Patterns

- Engine already built: `src/Payroll.Engine/PayrollEngine.cs` — `Compute(employees, runInput, config)` returns `IReadOnlyList<PayrollResult>`. All 5 statutory calculators implemented.
- `src/Payroll.Engine/Calculators/GrossCalculator.cs` — currently uses `workingDaysInMonth` for LOP (needs calendar-day per-component switch; see U5).
- `src/Payroll.Domain/Entities/PayrollRun.cs` — exists with wrong state machine (Pending → Processing → Draft → Finalised). States Approved, Paid, Deleted missing.
- `src/Payroll.Domain/Enums/PayrollRunStatus.cs` — has Pending, Processing, Draft, Finalised, Failed; needs Approved, Paid, Deleted added.
- `src/Payroll.Domain/Entities/EmployeeSalaryStructure.cs` — holds AnnualCTC, TemplateId, EffectiveFrom; used to build engine inputs.
- `src/Payroll.Domain/Entities/PaySchedule.cs` — holds PayDateDay, FirstPayPeriodMonth/Year, SalaryCalculationMethod, IsLockedAfterPayrun.
- QuestPDF 2024.10.4 already referenced in `src/Payroll.Infrastructure/Payroll.Infrastructure.csproj`.
- MediatR, FluentValidation pipeline, MinIO service, Hangfire — all wired in infrastructure. Follow patterns in `src/Payroll.Application/Commands/Employees/`.
- EF config pattern: `IEntityTypeConfiguration<T>` in `src/Payroll.Infrastructure/Persistence/EntityConfigurations/`. Soft-delete filter via `HasQueryFilter`.
- Frontend patterns: React Query for data fetching, slide-in panels (see `EmployeeOverviewTab.tsx`), modal dialogs (see `InlineCreateModal.tsx`), design system from `docs/design-system/DESIGN-SYSTEM.md`.

### Institutional Learnings

- Decimal for all monetary values — project-wide invariant. Never float/double.
- Engine is pure: no async, no DI, no EF. All inputs passed as parameters.
- Schema-per-tenant: every EF entity is tenant-scoped. PayrollRun already has `TenantId`.
- PAN/bank account encrypted at rest. Not relevant to payroll run (no new PII stored on run entities).
- Migrations need reversible Up + Down. Test rollback.

### External References

- Zoho Payroll audit: `docs/ba-audit/payroll-run-module.md` (primary source for all business rules and UX).
- LOP proration formula confirmed: `docs/ba-audit/payroll-run-module.md` § 18 (proration section).
- TDS new regime slabs: in `StatutoryConfig` from DB — not hardcoded. Engine reads from `config.NewRegimeSlabs`.

---

## Key Technical Decisions

- **State machine**: Keep Pending (period card shown, no run created yet) and Draft (run created, editable). Add Approved, Paid, Deleted. Remove Processing and Finalised — engine computation is synchronous at initiation, so no intermediate Processing state needed in the new model. Finalised is replaced by Paid.
- **Per-component LOP proration**: Refactor `GrossCalculator` to compute each component's prorated amount individually (needed for `PayrunComponentBreakdown` storage and payslip table). Use calendar days (`baseDays` = days in the calendar month), matching Zoho's confirmed formula.
- **PayrunEmployee as the unit of truth**: After initiation, one `PayrunEmployee` row per eligible employee stores all computed amounts (gross, net, each statutory). When LOP changes, the handler re-runs the engine for that employee and updates the row + component breakdown.
- **Payslip generation on RecordPayment**: Triggered as a Hangfire background job from `RecordPaymentCommandHandler`. Generates PDFs for all `Active` PayrunEmployees, stores to MinIO, sets `Payslip.IsPublished = true`. Not inline — keeps payment confirmation fast.
- **TdsWorksheet stored per employee per run**: Captures full TDS computation detail at run time. Basis for future Form 24Q. Stored on approve (not draft) so it reflects the locked final inputs.
- **Bank Advice XLS**: Generated on-demand (not stored) using ClosedXML (already in the .NET ecosystem; check if referenced, else add). Standard Format only in Phase 1; bank-specific formats deferred.
- **No PayrunEmployee soft-delete**: Skipped employees remain in the table with `Status = Skipped`. `IsDeleted` not used on this entity.
- **API versioning**: `v1` prefix consistent with existing controllers.
- **Frontend routing**: `/pay-runs` (list/period card), `/pay-runs/:id` (detail), `/pay-runs/history` (history). Tabs handled by React state, not URL params (simpler than Zoho's `?selectedTab=` approach).

---

## Open Questions

### Resolved During Planning

- **Does ClosedXML exist in the project?** Not yet referenced — add `ClosedXML` NuGet to `Payroll.Infrastructure`. QuestPDF handles PDF; ClosedXML handles XLS. Both are MIT-licensed.
- **What triggers `PayrunEmployee` population?** `InitiatePayrollRunCommand` — fetches all active employees with a complete salary structure, runs engine once to get initial (zero-LOP) computed amounts, persists `PayrunEmployee` + `PayrunComponentBreakdown` rows.
- **When is TdsWorksheet written?** On `ApprovePayrollRunCommand` — inputs are locked at that point, so the TDS computation reflects final values.
- **Does PaySchedule already have `PayDay` computation?** PaySchedule has `PayDateType` and `PayDateDay`. A helper method `GetPayDay(year, month)` needs to be added (or placed in Application layer) — not in Engine.
- **Mid-month joiner auto-LOP?** Deferred. Admin enters LOP manually in V1 (same as Zoho).

### Deferred to Implementation

- Exact ClosedXML cell styles for bank-specific format columns — implement Standard Format first, check if ClosedXML needs upgrade.
- Whether `SalaryCalculationMethod` on PaySchedule affects LOP base (calendar vs. actual working days) — for V1 always use calendar days matching Zoho's confirmed behavior.

---

## High-Level Technical Design

> *Directional guidance for review, not implementation specification.*

```
                    ┌──────────────────────────────────────────┐
                    │            Pay Run Lifecycle              │
                    └──────────────────────────────────────────┘

  [Period Card Query]                         [Run Payroll page]
  GET /current-period ──────────────────────► PeriodCard component

  POST /initiate ──────────────────────────► PayrollRun (Draft)
                                             PayrunEmployee × N (one per eligible emp)
                                             PayrunComponentBreakdown × N × M

  PUT /{id}/employees/{eid}/lop ───────────► Re-run GrossCalculator for employee
                                             Update PayrunEmployee amounts
                                             Update PayrunComponentBreakdown rows

  POST /{id}/approve ─────────────────────► Guard: no hard-block pending tasks
                                             Write TdsWorksheet per employee
                                             PayrollRun.Status → Approved
                                             PayrollRunAuditLog entry

  POST /{id}/record-payment ──────────────► PayrollRun.Status → Paid
                                             Hangfire: GeneratePayslipJob(runId)
                                               └► QuestPDF → MinIO per employee
                                               └► Payslip entity (IsPublished=true)

  GET /{id}/bank-advice ──────────────────► ClosedXML XLS → HTTP response (sync)
```

**State machine:**
```
[Period card visible — no run entity yet]
         │ POST /initiate
         ▼
      DRAFT  ◄────────────────── POST /reject-approval
         │                               ▲
         │ POST /approve                 │
         ▼                               │
     APPROVED ──────────────────────────┘
         │
         │ POST /record-payment
         ▼
       PAID ──────[DELETE /payment]──► APPROVED

DRAFT ──────────[DELETE /{id}]──────► DELETED (soft)
```

---

## Implementation Units

- U1. **PayrollRun entity + status enum redesign**

**Goal:** Extend `PayrollRun` with the financial summary fields and approval/payment state the module needs. Update `PayrollRunStatus` enum.

**Requirements:** R1, R6, R9

**Dependencies:** None

**Files:**
- Modify: `src/Payroll.Domain/Enums/PayrollRunStatus.cs`
- Modify: `src/Payroll.Domain/Entities/PayrollRun.cs`
- Modify: `src/Payroll.Domain/Enums/` — add `PayrollRunType.cs`

**Approach:**
- Add to `PayrollRunStatus`: `Approved`, `Paid`, `Deleted`. Rename `Finalised` → `Paid` (migration maps old values). Keep `Failed`. Remove `Processing` and `Pending` from enum — they're replaced by the period-card design (no run entity in "pending" state).
- Add `PayrollRunType` enum: `Regular`, `OffCycle`, `OneTimePayout`, `Resettlement`.
- Add fields to `PayrollRun` entity: `Type`, `PayDay`, `PayrollCost`, `TotalNetPay`, `TotalEmployerPf`, `TotalEmployerEsi`, `TotalEdli`, `TotalTds`, `TotalPt`, `ApprovedAt`, `ApprovedBy`, `ApprovalRejectionReason`, `PaymentDate`, `PaymentMode`, `PaymentReference`, `PaidAt`, `BankAdviceFileKey`.
- Add domain methods: `Approve(actorId)`, `RejectApproval(reason, actorId)`, `RecordPayment(date, mode, reference, actorId)`, `DeletePayment(actorId)`, `Delete(actorId)`. Each validates current state before transitioning.
- Remove `MarkProcessing()` and `MarkDraft()` (those were for the old async-engine flow).
- Add `StatutoryConfigSnapshot` (string, JSON column) to `PayrollRun` — set at initiation time, read by all subsequent engine calls for reproducibility.
- Add domain method `Delete(actorId)` → Status = Deleted (soft) — guard: must be Draft.

**Patterns to follow:**
- `src/Payroll.Domain/Entities/PayrollRun.cs` — existing guard + private-set pattern.

**Test scenarios:**
- Happy path: `Approve()` on Draft run → status = Approved, ApprovedAt set.
- Happy path: `RecordPayment()` on Approved run → status = Paid.
- Happy path: `DeletePayment()` on Paid run → status = Approved, payment fields cleared.
- Error path: `Approve()` on Paid run → `InvalidOperationException`.
- Error path: `RecordPayment()` on Draft run → exception.
- Edge case: `RejectApproval()` with null reason → accepted (optional field).

**Verification:** Build succeeds zero warnings. Domain tests green.

---

- U2. **PayrunEmployee + PayrunComponentBreakdown entities**

**Goal:** New entities to hold per-employee per-run computed amounts and per-component breakdown.

**Requirements:** R2, R10

**Dependencies:** U1

**Files:**
- Create: `src/Payroll.Domain/Entities/PayrunEmployee.cs`
- Create: `src/Payroll.Domain/Entities/PayrunComponentBreakdown.cs`
- Create: `src/Payroll.Domain/Enums/PayrunEmployeeStatus.cs` (Active, Skipped, Withheld)
- Create: `src/Payroll.Domain/Enums/PaymentMode.cs` (BankTransfer, DirectDeposit, Cheque, Cash)

**Approach:**
- `PayrunEmployee`: fields for PayrollRunId, EmployeeId, Status, BaseDays, LopDays, ActualPayableDays, GrossPay, TaxesAmount, BenefitsAmount, ReimbursementsAmount, NetPay, EmployeePf, EmployerPf, EmployeeEsi, EmployerEsi, PtAmount, TdsAmount, TdsOverrideAmount (nullable), TdsOverrideReason (nullable), PaymentMode, PaymentDate (nullable), SkipReason (nullable), IsWithheld.
- `PayrunComponentBreakdown`: PayrollRunId, EmployeeId, SalaryComponentId, ComponentCode, ComponentName, FullAmount (pre-LOP), ProratedAmount (after LOP), IsOneTimeEarning (bool — true for bonus/commission/leave encash added via Add Earning).
- Domain methods on `PayrunEmployee`: `SetLop(lopDays, baseDays, actor)`, `SetTdsOverride(amount, reason, actor)`, `AddOneTimeEarning(componentId, amount, actor)`, `Skip(reason, actor)`, `UndoSkip(actor)`, `Withhold(actor)`, `ReleaseWithheld(actor)`.
- No soft-delete on PayrunEmployee — skipped rows remain with Status = Skipped.

**Patterns to follow:**
- `src/Payroll.Domain/Entities/EmployeeSalaryStructure.cs` — private setters, factory Create method, domain methods for state transitions.

**Test scenarios:**
- Happy path: `SetLop(2, 31)` → `LopDays = 2`, `ActualPayableDays = 29`.
- Error path: `SetLop(31, 31)` → exception (cannot LOP entire month — max LOP = baseDays − 1).
- Error path: `SetTdsOverride(amount, null)` → exception (reason mandatory).
- Error path: `Skip(null)` → exception (reason mandatory per business rule R2).
- Happy path: `Skip("reason")` → Status = Skipped. `UndoSkip()` → Status = Active.
- Error path: `UndoSkip()` on Active employee → exception.

**Verification:** Domain layer compiles zero warnings. Unit tests green.

---

- U3. **Payslip + TdsWorksheet + PayrollRunAuditLog entities**

**Goal:** Entities for generated payslip artifacts, TDS computation detail, and audit trail of state transitions.

**Requirements:** R6, R7

**Dependencies:** U1

**Files:**
- Create: `src/Payroll.Domain/Entities/Payslip.cs`
- Create: `src/Payroll.Domain/Entities/TdsWorksheet.cs`
- Create: `src/Payroll.Domain/Entities/PayrollRunAuditLog.cs`

**Approach:**
- `Payslip`: PayrollRunId, EmployeeId, PdfStorageKey (MinIO), GeneratedAt, IsPublished, NetPay, NetPayInWords, YtdDataJson (JSON column — per-component YTD amounts for the fiscal year; denormalized for PDF rendering speed).
- `TdsWorksheet`: PayrollRunId, EmployeeId, FiscalYear (int), TaxRegime ("New"), AnnualProjectedIncome, StandardDeduction, TaxableIncome, TaxBeforeRebate, Rebate87A, Surcharge, Cess, AnnualTaxLiability, YtdTdsDeducted, RemainingMonthsInFy, TdsThisMonth, HasPanOverride (bool — 20% flat if true).
- `PayrollRunAuditLog`: PayrollRunId, FromStatus, ToStatus, ActorUserId, Timestamp, Reason (nullable). Written by domain methods on `PayrollRun` via domain events or directly in handlers (simpler: directly in handlers for V1).

**Patterns to follow:**
- `src/Payroll.Domain/Entities/PayrollRun.cs` — private constructor, factory Create.

**Test scenarios:**
- Test expectation: none — pure data holders with no behavioral logic beyond factory construction.

**Verification:** Domain compiles zero warnings.

---

- U4. **EF configurations + migration**

**Goal:** Persist all new entities in the tenant schema with correct column types, indexes, and FK constraints.

**Requirements:** R11 (decimal precision)

**Dependencies:** U1, U2, U3

**Files:**
- Create: `src/Payroll.Infrastructure/Persistence/EntityConfigurations/PayrunEmployeeConfiguration.cs`
- Create: `src/Payroll.Infrastructure/Persistence/EntityConfigurations/PayrunComponentBreakdownConfiguration.cs`
- Create: `src/Payroll.Infrastructure/Persistence/EntityConfigurations/PayslipConfiguration.cs`
- Create: `src/Payroll.Infrastructure/Persistence/EntityConfigurations/TdsWorksheetConfiguration.cs`
- Create: `src/Payroll.Infrastructure/Persistence/EntityConfigurations/PayrollRunAuditLogConfiguration.cs`
- Modify: `src/Payroll.Infrastructure/Persistence/EntityConfigurations/PayrollRunConfiguration.cs` — add new columns
- Modify: `src/Payroll.Infrastructure/Persistence/PayrollDbContext.cs` — add DbSets
- Create: `src/Payroll.Infrastructure/Persistence/Migrations/{timestamp}_AddPayrollRunModule.cs` (Up + Down)

**Approach:**
- All `decimal` monetary columns: `HasColumnType("numeric(18,2)")`.
- All `DateTimeOffset` columns: `HasColumnType("timestamptz")`.
- All `DateOnly` payment dates: `HasColumnType("date")`.
- `PayrunEmployee`: index on `(PayrollRunId, EmployeeId)` unique. Index on `(PayrollRunId, Status)` for filtering.
- `PayrunComponentBreakdown`: index on `(PayrollRunId, EmployeeId)`. No unique — one employee can have many components.
- `Payslip`: unique index on `(PayrollRunId, EmployeeId)`. FK to `PayrunEmployee`.
- `TdsWorksheet`: unique index on `(PayrollRunId, EmployeeId)`.
- `PayrollRunAuditLog`: index on `PayrollRunId`. No FK cascade delete — preserve audit trail even if run soft-deleted.
- `PayrollRun` new enum columns: `HasConversion<string>()` — `PayrollRunType`, `PaymentMode`.
- `PayrollRunStatus` migration: map old `Finalised` → `Paid`; old `Pending`/`Processing` — assess in migration if any rows exist (should be none in dev).
- Migration must have a working `Down()` method.

**Patterns to follow:**
- `src/Payroll.Infrastructure/Persistence/EntityConfigurations/EmployeeSalaryStructureConfiguration.cs` — numeric precision pattern.
- `src/Payroll.Infrastructure/Persistence/EntityConfigurations/PayrollRunConfiguration.cs` — existing PayrollRun config to extend.

**Test scenarios:**
- Integration: migration Up applies without error against a fresh Testcontainers Postgres instance.
- Integration: migration Down rolls back cleanly.
- Integration: `PayrunEmployee` unique constraint rejects duplicate (runId, employeeId) insert.

**Verification:** `dotnet ef migrations script` runs without error. Migration applied; EF model validates against schema.

---

- U5. **Engine: per-component calendar-day LOP proration**

**Goal:** Refactor `GrossCalculator` to prorate each salary component individually using calendar days, and return a per-component breakdown alongside the gross total. Used by `PayrunComponentBreakdown` persistence.

**Requirements:** R10, R12

**Dependencies:** None (engine is standalone)

**Files:**
- Modify: `src/Payroll.Engine/Calculators/GrossCalculator.cs`
- Modify: `src/Payroll.Engine/Inputs/PayrollRunInput.cs` — rename `WorkingDaysInMonth` → `CalendarDaysInMonth` (breaking change, update callers)
- Modify: `src/Payroll.Engine/Inputs/SalaryComponentInput.cs` — add `IsLopProrated` bool (some components may be marked non-prorated in the org's config)
- Create: `src/Payroll.Engine/Outputs/ComponentAmountResult.cs` — `record(Guid ComponentId, string Code, decimal FullAmount, decimal ProratedAmount, bool IsOneTimeEarning)`
- Modify: `src/Payroll.Engine/Outputs/GrossResult.cs` — add `IReadOnlyList<ComponentAmountResult> ComponentBreakdown`
- Test: `tests/Payroll.Engine.Tests/Calculators/GrossCalculatorTests.cs`

**Approach:**
- `GrossCalculator.Compute()`: for each component, if `IsLopProrated` and LopDays > 0: `proratedAmount = Math.Round(component.Amount × (calendarDays - lopDays) / calendarDays, 2, MidpointRounding.AwayFromZero)`. Else: `proratedAmount = component.Amount`.
- `GrossWage` = sum of all `ProratedAmount` values.
- Return `ComponentBreakdown` list alongside the existing gross outputs.
- Update `PayrollEngine.cs` caller to pass the new input shape.
- Existing `PayrollRunInput.WorkingDaysInMonth` usages: search and update all — only used in GrossCalculator.

**Execution note:** Implement test-first. Write tests with exact decimal expectations before changing calculator.

**Technical design:**
```
// Directional — not implementation spec
for each component:
    prorated = IsLopProrated && lopDays > 0
        ? Round(component.Amount × (calendarDays - lopDays) / calendarDays, 2)
        : component.Amount
```

**Patterns to follow:**
- `src/Payroll.Engine/Calculators/TDSCalculator.cs` — `Round(..., MidpointRounding.AwayFromZero)` pattern, pure static method.

**Test scenarios:**
- Happy path: 0 LOP days → all components return `ProratedAmount == FullAmount`.
- Happy path: 2 LOP, 31 calendar days, Basic = 40000 → `ProratedAmount = Round(40000 × 29/31, 2) = 37419.35`.
- Happy path: component with `IsLopProrated = false`, 5 LOP → `ProratedAmount == FullAmount` (non-prorated).
- Edge case: LopDays = 30 (baseDays - 1), 31 calendar days → `ProratedAmount = Round(amount × 1/31, 2)`.
- Edge case: all components `IsLopProrated = false` → `GrossWage` unchanged by any LOP value.
- Error path: LopDays > CalendarDaysInMonth → GrossCalculator throws `ArgumentOutOfRangeException`.
- Integration: `PayrollEngine.Compute()` returns `ComponentBreakdown` populated for each employee.
- **PFWage must reflect LOP**: `PFWage = sum of BASIC/DA prorated amounts` (not raw amounts). Test: 2 LOP days on 31-day month, BASIC = 40000 → `PFWage = 40000 × 29/31 = 37419.35`; `EmployeePf = Round(min(37419.35, 15000) × 12%, 2)`.

**Verification:** `Payroll.Engine.Tests` all pass. 95%+ coverage on GrossCalculator. `dotnet build` zero warnings.

---

- U6. **Initiate pay run command + current-period query**

**Goal:** Create a regular monthly pay run for the next payable period. Populate `PayrunEmployee` for all eligible employees with initial computed amounts. Provide the "period card" data to the frontend.

**Requirements:** R1, R2

**Dependencies:** U1, U2, U4, U5

**Files:**
- Create: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs` (command + handler)
- Create: `src/Payroll.Application/Queries/PayrollRuns/GetCurrentPayPeriodQuery.cs` (query + handler)
- Create: `src/Payroll.Application/Queries/PayrollRuns/GetPayrollRunSummaryQuery.cs` (query + handler)
- Modify: `src/Payroll.Api/Controllers/PayrollRunsController.cs` — add `POST /initiate` and `GET /current-period`
- Test: `tests/Payroll.Application.Tests/Commands/PayrollRuns/InitiatePayrollRunCommandTests.cs`

**Approach:**
- `GetCurrentPayPeriodQuery`: reads `PaySchedule` → computes next payable period (last paid run's period + 1 month; or `FirstPayPeriodMonth/Year` if no prior runs). Returns DTO with `Period`, `PayDay`, `ActiveEmployeeCount`, `HasOutstandingRun` (bool — if a Draft/Approved run already exists for this period).
- `InitiatePayrollRunCommand`: validates no run exists for the period. Creates `PayrollRun` (Status = Draft, Type = Regular). For each active `Employee` with a complete onboarding + active `EmployeeSalaryStructure`: run engine (`GrossCalculator` → PF/ESI/PT/LWF/TDS), persist `PayrunEmployee` with computed amounts + `PayrunComponentBreakdown` with per-component amounts. Employees missing salary structure: excluded with a note (they'll appear in pending tasks).
- **Eligibility gate**: employee is eligible if `EmployeeSalaryStructure.EffectiveTo == null` AND employee `Status = Active`. Missing DOB, Father's Name, Bank Account → `PayrunEmployee.Status = Skipped` with system skip reason "Onboarding incomplete" (matches Zoho hard-block). Missing PAN only → `PayrunEmployee.Status = Active` (soft warning, TDS at 20%).
- Return `PayrollRunSummaryDto` with run ID, period, PayDay, totals, employee count.
- **StatutoryConfig snapshot**: load from `StatutoryOrgConfig`, `IncomeTaxSlab`, `ProfessionalTaxSlab`, `LwfStateConfig` at initiation time. Serialize as JSON and store on `PayrollRun.StatutoryConfigSnapshot` (added in U1). All subsequent engine calls (LOP recalc in U7, TDS worksheet in U10) deserialize from this snapshot rather than querying live DB — ensuring the run is reproducible even if slabs are edited mid-cycle.

**Patterns to follow:**
- `src/Payroll.Application/Commands/Employees/AssignSalaryStructureCommand.cs` — MediatR handler structure.
- `src/Payroll.Application/Queries/Employees/GetEmployeeSalaryStructureQuery.cs` — query + Dapper read pattern.

**Test scenarios:**
- Happy path: 3 active employees, all eligible → run created, 3 PayrunEmployee rows, PayrunComponentBreakdown rows per employee.
- Edge case: employee without salary structure → excluded from run; appears in pending tasks.
- Edge case: employee with missing DOB → `PayrunEmployee.Status = Skipped` (system reason).
- Edge case: employee without PAN → `PayrunEmployee.Status = Active`, TDS computed at 20% flat.
- Error path: `InitiatePayrollRun` when a Draft run already exists for the period → `ValidationException`.
- Error path: no `PaySchedule` configured → `ValidationException` ("Pay Schedule not configured").
- Integration: handler persists correct `PayrunComponentBreakdown` rows matching engine output.

**Verification:** API `POST /api/v1/payroll-runs/initiate` returns 201 with run ID. `GET /api/v1/payroll-runs/current-period` returns next period + employee count.

---

- U7. **Variable inputs commands: LOP, one-time earnings, TDS override**

**Goal:** Commands that mutate a Draft pay run's per-employee variable inputs and persist recalculated amounts.

**Requirements:** R2, R10

**Dependencies:** U2, U5, U6

**Files:**
- Create: `src/Payroll.Application/Commands/PayrollRuns/SetLopCommand.cs`
- Create: `src/Payroll.Application/Commands/PayrollRuns/AddOneTimeEarningCommand.cs`
- Create: `src/Payroll.Application/Commands/PayrollRuns/RemoveOneTimeEarningCommand.cs`
- Create: `src/Payroll.Application/Commands/PayrollRuns/OverrideTdsCommand.cs`
- Create: `src/Payroll.Application/Queries/PayrollRuns/GetEmployeeVariableInputsQuery.cs`
- Test: `tests/Payroll.Application.Tests/Commands/PayrollRuns/VariableInputCommandTests.cs`

**Approach:**
- `SetLopCommand(runId, employeeId, lopDays, actorId)`: guard run is Draft; guard `PayrunEmployee.Status = Active`; run `GrossCalculator` with new LOP days → update `PayrunEmployee` gross/net amounts; update `PayrunComponentBreakdown` prorated amounts; re-run downstream statutory calculators (PF/ESI/PT/LWF/TDS) to recompute taxes. All in one SaveChanges.
- `AddOneTimeEarningCommand(runId, employeeId, componentId, amount, actorId)`: adds a new `PayrunComponentBreakdown` row with `IsOneTimeEarning = true`; recalculates `PayrunEmployee.GrossPay` and `NetPay`. Component must exist in `SalaryComponent` with type allowing one-time use.
- `RemoveOneTimeEarningCommand(runId, employeeId, breakdownId, actorId)`: removes the specific breakdown row; recalculates totals.
- `OverrideTdsCommand(runId, employeeId, overrideAmount, reason, actorId)`: validates reason non-null and non-empty; sets `PayrunEmployee.TdsOverrideAmount` + `TdsOverrideReason`; updates `NetPay`.
- `GetEmployeeVariableInputsQuery`: returns all data for the split panel (component breakdown, LOP days, one-time earnings, TDS override if any).
- All commands guard: `PayrollRun.Status == Draft` — throw `InvalidOperationException` if Approved or Paid.

**Patterns to follow:**
- Same handler structure as `SetLopCommand` within wizard — `AssignSalaryStructureCommand` pattern.

**Test scenarios:**
- Happy path: SetLop(2, 31) → PayrunEmployee.LopDays = 2, ActualPayableDays = 29, GrossPay updated.
- Happy path: AddOneTimeEarning → PayrunComponentBreakdown row added, GrossPay increases by amount.
- Happy path: OverrideTds(5000, "reason") → TdsOverrideAmount = 5000, NetPay recalculated.
- Error path: SetLop on Approved run → `InvalidOperationException`.
- Error path: OverrideTds with empty reason → `ValidationException`.
- Error path: SetLop with lopDays ≥ baseDays → `ValidationException`.
- Edge case: RemoveOneTimeEarning → amounts revert to pre-add state.

**Verification:** Split panel data matches applied variable inputs via `GetEmployeeVariableInputsQuery`.

---

- U8. **Skip / undo-skip employee commands**

**Goal:** Allow admin to skip an employee for a pay run (with mandatory reason) or undo the skip, both in Draft state.

**Requirements:** R2, R3

**Dependencies:** U2, U6

**Files:**
- Create: `src/Payroll.Application/Commands/PayrollRuns/SkipEmployeeCommand.cs`
- Create: `src/Payroll.Application/Commands/PayrollRuns/UndoSkipEmployeeCommand.cs`
- Test: `tests/Payroll.Application.Tests/Commands/PayrollRuns/SkipEmployeeCommandTests.cs`

**Approach:**
- `SkipEmployeeCommand(runId, employeeId, reason, actorId)`: guard Draft; validate reason non-empty; set `PayrunEmployee.Status = Skipped`, `SkipReason = reason`. Update `PayrollRun` totals (subtract this employee's gross/net from run-level aggregates).
- `UndoSkipEmployeeCommand(runId, employeeId, actorId)`: guard Draft; guard `PayrunEmployee.Status == Skipped`; set `Status = Active`; add back to run totals.
- Note: Zoho states skip is "permanent for this cycle" but undo-skip is shown in the kebab matrix for Draft state. We implement undo-skip in Draft only.

**Patterns to follow:**
- `SetLopCommand` guard pattern.

**Test scenarios:**
- Happy path: skip Active employee with reason → Status = Skipped, reason stored.
- Happy path: undo-skip Skipped employee → Status = Active.
- Error path: skip with empty reason → `ValidationException`.
- Error path: undo-skip Active employee → `InvalidOperationException`.
- Error path: skip on Approved run → `InvalidOperationException`.

**Verification:** Skipped employee excluded from Payroll Cost / Total Net Pay in run summary.

---

- U9. **Pending tasks query**

**Goal:** Return the list of pending tasks (hard blocks + soft warnings) for a Draft pay run, powering the collapsible banner.

**Requirements:** R3, R4

**Dependencies:** U2, U6

**Files:**
- Create: `src/Payroll.Application/Queries/PayrollRuns/GetPendingTasksQuery.cs`
- Test: `tests/Payroll.Application.Tests/Queries/PayrollRuns/GetPendingTasksQueryTests.cs`

**Approach:**
- Query `PayrunEmployee` for the run. Compute:
  - Hard block tasks: count of employees where `Status = Active` AND `Employee.OnboardingComplete = false` AND not explicitly skipped. (Onboarding complete = DOB + FathersName + PersonalEmail + Address + BankAccount + SalaryStructure all present — composite check.)
  - Soft warnings: count of employees where `Status = Active` AND `Employee.EncryptedPAN = null`.
- Return `PendingTasksDto { HardBlocks: [...], SoftWarnings: [...] }`.
- `HasAnyHardBlocks` bool on the DTO used by the approve command guard.

**Test scenarios:**
- Happy path: all employees complete → empty hard blocks, empty soft warnings.
- Happy path: 1 employee with missing DOB, not skipped → 1 hard block.
- Happy path: 1 employee without PAN → 0 hard blocks, 1 soft warning.
- Edge case: employee is Skipped → not counted in either hard blocks or soft warnings.

**Verification:** `GET /api/v1/payroll-runs/{id}/pending-tasks` returns correct counts.

---

- U10. **Approve + reject approval commands**

**Goal:** Transition a Draft pay run to Approved (with task gate), and revert Approved back to Draft.

**Requirements:** R3, R5

**Dependencies:** U1, U9

**Files:**
- Create: `src/Payroll.Application/Commands/PayrollRuns/ApprovePayrollRunCommand.cs`
- Create: `src/Payroll.Application/Commands/PayrollRuns/RejectApprovalCommand.cs`
- Test: `tests/Payroll.Application.Tests/Commands/PayrollRuns/ApprovalCommandTests.cs`

**Approach:**
- `ApprovePayrollRunCommand(runId, actorId)`:
  1. Guard: run must be Draft.
  2. Check `GetPendingTasksQuery` — if any hard blocks → throw `PayrollRunHasBlockingTasksException` (maps to 422 in API).
  3. Write `TdsWorksheet` per active `PayrunEmployee` using stored `TdsOverrideAmount` if set.
  4. Call `payrollRun.Approve(actorId)` → status = Approved.
  5. Write `PayrollRunAuditLog` entry (Draft → Approved, actor, timestamp).
  6. `SaveChanges()`.
- `RejectApprovalCommand(runId, reason, actorId)`:
  1. Guard: run must be Approved.
  2. Call `payrollRun.RejectApproval(reason, actorId)` → status = Draft.
  3. Write audit log (Approved → Draft).
  4. `SaveChanges()`.
- Note: approval locks reimbursements and IT declarations — for V1, enforce at API level (commands for adding reimbursements / updating IT declarations must check if a run is Approved for that period).

**Patterns to follow:**
- `src/Payroll.Application/Commands/Employees/` handler structure.

**Test scenarios:**
- Happy path: approve Draft run with no pending tasks → Status = Approved, TdsWorksheet rows written.
- Error path: approve with hard-block pending tasks → `PayrollRunHasBlockingTasksException`.
- Error path: approve Approved run → `InvalidOperationException`.
- Happy path: reject-approval Approved run → Status = Draft, reason stored in audit log.
- Error path: reject-approval Draft run → `InvalidOperationException`.

**Verification:** `POST /api/v1/payroll-runs/{id}/approve` returns 200. `POST /api/v1/payroll-runs/{id}/reject-approval` returns 200. Status transitions correct.

---

- U11. **Record payment + delete payment commands**

**Goal:** Transition Approved → Paid (recording payment metadata), and reverse Paid → Approved.

**Requirements:** R6, R9

**Dependencies:** U1, U10

**Files:**
- Create: `src/Payroll.Application/Commands/PayrollRuns/RecordPaymentCommand.cs`
- Create: `src/Payroll.Application/Commands/PayrollRuns/DeleteRecordedPaymentCommand.cs`
- Test: `tests/Payroll.Application.Tests/Commands/PayrollRuns/PaymentCommandTests.cs`

**Approach:**
- `RecordPaymentCommand(runId, paymentDate, paymentMode, reference, notifyEmployees, actorId)`:
  1. Guard: run must be Approved.
  2. Call `payrollRun.RecordPayment(...)` → Status = Paid.
  3. Write audit log (Approved → Paid).
  4. Enqueue Hangfire job: `generateJobId = BackgroundJob.Enqueue<GeneratePayslipsJob>(j => j.Execute(runId))`.
  5. If `notifyEmployees = true`: chain via `BackgroundJob.ContinueJobWith(generateJobId, ...)` → `SendPayslipNotificationJob` runs only after PDFs are generated and published to avoid sending email before attachments exist.
  6. `SaveChanges()`.
- `DeleteRecordedPaymentCommand(runId, actorId)`:
  1. Guard: run must be Paid.
  2. Call `payrollRun.DeletePayment(actorId)` → Status = Approved, payment fields cleared.
  3. Write audit log (Paid → Approved).
  4. Mark `Payslip.IsPublished = false` for all payslips of this run (unpublish from portal).
  5. `SaveChanges()`.

**Test scenarios:**
- Happy path: record payment on Approved run → Status = Paid, PaymentDate/Mode/Reference stored.
- Happy path: delete payment on Paid run → Status = Approved, payment fields null.
- Error path: record payment on Draft run → exception.
- Error path: delete payment on Draft run → exception.
- Integration: Hangfire `GeneratePayslipsJob` is enqueued after successful RecordPayment.

**Verification:** `POST /api/v1/payroll-runs/{id}/record-payment` → 200, run status = Paid. `DELETE /api/v1/payroll-runs/{id}/payment` → 200, run status = Approved.

---

- U12. **Payslip PDF generation (QuestPDF + MinIO)**

**Goal:** Generate per-employee payslip PDFs on pay run payment recording. Store in MinIO. Expose JSON and PDF endpoints.

**Requirements:** R7

**Dependencies:** U3, U11

**Files:**
- Create: `src/Payroll.Infrastructure/Services/PayslipPdfGenerator.cs`
- Create: `src/Payroll.Infrastructure/Jobs/GeneratePayslipsJob.cs` (Hangfire job)
- Create: `src/Payroll.Application/Commands/PayrollRuns/GeneratePayslipCommand.cs`
- Create: `src/Payroll.Application/Queries/PayrollRuns/GetPayslipDataQuery.cs`
- Create: `src/Payroll.Application/Commands/PayrollRuns/SendPayslipEmailCommand.cs`
- Test: `tests/Payroll.Infrastructure.Tests/Services/PayslipPdfGeneratorTests.cs`

**Approach:**
- `GeneratePayslipsJob`: for each `PayrunEmployee` with `Status = Active` in the run:
  1. Run `GetPayslipDataQuery` — assembles `PayslipData` (employee details, org profile, component breakdown, deductions, net pay, YTD data from prior runs' `PayrunComponentBreakdown`).
  2. Call `PayslipPdfGenerator.Generate(payslipData)` → PDF bytes.
  3. Upload to MinIO key: `payslips/{tenantId}/{runId}/{employeeId}.pdf`.
  4. Upsert `Payslip` entity (create or update if re-generating): `PdfStorageKey`, `GeneratedAt = now`, `IsPublished = true`, `NetPay`, `NetPayInWords`, `YtdDataJson`.
- `PayslipPdfGenerator` uses QuestPDF. Layout matches audit doc §9: header (company, period), employee summary table (left), net pay banner (right), earnings table with YTD column, deductions table with YTD, amount in words footer.
- `NetPayInWords`: Indian numbering system (lakh/crore), e.g., "Indian Rupee Sixty-Five Thousand Four Hundred Eighty-Four Only". Use a utility method.
- **YTD earnings sourcing**: sum `ProratedAmount` from `PayrunComponentBreakdown` grouped by `SalaryComponentId` for the employee across all Paid runs in the same fiscal year (FY April–March) up to and including the current run.
- **YTD deductions sourcing**: sum `TdsAmount`, `EmployeePf`, `EmployeeEsi`, `PtAmount` from `PayrunEmployee` for the same employee across all Paid runs in the same fiscal year. These live on `PayrunEmployee`, not on `PayrunComponentBreakdown`. Both sets of YTD values are written to `YtdDataJson` on the `Payslip` entity for fast PDF rendering without re-querying.
- `GetPayslipDataQuery`: returns JSON DTO (for slide-in panel rendering — no PDF needed for viewing).
- `SendPayslipEmailCommand`: retrieves `Payslip.PdfStorageKey` from MinIO, attaches to email via existing email service, sends to `Employee.PersonalEmail` (or `WorkEmail` if personal not set).
- Bank account number on payslip: masked to last 4 digits (our differentiator vs Zoho's unmasked — see build guide §G).

**Patterns to follow:**
- `src/Payroll.Infrastructure/Services/TenantSchemaProvisioner.cs` — MinIO upload pattern.
- QuestPDF API: `Document.Create(container => ...)`. Community license (free).

**Test scenarios:**
- Unit: `PayslipPdfGenerator.Generate(payslipData)` → returns non-empty byte array, is valid PDF.
- Unit: `NetPayInWords(65484)` → "Indian Rupee Sixty-Five Thousand Four Hundred Eighty-Four Only".
- Unit: `NetPayInWords(100000)` → "Indian Rupee One Lakh Only".
- Unit: YTD sum for April + May = sum of both months' component amounts.
- Integration: `GeneratePayslipsJob` creates `Payslip` entities with `IsPublished = true`.
- Integration: `GetPayslipDataQuery` returns non-null data for an Active PayrunEmployee.
- Error path: MinIO upload failure → Hangfire retries job (let Hangfire's default retry handle it; job is idempotent — re-generating overwrites the same key).

**Verification:** After `RecordPayment`, MinIO contains PDF at expected key. `GET /api/v1/payroll-runs/{id}/employees/{eid}/payslip` returns JSON. `GET .../payslip/pdf` returns `application/pdf` response.

---

- U13. **Bank advice XLS generation**

**Goal:** On-demand XLS file download containing bank transfer data for all bank-transfer employees in an Approved or Paid run.

**Requirements:** R8

**Dependencies:** U2, U10

**Files:**
- Create: `src/Payroll.Infrastructure/Services/BankAdviceGenerator.cs`
- Create: `src/Payroll.Application/Queries/PayrollRuns/GetBankAdviceQuery.cs`
- Modify: `src/Payroll.Infrastructure/Payroll.Infrastructure.csproj` — add `ClosedXML` NuGet package

**Approach:**
- `GetBankAdviceQuery(runId, format, actorId)`: validates run is Approved or Paid. Fetches `PayrunEmployee` rows with `PaymentMode = BankTransfer` and `Status = Active`. For each: joins with `Employee` to get decrypted bank account (AES-256), IFSC, bank name, account holder name. Calls `BankAdviceGenerator.Generate(employees, format)`.
- `BankAdviceGenerator.Generate(data, format)`: in Phase 1, only Standard Format XLS. 7 columns per audit doc §11: Employee No, Employee Name, Amount (decimal, no formatting), Bank Name, Bank Account No (full, unmasked — matches Zoho standard; we add a tenant config option later), IFSC Code, Beneficiary Name. No totals row. ClosedXML `XLWorkbook` → byte array.
- Return as `FileContentResult` with `application/vnd.ms-excel`, filename `Payroll_Bank_Statement.xls`.
- Bank account decryption: use same `IEncryptionService` as employee personal details update.

**Patterns to follow:**
- `src/Payroll.Infrastructure/Services/` — existing service pattern.
- `src/Payroll.Application/Queries/Employees/GetEmployeeSalaryStructureQuery.cs` — Dapper query + join pattern.

**Test scenarios:**
- Unit: `BankAdviceGenerator.Generate(2 employees, Standard)` → valid XLS byte array with 2 data rows, 7 columns.
- Unit: amount column is decimal value (e.g., 65484.0), not formatted string.
- Integration: endpoint returns 200 with `application/vnd.ms-excel` content type.
- Error path: request for Draft run → 422 (bank advice only available Approved or later).
- Edge case: no bank-transfer employees → empty XLS with header row only.

**Verification:** Downloaded file opens in Excel. Columns match Standard Format spec.

---

- U14. **Pay run queries + PayrollRunsController**

**Goal:** All query endpoints and the controller wiring all commands and queries to HTTP routes.

**Requirements:** R1 through R9

**Dependencies:** U6, U7, U8, U9, U10, U11, U12, U13

**Files:**
- Create: `src/Payroll.Application/Queries/PayrollRuns/GetPayrollRunEmployeesQuery.cs`
- Create: `src/Payroll.Application/Queries/PayrollRuns/GetPayrollHistoryQuery.cs`
- Modify: `src/Payroll.Api/Controllers/PayrollRunsController.cs` — add all endpoints (create or extend)

**Approach:**
- `GetPayrollRunEmployeesQuery(runId, filter)`: returns list of `PayrunEmployee` joined with `Employee` and `PayrunComponentBreakdown`. Filter options: All, Active, Skipped. Column set in DTO matches state-dependent table (draft vs approved/paid columns differentiated by run status in DTO).
- `GetPayrollHistoryQuery(typeFilter, pageSize, page)`: returns completed (Paid) runs ordered by payment date desc.
- Controller routes (all under `/api/v1/payroll-runs`):

| Method | Path | Handler |
|--------|------|---------|
| GET | `/current-period` | `GetCurrentPayPeriodQuery` |
| POST | `/initiate` | `InitiatePayrollRunCommand` |
| GET | `/{id}` | `GetPayrollRunSummaryQuery` |
| GET | `/{id}/employees` | `GetPayrollRunEmployeesQuery` |
| GET | `/{id}/employees/{eid}/inputs` | `GetEmployeeVariableInputsQuery` |
| PUT | `/{id}/employees/{eid}/lop` | `SetLopCommand` |
| POST | `/{id}/employees/{eid}/earnings` | `AddOneTimeEarningCommand` |
| DELETE | `/{id}/employees/{eid}/earnings/{breakdownId}` | `RemoveOneTimeEarningCommand` |
| PUT | `/{id}/employees/{eid}/tds-override` | `OverrideTdsCommand` |
| POST | `/{id}/employees/{eid}/skip` | `SkipEmployeeCommand` |
| DELETE | `/{id}/employees/{eid}/skip` | `UndoSkipEmployeeCommand` |
| GET | `/{id}/pending-tasks` | `GetPendingTasksQuery` |
| POST | `/{id}/approve` | `ApprovePayrollRunCommand` |
| POST | `/{id}/reject-approval` | `RejectApprovalCommand` |
| POST | `/{id}/record-payment` | `RecordPaymentCommand` |
| DELETE | `/{id}/payment` | `DeleteRecordedPaymentCommand` |
| GET | `/{id}/employees/{eid}/payslip` | `GetPayslipDataQuery` |
| GET | `/{id}/employees/{eid}/payslip/pdf` | PDF stream |
| POST | `/{id}/employees/{eid}/payslip/send` | `SendPayslipEmailCommand` |
| GET | `/{id}/bank-advice` | `GetBankAdviceQuery` |
| GET | `/history` | `GetPayrollHistoryQuery` |
| DELETE | `/{id}` | `DeletePayrollRunCommand` (Draft only) |
| POST | `/{id}/regenerate-payslips` | Re-enqueue `GeneratePayslipsJob` for a Paid run (idempotent; overwrites MinIO keys) |

**Patterns to follow:**
- `src/Payroll.Api/Controllers/EmployeesController.cs` — thin controller, MediatR sender, request records at bottom of file.

**Test scenarios:**
- Integration: `POST /initiate` → 201 with run ID.
- Integration: `PUT /{id}/employees/{eid}/lop` with invalid LOP days → 422.
- Integration: `POST /{id}/approve` with pending hard-block tasks → 422 with task details.
- Integration: `GET /{id}/bank-advice` before approval → 422.
- Integration: all endpoints return 403 if tenant mismatch.

**Verification:** All routes return expected status codes in integration tests. `dotnet build` zero warnings.

---

- U15. **Frontend: Run Payroll page + Payroll History**

**Goal:** The top-level Pay Runs section: period card with initiate action, and payroll history table.

**Requirements:** R1

**Dependencies:** U14

**Files:**
- Create: `web/src/pages/payroll/PayRunsPage.tsx` — tab switcher: "Run Payroll" | "Payroll History"
- Create: `web/src/pages/payroll/components/PeriodCard.tsx`
- Create: `web/src/pages/payroll/PayrollHistoryPage.tsx` — table with type filter
- Modify: `web/src/router.tsx` — add `/pay-runs` route
- Modify: `web/src/components/Sidebar.tsx` (or nav component) — add "Pay Runs" nav item

**Approach:**
- `PayRunsPage`: fetches `GET /current-period`. If `HasOutstandingRun = true`, shows a card linking to `/pay-runs/{id}`. If not, shows `PeriodCard` with "Process Pay Run for {Month} {Year}" heading, period range, pay day, employee count, and primary "Process Payroll" button.
- "Process Payroll" button: calls `POST /initiate`; on success, navigate to `/pay-runs/{runId}`.
- Empty state when no outstanding run: "You deserve a break today!" heading + "You have no outstanding pay runs."
- Past-pay-day warning banner: shown if pay day has passed and no run created.
- `PayrollHistoryPage`: `GET /history`, table with columns: Payment Date, Payroll Type badge, Period, Status badge "Paid". Row click → navigate to `/pay-runs/{id}`.

**Patterns to follow:**
- `web/src/pages/employees/` — React Query data fetching, design system tokens, Lucide icons.
- `web/src/pages/employees/EmployeeListPage.tsx` — list + empty state pattern.

**Test scenarios:**
- Test expectation: none — visual page; verify via browser test.

**Verification:** `/pay-runs` renders period card. "Process Payroll" creates run and navigates to detail page. History tab shows past runs.

---

- U16. **Frontend: Pay Run detail page — Draft state + variable inputs panel**

**Goal:** The pay run detail page for Draft state: info strip, pending tasks banner, employee summary table with variable inputs split panel.

**Requirements:** R2, R3, R4

**Dependencies:** U15

**Files:**
- Create: `web/src/pages/payroll/PayRunDetailPage.tsx` — page shell (tabs, header, routing)
- Create: `web/src/pages/payroll/components/PayRunHeader.tsx` — status badge, action buttons, info strip
- Create: `web/src/pages/payroll/components/PendingTasksBanner.tsx` — collapsible hard/soft task list
- Create: `web/src/pages/payroll/components/EmployeeSummaryTable.tsx` — table (column set varies by run status)
- Create: `web/src/pages/payroll/components/VariableInputsPanel.tsx` — slide-in drawer

**Approach:**
- `PayRunDetailPage`: fetches `GET /pay-runs/{id}` (run summary). Shows `PayRunHeader` + tabs (Employee Summary / Taxes & Deductions / Overall Insights — last two show "Coming soon" in this phase). Content area renders appropriate tab component.
- `PayRunHeader`: displays period, base days, payroll cost, net pay, pay day, employee count, status badge (Draft / Approved / Payment Due / Paid). In Draft: primary "Approve Payroll" button. Page kebab: "Delete Pay Run".
- `PendingTasksBanner`: fetches `GET /pending-tasks`. Shows hard blocks with "View Employees" link (for now, a toast "Feature coming soon" — add-employees page deferred). Shows PAN warning count as soft warning. Collapsible.
- `EmployeeSummaryTable` (Draft): columns — Checkbox, Employee Name (clickable), Paid Days, Gross Pay, Deductions, Taxes, Benefits, Net Pay, row kebab (Skip / Undo Skip). Data from `GET /{id}/employees`.
- `VariableInputsPanel`: slide-in from right, triggered by clicking employee name. Sections:
  - Paid Days / LOP entry: spinbutton (0 to baseDays-1), "Actual Payable Days" auto-computed.
  - Earnings table: per-component amounts. "Add Earning" button → listbox (Bonus / Commission / Leave Encashment) + amount input.
  - TDS section: computed TDS amount, "Edit" → override amount + mandatory reason textarea.
  - Net Pay footer (auto-updated).
  - Save / Cancel buttons.
- Panel Save: calls relevant mutation (SetLop / AddEarning / OverrideTds). On success: invalidate employee table query + run summary query.
- Cancel with unsaved changes: show confirmation dialog matching Zoho text.

**Patterns to follow:**
- `web/src/pages/employees/tabs/EmployeeOverviewTab.tsx` — inline-edit section pattern.
- `web/src/pages/employees/wizard/WizardStep2Salary.tsx` — live computation display.

**Test scenarios:**
- Test expectation: none — visual; verify via browser test.

**Verification:** Open pay run in Draft. Click employee name → panel slides in. Enter LOP → Actual Payable Days updates. Add Earning → gross increases. Save → table refreshes with new values.

---

- U17. **Frontend: Approval / payment dialogs + state badge handling**

**Goal:** All dialogs for approve, reject, skip, record payment, delete payment, and the state-dependent UI changes.

**Requirements:** R5, R6, R9

**Dependencies:** U16

**Files:**
- Create: `web/src/pages/payroll/components/ApprovePayrollDialog.tsx`
- Create: `web/src/pages/payroll/components/RejectApprovalDialog.tsx`
- Create: `web/src/pages/payroll/components/SkipEmployeeDialog.tsx`
- Create: `web/src/pages/payroll/components/RecordPaymentDialog.tsx`
- Create: `web/src/pages/payroll/components/DeletePaymentDialog.tsx`

**Approach:**
- `ApprovePayrollDialog`: warning bullets (reimbursements, IT declaration lock — same text as Zoho). Period + employee count + total net pay summary. "Submit and Approve" CTA. On submit: calls `POST /approve`. If 422 (blocking tasks): show error toast with task count; do not close dialog. On success: refetch run summary, navigate to Approved state view.
- `RejectApprovalDialog`: optional reason textarea. "Reject" button → `POST /reject-approval`. On success: refetch.
- `SkipEmployeeDialog`: employee name + period (read-only). Mandatory reason textarea (validated non-empty client-side before submit). Warning text matching Zoho. Calls `POST /{id}/employees/{eid}/skip`. On success: close + refetch.
- `RecordPaymentDialog`: payment date picker (default = pay schedule pay day), payment mode summary (read-only table from run data), reference number input, notes input, "Send payslip notification" checkbox (default checked). Calls `POST /{id}/record-payment`. On success: navigate page to Paid state view.
- `DeletePaymentDialog`: simple yes/no confirmation matching Zoho text. Calls `DELETE /{id}/payment`. On success: refetch to Approved state.
- Status badge: computed from `run.status` field — "Draft", "Approved", "Payment Due" (if status = Approved AND today > payDay), "Paid". Use CSS custom props from design system for badge colors.

**Patterns to follow:**
- `web/src/pages/employees/wizard/InlineCreateModal.tsx` — modal structure, error handling.

**Test scenarios:**
- Test expectation: none — visual; verify via browser test.

**Verification:** Approve → status badge changes to "Approved". Record Payment → status badge = "Paid". Delete Payment → status badge = "Approved".

---

- U18. **Frontend: Approved / Paid state + payslip panel + bank advice**

**Goal:** The post-approval pay run UI: read-only employee table with payslip view columns, payslip slide-in panel, bank advice download, payroll history access.

**Requirements:** R7, R8

**Dependencies:** U17

**Files:**
- Create: `web/src/pages/payroll/components/PayslipPanel.tsx` — slide-in panel
- Create: `web/src/pages/payroll/components/PayslipDownloadDialog.tsx` — password protection toggle
- Create: `web/src/pages/payroll/components/BankAdviceModal.tsx` — format selector + download

**Approach:**
- `EmployeeSummaryTable` (Approved/Paid state): different column set — Checkbox, Employee Name, Paid Days, Net Pay, Payslip ("View" button), TDS Sheet ("View" button), Payment Mode, Payment Status ("Paid on {date}" or "Yet To Pay"). No Gross/Deductions/Taxes/Benefits columns. Import disabled — Export Data only.
- `PayslipPanel`: slide-in drawer. Sections matching audit §8: header (company name, period, employee name, net pay, payment info banner), attendance section (payable days, LOP, actual payable days), earnings table with YTD column, deductions table with YTD, net pay footer. Data from `GET /{id}/employees/{eid}/payslip`.
- "Download Payslip" in panel: opens `PayslipDownloadDialog` (password protection checkbox, default checked). On confirm: calls `GET .../payslip/pdf` with query param `?password=true/false` → file download.
- "Send Payslip" in panel: calls `POST .../payslip/send` → success toast.
- "Download Bank Advice" button in info strip (appears after Approval): opens `BankAdviceModal`. Format dropdown (Phase 1: Standard Format only — others shown as "Coming soon"). Download calls `GET /{id}/bank-advice?format=Standard`.
- Paid state header CTA changes: "Record Payment" → "Send Payslip" (bulk send to all employees).
- Page kebab (Paid): "Download all Payslips" (links to async job — Phase 1: show "Coming soon" toast), "Delete Recorded Payment" → `DeletePaymentDialog`.

**Patterns to follow:**
- `VariableInputsPanel` slide-in drawer structure.
- `web/src/lib/api.ts` for file download via `blob` response type.

**Test scenarios:**
- Test expectation: none — visual; verify via browser test.

**Verification:** After RecordPayment, click "View" in Payslip column → panel renders with correct amounts and YTD. Download → PDF opens. Bank Advice download → XLS with correct columns.

---

- U19. **Delete pay run command**

**Goal:** Soft-delete a Draft pay run and all its employee rows when the admin abandons it.

**Requirements:** R1

**Dependencies:** U1, U2

**Files:**
- Create: `src/Payroll.Application/Commands/PayrollRuns/DeletePayrollRunCommand.cs`
- Test: `tests/Payroll.Application.Tests/Commands/PayrollRuns/DeletePayrollRunCommandTests.cs`

**Approach:**
- `DeletePayrollRunCommand(runId, actorId)`: guard `Status == Draft` (cannot delete Approved or Paid runs — must reject approval first). Call `payrollRun.Delete(actorId)` → Status = Deleted (which triggers EF soft-delete via `IsDeleted = true`). Write audit log (Draft → Deleted). `PayrunEmployee` and `PayrunComponentBreakdown` rows are cascade-soft-deleted or left as orphans (soft-delete filter on `PayrollRun` is sufficient — child rows unreachable via normal queries). `SaveChanges()`.

**Patterns to follow:**
- Domain `Delete()` method pattern consistent with existing soft-delete on other entities.

**Test scenarios:**
- Happy path: delete Draft run → `PayrollRun.IsDeleted = true`, not returned by `GetCurrentPayPeriod` query.
- Error path: delete Approved run → `InvalidOperationException`.
- Error path: delete non-existent run → 404.

**Verification:** `DELETE /api/v1/payroll-runs/{id}` on Draft run → 204. Run no longer appears in outstanding runs list.

---

## System-Wide Impact

- **Interaction graph:** `RecordPaymentCommandHandler` enqueues a Hangfire job → `GeneratePayslipsJob` → MinIO + `Payslip` entities. No direct callback chain outside of Hangfire.
- **Error propagation:** Hangfire job failures retry by default (3 attempts). Payment confirmation is not blocked by payslip generation — fast response to frontend, PDF generation is async.
- **State lifecycle risks:** `ApprovePayrollRunCommand` writes `TdsWorksheet` and transitions status in a single transaction. If payslip generation job exhausts Hangfire retries, run stays Paid but payslips unpublished. Recovery: `POST /{id}/regenerate-payslips` (added to U14) re-enqueues the job; the job is idempotent (upsert on `Payslip` entity, overwrite on MinIO key).
- **API surface parity:** `/api/v1/payroll-runs/` namespace. No overlap with existing employee or settings routes.
- **Integration coverage:** The critical path (initiate → LOP → approve → record-payment → payslip) must be covered by an end-to-end integration test using Testcontainers (real Postgres), not mocks.
- **Unchanged invariants:** Employee CRUD, salary structure assignment, settings module — unchanged. `PayrollRun` entity gains new fields but existing `TenantId` + `PayPeriod` + schema-per-tenant isolation remain.

---

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| GrossCalculator change breaks engine tests | Test-first U5 — run tests before and after. Any existing engine tests must pass. |
| `PayrollRunStatus` enum rename (Finalised → Paid) breaks existing DB rows | Migration maps old values. Check for any existing Finalised rows in dev DB before migrating. |
| QuestPDF payslip layout requires iteration | U12 is backend-focused; layout is a "good-enough first pass" — iterate on frontend feedback. Not blocking release. |
| ClosedXML not previously used — may need tuning | Standard Format is 7 columns, no formulas — low risk. Add NuGet, write test, done. |
| TDS YTD aggregation across prior runs is a join across many rows | Use Dapper for the YTD query; simple GROUP BY per component in same FY. Fast enough for V1 employee counts. |
| Approval lock on reimbursements / IT declarations requires cross-module coordination | V1: check `PayrollRun.Status == Approved` in reimbursement and IT declaration command handlers — simple guard, no event system needed yet. |

---

## Phased Delivery

### Phase 1 (this plan) — Core Lifecycle
U1–U14: All domain, engine, infrastructure, and API work. Backend fully functional.
U15–U18: Frontend complete.

### Phase 2 (follow-up)
- Overall Insights tab + Taxes & Deductions tab frontend.
- Bulk CSV imports (LOP, earnings, reimbursements).
- Off-cycle pay run creation + one-time payout.
- Resettlement / arrear run trigger from salary revision.

### Phase 3 (follow-up)
- ECR generation, ESI return, Form 24Q, PT challan.
- `POST /reprocess` (Paid → Draft in one step).
- Mid-month joiner auto-LOP.
- Async bulk payslip download with progress indicator.

---

## Sources & References

- **Origin document:** [docs/ba-audit/payroll-run-module.md](docs/ba-audit/payroll-run-module.md)
- Engine entry point: `src/Payroll.Engine/PayrollEngine.cs`
- Existing PayrollRun entity: `src/Payroll.Domain/Entities/PayrollRun.cs`
- GrossCalculator (to modify): `src/Payroll.Engine/Calculators/GrossCalculator.cs`
- TDSCalculator (already complete): `src/Payroll.Engine/Calculators/TDSCalculator.cs`
- QuestPDF in infrastructure: `src/Payroll.Infrastructure/Payroll.Infrastructure.csproj`
- EF config pattern: `src/Payroll.Infrastructure/Persistence/EntityConfigurations/EmployeeSalaryStructureConfiguration.cs`
- Controller pattern: `src/Payroll.Api/Controllers/EmployeesController.cs`
- Frontend panel pattern: `web/src/pages/employees/tabs/EmployeeOverviewTab.tsx`
