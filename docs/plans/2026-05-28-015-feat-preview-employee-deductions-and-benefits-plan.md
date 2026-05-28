# PR4 — Employee deductions, net pay, and benefits in salary preview

Status: planned
Date: 2026-05-28
Predecessor PRs (already shipped):
- `2c3c225` — PR1: shared SalaryStructurePreviewCalculator
- `b133486` — PR1 follow-up: orgFlags wiring
- `8ab1409` — PR2: template-level statutory toggles
- `acfc7b2` — PR3: backend-authoritative preview endpoint

---

## Problem

The salary-structure preview currently shows:

1. Earnings (Basic, HRA, Special Allowance) — correct
2. Employer-side statutory in CTC (employer EPF + EPS, gratuity accrual) — correct

It does **not** show:

3. Employee-side statutory deductions: employee EPF (12% of PF wage), employee ESI (0.75% if below cap), Professional Tax (per state slab), employee LWF (per state)
4. Net pay (take-home) — gross minus employee deductions
5. Benefits as a separate section — Category=Benefit components (LTA, food coupons, VPF, NPS) either silently roll into earnings or are tracked in the wizard's `addedBenefits` array but never fed into the preview

Net effect: operator setting up ₹6L CTC sees employer cost lines but not what the employee actually takes home, and benefits are invisible. The number that matters most to the employee — monthly take-home — is missing from every preview surface.

---

## Goal

Single shared calculator (backend + TS mirror per PR3 contract) produces a full breakdown:

```
Earnings:
  Basic                ₹25,000
  HRA                  ₹10,000
  Special Allowance    ₹11,998
  ─────────────────
  Gross                ₹46,998

Benefits (not in monthly gross):
  LTA                  ₹4,167 (annualised /12)
  Food Coupons         ₹1,500

Employee deductions:
  Employee EPF         −₹1,800
  Employee ESI              −0   (above cap)
  PT (Kerala)            −₹100
  Employee LWF            −₹50
  ─────────────────
  Take-home (net)      ₹45,048

Employer contributions (included in CTC):
  Employer EPF + EPS   ₹1,800
  Gratuity accrual     ₹1,202
  ─────────────────
  CTC (annual)         ₹6,00,000
```

Three render contexts:
- **Settings builder** — template-level; no employee context. State-agnostic deductions estimated using a tenant-default state (configurable) or shown as "varies by employee state".
- **Hire wizard** — full employee context (state from work location). Exact deductions.
- **Employee detail tab** — full context, same as wizard.

---

## Decision points

### D1. State source for builder preview

Builder doesn't have an employee → no state → can't run PT/LWF slab match.

**Option A (recommended):** Skip PT + LWF in builder preview. Show a note: "PT and LWF depend on employee work-location state; see hire wizard for exact figures."
**Option B:** Operator selects a "preview state" dropdown on builder. Adds UI complexity.
**Option C:** Use the tenant's first work location's state as default.

**Recommendation: Option A.** PT/LWF are small fixed amounts (≤ ₹250/mo), don't materially change builder preview's purpose (gross + CTC composition). Adding them needs state UI churn for marginal accuracy.

### D2. Employee EPF rate source

Engine reads `IncomeTaxConfig.EpfEmployeeRate` (0.12 default). Org config has `EpfEmployeeContributionRate` as a string enum (`ActualPfWage12`, `RestrictedWage12`). Apply same restrict-employer-wage logic for employee side.

**Decision:** Mirror engine: `employeeEpf = round(min(pfWage, PfWageCap) × EpfEmployeeRate, 2, AwayFromZero)` when both org+employee EPF enabled.

### D3. Benefits — taxable or not?

Benefits live in their own DTO array in wizard (`addedBenefits[]`) separate from `addedComps` (earnings). Some benefits (VPF, NPS) reduce taxable income; others (LTA) are partially taxable. Preview shouldn't compute tax implications — engine handles. Preview just **shows** benefits as a separate section so operator sees the full compensation package.

