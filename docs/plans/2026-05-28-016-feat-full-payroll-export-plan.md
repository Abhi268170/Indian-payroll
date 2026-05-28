# Plan 016 — Payroll Details + TDS Breakup Exports

**Created:** 2026-05-28
**Status:** Draft, awaiting approval
**Scope:** Two independent exports per payroll run, each downloadable as CSV or XLSX.

## Goal

Two flat, row-per-employee exports for a payroll run:

1. **Payroll Details Export** — full salary breakup per employee (every component + statutory amounts + gross/net/CTC).
2. **TDS Breakup Export** — per-employee TDS working (taxable income, slab-wise tax, 87A, surcharge, cess, monthly TDS).

Each as a separate file with format toggle (CSV or XLSX). No multi-sheet workbooks. No bundled ZIPs. One file per click.

---

## What's Already in Place

| Concern | Location | Status |
|---|---|---|
| ClosedXML library | `Payroll.Infrastructure.csproj` | Ready |
| Existing CSV/XLS export | `PayrollExportService.cs` (12 cols, summary only) | Replace `PayrollDetailsExport` with richer version |
| Per-employee data | `payrun_employees` table | All needed fields present |
| Per-component data | `payrun_component_breakdowns` table | All flags present incl. `is_benefit` |
| Engine TDS calc | `TDSCalculator.Compute` returns aggregates | Slab-by-slab amounts discarded — needs verbose variant |
| Snapshot for TDS replay | `payroll_runs.statutory_config_snapshot` JSON | Pinned per run, deterministic |
| Export endpoint | `GET /payroll-runs/{id}/export?format=csv\|xls` | Add second endpoint for TDS, or branch on `type=` param |
| Frontend modal | `ExportModal.tsx` (CSV / XLS toggle) | Reuse pattern for second export |

---

## Endpoint Design

Two endpoints, one shape each. Cleaner than multiplexing on a query param.

```
GET /api/v1/payroll-runs/{id}/export/payroll-details?format=csv|xlsx
GET /api/v1/payroll-runs/{id}/export/tds-breakup?format=csv|xlsx
```

- Existing `?format=` endpoint stays for backwards compatibility, deprecated in comment.
- Both new endpoints return `application/octet-stream` with `Content-Disposition: attachment; filename=...`.
- Filenames: `PayrollDetails_{period}_{tenant}.{ext}` and `TDSBreakup_{period}_{tenant}.{ext}`.

---

## Export 1 — Payroll Details

One row per employee. Columns ordered: identity → attendance → earnings → benefits → reimbursements → employee deductions → totals → employer contributions → CTC reconciliation.

**Component columns are dynamic** — built from the set of distinct component codes present in this run's breakdowns. Categories grouped, columns within a category ordered by `SalaryComponent.DisplayOrder`. Empty cell = component not applicable to that employee (not 0 — avoids inflating sums).

### Column order

