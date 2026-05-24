---
title: "feat: TDS Calculation End-to-End — FY 2026-27 New Regime"
type: feat
status: active
date: 2026-05-21
---

# feat: TDS Calculation End-to-End — FY 2026-27 New Regime

## Overview

TDS (Tax Deducted at Source) computation exists in the engine but is silently broken end-to-end. Six distinct defects mean every payroll run today produces ₹0 TDS for every employee. This plan fixes all six defects, adds statutory compliance for missing PAN (§206AA) and surcharge marginal relief, loads prior employer income for mid-year joiners, and persists the per-employee TDS worksheet so the Taxes tab shows real data.

New regime (Section 115BAC) only. Old regime is deferred per V1 scope.

---

## Problem Frame

When HR runs payroll, the engine computes TDS correctly in memory but the result is always zero because:
1. The tax slab table is empty — no rates in the database.
2. The fiscal year query key format mismatches — code queries `"2027"`, data would be `"2026-27"`.
3. Even if slabs existed, the result is never written to `tds_worksheets` — the Taxes tab reads that table.
4. Employees with no PAN should be taxed at 20% flat (§206AA) — not implemented.
5. Mid-year joiners carry prior employer income that affects TDS spreading — always hardcoded to ₹0.
6. Fallback values in `StatutoryConfigBuilder` are FY 2025-26 values — wrong rebate limit and amount.

Additionally: surcharge marginal relief is not implemented, creating over-deduction for employees earning just above the 50L/1Cr/2Cr thresholds.

---

## Requirements Trace

- R1. TDS computed at correct slab rates for FY 2026-27 new regime (Finance Act 2025).
- R2. Section 87A rebate applied correctly — zero tax for employees with taxable income ≤ ₹12L.
- R3. Surcharge applied at correct rates (10%/15%/25%) with marginal relief at each threshold.
- R4. Cess at 4% on (tax + surcharge).
- R5. §206AA: employees without PAN deducted at 20% flat of annual projected gross; no slab/rebate applied.
- R6. Prior employer gross salary and TDS deducted loaded from DB for mid-year joiners; used in annual projection and YTD offset.
- R7. One `TdsWorksheet` row persisted per included employee per payroll run.
- R8. Taxes tab displays per-employee TDS breakdown immediately after payroll run initiation.
- R9. Re-evaluate and LOP commands keep TdsWorksheet in sync after re-computation.
- R10. TDS override command keeps TdsWorksheet `TdsThisMonth` in sync with the manual override.
- R11. Engine coverage ≥ 95% (CI gate).

---

## Scope Boundaries

- Old tax regime (Section 115BAC old): deferred, mark `// DEFERRED: old-regime` at any touchpoint.
- Investment declarations by employees (80C, HRA, LTA): new regime does not need them; deferred.
- Form 16 / Form 16A generation: separate feature, not part of this plan.
- Other income sources (FD interest, rental): `PriorEmployerYtd.OtherIncome` field exists but not consumed in new regime V1 — deferred.
- Prior employer data entry UI: `PriorEmployerYtd` CRUD API and UI are a separate feature; this plan only loads the data if it already exists.

### Deferred to Follow-Up Work

- Prior employer YTD entry UI + API (CRUD commands/queries for `PriorEmployerYtd`): separate feature.
- `PriorEmployerYtd.OtherIncome` inclusion in taxable projection: deferred until other income module.

---

## Context & Research

### Relevant Code and Patterns

- `src/Payroll.Engine/Calculators/TDSCalculator.cs` — pure static, no I/O; `Compute()` signature must stay synchronous.
- `src/Payroll.Engine/Calculators/PFCalculator.cs` — reference pattern for engine calculator structure.
- `src/Payroll.Engine/Inputs/EmployeeInput.cs` — add `HasPan` bool here (mirrors `IsPWD`, `IsESIExempt` pattern).
- `src/Payroll.Engine/Inputs/StatutoryConfig.cs` — `TaxSlab`, `SurchargeConfig` records already exist.
- `src/Payroll.Engine/PayrollEngine.cs` — `ComputeOne` passes inputs to each calculator; wire `hasPan` here.
- `src/Payroll.Engine/Outputs/TDSResult.cs` — add `HasPanOverride` bool to result (already on `TdsWorksheet` entity).
- `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs` — already builds `EmployeeInput` per employee; `hasPan` computed at line 180 but unused.
- `src/Payroll.Application/Commands/PayrollRuns/ReEvaluateSkippedCommand.cs` — re-runs engine for skipped employees; needs `ITdsWorksheetRepository` injected (currently absent).
- `src/Payroll.Application/Commands/PayrollRuns/SetLopCommand.cs` — re-runs engine for one employee; needs `ITdsWorksheetRepository` injected (currently absent).
- `src/Payroll.Application/Commands/PayrollRuns/OverrideTdsCommand.cs` — updates `PayrunEmployee.TdsAmount`; needs to also update `TdsWorksheet.TdsThisMonth`.
- `src/Payroll.Application/Services/StatutoryConfigBuilder.cs` — fallback values must be updated to FY 2026-27.
- `src/Payroll.Domain/ValueObjects/PayPeriod.cs` — `FiscalYear` int property is correct (e.g., 2026 for FY 2026-27); only the DB key derivation string is wrong.
- `src/Payroll.Domain/Entities/TdsWorksheet.cs` — `HasPanOverride` field present; `Create()` factory exists.
- `src/Payroll.Domain/Interfaces/ITdsWorksheetRepository.cs` — `AddAsync`, `AddRangeAsync`, `GetByRunIdAsync`, `GetByRunAndEmployeeAsync`, `DeleteByRunIdAsync` all present.
- `src/Payroll.Domain/Entities/PriorEmployerYtd.cs` — entity exists with `GrossSalary`, `TdsDeducted`, `FinancialYear` (int, e.g., 2026).
- `src/Payroll.Infrastructure/Persistence/PayrollDbContext.cs` — `PriorEmployerYtds` DbSet exists; no repository yet.
- `tests/Payroll.Engine.Tests/PFCalculatorTests.cs` — reference pattern for engine unit tests (use Bogus for fixture data, exact decimal assertions, no mocks).
- `tests/Payroll.Engine.Tests/GratuityCalculatorTests.cs` — same pattern.

