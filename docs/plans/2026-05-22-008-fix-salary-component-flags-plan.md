---
title: "fix: Enforce salary component flags in payroll engine and payslip pipeline"
type: fix
status: active
date: 2026-05-22
---

# fix: Enforce salary component flags in payroll engine and payslip pipeline

## Overview

Six salary component flags are defined and stored in the domain but silently ignored during payroll computation. This causes incorrect TDS (overtaxed), wrong ESI wage (wrong eligibility), all components pro-rated regardless of flag (wrong LOP deductions), flat components wrongly docked, all components shown on payslip, and no active/inactive toggle in the UI. Each fix is surgical — one flag, one computation site, one test pass.

---

## Problem Frame

Flags are wired at entity definition but never consumed downstream:
- `IsTaxable` → TDS uses full gross (employees overtaxed on non-taxable allowances)
- `ConsiderForEsi` → ESI uses full gross (wrong eligibility + wrong contribution amounts)
- `CalculateOnProRata` → GrossCalculator pro-rates every component unconditionally
- `PayType (FlatAmount)` → flat one-time components docked by LOP days
- `ShowInPayslip` → all breakdowns returned to payslip regardless of flag
- `IsActive` → no UI toggle in add/edit modals (logic correct, UX gap)

---

## Requirements Trace

- R1. Components with `IsTaxable = false` must be excluded from TDS taxable gross
- R2. Components with `ConsiderForEsi = false` must be excluded from ESI wage
- R3. Components with `CalculateOnProRata = false` must not be reduced by LOP proration
- R4. Components with `PayType = FlatAmount` must not be pro-rated by LOP days
- R5. Components with `ShowInPayslip = false` must not appear on payslip
- R6. Add/Edit earning modals must expose `IsActive` toggle

---

## Scope Boundaries

- No changes to `isPartOfSalaryStructure` (variable inputs) — separate feature
- No changes to PT, LWF, PF calculators — those wage bases are already correct
- No breaking changes to `GrossResult` public record surface beyond additions

---

## Context & Research

### Relevant Code and Patterns

- `src/Payroll.Engine/Inputs/SalaryComponentInput.cs` — record, positional params
- `src/Payroll.Engine/Outputs/GrossResult.cs` — record, positional params
- `src/Payroll.Engine/Calculators/GrossCalculator.cs` — 46 lines, single loop
- `src/Payroll.Engine/Calculators/TDSCalculator.cs` — takes `annualProjectedGross`
- `src/Payroll.Engine/Calculators/ESICalculator.cs` — takes `grossWage`
- `src/Payroll.Engine/PayrollEngine.cs` — ComputeOne wires gross → calculators
- `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs` — `BuildComponentInputs` passes flags into `SalaryComponentInput`
- `src/Payroll.Domain/Entities/PayrunComponentBreakdown.cs` — Create factory
- `src/Payroll.Application/Queries/PayrollRuns/GetPayslipDataQuery.cs` — no filter on breakdowns today
- `src/Payroll.Infrastructure/Services/PayslipPdfGenerator.cs` — renders all `data.Components`
- `tests/Payroll.Engine.Tests/GrossCalculatorTests.cs` — existing test pattern
- `tests/Payroll.Engine.Tests/TDSCalculatorTests.cs` — existing test pattern

### Key Pattern: Record Extension

`SalaryComponentInput` is a positional record. Adding a new parameter requires updating every construction site in `BuildComponentInputs` and all engine tests. Use optional parameters with sensible defaults to minimise blast radius.

---

## Key Technical Decisions

- **Taxable gross in GrossResult, not TDSCalculator**: TDSCalculator stays pure (no component awareness). GrossCalculator builds `TaxableGrossWage` alongside `GrossWage`. PayrollEngine passes the right value to each calculator.
- **ESI wage in GrossResult**: Same pattern — GrossCalculator builds `ESIWage`; PayrollEngine passes it to ESICalculator.
- **`CalculateOnProRata` and `IsFlat` both skip pro-rata in GrossCalculator**: One condition `(!c.CalculateOnProRata || c.IsFlat)` → use full amount.
- **`ShowInPayslip` stored in breakdown at run time**: Simpler than re-joining to `salary_components` at payslip query time. Requires migration + updated `Create` factory.
- **DB migration for `show_in_payslip`**: Backfill default `true`. `nullable: false, defaultValue: true`.