| # | Group | Column | Source |
|---|---|---|---|
| 1 | Identity | Employee Code | `Employee.EmployeeCode` |
| 2 | | Employee Name | `Employee.FullName` |
| 3 | | Designation | `Employee.Designation` |
| 4 | | Department | `Employee.Department` |
| 5 | | Work Location | `Employee.WorkLocation` |
| 6 | | Date of Joining | `Employee.DateOfJoining` |
| 7 | | Date of Leaving | `Employee.DateOfLeaving` (blank if active) |
| 8 | | Status | `Active` / `Skipped` / `Withheld` |
| 9 | | Skip Reason | blank if not skipped |
| 10 | Attendance | Base Days | `PayrunEmployee.BaseDays` |
| 11 | | LOP Days | `PayrunEmployee.LopDays` |
| 12 | | Payable Days | `PayrunEmployee.ActualPayableDays` |
| 13..N | Earnings | one col per Earning-category component code | `PayrunComponentBreakdown.ProratedAmount` where `Category=Earning` |
| | | Subtotal: Gross Pay | `PayrunEmployee.GrossPay` |
| | | Taxable Gross | `PayrunEmployee.TaxableGrossPay` |
| | Benefits | one col per Benefit-category component code | `ProratedAmount` where `IsBenefit=true` |
| | | Subtotal: Benefits | `PayrunEmployee.BenefitsAmount` |
| | Reimbursements | one col per Reimbursement code | `ProratedAmount` where `Category=Reimbursement` |
| | | Subtotal: Reimbursements | `PayrunEmployee.ReimbursementsAmount` |
| | Employee Deductions | Employee PF | `PayrunEmployee.EmployeePf` |
| | | Employee ESI | `PayrunEmployee.EmployeeEsi` |
| | | Professional Tax | `PayrunEmployee.PtAmount` |
| | | LWF (Employee) | `PayrunEmployee.LwfEmployeeAmount` |
| | | TDS | `PayrunEmployee.TdsAmount` |
| | | TDS Override | `PayrunEmployee.TdsOverrideAmount` (blank if none) |
| | | TDS Override Reason | `PayrunEmployee.TdsOverrideReason` |
| | | one col per Deduction-category component code (one-time deductions etc) | `ProratedAmount` where `Category=Deduction` |
| | | Total Deductions | computed = `GrossPay - NetPay` |
| | Net | Net Pay | `PayrunEmployee.NetPay` |
| | Employer Contributions | Employer PF | `PayrunEmployee.EmployerPf` |
| | | EPS | `PayrunEmployee.EpsAmount` |
| | | Employer ESI | `PayrunEmployee.EmployerEsi` |
| | | LWF (Employer) | `PayrunEmployee.LwfEmployerAmount` |
| | | Gratuity Accrual | `PayrunEmployee.GratuityAmount` |
| | | Total Employer Cost | computed = sum of preceding employer columns + benefits subtotal |
| | CTC | Monthly CTC | `PayrunEmployee.MonthlyCTC` |
| | | Annual CTC | `MonthlyCTC × 12` |
| | | CTC Reconciliation Δ | `MonthlyCTC − (GrossPay + Total Employer Cost)` (should be 0 ± rounding) |

**XLSX formatting only** (CSV gets plain numbers):
- Indian currency format `#,##,##0.00` on all decimal columns
- Bold header row, sticky frozen at row 1
- Sticky frozen first two columns (Employee Code + Name)
- Subtotal columns shaded `#f1f5f9`
- Column auto-width

### Footer row (XLSX) / final row (CSV)