### Institutional Learnings

- No hardcoded statutory values in engine or application code — all values come from `StatutoryConfig` built from DB.
- Engine is pure: no EF, no DI, no async, no side effects. All inputs passed as parameters.
- `decimal` only for all monetary values. Never `float` or `double`.
- TDD: failing test first, minimum code to pass, then refactor.
- Schema-per-tenant: `PriorEmployerYtd` lives in tenant schema, loaded via `PayrollDbContext` (tenant-scoped).

### External References

- Finance Act 2025 (Budget 2025): new regime slabs for AY 2026-27 — nil slab extended to ₹4L, 25% band added (20–24L), standard deduction ₹75,000, 87A rebate limit ₹12L, max rebate ₹60,000.
- Section 206AA, Income Tax Act: 20% flat TDS when PAN not furnished; applies on "any sum" including salary.
- Marginal relief on surcharge: extra (tax + surcharge) increment must not exceed income increment above threshold.

---

## Key Technical Decisions

- **Fiscal year DB key format**: Use `$"{period.FiscalYear}-{(period.FiscalYear + 1) % 100:D2}"` (e.g., `"2026-27"`) as the DB query key. Do NOT change `FiscalYearLabel` property — it returns `"FY2027"` for display and other callers depend on that format.
- **§206AA application**: If `hasPan = false`, compute TDS as `20% × annualProjectedGross` directly. Skip slabs, standard deduction, rebate, surcharge, and cess entirely. The statutory rule is "higher of slab rate or 20%", but 20% flat on gross will always exceed slab rate in practice; this is the standard industry implementation.
- **Prior employer taxable income mapping**: Map `PriorEmployerYtd.GrossSalary` → `PriorEmployerYTDTaxableIncome`. Standard deduction (₹75,000) is applied once on total annual projected gross in `TDSCalculator`, covering both current and prior employer income. Do not use `StandardDeductionClaimed` from the entity (old regime concept).
- **TdsWorksheet upsert strategy**: `InitiatePayrollRunCommand` bulk-inserts via `AddRangeAsync`. `SetLopCommand` and `ReEvaluateSkippedCommand` upsert per-employee via `GetByRunAndEmployeeAsync` + delete-then-insert (repository already has `DeleteByRunIdAsync`; prefer per-employee delete for surgical re-runs).
- **`PriorEmployerYtdRepository`**: No repository interface or implementation exists. Must create both. Pattern: Dapper read-model query (thin, no EF tracking needed since it's read-only in this flow).
- **Marginal relief formula**: `surchargeAmount = Min(computedSurcharge, income - threshold)` where `threshold` is the lower boundary of the surcharge slab that triggered. Applied per slab independently.
- **Dead parameter removal**: Remove `ptDeduction` and `pfEmployeeContribution` from `TDSCalculator.Compute()` signature. In new regime, neither is deductible. Removing them avoids misleading callers and is safe — engine has no other callers outside `PayrollEngine.cs`.
- **`TDSResult.HasPanOverride`**: Add this field to the result record so `InitiatePayrollRunCommand` can write it to `TdsWorksheet.HasPanOverride` without re-checking PAN state after the engine call.

---

## Open Questions

### Resolved During Planning

- **Is ₹75,000 standard deduction applied once for combined current + prior employer income?** Yes — `TDSCalculator` applies it once on `annualProjectedGross` which already includes prior employer gross. Do not double-deduct.
- **Should `PriorEmployerYtd.OtherIncome` be added to taxable income?** No — deferred to other income module. Note with `// DEFERRED: other-income`.
- **Is `FiscalYearLabel` used anywhere else besides the DB query?** Yes — `PayrollRunSummaryDto.FiscalYearLabel` uses it for display. Do not change the property; only change the DB key derivation in the command.
- **Does `PriorEmployerYtdRepository` need a write path?** No — this plan is read-only. Write path (CRUD for HR to enter prior employer data) is a separate feature.

### Deferred to Implementation

- Whether a DB unique constraint exists on `prior_employer_ytds(employee_id, financial_year)` — check migration before writing query; if multiple rows exist per employee-year, take the sum.
- Exact Dapper SQL for `PriorEmployerYtdRepository` — depends on column names in migration.
- Whether `TdsWorksheet` needs an `UpdateTdsThisMonth` method on the entity for the override case, or if a direct property setter is acceptable — follow `PayrunEmployee` pattern for override fields.

---

## High-Level Technical Design

> *This illustrates the intended approach and is directional guidance for review, not implementation specification.*

```
PayrollRun Initiation Flow (after fixes):

InitiatePayrollRunCommand
  │
  ├─ Build FY key: "{FiscalYear}-{FiscalYear+1 % 100:D2}"  [U1]
  │
  ├─ Load from DB: IncomeTaxConfig, IncomeTaxSlabs, SurchargeSlabs  [U2]
  │
  ├─ StatutoryConfigBuilder.Build(...)  [U2]
  │
  ├─ Per employee:
  │    ├─ Load PriorEmployerYtd (if any) → PriorEmployerYTDTaxableIncome, PriorEmployerYTDTDSDeducted  [U6]
  │    ├─ hasPan = !string.IsNullOrWhiteSpace(emp.EncryptedPAN)  [U4]
  │    └─ Build EmployeeInput { ..., HasPan, PriorEmployerYTDTaxableIncome, PriorEmployerYTDTDSDeducted }
  │
  ├─ PayrollEngine.Compute(inputs, runInput, config)
  │    └─ Per employee → TDSCalculator.Compute(annualGross, hasPan, priorYtdTds, config, monthsRemaining)
  │         ├─ if !hasPan → return 20% flat  [U4]
  │         ├─ taxableIncome = annualGross - standardDeduction
  │         ├─ annualTax = ComputeSlabTax(taxableIncome, slabs)
  │         ├─ apply 87A rebate
  │         ├─ surcharge = ComputeSurcharge(taxableIncome, annualTax, slabs) with marginal relief  [U5]
  │         ├─ cess = (annualTax + surcharge) × cessRate
  │         └─ monthlyTDS = (totalAnnualTax - priorYtdTds) / monthsRemaining
  │
  └─ Bulk insert TdsWorksheet rows  [U7]

Taxes Tab: GET /api/v1/payroll-runs/{id}/taxes
  └─ GetPayRunTaxesQuery → ITdsWorksheetRepository.GetByRunIdAsync → PayRunTaxLineDto[]
     (no change needed — already correct once U7 writes the rows)
```

---

## Implementation Units

- U1. **Fix fiscal year DB key format**

**Goal:** Make payroll run commands query the correct fiscal year string from `income_tax_configs`, `income_tax_slabs`, and `income_tax_surcharge_slabs`.

**Requirements:** R1, R2

**Dependencies:** None

**Files:**
- Modify: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/ReEvaluateSkippedCommand.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/SetLopCommand.cs`

**Approach:**
- Replace `period.FiscalYearLabel.Replace("FY", "")` with `$"{period.FiscalYear}-{(period.FiscalYear + 1) % 100:D2}"` in all three commands.
- This produces `"2026-27"` for periods in FY 2026-27 (April 2026 – March 2027) and `"2025-26"` for earlier periods.
- Do NOT modify `PayPeriod.FiscalYearLabel` — it is used for display purposes elsewhere.

**Patterns to follow:**
- `src/Payroll.Domain/ValueObjects/PayPeriod.cs` — `FiscalYear` int property is the source of truth.

**Test scenarios:**
- Test expectation: none — pure string derivation, verified indirectly by U2/U3 integration once slabs load correctly.

**Verification:**
- All three commands compute `fiscalYear = "2026-27"` for a May 2026 period.
- Build passes with zero warnings.

---

- U2. **Seed FY 2026-27 income tax data + fix StatutoryConfigBuilder fallbacks**

**Goal:** Populate the empty `income_tax_configs`, `income_tax_slabs`, and `income_tax_surcharge_slabs` tables with correct FY 2026-27 values. Fix hardcoded fallback values in `StatutoryConfigBuilder`.

**Requirements:** R1, R2, R3, R4

**Dependencies:** U1 (FY key format must match what the migration inserts)

**Files:**
- Create: `src/Payroll.Infrastructure/Migrations/<timestamp>_SeedFY2026_27IncomeTaxSlabs.cs`
- Modify: `src/Payroll.Application/Services/StatutoryConfigBuilder.cs`

**Approach:**

Migration inserts (all using `migrationBuilder.Sql()`):

`income_tax_configs` — 1 row:
```
fiscal_year = "2026-27", regime = "New"
standard_deduction = 75000, rebate87a_limit = 1200000, rebate87a_amount = 60000
cess_rate = 0.04, employer_statutory_cap = 0, nps_employer_max_rate = 0.14
pf_wage_cap = 15000, epf_employee_rate = 0.12, eps_employer_rate = 0.0833, eps_cap = 1250
esi_wage_limit = 21000, esi_pwd_wage_limit = 25000
esi_employee_rate = 0.0075, esi_employer_rate = 0.0325
```

`income_tax_slabs` — 7 rows, fiscal_year="2026-27", regime="New":
```
(0, 400000, 0.00)
(400000, 800000, 0.05)
(800000, 1200000, 0.10)
(1200000, 1600000, 0.15)
(1600000, 2000000, 0.20)
(2000000, 2400000, 0.25)
(2400000, NULL, 0.30)
```

`income_tax_surcharge_slabs` — 3 rows, fiscal_year="2026-27", regime="New":
```
(5000000, 10000000, 0.10)
(10000000, 20000000, 0.15)
(20000000, NULL, 0.25)   -- capped at 25% under 115BAC; 37% does not apply
```

`StatutoryConfigBuilder` fallback corrections:
- `Rebate87ALimit ?? 700_000m` → `1_200_000m`
- `Rebate87AAmount ?? 25_000m` → `60_000m`

Migration `Down()` deletes the inserted rows by fiscal_year + regime.

**Patterns to follow:**
- `src/Payroll.Infrastructure/Migrations/20260519090000_AlignKeralaStatutoryToPIT.cs` — raw SQL insert pattern via `migrationBuilder.Sql()`.

**Test scenarios:**
- Test expectation: none for the migration itself — data correctness verified by engine tests in U3.

**Verification:**
- After migration runs, `SELECT COUNT(*) FROM income_tax_slabs WHERE fiscal_year = '2026-27'` returns 7.
- `StatutoryConfigBuilder.Build(null, [], [], [], [])` returns `Rebate87ALimit = 1_200_000` and `Rebate87AAmount = 60_000`.

---

- U3. **Write TDSCalculatorTests (TDD — write before U4/U5)**

**Goal:** Full test coverage for `TDSCalculator`. Some tests will be red until U4 and U5 are implemented. Covers existing correct behavior plus §206AA and marginal relief paths.

**Requirements:** R1, R2, R3, R4, R5, R11

**Dependencies:** U2 (need correct slab values to assert against)

**Files:**
- Create: `tests/Payroll.Engine.Tests/TDSCalculatorTests.cs`

**Execution note:** Write all tests first. Red tests for §206AA and marginal relief are intentional — they become green after U4 and U5.

**Approach:**
- Use a helper method `MakeConfig(slabs, surchargeSlabs, stdDeduction, rebateLimit, rebateAmount, cessRate)` to build `StatutoryConfig` with only TDS-relevant fields, defaulting others.
- Use FY 2026-27 slab values (from U2) as the test fixture.
- All assertions use exact `decimal` values computed by hand.

**Patterns to follow:**
- `tests/Payroll.Engine.Tests/GratuityCalculatorTests.cs`
- `tests/Payroll.Engine.Tests/PFCalculatorTests.cs`

**Test scenarios:**

*Happy path:*
- Happy path: annual gross = ₹10,00,000 → taxable = 9,25,000 (after ₹75K SD) → tax = 0+20K+12,500 = 32,500 → 87A limit = 12L → rebate applied → annualTax = 0, monthlyTDS = 0 (income below ₹12L rebate threshold).
- Happy path: annual gross = ₹15,00,000 → taxable = 14,25,000 → tax = 0+20K+40K+33,750 = 93,750 → no rebate (taxable > 12L) → no surcharge (income < 50L) → cess = 93,750 × 0.04 = 3,750 → totalAnnualTax = 97,500 → monthlyTDS = 97,500 / monthsRemaining.
- Happy path: annual gross = ₹12,75,000 exactly → taxable = 12,00,000 → tax = 60,000 → 87A rebate = 60,000 → monthlyTDS = 0.
- Happy path: annual gross = ₹12,75,001 → taxable = 12,00,001 → tax = 60,000 + 15% of 1 → no rebate → monthlyTDS > 0 (cliff edge test).

*Edge cases:*
- Edge case: annual gross ≤ ₹75,000 (below standard deduction) → taxableIncome ≤ 0 → monthlyTDS = 0, no divide-by-zero.
- Edge case: monthsRemainingInFY = 1 (March, last month) → all remaining tax deducted in single month.
- Edge case: monthsRemainingInFY = 12 (April, first month) → spread evenly.
- Edge case: priorEmployerYTDTDSDeducted > totalAnnualTax → remainingTax negative → monthlyTDS = 0 (not negative).
- Edge case: annual gross exactly at 87A limit boundary (₹12,75,000 gross = ₹12,00,000 taxable) → rebate = 100%, TDS = 0.

*§206AA (no PAN) — red until U4:*
- Error path (§206AA): hasPan = false, annual gross = ₹8,00,000 → TDS = 20% × 8,00,000 / monthsRemaining. No standard deduction, no slabs, no rebate.
- Error path (§206AA): hasPan = false, annual gross = ₹20,00,000 → TDS = 20% × 20,00,000 / monthsRemaining (confirms 20% > slab rate).
- Error path (§206AA): hasPan = false, TDSResult.HasPanOverride = true.
- Error path (§206AA): hasPan = true → §206AA does NOT apply even if income is high.

*Surcharge:*
- Happy path: annual gross = ₹52,00,000 → taxable = 51,25,000 → surcharge = 10% of annualTax → verify correct.
- Happy path: annual gross = ₹1,05,00,000 → surcharge = 15% of annualTax.
- Happy path: annual gross = ₹2,10,00,000 → surcharge = 25% of annualTax (capped at 25%).

*Marginal relief — red until U5:*
- Edge case (marginal relief): income just above ₹50L threshold → tax+surcharge increment must not exceed income increment above ₹50L.
- Edge case (marginal relief): income just above ₹1Cr threshold → same constraint at 1Cr boundary.

*Prior employer YTD:*
- Integration: priorEmployerYTDTaxableIncome = ₹4,00,000, current gross = ₹8,00,000/month, monthsRemaining = 6 → annualProjected = 8,00,000×6 + 4,00,000 = 52,00,000 → verify correct annual projection fed into TDS.
- Integration: priorEmployerYTDTDSDeducted = ₹50,000 → deducted from remaining tax before spreading.

**Verification:**
- All tests for existing behavior green immediately after writing.
- §206AA and marginal relief tests red until U4/U5.
- `dotnet test tests/Payroll.Engine.Tests` passes all non-§206AA non-marginal-relief tests.

---

- U4. **Engine changes: §206AA, remove dead params, add HasPan to inputs/outputs**

**Goal:** Implement §206AA flat rate, remove unused parameters from TDSCalculator, wire `HasPan` through `EmployeeInput` → `PayrollEngine` → `TDSCalculator`, surface `HasPanOverride` in `TDSResult`.

**Requirements:** R5

**Dependencies:** U3 (tests written and red, this makes them green)

**Files:**
- Modify: `src/Payroll.Engine/Calculators/TDSCalculator.cs`
- Modify: `src/Payroll.Engine/Inputs/EmployeeInput.cs`
- Modify: `src/Payroll.Engine/Outputs/TDSResult.cs`
- Modify: `src/Payroll.Engine/PayrollEngine.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs` (pass `HasPan` when building `EmployeeInput`)
- Modify: `src/Payroll.Application/Commands/PayrollRuns/ReEvaluateSkippedCommand.cs` (same)
- Modify: `src/Payroll.Application/Commands/PayrollRuns/SetLopCommand.cs` (same)

**Approach:**
- `EmployeeInput`: add `bool HasPan` property. Default `true` in existing test fixtures to avoid breaking other tests.
- `TDSResult`: add `bool HasPanOverride` to the record (alongside existing fields).
- `TDSCalculator.Compute()`: remove `ptDeduction` and `pfEmployeeContribution` parameters. Add `bool hasPan`. If `!hasPan` → return early with `annualProjectedGross × 0.20m / monthsRemainingInFY` as `MonthlyTDS`, `HasPanOverride = true`, other fields zero.
- `PayrollEngine.ComputeOne()`: pass `emp.HasPan` to `TDSCalculator.Compute()`.
- All three commands: set `HasPan = hasPan` (already computed as `!string.IsNullOrWhiteSpace(emp.EncryptedPAN)`) when constructing `EmployeeInput`.

**Patterns to follow:**
- `src/Payroll.Engine/Inputs/EmployeeInput.cs` — `IsPWD`, `IsESIExempt` bool fields pattern.
- `src/Payroll.Engine/Outputs/ESIResult.cs` — result record shape.

**Test scenarios:**
- All §206AA test cases from U3 now green.
- Regression: existing non-§206AA tests still pass (HasPan defaults correctly).

**Verification:**
- `dotnet test tests/Payroll.Engine.Tests` — §206AA tests green.
- `dotnet build` — zero warnings (removed params mean no dead code).

---

- U5. **Surcharge marginal relief**

**Goal:** Prevent over-deduction for employees earning just above a surcharge slab boundary (₹50L, ₹1Cr, ₹2Cr).

**Requirements:** R3

**Dependencies:** U4 (clean TDSCalculator post dead-param removal)

**Files:**
- Modify: `src/Payroll.Engine/Calculators/TDSCalculator.cs`

**Approach:**
- In `ComputeSurcharge()`, after computing `computedSurcharge`, apply marginal relief:
  - `marginalRelief = computedSurcharge - Max(0, income - threshold)`
  - If `marginalRelief > 0`, reduce surcharge by that amount.
  - `threshold` = `slab.IncomeFrom` of the matched surcharge slab.
- This ensures: `(tax + surcharge for income X)` ≤ `(tax for threshold) + (X - threshold)`.

**Patterns to follow:**
- Existing `ComputeSlabTax()` loop structure in `TDSCalculator.cs`.

**Test scenarios:**
- Marginal relief test cases from U3 now green.
- Happy path surcharge tests (income well above threshold) unaffected.

**Verification:**
- `dotnet test tests/Payroll.Engine.Tests` — all surcharge + marginal relief tests green.

---

- U6. **Load prior employer YTD from DB in payroll run initiation**

**Goal:** For mid-year joiners, add prior employer gross salary and TDS deducted into the engine input so TDS projection and spreading are correct.

**Requirements:** R6

**Dependencies:** U1 (FY key format needed to query correct financial year)

**Files:**
- Create: `src/Payroll.Domain/Interfaces/IPriorEmployerYtdRepository.cs`
- Create: `src/Payroll.Infrastructure/Persistence/Repositories/PriorEmployerYtdRepository.cs`
- Modify: `src/Payroll.Infrastructure/Extensions/ServiceCollectionExtensions.cs` (register new repo)
- Modify: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/ReEvaluateSkippedCommand.cs`

**Approach:**

`IPriorEmployerYtdRepository`:
- `GetByEmployeeAndFiscalYearAsync(Guid employeeId, int fiscalYear, CancellationToken ct)` → `IReadOnlyList<PriorEmployerYtd>` (multiple records possible per employee-year if they had multiple prior employers).

`PriorEmployerYtdRepository`:
- Dapper read query against `prior_employer_ytds` table, filter by `employee_id` and `financial_year`, exclude soft-deleted rows.
- Returns list; caller sums `GrossSalary` and `TdsDeducted` across rows.

`InitiatePayrollRunCommand`:
- Inject `IPriorEmployerYtdRepository`.
- For each active employee: load prior YTD for `period.FiscalYear`.
- Sum `GrossSalary` across rows → `PriorEmployerYTDTaxableIncome`.
- Sum `TdsDeducted` across rows → `PriorEmployerYTDTDSDeducted`.
- Replace hardcoded `0` values at lines 195–196.
- Note `OtherIncome` with `// DEFERRED: other-income`.

`ReEvaluateSkippedCommand`:
- Same injection and loading pattern as InitiatePayrollRunCommand.

`SetLopCommand`:
- Does not re-build `EmployeeInput` from scratch (re-uses snapshot config). Check if prior YTD needs loading here or if the prior YTD is already baked into the annual projection via snapshot. **Deferred to implementation** — may not be needed if SetLop re-uses the existing projection.

**Patterns to follow:**
- `src/Payroll.Infrastructure/Persistence/Repositories/PayrunComponentBreakdownRepository.cs` — Dapper read query pattern.
- `src/Payroll.Domain/Interfaces/IPayrollRunRepository.cs` — repository interface naming and method style.

**Test scenarios:**
- Integration: employee with one prior employer record (GrossSalary=4L, TdsDeducted=5K) → `EmployeeInput.PriorEmployerYTDTaxableIncome = 400000`, `PriorEmployerYTDTDSDeducted = 5000`.
- Integration: employee with two prior employer records same FY → amounts summed correctly.
- Integration: employee with no prior employer record → values remain 0, no exception.
- Integration: employee with prior employer record for a different FY → not loaded (ignored).

**Verification:**
- Mid-year joiner's TDS computation reflects prior employer income in `TdsWorksheet.AnnualProjectedIncome`.
- `dotnet build` zero warnings.

---

- U7. **Persist TdsWorksheet rows in InitiatePayrollRunCommand**

**Goal:** After engine computation, write one `TdsWorksheet` row per included employee. This is what `GetPayRunTaxesQuery` reads — currently always returns empty.

**Requirements:** R7, R8

**Dependencies:** U4 (TDSResult now has HasPanOverride), U6 (prior YTD loaded)

**Files:**
- Modify: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs`

**Approach:**
- Inject `ITdsWorksheetRepository` into `InitiatePayrollRunHandler`.
- After the `foreach` loop that creates `PayrunEmployee` rows, collect `TdsWorksheet.Create(...)` calls for each employee whose `result` was computed (i.e., not skipped).
- Use `AddRangeAsync(worksheets, ct)` for bulk insert before `uow.SaveChangesAsync()`.
- Fields to map from `TDSResult`:
  - `AnnualProjectedIncome` = `result.Gross.AnnualProjectedGross`
  - `StandardDeduction` = `config.StandardDeduction`
  - `TaxableIncome` = `result.TDS.TaxableIncome`
  - `TaxBeforeRebate` = `annualTax` (add to `TDSResult` or derive as `AnnualProjectedTax` before rebate — see deferred note)
  - `Rebate87A` = `result.TDS.Rebate87AApplied ? config.Rebate87AAmount : 0`
  - `Surcharge` = `result.TDS.Surcharge`
  - `Cess` = `result.TDS.Cess`
  - `AnnualTaxLiability` = `result.TDS.AnnualProjectedTax`
  - `YtdTdsDeducted` = `emp.PriorEmployerYTDTDSDeducted`
  - `RemainingMonthsInFy` = `run.MonthsRemainingInFY`
  - `TdsThisMonth` = `result.TDS.MonthlyTDS`
  - `HasPanOverride` = `result.TDS.HasPanOverride`
  - `FiscalYear` = `period.FiscalYear`

**Deferred to implementation:** `TdsWorksheet.TaxBeforeRebate` requires tax computed before applying rebate. Check if `TDSResult.AnnualProjectedTax` is pre- or post-rebate — if post-rebate, add a `TaxBeforeRebate` field to `TDSResult` or compute it at call site.

**Patterns to follow:**
- Existing `PayrunComponentBreakdown` bulk-insert pattern in `InitiatePayrollRunCommand` (lines ~282–294).

**Test scenarios:**
- Integration: after payroll run initiation with 3 employees (2 included, 1 skipped), `tds_worksheets` table has exactly 2 rows.
- Integration: worksheet row for employee with hasPan=false has `has_pan_override = true` and `tds_this_month` = 20% of gross / months.
- Integration: worksheet row for employee earning below ₹12.75L has `tds_this_month = 0` and `rebate87a > 0`.
- Integration: `GET /payroll-runs/{id}/taxes` returns 2 rows with correct values after initiation.

**Verification:**
- Taxes tab on any new payroll run shows real data instead of "No TDS data" empty state.

---

- U8. **Keep TdsWorksheet in sync on re-computation commands**

**Goal:** `ReEvaluateSkippedCommand`, `SetLopCommand`, and `OverrideTdsCommand` all change TDS amounts after initial computation. Each must keep `TdsWorksheet` in sync.

**Requirements:** R9, R10

**Dependencies:** U7 (TdsWorksheet rows exist to update)

**Files:**
- Modify: `src/Payroll.Application/Commands/PayrollRuns/ReEvaluateSkippedCommand.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/SetLopCommand.cs`
- Modify: `src/Payroll.Application/Commands/PayrollRuns/OverrideTdsCommand.cs`

**Approach:**

`ReEvaluateSkippedCommand`:
- Inject `ITdsWorksheetRepository`.
- After engine re-computation for each newly-included employee, call `TdsWorksheet.Create(...)` and `AddAsync`.
- (These employees had no worksheet before — they were skipped during initiation.)

`SetLopCommand`:
- Inject `ITdsWorksheetRepository`.
- After re-computing engine result for the one employee:
  - `GetByRunAndEmployeeAsync(runId, employeeId)` → if exists, delete via per-employee delete, then `AddAsync` new row with updated values.
  - If no existing row (edge case): just `AddAsync`.

`OverrideTdsCommand`:
- After updating `PayrunEmployee.TdsAmount` with the override:
  - `GetByRunAndEmployeeAsync(runId, employeeId)` → if found, call `UpdateTdsThisMonth(overrideAmount)` on entity (add this method to `TdsWorksheet` if not present, following `PayrunEmployee` override pattern).
  - This keeps the Taxes tab showing the actual deduction amount, not the computed amount.

**Patterns to follow:**
- `src/Payroll.Domain/Entities/PayrunEmployee.cs` — override method pattern.
- `src/Payroll.Application/Commands/PayrollRuns/SetLopCommand.cs` — existing re-compute-and-update pattern.

**Test scenarios:**
- Integration (SetLop): after setting LOP=5 days for an employee, their `TdsWorksheet.TdsThisMonth` reflects the recomputed (lower gross) TDS amount.
- Integration (ReEvaluate): after re-evaluating a skipped employee who now has a complete profile, a `TdsWorksheet` row is created.
- Integration (Override): after HR overrides TDS to ₹0 for an employee on leave, `TdsWorksheet.TdsThisMonth = 0` and Taxes tab shows ₹0.
- Integration (Override): override does not change `AnnualTaxLiability` or other worksheet fields — only `TdsThisMonth`.

**Verification:**
- Taxes tab reflects override amounts correctly.
- LOP changes update the Taxes tab on next page load.

---

- U9. **Frontend verification — PayRunTaxesTab**

**Goal:** Confirm the Taxes tab shows correct data end-to-end after all backend changes. No code changes expected.

**Requirements:** R8

**Dependencies:** U7, U8

**Files:**
- Verify: `web/src/pages/payroll/tabs/PayRunTaxesTab.tsx` (no changes expected)
- Verify: `web/src/types/api.ts` (confirm `PayRunTaxLineDto` type matches API response)

**Approach:**
- Run the app, initiate a payroll run, open the Taxes tab.
- Verify columns display: Employee, Annual Projected, Taxable Income, Annual Tax, TDS This Month, PAN Status.
- Verify §206AA badge (⚠ §206AA) appears for employees without PAN.
- Verify formatINR() formatting on all currency columns.
- If `PayRunTaxLineDto` type in `web/src/types/api.ts` is missing `hasPanOverride`, add it (it's already in the backend DTO).

**Test scenarios:**
- Test expectation: none — visual verification in browser.

**Verification:**
- Taxes tab shows non-zero TDS for employees earning above ₹12.75L.
- Taxes tab shows ₹0 TDS for employees earning below ₹12.75L (rebate applied).
- §206AA warning badge visible for employee with no PAN.

---

## System-Wide Impact

- **Interaction graph:** `InitiatePayrollRunCommand`, `ReEvaluateSkippedCommand`, `SetLopCommand`, and `OverrideTdsCommand` all write to `tds_worksheets`. All writes go through `ITdsWorksheetRepository` + `IUnitOfWork`. `GetPayRunTaxesQuery` is read-only.
- **Error propagation:** If `GetIncomeTaxConfigAsync` returns null (slabs not seeded for a fiscal year), `StatutoryConfigBuilder` falls back to hardcoded values (fixed in U2). Engine will compute TDS using fallback — no exception thrown. Add a warning log in `InitiatePayrollRunCommand` if `taxConfig` is null post-U2.
- **State lifecycle risks:** TdsWorksheet rows are tied to a payroll run. If a payroll run is deleted (`DeletePayrollRunCommand`), TdsWorksheet rows must also be deleted. Verify `DeletePayrollRunCommand` includes `ITdsWorksheetRepository.DeleteByRunIdAsync` — if not, add it.
- **API surface parity:** `PayRunTaxLineDto` is returned by `GET /payroll-runs/{id}/taxes`. Adding `HasPanOverride` to `TDSResult` flows through to this DTO — frontend already handles it (`PayRunTaxesTab.tsx` already renders the §206AA badge).
- **Integration coverage:** Unit tests alone (engine tests) do not prove worksheet persistence. U7/U8 must include API integration tests hitting real PostgreSQL via Testcontainers.
- **Unchanged invariants:** `PayrollRun` status transitions, `PayrunEmployee` financial fields, payslip generation, bank advice — none of these change. TDS amounts in `PayrunEmployee.TdsAmount` are the source of truth for net pay; `TdsWorksheet` is audit/display data only.

---

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| `TdsWorksheet.TaxBeforeRebate` mapping unclear — `TDSResult.AnnualProjectedTax` may already be post-rebate | During U7 implementation, trace through `TDSCalculator` to confirm; add `TaxBeforeRebate` field to `TDSResult` if needed |
| Multiple `PriorEmployerYtd` rows per employee-FY — no DB unique constraint | Sum all rows; add comment. If data integrity requires unique constraint, add migration in U6. |
| `SetLopCommand` may need prior YTD loaded to correctly recompute annual projection | Verify during U6 implementation; if LOP recomputation uses existing snapshot, prior YTD may already be embedded. |
| `DeletePayrollRunCommand` may not delete `TdsWorksheet` rows — FK violation or orphans | Verify before U7; add `DeleteByRunIdAsync` call if absent. |
| FY 2025-26 runs (April–Dec 2025 period) will query for `"2025-26"` slabs which are not seeded | Acceptable for V1 — document as known gap. Add FY 2025-26 seed data if needed in a follow-up. |
| Engine test coverage may drop below 95% gate if new code paths are only partially tested | U3 writes comprehensive tests before U4/U5 are implemented — ensures coverage gate is met on merge. |

---

## Documentation / Operational Notes

- After deploying, run `dotnet ef database update` in tenant schema context to apply the slab seed migration.
- The slab data is global (not tenant-specific) — it lives in the shared `income_tax_slabs` table. Only needs to be seeded once per environment.
- For any existing payroll run (pre-fix) that shows ₹0 TDS on the Taxes tab: re-initiating the run (delete + re-initiate) is the only fix. No backfill migration provided.
- Monitoring: after deploy, spot-check `SELECT COUNT(*) FROM tds_worksheets` after the first new payroll run — should equal the number of included (non-skipped) employees.

---

## Sources & References

- Related code: `src/Payroll.Engine/Calculators/TDSCalculator.cs`
- Related code: `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs`
- Related code: `src/Payroll.Application/Services/StatutoryConfigBuilder.cs`
- Related code: `src/Payroll.Domain/ValueObjects/PayPeriod.cs`
- Finance Act 2025 — new regime slab revision for AY 2026-27
- Section 206AA, Income Tax Act — 20% TDS on PAN non-furnishment
- Section 87A, Income Tax Act — rebate for income ≤ ₹12L (FY 2026-27)