---

## High-Level Technical Design

> *Directional guidance, not implementation specification.*

```
BuildComponentInputs() → SalaryComponentInput {
    + CalculateOnProRata: bool
    + ConsiderForEsi: bool
    + IsFlat: bool          // PayType == FlatAmount
}

GrossCalculator.Compute() → GrossResult {
    + TaxableGrossWage: decimal     // Σ prorated where IsTaxable
    + ESIWage: decimal              // Σ prorated where ConsiderForEsi
    // per-component: skip prorata if !CalculateOnProRata || IsFlat
}

PayrollEngine.ComputeOne():
    ESICalculator.Compute(gross.ESIWage, ...)       // was gross.GrossWage
    TDSCalculator.Compute(gross.AnnualProjectedTaxableGross, ...)  // was gross.AnnualProjectedGross

PayrunComponentBreakdown.Create(..., showInPayslip: bool)
InitiatePayrollRunCommand: stores component.ShowInPayslip ?? true
GetPayslipDataQuery: breakdowns.Where(b => b.ShowInPayslip)
```

---

## Implementation Units

- U1. **Add `CalculateOnProRata` and `IsFlat` to SalaryComponentInput; fix GrossCalculator**

**Goal:** Pro-rata skipped for components where `CalculateOnProRata = false` or `PayType = FlatAmount`.

**Requirements:** R3, R4

**Dependencies:** None

**Files:**
- Modify: `src/Payroll.Engine/Inputs/SalaryComponentInput.cs`
- Modify: `src/Payroll.Engine/Calculators/GrossCalculator.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/AddOneTimeEarningCommand.cs`
- Test: `tests/Payroll.Engine.Tests/GrossCalculatorTests.cs`

**Approach:**
- Add `CalculateOnProRata = true` and `IsFlat = false` as optional params with defaults to `SalaryComponentInput` record
- In `GrossCalculator` loop: `bool skipProRata = !c.CalculateOnProRata || c.IsFlat;` → use `c.Amount` directly when true
- In `BuildComponentInputs` final `.Select()` pass: also map `CalculateOnProRata` from `sc.CalculateOnProRata ?? true` and `IsFlat` from `sc.PayType == PayType.FlatAmount`
- `AddOneTimeEarningCommand` one-time earnings already use `fullAmount = proratedAmount` so `IsFlat = true` is correct semantically

**Test scenarios:**
- Happy path: component with `CalculateOnProRata = true` and 2 LOP days → amount reduced proportionally
- Happy path: component with `CalculateOnProRata = false` and 2 LOP days → full amount returned, not reduced
- Happy path: component with `IsFlat = true` and 5 LOP days → full amount returned
- Edge case: `LOPDays = 0` → all components return full amount regardless of flags
- Edge case: mix of pro-ratable and non-pro-ratable components in same run → correct split

**Verification:** All GrossCalculatorTests green; new flag scenarios pass; `LOPDeduction` field only reflects pro-ratable components.

---

- U2. **Build `ESIWage` in GrossResult; fix ESICalculator intake**

**Goal:** ESI eligibility and contribution computed on ESI wage (only `ConsiderForEsi = true` components), not full gross.

**Requirements:** R2

**Dependencies:** U1 (SalaryComponentInput already extended)

**Files:**
- Modify: `src/Payroll.Engine/Inputs/SalaryComponentInput.cs`
- Modify: `src/Payroll.Engine/Outputs/GrossResult.cs`
- Modify: `src/Payroll.Engine/Calculators/GrossCalculator.cs`
- Modify: `src/Payroll.Engine/PayrollEngine.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs`
- Test: `tests/Payroll.Engine.Tests/GrossCalculatorTests.cs`

