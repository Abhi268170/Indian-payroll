# Fractional LOP Days — Implementation Plan

**WI:** 019
**Date:** 2026-06-05
**Scope:** Allow LOP (Loss of Pay) days to be fractional (half-days) end-to-end: bulk import, manual entry, FnF, storage, exports, payslip, frontend.
**Out of scope:** Hourly / quarter-day LOP, attendance-system integration, retro-recompute of already-finalised runs.
**Driver:** Accounts-team `April2026.xlsx` test data contains `0.5 / 1.5 / 4` LOP days (column total `261.5`); current LOP import rejects non-integers, blocking the data-prep test run.

---

## 1. Decisions (locked)

1. **Precision = `numeric(4,1)`** — half-day granularity. Source needs only `.5` / `.0`. Three integer digits + one decimal (LOP always < base days ≤ 31).
2. **Input step = `0.5`** on number inputs; server rejects finer than 1 decimal place.
3. **Down-migration rounds** (`ROUND(col)::int`, away-from-zero) so rollback never fails on fractional data; documented as lossy.
4. **`BaseDays` and `SalaryDivisor` stay `int`** (always whole). Only `LopDays`, `ActualPayableDays`, and FnF `WorkedDays` widen to `decimal`.
5. **Engine is untouched** — `EmployeeInput.LOPDays` and all proration math are already `decimal`. This WI is plumbing only.

---

## 2. What already exists (no rebuild)

| Asset | Location | State |
|---|---|---|
| `decimal LOPDays` engine input | `Payroll.Engine/Inputs/EmployeeInput.cs:12` | Done |
| Decimal proration / LOP deduction | `Engine/Calculators/GrossCalculator.cs:11,31`, `PFCalculator.cs:11,32` | Done |
| Implicit `int -> decimal` widening at engine boundary | `PayrollRecomputeService`, `PayrollFnfOrchestrator` | Works once upstream types widen |

---

## 3. Implementation steps (TDD, in order)

Each step: failing test -> minimal change -> `dotnet build` (zero warnings) -> `dotnet test`.

### Step 1 — Domain
- `PayrunEmployee`: `LopDays`, `ActualPayableDays` -> `decimal`; `SetLop(int)` -> `SetLop(decimal)`. Guards (`<0`, `>=BaseDays`) unchanged.
- **Verify:** `SetLop(1.5m)` -> `ActualPayableDays == BaseDays - 1.5m`.

### Step 2 — DB migration + EF config
- `PayrunEmployeeConfiguration`: `.HasColumnType("numeric(4,1)")` on both columns.
- Migration `FractionalLopDays`: `Up` widens (`ALTER COLUMN TYPE numeric(4,1)`); `Down` rounds (`USING ROUND("col")::int`). Regenerate snapshot.
- **Verify:** Up then Down on Testcontainers DB — no error, column type flips.

### Step 3 — Application
- `SetLopCommand`, `UpdateFnfRunCommand`, `PayrollRunDtos`, `GetFnfPreviewQuery`, `GetFnfSummaryQuery` (`WorkedDays`), `GetPayrollRunEmployeesQuery`, `GetEmployeeVariableInputsQuery` -> `decimal`.
- `PayrollFnfOrchestrator`: `effectiveLopDays = lopFromExit + payrunEmp.LopDays` -> `decimal` (`lopFromExit` stays `int`).
- FluentValidation `>=0` rules unchanged.
- **Verify:** `SetLopCommand(…, 2.5m)` persists & recomputes.

### Step 4 — Import parser
- `BulkImportLopCommand`: `int.TryParse` -> `decimal.TryParse`; error text "whole number" -> "number"; reject `Math.Round(v,1) != v`; divisor guard decimal-safe.
- **Verify:** `1.5` applies; `1.55` rejected; `-1` rejected; `>=divisor` rejected.

### Step 5 — API
- `PayrollRunsController`: `SetLopRequest(decimal LopDays)`; FnF request record `LopDays` -> `decimal`.
- **Verify:** `PUT …/lop {lopDays:1.5}` -> 200, round-trips.

