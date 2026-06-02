# Salary Revision & Arrear Handling — Implementation Plan

**WI:** 018
**Date:** 2026-06-02
**Scope:** Salary revision (create + apply) and arrears due to backdated revisions.
**Out of scope (v1):** Approval workflow, Section 89(1)/Form 10E relief, old regime, back-month statutory (PF/ESI/PT) re-collection.
**Reference product:** Zoho Payroll India.

---

## 1. Reference behaviour (Zoho)

- Salary revision created against an employee, with an **Effective From** month and a **Payout** month.
- When `Effective From < Payout`, the old salary is paid for the in-between months; the **excess (new − old)** for those months is held and paid in the payout month as **arrears**, alongside the revised salary. No manual arrear component needed — it is auto-computed.
- Arrears are fully taxable in the FY they are paid. **Note:** our engine annualises TDS, so the *incremental* tax from arrears is spread across the remaining months of the payout FY — it does **not** all land in the payout month's TDS line (see §3 decision 3 and §4). PT, by contrast, does land in the payout month (slab tested on that month's gross).

Sources: [Salary Revision](https://www.zoho.com/in/payroll/help/employer/approvals/salary-revision.html), [Arrear Handling KB](https://www.zoho.com/in/payroll/kb/employer/approvals/arrear-component.html), [Arrears of salary](https://www.zoho.com/in/payroll/academy/payroll-operations/arrears-of-salary.html).

---

## 2. What already exists (no rebuild needed)

| Asset | Location | State |
|---|---|---|
| `SalaryRevision` entity (Prev/NewCTC, EffectiveFromMonth/Year, PayoutMonth/Year, optional template, `Pending`/`Applied`) | `Payroll.Domain/Entities/SalaryRevision.cs` | Already EF-mapped (`SalaryRevisionConfiguration`, registered in `PayrollDbContext`, migrated). **Not wired** at the behaviour layer — no repo, command, or engine path. §5's FK/override additions extend the existing configuration + add a new migration. |
| `EmployeeSalaryStructure` — effective-dated, immutable (close old / create new) | `Payroll.Domain/Entities/EmployeeSalaryStructure.cs` | Done. Used by `AssignSalaryStructureCommand` |
| `EarningType.ArrearsEarning` | `Payroll.Domain/Enums/EarningType.cs` | Defined |
| `GrossResult.ArrearAmount` (stubbed `0m`) | `Payroll.Engine/Outputs/GrossResult.cs` | Placeholder — anticipates arrears |
| FnF pattern: coded breakdown rows + preview-without-save + orchestrator recompute + TDS worksheet | `PayrollFnfOrchestrator`, `GetFnfPreviewQuery`, `UpdateFnfRunCommand` | **Template to copy** |

**Key gaps in `SalaryRevision`:**
1. Stores CTC + optional template only — per-component arrears need the **full new component breakdown**, not a CTC number.
2. No FK to the resulting backdated `EmployeeSalaryStructure`.
3. `AssignSalaryStructureCommand` hardcodes `EffectiveFrom = today` (line 59) — no backdate path.

---

## 3. Design decisions (locked)

1. **Per-back-month diff, not lump.** Arrear per component per back month = `newProrated(month) − oldProrated(month)`. Old amount read from that month's **finalised** `PayrunComponentBreakdown`. New amount = recompute that month against the backdated structure.
   - **Reproduction basis (not just LOP).** "Same LOP/days" is insufficient for mid-month joiners and flat components. Replay the **exact persisted basis** for that run: `payableDays`, `SalaryDivisor`, and each component's `CalculateOnProRata`/`IsFlat` flags from the stored breakdown — not `LopDays` alone — so the new and old amounts share the same denominator.
   - **Component-set join.** Old and new structures may differ in shape (template change adds/removes components). Diff by **full outer join on component code**: present only in NEW → arrear = full new prorated amount; present only in OLD → see negative-arrears policy (decision 7).
2. **No back-month statutory recompute.** PF/ESI for past months are already paid and remitted; arrears do **not** re-trigger PF/ESI collection. *(v1 stance — **not yet a confirmed compliance ruling**; see Deferred / Open Questions for the EPFO/ESIC + Zoho-behaviour confirmation this depends on.)*
3. **Payout-month statutory.** Arrear rows are added to the payout-month gross. **PT** is computed on that month's gross **including arrears** (captures any slab jump, lands in the payout month). **TDS:** the arrear taxable total is added to annual projected taxable income **exactly once** (see §4 — must not be projected ×`MonthsRemainingInFY`). Because the engine annualises and divides by `MonthsRemainingInFY`, the resulting incremental TDS is **spread over the remaining months of the payout FY**, not concentrated in the payout month. This is the intended new-regime annualised-TDS behaviour; §1's "tax lands in payout month" is corrected accordingly.
4. **Per-component arrear rows**, taxable, with `ConsiderForEpf = false` and `ConsiderForEsi = false` (consequence of decision 2) and `IsOneTimeEarning = true` (flat, not prorated again). Each row carries the **real `SalaryComponentId`** of the component it is an arrear of (see §4 / §6 — a null id misroutes to the reimbursement bucket).
5. **Future-dated revision** (effective in a month with no finalised run): no arrears; the new structure simply applies forward. **Partial gap:** if *some* months in `[EffectiveFrom .. Payout−1]` have no finalised run, those months contribute zero and are surfaced in the preview as a warning (`N months in range had no finalised run — excluded`), never silently dropped.
6. **Cross-fiscal-year arrears.** When back months fall in a prior FY but payout is in a later FY: the arrear amount is **sized** using each back month's own frozen config, but the resulting taxable lump is **taxed in the payout FY** only (injected into the payout-month / payout-FY projection). Arrears are never retro-taxed into the prior FY.
7. **Negative arrears (downward revision or removed component).** v1 does **not** recover already-paid net pay. `CreateSalaryRevisionCommand` **warns** when `NewAnnualCTC < PreviousAnnualCTC` with effective < payout. Net-negative per-employee arrears are **clamped to zero**; recovery is `// DEFERRED: arrears-recovery`. The new (lower) structure still applies forward.
8. **Apply trigger — explicit only (locked).** A revision is applied by an explicit operator action (`ApplySalaryRevisionCommand`), which performs the backdated structure swap and sets status `Applied`. Payout-run injection reads **`Applied`** revisions by payout period — never `Pending`. There is no auto-apply on run initiation. (Resolves the former §10 open item.)
9. **Sec 89(1)/Form 10E: deferred.** Employee-claimed relief, not required to compute the payout. `// DEFERRED: relief`.

---

## 4. The one required engine change

`GrossCalculator.cs:45-46` projects the **entire** month wage:

```csharp
annualProjectedTaxable = CurrentEmployerYTDTaxable + taxableWage * run.MonthsRemainingInFY;
```

If arrears are folded into `taxableWage`, they get multiplied ×`MonthsRemainingInFY` → gross over-taxation. Arrears are one-time and must hit annual taxable **once**.

**Change:** add a one-time arrear taxable input that is added to the month's gross/taxable (so it is paid and taxed this month) but **excluded from the ×N projection base**:

```csharp
// recurring projected ×N, arrears added once
decimal annualProjectedTaxable =
    employee.CurrentEmployerYTDTaxable
    + taxableWage * run.MonthsRemainingInFY      // recurring only
    + employee.ArrearTaxableAmount;              // one-time, not projected
grossWage += employee.ArrearTaxableAmount;       // paid this month
```

- Add `decimal ArrearTaxableAmount = 0m` to `EmployeeInput`.
- Populate `GrossResult.ArrearAmount` (currently stubbed `0m`).
- **Add the arrear to `grossWage` AFTER `AnnualProjectedGross` is computed** (`GrossCalculator.cs:45`), and exclude it from the ×N base for the gross projection too — otherwise `AnnualProjectedGross` is also inflated. Mirror the taxable handling: recurring ×N, arrear ×1, for **both** the gross and taxable projections.
- Engine stays pure/synchronous/decimal — no other calculator changes.
- **No double count (critical).** The arrear enters the engine **once**, via `EmployeeInput.ArrearTaxableAmount`. The persisted `ARREAR_*` breakdown rows are **persist/display-only** — they must be **excluded from `engineRows`** in the recompute partition (same treatment as reimbursement / `IsBenefit` rows in `PayrollRecomputeService` and `PayrollFnfOrchestrator`). Feeding the rows through the engine *and* setting `ArrearTaxableAmount` would count arrears twice.
- **Bonus ×N is a prerequisite, not a follow-up.** Existing one-time earnings (bonus, WI-011) flow through `taxableWage` and are currently projected ×N. Resolve whether that is a bug or an intended approximation **before** build step 1 — it decides whether the engine has one or two one-time-taxable code paths. Build the project-once path so it covers both arrears and bonus consistently. Tracked in Deferred / Open Questions.

---

## 5. Data model changes

`SalaryRevision` — add:
- `Guid? ResultingSalaryStructureId` (FK to the backdated `EmployeeSalaryStructure` created on apply). Set via an **extended `Apply(Guid resultingSalaryStructureId, Guid updatedBy)`** method (the entity uses private setters; do not add a public setter). The property has a private setter consistent with the entity's encapsulation.
- `Guid? ArrearPaidRunId` — set when a payout run carrying this revision's arrears is **finalised** (not on apply, not on approval). Injection skips revisions that already have `ArrearPaidRunId`; if that run is later rejected/deleted, clear the field so a re-run re-injects (see §6).
- **Override capture (locked): `SalaryRevisionComponentOverride` child table** mirroring `EmployeeSalaryComponentOverride`. CTC + template alone cannot reproduce per-component arrears; the snapshot makes the new structure fully reproducible. On apply, hydrate the new `EmployeeSalaryStructure.ComponentOverrides` from this snapshot. **Invariant:** a revision must resolve to a complete component set (template required, or a complete overrides snapshot) — `BuildComponentInputs` returns `[]` when template is null, so a template-less, override-less revision is rejected at create time.

Migration: reversible `Up`/`Down`, `timestamptz`, schema-per-tenant. Extends the existing `SalaryRevisionConfiguration`; adds the FK columns + the `SalaryRevisionComponentOverride` child table.

---

## 6. Application layer (follows FnF template)

**Repository:** `ISalaryRevisionRepository` (Domain.Interfaces) + EF impl
- `AddAsync`, `GetPendingByPayoutPeriodAsync(year, month)`, `GetByEmployeeAsync`, `Update`.

**Commands** (`Commands/Employees/` or new `Commands/SalaryRevisions/`):
- `CreateSalaryRevisionCommand` — capture EmployeeId, NewAnnualCTC, EffectiveFromMonth/Year, PayoutMonth/Year, template, overrides, notes. Validator: new CTC > 0; effective ≤ payout; effective month has/will have a completed run for arrears (warn, don't block). Creates `SalaryRevision` (`Pending`).
- `ApplySalaryRevisionCommand` — backdated structure swap (explicit operator action per §3 decision 8):
  1. Close active `EmployeeSalaryStructure` at `EffectiveFrom − 1 day`.
  2. Create new structure with `EffectiveFrom =` revision effective date, hydrating overrides from the `SalaryRevisionComponentOverride` snapshot. *(Generalise `AssignSalaryStructureCommand` to accept an explicit `EffectiveFrom` — preferred over a sibling, to avoid duplicating the close/create logic. The existing caller passes `today`.)*
  3. `revision.Apply(resultingStructureId, actorId)` — links `ResultingSalaryStructureId` and sets `Applied`.

**Arrear computation service** `ISalaryArrearCalculator` (Application service, mirrors `PayrollFnfOrchestrator`):
- **Inputs (full set):** the revision; its resolved new structure **with template + components** (load via the template repo, like `InitiatePayrollRunCommand`); added-component details from the component repo; and, per back month, that run's persisted `PayrunComponentBreakdown` + frozen `StatutoryConfigSnapshot`. Reuse `InitiatePayrollRunCommand.BuildComponentInputs` (make it shared, don't duplicate) to resolve the new structure into per-component amounts.
- For each back month in `[EffectiveFrom .. Payout − 1]` that has a finalised run:
  - Recompute the month against the new structure, replaying the **exact persisted proration basis** (`payableDays`, `SalaryDivisor`, per-component `CalculateOnProRata`/`IsFlat`) — not `LopDays` alone (§3 decision 1).
  - Diff per component (full-outer-join on code) → arrear per component.
- Output: per-component `ARREAR_<CODE>` rows (taxable, `ConsiderForEpf/Esi = false`, `IsOneTimeEarning = true`, **real `SalaryComponentId`**) + total arrear taxable. Months in range with no finalised run are returned as a warning list (§3 decision 5). Net-negative total is clamped to zero (§3 decision 7).

> **Naming:** `ARREAR_<CODE>` (e.g. `ARREAR_BASIC`, `ARREAR_HRA`) is the internal row code/identifier; it renders on the payslip as `Arrears - Basic`, `Arrears - HRA` (§8a). The two are the same row, code vs display name.

**Injection into payout run** (mirrors FnF row-build in `PayrollRecomputeService` / `UpdateFnfRunHandler`):
- On initiate/recompute of the payout-month regular run, pull **`Applied`** revisions for the period whose `ArrearPaidRunId` is null (never `Pending` — §3 decision 8).
- **Idempotent:** before re-injecting, **delete existing `ARREAR_*` rows tagged with the revision id** for that run, then rebuild — so recompute-before-approval never duplicates arrears. (Test: recompute twice → one set of ARREAR rows, one arrear TDS.)
- Add `ARREAR_*` breakdown rows (persist/display-only — excluded from `engineRows`, §4); pass the per-employee arrear taxable total to the engine **once** via `ArrearTaxableAmount`.
- **On run finalisation** (not approval): set `ArrearPaidRunId` on each revision. **On run reject/delete:** clear `ArrearPaidRunId` so the next run re-injects (§5).

**Preview query** `GetSalaryRevisionArrearPreviewQuery` — copy `GetFnfPreviewQuery`: build hypothetical arrear rows in memory, recompute, return a DTO (gross, arrear total, per-month/per-component breakdown, payout-month TDS/PT impact) **without persisting**.

### 6.1 Bulk import (Zoho: revisions imported under the Employees module)

Follow the existing **two-phase validate→commit** employee-import pattern (`ValidateEmployeeImportCommand` + `CommitEmployeeImportCommand`), not the run-scoped bulk commands. Flow: download template → fill → upload `.xls`/`.csv` → preview (flag skipped/invalid rows) → commit, with **Skip/Overwrite** duplicate handling.

**No field-mapping step.** The user must adhere to the downloaded template's fixed column order/names; the validator parses by the expected header schema directly. Reject the file if headers don't match the template (clear error), rather than offering a mapping UI.

**Template generator** (mirror `IEmployeeImportTemplateGenerator`): emits the `.xls` with a **colour-coded header row** — one colour for **required** columns, another for **optional** — plus a header legend. Columns: EmployeeCode `[req]`, NewAnnualCTC `[req]`, EffectiveFromMonth `[req]`, EffectiveFromYear `[req]`, PayoutMonth `[req]`, PayoutYear `[req]`, TemplateName `[opt]`, per-component override columns `[opt]`, Notes `[opt]`.

- `ValidateSalaryRevisionImportCommand` — parse against the fixed schema, validate per row (employee exists, CTC > 0, effective ≤ payout, template resolvable), return a preview report (valid rows, skipped rows with reasons). **No persistence.**
- `CommitSalaryRevisionImportCommand` — create `SalaryRevision` (`Pending`) per valid row; duplicate (existing pending revision for same employee+payout) handled by **Skip** or **Overwrite** flag.
- Arrears for imported revisions compute through the **same** calculator (§6) when their payout run is initiated — no separate bulk-arrear path.

**Security (mandatory — bulk import is kept in this WI, so these are not optional):**
- **RBAC.** Restrict `/import/validate` and `/import/commit` to the `PayrollAdmin` role/policy — not bare `[Authorize]`. A read-only user must not create revisions. (Applies equally to the §7 single-revision endpoints.)
- **Tenant-scoped EmployeeCode resolution.** Resolve each `EmployeeCode` **only within the caller's tenant schema**. A code that doesn't resolve in-tenant is reported as "employee not found" — never a cross-tenant lookup. Prevents cross-tenant writes via a crafted file in this schema-per-tenant system.
- **Upload hardening.** Enforce a max upload size (e.g. 5 MB) and MIME validation **before** the parser runs; use a streaming/row-capped parser with **formula evaluation disabled** for `.xls` (zip-bomb / formula-injection / DoS protection).
- **Formula-injection on output.** The template generator (and any preview-report export) must write string cells as text (prefix `'` or force `CellType.String`) so values beginning `=`, `+`, `-`, `@` are not executed by Excel on the client.
- **Audit.** Commit stamps every created row's `CreatedBy` with the importing user's identity (from the JWT `sub` claim via `ICurrentUserService`).

Endpoints (mirror employee import):
- `POST /api/v1/salary-revisions/import/validate`
- `POST /api/v1/salary-revisions/import/commit`
- `GET  /api/v1/salary-revisions/import/template`

---

## 7. API (follows `EmployeesController` / `PayrollRunsController`)

- `POST /api/v1/employees/{id}/salary-revisions` → `CreateSalaryRevisionCommand`
- `POST /api/v1/employees/{id}/salary-revisions/{rid}/preview-arrears` → `GetSalaryRevisionArrearPreviewQuery`
- `POST /api/v1/employees/{id}/salary-revisions/{rid}/apply` → `ApplySalaryRevisionCommand`
- `GET  /api/v1/employees/{id}/salary-revisions` → list (status, effective, payout, amounts)

**Authz:** `[Authorize(Roles = "PayrollAdmin")]` (or policy equivalent) on create/apply/import — bare `[Authorize]` is insufficient for a privileged, irreversible compensation action. The **apply** endpoint additionally re-checks that the revision's `EmployeeId` belongs to the caller's tenant before swapping the structure. `CreateSalaryRevisionCommand` and `ApplySalaryRevisionCommand` populate `CreatedBy` / applied-by from `ICurrentUserService` (JWT `sub`) for the audit trail. Tenant from JWT; exceptions mapped Domain→422, NotFound→404, Validation→400.

---

## 8. Frontend (follows existing revise flow + FnF page)

UI surfaces:
1. **Revise form** — entry already exists: `EmployeeSalaryTab.tsx` "Revise" → `WizardStep2Salary` (`?revise=1`). Extend to capture **Effective From** and **Payout** alongside CTC/template/override inputs.
   - **Month input:** two controlled selects per field — Month (Jan–Dec by name) + Year — matching the payroll-run period-selection pattern (month-only, so the `dd/MM/yyyy` display rule doesn't apply). Enforce `Payout ≥ Effective From` inline on blur of the later field.
   - **Warn state (non-blocking):** when the selected Effective-From month has no finalised run, show an inline warning (Lucide `AlertTriangle`, warning token) — "No finalised run for {month/year}; arrears can't be computed for it — revision applies forward only." Must **not** disable submit (mirrors the §6 validator's warn-don't-block rule).
2. **Arrears preview panel** (mirror `FnfSettlementPage`): on input change, POST preview → per-month + per-component arrear lines, arrear total, payout-month PT delta and the (spread) TDS impact, **before** applying. Define explicit states:
   - **Zero** (future-dated / no finalised back-months): empty state, "No arrears — revision applies forward only."
   - **Negative** (decrease revision): warning banner "This revision reduces prior-month pay; v1 does not recover already-paid amounts" — negative total clamped to zero per §3 decision 7.
   - **Excluded-months** warning when some in-range months had no finalised run (§3 decision 5).
   - Reuse / confirm `FnfSettlementPage`'s loading + error states.
3. **Revisions list** (employee Salary tab): table — status (`Pending`/`Applied`), effective, payout, prev→new CTC, arrear total. Default sort: payout desc; show 10 most recent with "Show all"; `Pending` and `Applied` both shown.
4. **Apply action:** explicit (§3 decision 8). Surface as the primary CTA in the arrears preview panel; confirmation dialog showing effective date, payout month, arrear total; on success the revision row flips to `Applied` inline. Error state when apply fails (structure already swapped, run already finalised).
5. **Bulk import screen** (reuse the employee-import UI shell): download template → upload → preview → Skip/Overwrite → commit. **No field-mapping step** (validator enforces template adherence). Preview screen: summary count ("N valid, M skipped"), a rows table with a Status column and a Reason column populated only for skipped rows; commit enabled when M > 0 (Skip/Overwrite is the mode). Skip/Overwrite chosen via a labelled toggle/radio. Header-mismatch → clear top-level error.

Design system mandatory: `formatINR`, `dd/MM/yyyy` display, tokens, Lucide icons. No external component libraries.

## 8a. Payslip

`PayslipPdfGenerator.cs:145` renders earnings via `data.Components.Where(c => c.IsEarning)`, printing `ComponentName`. Arrear breakdown rows therefore appear automatically under **Earnings** once they map into `PayslipData.Components` as earnings — verify/extend the breakdown→`PayslipData` mapper (`PayslipRepository`) to include `ARREAR_*` rows.

**Granularity — per-component (locked).** One arrear line per affected component: "Arrears - Basic", "Arrears - HRA", … Each `ARREAR_<CODE>` row carries its own friendly name and amount. The arrear lines are part of the **payout-month** payslip gross; statutory deductions reflect §3 (TDS/PT include arrears, PF/ESI do not). Add a `PayslipNotes` line stating the revision effective date and arrear period (the FnF flow already has a payslip-notes field as precedent).

---

## 9. Build order (TDD, each step verifiable)

1. **Engine:** `EmployeeInput.ArrearTaxableAmount` + `GrossCalculator` project-once → unit tests with exact decimals (recurring ×N + arrear ×1). *Verify: arrear not multiplied.*
2. **Data model:** migration (FK + overrides) → rollback tested.
3. **Apply command:** backdated structure swap → integration test (old closed at effective−1, new effective-dated, revision `Applied`).
4. **Arrear calculator:** per-back-month diff from persisted breakdowns + frozen config → unit/integration tests (single month, multi-month, mid-month LOP, future-dated = zero).
5. **Payout injection + preview:** `ARREAR_*` rows into payout run, preview query → integration tests (PT slab jump captured; TDS arrear taxed once).
6. **Payslip:** map `ARREAR_*` rows into `PayslipData.Components` as **per-component** earning lines ("Arrears - Basic", "Arrears - HRA", …) per §8a → snapshot/render test.
7. **API + frontend:** revision endpoints, revise form fields, preview panel, revisions list.
8. **Bulk import:** validate→commit commands + template generator + import UI → integration tests (valid/skipped rows, Skip/Overwrite).

---

## 10. Deferred / Open Questions

Genuinely unresolved items that must be answered before the dependent build step — not leftover ambiguity (apply-trigger, override shape, payslip granularity are now locked in §3/§5/§8a).

- **[Compliance — blocks §3 decision 2] Back-month PF/ESI on arrears.** The "already paid, don't re-collect" stance is **unconfirmed**. Before locking: confirm actual EPFO/ESIC treatment of backdated-revision arrears in the payout month, and verify Zoho's behaviour. Outcome is either (a) an owned, cited v1 stance, or (b) a disclosed customer-facing limitation ("v1 arrears do not collect PF/ESI"). Do not ship arrears to a paying customer until this is decided. *(Deferred from review — product-lens.)*
- **[Prerequisite to build step 1] Bonus/one-time TDS ×N.** Existing bonus (WI-011) flows through `taxableWage` and is projected ×N. Determine bug vs intended approximation; the project-once engine path (§4) should cover arrears **and** bonus consistently so the engine has a single one-time-taxable code path. Resolve before authoring the step-1 engine tests.