**Approach:**
- Add `ConsiderForEsi = false` optional param to `SalaryComponentInput`
- In `GrossCalculator` loop: accumulate `esiWage += prorated` when `c.ConsiderForEsi`
- Add `ESIWage: decimal` to `GrossResult` positional record
- In `PayrollEngine.ComputeOne`: `ESICalculator.Compute(gross.ESIWage, ...)`
- In `BuildComponentInputs` final select: map `ConsiderForEsi` from `sc.ConsiderForEsi ?? false`

**Test scenarios:**
- Happy path: employee with Basic (ConsiderForEsi=true) + Bonus (ConsiderForEsi=false), gross=25000 → ESI wage = Basic only; ESI applied if Basic ≤ limit
- Happy path: employee where ESI wage < limit but gross > limit → ESI applies (eligibility on ESI wage, not gross)
- Happy path: employee where ESI wage > limit → ESI exempt
- Edge case: all components have `ConsiderForEsi = false` → ESI wage = 0 → exempt
- Integration: ESI contribution amount matches `esiWage × rate`, not `grossWage × rate`

**Verification:** ESICalculatorTests pass; employees with non-ESI components have correct eligibility; `gross.ESIWage` correctly excludes non-ESI components.

---

- U3. **Build `TaxableGrossWage` in GrossResult; fix TDS intake**

**Goal:** TDS computed on taxable gross only (components where `IsTaxable = true`).

**Requirements:** R1

**Dependencies:** U1, U2 (GrossResult already being extended)

**Files:**
- Modify: `src/Payroll.Engine/Outputs/GrossResult.cs`
- Modify: `src/Payroll.Engine/Calculators/GrossCalculator.cs`
- Modify: `src/Payroll.Engine/PayrollEngine.cs`
- Test: `tests/Payroll.Engine.Tests/GrossCalculatorTests.cs`
- Test: `tests/Payroll.Engine.Tests/TDSCalculatorTests.cs`

**Approach:**
- In `GrossCalculator` loop: accumulate `taxableWage += prorated` when `c.IsTaxable`
- Add `TaxableGrossWage: decimal` and `AnnualProjectedTaxableGross: decimal` to `GrossResult`
- `AnnualProjectedTaxableGross = currentEmployerYTDTaxableGross + taxableWage * monthsRemainingInFY` — but `monthsRemainingInFY` lives in `PayrollRunInput`, not in `EmployeeInput`. GrossCalculator receives `PayrollRunInput`. Add to the calculation the same way `AnnualProjectedGross` is built.
- Actually: `GrossResult.AnnualProjectedTaxableGross` can be computed identically: `employee.CurrentEmployerYTDGross` → need a separate `CurrentEmployerYTDTaxableGross`. But that's a bigger change. Simpler: compute `AnnualProjectedTaxableGross` in `GrossCalculator` using `taxableWage * run.MonthsRemainingInFY + employee.CurrentEmployerYTDGross` (same YTD base, different current month). Note the YTD base is gross not taxable-gross — this is a known simplification: YTD from prior months doesn't break down by IsTaxable. Acceptable for v1; mark as deferred improvement.
- In `PayrollEngine.ComputeOne`: pass `gross.AnnualProjectedTaxableGross` to `TDSCalculator.Compute`

**Test scenarios:**
- Happy path: Basic (taxable) + HRA (non-taxable, 14000) → taxable gross = gross − HRA amount; TDS computed on taxable gross
- Happy path: all components taxable → `TaxableGrossWage == GrossWage`
- Happy path: no components taxable → `TaxableGrossWage = 0`, TDS = 0
- Edge case: LOP days affect taxable component → prorated taxable amount used
- Integration: employee with large non-taxable allowances → TDS lower than current (regression catch)

**Verification:** TDSCalculatorTests pass; employee with non-taxable HRA has lower TDS than same employee with all-taxable components.

---

- U4. **Add `ShowInPayslip` to PayrunComponentBreakdown; filter in payslip pipeline**

**Goal:** Components with `ShowInPayslip = false` excluded from payslip PDF and API response.

**Requirements:** R5

**Dependencies:** U1 (SalaryComponentInput extended; InitiatePayrollRunCommand patterns clear)