**Decision:** Show benefits as a separate "Benefits (not in monthly gross)" section. No tax math in preview. Annual amounts displayed.

### D4. Source of truth for PT slabs + LWF configs in preview

Engine reads these from DB via `IProfessionalTaxSlabRepository` + `ILwfStateConfigRepository`. Preview handler needs same.

**Decision:** Inject both repos into `GetSalaryStructurePreviewHandler` + `GetEmployeeSalaryStructureHandler`. Load slabs for the relevant state (passed in by caller for wizard, omitted for builder per D1).

### D5. Calculator API expansion

Add to `SalaryStructurePreviewCalculator.Inputs`:
- `WorkStateCode: string?` (null in builder mode → skip PT/LWF)
- `Benefits: IReadOnlyList<BenefitInput>` (annual amounts)
- `PtSlabs: IReadOnlyList<PtSlab>` (loaded by handler)
- `LwfConfigs: IReadOnlyList<LwfStateConfig>` (loaded by handler)

Extend `Output` with:
- `EmployeeDeductions: IReadOnlyList<DeductionRow>` (EPF, ESI, PT, LWF — only those that apply)
- `NetPayMonthly: decimal`
- `BenefitRows: IReadOnlyList<BenefitRow>`

### D6. Reuse engine code vs reimplement

Engine has `PTCalculator`, `LWFCalculator`, `ESICalculator`, `PFCalculator`. Preview should ideally call them directly — single source of truth, matches what payroll will deduct.

**Decision:** Call engine calculators from preview. Engine is pure (no DI, no I/O) so the dependency is clean. Inputs require building a minimal `EmployeeInput` + `PayrollRunInput` for the preview. Trade-off: more entity construction in preview handler, but eliminates duplicated statutory logic. Already accepted similar trade-off for PF wage cap subtraction in PR1.

---

## Scope

### Backend

1. **`SalaryStructurePreviewCalculator`** (Application/pure):
   - Add `WorkStateCode`, `Benefits`, `PtSlabs`, `LwfConfigs`, `EsiWageLimit`, `EsiPwdWageLimit`, `EsiEmployeeRate` to `Inputs`
   - Add `IsPwd: bool` to `EmployeeStatutoryFlags`
   - Compute employee deductions by delegating to engine calculators (`PFCalculator`, `ESICalculator`, `PTCalculator`, `LWFCalculator`)
   - Compute net pay = gross − sum of employee deductions
   - Separate benefits into output

2. **`GetSalaryStructurePreviewQuery`**:
   - Accept `WorkStateCode` + `Benefits[]` in request
   - Inject `IProfessionalTaxSlabRepository`, `ILwfStateConfigRepository`, `IIncomeTaxConfigRepository` to load slabs/caps
   - Build `EmployeeStatutoryFlags` with IsPwd from employee (for wizard) or false (for builder)
   - Pass through to calculator

3. **`GetEmployeeSalaryStructureQuery`**:
   - Same updates; load `WorkLocation.State.ToIsoCode()` for the employee
   - Include benefits from `employee_salary_component_overrides` where component category=Benefit

4. **Response DTOs**:
   - Add `EmployeeDeductions[]`, `NetPayMonthly`, `Benefits[]` to `SalaryStructurePreviewDto`
   - Add same to `EmployeeSalaryStructureDto`

5. **Controller**:
   - Extend `SalaryStructurePreviewRequest` with `WorkStateCode` + `Benefits[]`

### Frontend

1. **`salaryStructurePreview.ts`**:
   - Mirror calculator extensions: types + local `computePreview`
   - TS port can either duplicate (drift risk acknowledged) or strip the local calc and rely solely on API (no instant fallback). Per PR3 reviewer findings, prefer the latter — drop local mirror entirely for new fields (deductions + benefits + net pay). Existing earnings/employer fallback stays.

2. **`useSalaryStructurePreview`**:
   - Pass `workStateCode` + `benefits` from caller
   - Honor new response fields