Single "TOTALS" row summing every numeric column. Skipped/Withheld employees excluded from totals (they didn't pay).

---

## Export 2 — TDS Breakup

One row per employee. Flat table — works in CSV. Contains every input + intermediate + output from the TDS calculation.

### Column order

| # | Group | Column | Source |
|---|---|---|---|
| 1 | Identity | Employee Code | |
| 2 | | Employee Name | |
| 3 | | PAN | masked `XXXXXXX1234` (last 4 only — security policy) |
| 4 | | PAN Furnished | Yes/No → drives 206AA |
| 5 | | Regime | `New (Sec 115BAC)` constant for V1 |
| 6 | Period Context | Pay Month | `MMM-YYYY` |
| 7 | | FY Label | e.g. `2026-27` |
| 8 | | Months Remaining in FY | from `TDSCalculator` input |
| 9 | Income | Monthly Taxable Gross | `PayrunEmployee.TaxableGrossPay` |
| 10 | | Annual Projected Gross | `TaxableGrossPay × MonthsRemaining + YTD` from query |
| 11 | | Prior Employer YTD Taxable Income | from employee KYC / income history (blank if none) |
| 12 | | Total Projected Income | sum of 10 + 11 |
| 13 | | Standard Deduction | from snapshot |
| 14 | | Taxable Income | 12 − 13 |
| 15 | Slab-wise Tax | Tax @ Slab 0–4L | computed via replay |
| 16 | | Tax @ Slab 4L–8L | |
| 17 | | Tax @ Slab 8L–12L | |
| 18 | | Tax @ Slab 12L–16L | |
| 19 | | Tax @ Slab 16L–20L | |
| 20 | | Tax @ Slab 20L–24L | |
| 21 | | Tax @ Above 24L | |
| 22 | | Tax Before Rebate | sum of 15–21 |
| 23 | Rebate | 87A Rebate Applied | Yes/No |
| 24 | | 87A Rebate Amount | |
| 25 | | Tax After Rebate | 22 − 24 |
| 26 | Surcharge | Surcharge Slab Rate | `10%` / `15%` / `25%` / `37%` / blank |
| 27 | | Raw Surcharge | `tax_after_rebate × surcharge_rate` |
| 28 | | Marginal Relief Applied | Yes/No |
| 29 | | Surcharge After Relief | engine's final surcharge |
| 30 | | Subtotal (Tax + Surcharge) | 25 + 29 |
| 31 | Cess | Cess Rate | `4%` constant for V1 |
| 32 | | Cess Amount | `30 × cess_rate` |
| 33 | Totals | Total Annual Tax Liability | 30 + 32 |
| 34 | | Prior Employer TDS Deducted | from employee KYC |
| 35 | | Current Employer YTD TDS Deducted | sum of `tds_amount` from prior runs this FY for this employee |
| 36 | | Remaining Tax for FY | 33 − 34 − 35 |
| 37 | Monthly | Monthly TDS (engine) | `TDSCalculator.MonthlyTDS` |
| 38 | | Monthly TDS Override | `PayrunEmployee.TdsOverrideAmount` (blank if none) |
| 39 | | Override Reason | `PayrunEmployee.TdsOverrideReason` |
| 40 | | Effective TDS This Run | override if set, else 37 |
| 41 | No-PAN Override | 206AA 20% Flat Annual | populated only when PAN missing; replaces 9–32 |
| 42 | | 206AA Monthly TDS | |

XLSX formatting:
- Currency format on all decimal columns
- Slab columns shaded light blue, surcharge shaded amber, cess shaded grey
- Sticky header + first 2 columns
- Footer "TOTALS" row sums applicable numeric columns

### Special rows

- Skipped employees → still listed, all tax columns blank, status column shows `Skipped`
- Withheld employees → listed, columns populated, status `Withheld` (TDS still computed; payment held)
- No-PAN employees → columns 9–32 blank, columns 41–42 populated, header note

---

## Engine Change Required

`TDSCalculator.Compute()` returns aggregate `TDSResult`. The slab-by-slab numbers and the raw-vs-adjusted surcharge are computed internally but discarded.

Add a sibling method:

```csharp
public static TDSWorkingResult ComputeVerbose(
    decimal annualProjectedGross,
    decimal priorEmployerYTDTaxableIncome,
    decimal priorEmployerYTDTDSDeducted,
    decimal currentEmployerYTDTDSDeducted,
    bool hasPan,
    StatutoryConfig config,
    int monthsRemainingInFY);
```

Returning a richer record:

```csharp
public sealed record TDSWorkingResult(
    decimal MonthlyTDS,
    decimal AnnualProjectedTax,
    decimal TotalProjectedIncome,
    decimal StandardDeduction,
    decimal TaxableIncome,
    IReadOnlyList<SlabTax> SlabBreakdown,        // [IncomeFrom, IncomeTo?, Rate, SlabIncome, Tax]
    decimal TaxBeforeRebate,
    bool Rebate87AApplied,
    decimal Rebate87AAmount,
    decimal TaxAfterRebate,
    decimal? SurchargeRate,
    decimal RawSurcharge,
    bool MarginalReliefApplied,
    decimal SurchargeAfterRelief,
    decimal CessRate,
    decimal CessAmount,
    decimal PriorEmployerTDS,
    decimal CurrentEmployerYTDTDS,
    decimal RemainingTaxForFY,
    bool HasPanOverride,
    decimal? Pan206AAAnnual,
    decimal? Pan206AAMonthly);
```

Existing `Compute()` becomes a thin wrapper that calls `ComputeVerbose` and projects down to `TDSResult` — zero behavior change for current callers, zero new persistence.

---

## Implementation Plan

### PR 1 — Engine: TDS verbose variant

1. Add `SlabTax` record + `TDSWorkingResult` record in `Payroll.Engine/Outputs/`.
2. Add `TDSCalculator.ComputeVerbose(...)` — same algorithm, captures intermediates.
3. Refactor `Compute(...)` to delegate to `ComputeVerbose` + project to `TDSResult`. Keep existing signature/behavior identical.
4. Tests: extend existing `TDSCalculatorTests` with `ComputeVerbose` assertions across:
   - Zero income
   - Under-87A-threshold
   - 87A-edge (₹12,00,000 exactly)
   - Mid-slab (15L)
   - Surcharge-triggering (60L)
   - Marginal-relief-triggering (just above 50L)
   - No-PAN (206AA)
   - Mid-year joiner with prior employer YTD
5. Assertion: sum of `SlabBreakdown[].Tax` ≡ `TaxBeforeRebate` (invariant).

### PR 2 — Backend: Payroll Details Export

1. Rewrite `PayrollExportService` → split into `IPayrollDetailsExportService` and `ITDSBreakupExportService`. Old `IPayrollExportService` kept for backwards compat or marked obsolete.
2. New `PayrollDetailsExportService`:
   - Fetch run + all `payrun_employees` + all `payrun_component_breakdowns` + `salary_components` (for category/display_order)
   - Build dynamic column list from distinct component codes, ordered by category + display_order
   - Materialize rows
   - Render to CSV (StringBuilder, RFC 4180 escape) or XLSX (ClosedXML, currency + sticky header + shading)
3. Controller: `GET /payroll-runs/{id}/export/payroll-details?format=csv|xlsx` → `ExportPayrollDetailsQuery`
4. Tests:
   - Application: handler test — happy path, skipped employee, no-components-of-a-category
   - Infrastructure (Testcontainers): seeded run with 3 employees, 1 skipped, 1 with override, mixed categories. Read back XLSX with ClosedXML, assert column order, dynamic component cols present, totals row math.

### PR 3 — Backend: TDS Breakup Export

1. New `TDSBreakupExportService`:
   - Fetch run + all `payrun_employees` + deserialize `statutory_config_snapshot`
   - For each employee: pull inputs (taxable gross, PAN status, prior employer YTD, current employer YTD aggregated from same-FY prior runs) → call `TDSCalculator.ComputeVerbose` → flatten to row
   - Render CSV or XLSX
2. Need a query helper `GetCurrentEmployerYTDTdsAsync(employeeId, FY)` — sum `tds_amount` from prior runs in same FY.
3. Controller: `GET /payroll-runs/{id}/export/tds-breakup?format=csv|xlsx` → `ExportTdsBreakupQuery`
4. Tests:
   - Handler test: PAN furnished, PAN missing, override applied, skipped employee (TDS section blank)
   - Snapshot deserialization is exercised — verify it's been pinned correctly per run

### PR 4 — Frontend

1. Replace single `ExportModal.tsx` with `ExportMenu` button that opens a dropdown:
   - "Payroll Details (CSV)" / "Payroll Details (XLSX)"
   - "TDS Breakup (CSV)" / "TDS Breakup (XLSX)"
2. Or simpler: keep one modal, add radio for "Export type" (Payroll Details / TDS Breakup) + radio for format (CSV / XLSX) → single download button.
3. Recommend the modal approach — fewer dropdown items, matches existing pattern.
4. Tests: Vitest — both export type × format combinations call the right URL.

---

## Out of Scope

- Multi-sheet workbooks
- Bundling both exports as one download
- ZIP archive
- Custom column selection
- Scheduled exports
- ECR / Form 16 / Form 24Q (separate compliance pipelines)
- Old tax regime
- PDF format

---

## Open Questions

1. **PAN-furnished flag source** — derived from `Employee.PanNumber != null/empty`, or is there an explicit `IsPanVerified`?
2. **Prior-employer YTD inputs** — currently captured during mid-year onboarding? If not, default to zero and add a column note.
3. **Current-employer YTD TDS** — confirm "sum of `tds_amount` from prior `PayrollRun` in the same FY where employee was Active" is the right definition.
4. **Column shading on CSV** — N/A, CSV is plain. Confirm we don't need a "CSV-only flat" version with no formatting hints.
5. **Format toggle UX** — one modal with two radios, or two buttons each with format dropdown? (Recommend: one modal.)
6. **Audit log entry per export** — record who exported what and when?
