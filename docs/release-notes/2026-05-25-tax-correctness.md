# Payroll Tax Correctness Hardening — 2026-05-25

**Tag:** `payroll-tax-correctness-hardening`
**Master commit:** `9e7aabd`
**PRs merged:** `fix/tds-projection-correctness` (d905ec1), `fix/statutory-lookback-and-test-hardening` (33d0b1d)

## Why support/ops should read this

Existing tenants with active payroll history will see **payslip and TDS deltas
on the next pay run** for impacted employees. The deltas are not regressions —
they reflect the corrected statutory math. Expect inbound queries from
employees who track TDS deductions month over month.

## Who is impacted

| Profile | Symptom | Direction |
|---|---|---|
| Mid-year joiners with `PriorEmployerYtd` rows | TDS adjusts to reflect prior std deduction / PT / other income | Usually **lower** TDS (prior employer's std deduction reduces taxable base) |
| Any employee whose monthly pay includes **non-taxable components** (HRA, LTA, reimbursements) | Annual taxable projection no longer inflated by non-taxable YTD | **Lower** TDS |
| Employees who had a payroll run **recomputed after LOP correction** | Recomputed TDS now matches initial-run TDS for the same inputs | Round-trip identity (often **higher** vs prior buggy recompute) |
| Employees in a **Final Settlement (FnF)** within the same LWF half-year as a prior LWF deduction | LWF correctly skipped on FnF | **Lower** net deduction |
| Re-evaluated **skipped** employees | TDS worksheet `ytd_tds_deducted` now sums prior + current YTD | Worksheet figure changes; deducted amount unchanged this run |

## What changed (audit findings)

### #1, #2, #3, #5 — TDS calculation correctness (`d905ec1`)

- **#1 [High]** `GrossCalculator.AnnualProjectedTaxableGross` previously used
  `CurrentEmployerYTDGross` and overstated taxable income whenever earlier
  months included non-taxable components.
- **#2 [High]** Prior-employer `GrossSalary` was passed straight to the engine
  as `PriorEmployerYTDTaxableIncome` — `StandardDeductionClaimed`,
  `ProfessionalTaxPaid`, and `OtherIncome` were ignored. Now adjusted via
  `PriorEmployerYtdMapper.TaxableIncomeFor`.
- **#3 [High]** `PayrollRecomputeService` + `PayrollFnfOrchestrator` zeroed
  prior-employer YTD on every call. Recompute now loads
  `PriorEmployerYtd` and round-trips identically to the initial run.
- **#5 [Med]** `ReEvaluateSkippedCommand` worksheet `ytd_tds_deducted` summed
  prior only; now correctly sums prior + current YTD.

### #4, #6, #8 — Statutory hardening (`33d0b1d`)

- **#4 [Med]** `PayrollFnfOrchestrator.IsLwfAlreadyDeductedThisHalfYearAsync`
  was a placeholder that always returned `false`. Real lookback now scans
  approved/paid payrun_employee rows in the same H1/H2 window, including the
  H2 wrap (Jan–Mar inspects prior year's Oct–Dec).
- **#6 [Med]** `ConfigureEpfCommandValidator` no longer accepts `Gross12` as
  an employer contribution rate — it was accepted but never honored in
  `StatutoryConfigBuilder`.
- **#8 [Gap]** Added direct calculator tests:
  - `PTCalculatorTests` (12 tests)
  - `LWFCalculatorTests` (15 tests)
  - `ESICalculatorTests` (10 tests)
  - `LwfHalfYearLookbackTests` (11 tests for the FnF wrap logic)

## What did NOT change

- Approved/Paid payroll runs are immutable — historical payslips and TDS
  figures are not retroactively recomputed. Deltas only affect runs created
  on or after this deploy.
- Old regime is still out of scope (V1 = new regime only).

## Migration

Single forward migration `AddTaxableGrossPayToPayrunEmployee` adds the
`taxable_gross_pay numeric(18,2) NOT NULL DEFAULT 0` column to
`payrun_employees` in every tenant schema. Applied automatically on API
startup via `TenantSchemaProvisioner` sweep (`Program.cs:181-198`). No
manual DBA action.

Historical rows default to 0; the engine field is only consumed for
**future** runs' annual TDS projection, so the 0 default is correct (those
runs are already finalised and immutable).

## Known stale data

`statutory_org_config.epf_employer_contribution_rate = 'Gross12'` rows (if
any exist) will fail validation on the next `ConfigureEpf` save. Dev env
has none after the recent tenant reset. If discovered in another env, update
to `ActualPfWage12` directly in the DB — that matches what the engine was
already calculating.

## Out of scope (tracked)

- **#7** PT global on/off toggle — deferred pending product input. See
  `docs/follow-ups/2026-05-25-007-pt-global-toggle.md`.

## Test posture

- Engine: 112/112 (was 73 pre-merge)
- Application: 143/143 (was 132 pre-merge)
- Zero build warnings

## Support response template

> Your TDS amount this month reflects a correction to how we project annual
> taxable income for the year. The previous calculation could over-deduct
> when earlier-month earnings included non-taxable components (HRA, LTA,
> reimbursements). Your annual TDS liability for FY 2025-26 has not changed
> — only how it is distributed across remaining months. Net impact across
> the full year is zero.