### Step 6 — Exports
- `PayrollDetailsExportService`: `FormatInt(LopDays/ActualPayableDays)` -> 1-dp decimal formatter.
- `PayrollExportService`: verify CSV/XLSX render `1.5`.
- **Verify:** export run with `1.5` -> cell reads `1.5`.

### Step 7 — Frontend
- `EmployeePayBreakdown`: LOP input `step="0.5" min="0"`; payable display fractional.
- `FnfSettlementPage`: `parseInt` -> `parseFloat`.
- `ImportModal`: LOP template hint notes half-days.
- `types/api.ts`: already `number`.
- **Verify:** `npm run typecheck` + `npm run lint` zero; component test for `1.5` input.

---

## 4. Testing checklist

### Engine unit
- [ ] `GrossCalculator`: `1.5` LOP on 30-base -> `payableDays 28.5`, prorated = `amount × 28.5/30` (2dp away-from-zero).
- [ ] Flat / non-proRata component NOT reduced by fractional LOP.
- [ ] `PFCalculator`: restricted-wage cap prorated on `28.5/30`.
- [ ] LOP deduction = sum of `amount - prorated` exact decimal.

### Domain / application
- [ ] `SetLop(1.5m)` -> `LopDays==1.5`, `ActualPayableDays==BaseDays-1.5`.
- [ ] `SetLop(BaseDays)` throws; `SetLop(-0.5m)` throws.
- [ ] `SetLopCommand(2.5m)` persists + recompute reflects half-day.
- [ ] `UpdateFnfRunCommand` fractional operator LOP + calendar exit days -> correct `effectiveLopDays`.
- [ ] Bulk import: `1.5` ok; `1.55` error "number"; `-1`; `>=divisor`; non-numeric.
- [ ] Re-import same CSV -> identical result (determinism invariant).

### Integration (Testcontainers)
- [ ] Migration Up widens both cols to `numeric(4,1)`; existing int rows intact.
- [ ] Migration Down rounds (`1.5->2`) without error.
- [ ] `PUT …/employees/{id}/lop {lopDays:1.5}` -> 200, GET returns `1.5`.
- [ ] `POST …/import/lop` (`EMP001,1.5`) -> applied=1, run totals updated.

### Frontend (Vitest)
- [ ] LOP input accepts `1.5`, mutation fires with `1.5`.
- [ ] Payable-days display = `baseDays - 1.5`.
- [ ] Summary table shows `1.5d`.
- [ ] Dirty-flag logic unaffected.

### E2E (Playwright)
- [ ] Set `1.5` LOP -> payslip net drops by half-day proration.
- [ ] Import LOP CSV with half-days -> summary reflects them.

### Manual / data-driven (the real goal)
- [ ] Import April2026 LOP column verbatim (`0.5/1.5/4`) -> zero rejections.
- [ ] Spot-check 3 employees: app LOP deduction vs source "LOP Deduction" within ₹1 rounding.
- [ ] Run total LOP days == `261.5`.

### CI gates
- [ ] `dotnet build` zero warnings · `dotnet test` + coverage (Engine 95%) · `dotnet format` · NetArchTest.
- [ ] `npm run lint` · `npm run typecheck` · `vitest --coverage` · Playwright.

---

## 5. Rollout / rollback
- **Branch:** `fix/fractional-lop-days` off `master` (isolated worktree; current dev branch `feat/salary-revision-arrears` untouched).
- **Commits (conventional):**
  - `feat(payroll): widen LopDays to decimal for half-day LOP — domain + migration`
  - `feat(payroll): accept fractional LOP in import, commands, API`
  - `feat(payslip): render fractional LOP in exports + frontend`
  - `test(payroll): fractional LOP coverage across layers`
- **Rollback:** `Down` migration rounds fractional -> int (lossy by design); note in release notes.

---

## 6. Risk register
| Risk | Sev | Mitigation |
|---|---|---|
| Down-migration truncates real fractional data | Med | Round, not truncate; document lossy rollback |
| Missed `FormatInt` call -> build break | Low | Compiler catches; Step 6 |
| FnF int arithmetic silently truncates | Low | Step 3 explicit decimal |
| Engine rounding drift vs source | Low | 2dp away-from-zero matches existing; manual check §4 |
| Finalised-run immutability breach | None | Type change only, no data mutation |