3. **Settings builder** (`SalaryStructureBuilderPage.tsx`):
   - Pass `workStateCode: null`
   - Render note: "PT and LWF depend on employee work-location state; estimated as zero for template preview"
   - Render new sections: Employee deductions (EPF + ESI only when applicable), Net pay (computed), Benefits (currently zero since builder has no benefits)

4. **Hire wizard** (`WizardStep2Salary.tsx`):
   - Pass `workStateCode` from selected work location (already in wizard step 1)
   - Pass `benefits` from `addedBenefits[]` state (currently not threaded into preview)
   - Render new sections

5. **Employee tab** (`EmployeeSalaryTab.tsx`):
   - Render new sections from response

---

## Implementation order

1. **Backend calculator + handler changes** (~1.5h)
2. **Backend tests** — extend `SalaryStructurePreviewCalculatorTests` with deduction + benefits cases; new handler test for state-loading + benefit threading (~45m)
3. **Frontend types + hook updates** (~30m)
4. **Builder UI** — render employee deduction + net pay sections; "varies by state" note for PT/LWF (~45m)
5. **Wizard UI** — same render + thread `addedBenefits` into preview inputs (~45m)
6. **Employee tab UI** — render new sections (~30m)
7. **Build green + manual smoke** (~30m)

Total: ~5h focused. Same shape as PR1.

---

## Tests

Calculator unit tests (extend `SalaryStructurePreviewCalculatorTests`):
- `EmployeeEpf_SubtractsFromNet_WhenBothFlagsOn`
- `EmployeeEpf_Zero_WhenEmployeeOptedOut`
- `EmployeeEsi_AppliesAtWageBelowLimit`
- `EmployeeEsi_Zero_AboveLimit`
- `EmployeeEsi_PwdHigherLimit`
- `Pt_AppliesFromSlab_WhenStateProvided`
- `Pt_Skipped_WhenStateNull` (builder mode)
- `Lwf_AppliesFromConfig_WhenStateProvided`
- `Benefits_RenderedSeparately_FromEarnings`
- `NetPay_EqualsGross_MinusAllEmployeeDeductions`

Handler tests (extend `GetSalaryStructurePreviewHandlerTests`):
- `Preview_LoadsPtSlab_ForGivenState`
- `Preview_Skips_Pt_WhenStateNull`
- `Preview_IncludesBenefits_PassedInRequest`

Integration: existing wizard + employee tab smoke (no automated test in scope).

---

## Out of scope (deliberately deferred)

- **One-time earnings / deductions** — these are payroll-run-specific, not template preview
- **TDS preview** — engine derives from annual projection; preview would need YTD context, prior-employer, regime config. Different concern. Defer to PR5 if asked.
- **Variable inputs (LOP-prorated preview)** — preview is structural, not run-time
- **Statutory bonus** — engine handles per StatutoryOrgConfig.StatutoryBonusEnabled; not material to monthly take-home preview

---

## Open questions

1. **Where exactly do the new "Employee deductions" + "Net pay" sections render?** Below the components table, above CTC footer? Or as a parallel column? Decision after a Figma sketch — for now match the section-stack pattern from PR1's employer contributions.
2. **Should builder show employee deductions at all when state is null?** Per D1, employee EPF + ESI are state-independent so they could show. Only PT + LWF need the state. Compromise: show EPF + ESI in builder; note that PT + LWF "vary by employee state".

---

## Migration / data impact

None. Pure read-path additions. No schema changes. No backfills. Existing approved payroll runs unaffected.

---

## Risk

Low-medium.
- **Engine reuse risk:** if engine calculators evolve, preview must keep calling them — coupling is the point. Acceptable.
- **Builder state-agnostic confusion:** operator may not realize PT/LWF estimates are zero. Mitigated by explicit note.
- **TS mirror drift:** decision D6 trade-off — preview correctness depends on API roundtrip for the new sections. Acceptable since these sections only render after API responds (no fallback shown).