**Files:**
- Modify: `src/Payroll.Domain/Entities/PayrunComponentBreakdown.cs`
- Create: `src/Payroll.Infrastructure/Migrations/<timestamp>_AddShowInPayslipToBreakdown.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/AddOneTimeEarningCommand.cs`
- Modify: `src/Payroll.Application/Queries/PayrollRuns/GetPayslipDataQuery.cs`
- Modify: `src/Payroll.Infrastructure/Persistence/Repositories/PayrunComponentBreakdownRepository.cs`

**Approach:**
- Add `ShowInPayslip: bool` property to `PayrunComponentBreakdown`, default true
- Add `showInPayslip` param to `Create` factory, `defaultValue: true`
- Migration: `ALTER TABLE payrun_component_breakdowns ADD COLUMN show_in_payslip boolean NOT NULL DEFAULT true` — backfill all existing rows to true
- In `InitiatePayrollRunCommand`: pass `sc.ShowInPayslip ?? true` to `Create`
- In `AddOneTimeEarningCommand`: pass `showInPayslip: true` to `Create`
- In `GetPayslipDataQuery:56`: add `.Where(b => b.ShowInPayslip)` before the `.Select`

**Test scenarios:**
- Happy path: all components ShowInPayslip=true → all appear in payslip components list
- Happy path: one component ShowInPayslip=false → excluded from payslip list but gross/net unchanged
- Happy path: one-time earning always ShowInPayslip=true → always appears
- Edge case: all components ShowInPayslip=false → empty components list, gross/net still correct

**Verification:** Migration runs clean; existing payslips unaffected (all rows default true); hiding a component removes it from payslip without affecting net pay calculation.

---

- U5. **UI: Add `isActive` toggle to AddEarningModal and EditComponentModal**

**Goal:** Users can set/change the active state of an earning component from the add and edit forms.

**Requirements:** R6

**Dependencies:** None (UI-only, API already handles isActive on PUT)

**Files:**
- Modify: `web/src/pages/settings/salary-components/AddEarningModal.tsx`
- Modify: `web/src/pages/settings/salary-components/EditComponentModal.tsx`

**Approach:**
- `AddEarningModal`: add `const [isActive, setIsActive] = useState(true)` + Toggle component; include `isActive` in POST body
- `EditComponentModal`: add `isActive` to `ComponentDetail` interface; initialise from `detail.isActive`; include in PUT body for all categories (not just earning); expose as Toggle (not locked by `isAssociatedWithEmployee`)

**Test scenarios:**
- Test expectation: none — UI-only change; no business logic; covered by manual verification

**Verification:** Adding a new earning with isActive=false creates it inactive; EditComponentModal shows current state and saves correctly.

---

## System-Wide Impact

- **GrossResult record**: Adding `TaxableGrossWage`, `ESIWage` — all construction sites in tests must be updated
- **SalaryComponentInput record**: Adding optional params — all construction sites in tests must be updated (defaults cover them if positional)
- **PayrunComponentBreakdown.Create**: New `showInPayslip` param — all call sites updated
- **Net pay unchanged**: These fixes change taxable gross and ESI wage inputs, not the gross pay or net pay formula — net pay = grossWage − deductions still holds
- **Existing payslips**: Migration defaults all existing rows to `show_in_payslip = true` — no visual change for already-generated payslips
- **Unchanged invariants**: `GrossWage`, `PFWage`, `FullPFWage`, `LOPDeduction`, `AnnualProjectedGross` unchanged in semantics; PF calculation unaffected

---

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| Record positional changes break all construction sites | Use optional params with defaults; grep all usages before editing |
| AnnualProjectedTaxableGross uses gross YTD not taxable YTD | Documented simplification; prior-month YTD from DB is gross; acceptable for v1 |
| Migration on large table | Default value applied at DDL level — no row-by-row update needed in Postgres |
| ESI eligibility changes for existing employees | Intentional correctness fix; employees with non-ESI bonus components may shift eligibility |

---

## Sources & References

- Related code: `src/Payroll.Engine/`, `src/Payroll.Application/Commands/PayrollRuns/`
- Existing migration pattern: `src/Payroll.Infrastructure/Migrations/20260518120000_AddConsiderForEpfToBreakdown.cs`
