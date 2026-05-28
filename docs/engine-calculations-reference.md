# How payroll math works

Plain-language guide to what the engine does in each real-world scenario. For deeper internals, read the calculators in `src/Payroll.Engine/Calculators/`.

---

## The basic flow

Every employee, every month, the engine runs seven steps in order:

1. **Gross** — sum the salary components, prorate for LOP.
2. **PF** — employee + employer + EPS contributions.
3. **ESI** — only if salary is below the cap.
4. **PT** — flat amount based on state slab.
5. **LWF** — only in applicable states + months.
6. **TDS** — annual tax, divided across remaining months.
7. **Gratuity** — monthly accrual (booked, not paid).

Then: `Net Pay = Gross − TDS − Employee PF − VPF − Employee ESI − PT − Employee LWF`

All money uses `decimal`. Rounding is half-up to 2 places.

---

## Scenario 1 — Employee works full month, no LOP

Take Asha: ₹30k basic + ₹12k HRA + ₹18k allowance = ₹60k gross, Karnataka, EPF on.

| Step | What happens |
|---|---|
| Gross | Components added as-is. PF wage = ₹30k (only basic is PF-eligible). Taxable wage = ₹48k (HRA non-taxable). |
| PF | Employer caps PF wage at ₹15k → employee 12% = **₹1,800**. EPS = ₹1,250 (capped). EPF employer = ₹1,800 − ₹1,250 = **₹550**. |
| ESI | ₹60k > ₹21k limit → **exempt**. |
| PT | Karnataka slab: gross > ₹25k → **₹200**. |
| LWF | Karnataka only deducts in June + December → ₹0 in other months. |
| TDS | Project annual taxable = ₹48k × months left + prior YTD. Apply slabs, 87A rebate, surcharge, cess. Divide remaining tax across months. |
| Gratuity | ₹30k × 15 / 26 / 12 = **₹1,442/month** booked. |

---

## Scenario 2 — Employee on Loss of Pay

Asha takes 10 unpaid days in a 30-day month.

- Each salary component gets prorated by `(30 − 10) / 30` = 2/3.
- Components flagged `IsFlat` or `CalculateOnProRata = false` (bonuses, commissions) are NOT prorated — they land as typed.
- `LOPDeduction` = the rupees lost.
- PF: uses prorated wage **unless** "Consider Salary on LOP" is off (then PF runs on full structure regardless — rare).
- Optional: pro-rate the ₹15k PF cap itself by paid days — controlled by `EpfProRateRestrictedPfWage`.

---

## Scenario 3 — Employee joined mid-year from another company

Operator entered Form 12B data: prior gross ₹6L, std deduction ₹50k, PT ₹2.5k, TDS deducted ₹30k.

Engine adjusts the prior taxable income before adding to current projection:

```
priorTaxable = max(0, ₹6,00,000 − ₹50,000 − ₹2,500 + 0) = ₹5,47,500
totalProjected = newEmployerTaxable + ₹5,47,500
remainingTax = totalAnnualTax − newEmployerYtdTds − ₹30,000
```

Without the adjustment (pre-audit-#2 bug): would overstate income → over-deduct TDS for the whole second half of the year.

---

## Scenario 4 — Employee has no PAN

Section 206AA kicks in:

- Slabs, rebate, surcharge **all skipped**.
- TDS = flat 20% on total projected income.
- Worksheet shows `HasPanOverride = true` so the operator knows why.

---

## Scenario 5 — Salary above ESI cap

ESI limit is ₹21,000 (₹25,000 for differently-abled).

- Above limit → exempt.
- Mixed structure (some components `ConsiderForEsi = false`): only the flagged components count toward the ESI wage. So an employee at ₹30k gross with only ₹5k ESI-eligible → ESI applies on ₹5k.

---

## Scenario 6 — Kerala employee (half-yearly split PT)

Kerala uses Option-A rounding: total PT for the half-year (Apr–Sep or Oct–Mar) is fixed, but spread across 6 months as integer rupees.

Example: half-year gross ₹1.5L → slab ₹600 total → ₹100 each month.
Non-divisible: ₹250 total → ₹41 in months 1–5, ₹45 in the last month (absorbs remainder).

---

## Scenario 7 — Employee exit (Final Settlement)

`PayrollFnfOrchestrator.ComputeAsync` runs the engine with FnF-specific tweaks:

- `MonthsRemainingInFY = 1` → the entire remaining annual tax lands on this final payslip.
- `GratuityEnabled = false` → gratuity payout injected as a separate `IsFlat` earning component (operator-typed amount, no engine recomputation).
- LWF: if employee already had LWF deducted earlier in the same half-year, the FnF run skips LWF. Drives the half-year wrap logic in `LwfHalfYearLookback`.

---

## Scenario 8 — Operator edits LOP after running payroll

Variable inputs (LOP, one-time earnings, reimbursements) trigger `PayrollRecomputeService.RecomputeEmployeeAsync` for just that employee:

1. Loads stored config snapshot (not current DB config) → recompute is deterministic.
2. Loads prior employer YTD + current YTD identically to the initial run.
3. Calls engine, persists updated row + TDS worksheet.

Setting LOP back to zero produces identical numbers to the original initiate. (Was broken pre-audit-#3 — recompute hardcoded prior YTD to zero, causing silent TDS drift.)

---

## Scenario 9 — Re-evaluating a skipped employee

Employee skipped earlier in the month (missing PAN, DOB, or bank). Operator fills the gap.

`ReEvaluateSkippedCommand`:
- Undoes the skip.
- Builds fresh engine input.
- Writes payrun row + TDS worksheet with `ytd_tds_deducted = prior + current` (audit #5 fix — used to be prior only).

---

## Scenario 10 — Approving a run after a config change

Operator initiates April run, then edits EPF establishment code, then clicks Approve.

`ApprovePayrollRunCommand` recomputes every employee using the **snapshot taken at initiate**, not the new DB state. So Approve produces the same numbers as Initiate.

To use the new config: Reject the approval → mutate config → start a fresh run. This is intentional — once initiated, the run is frozen.

---

## Scenario 11 — High earner crossing ₹50L surcharge threshold

Surcharge kicks in at 10% above ₹50L taxable income. Without protection, crossing ₹50L by ₹1,000 could trigger ~₹1.5L in surcharge.

Engine applies marginal relief:

```
maxAllowed = taxAtThreshold + (income − threshold)
if tax + surcharge > maxAllowed:
    surcharge = maxAllowed − tax
```

So surcharge stays proportional to how far above the threshold the income actually sits.

---

## Scenario 12 — Taxable income near ₹12L (87A cliff)

This is a real cliff in the law itself:

- Income ≤ ₹12,00,000 → 87A rebate up to ₹60,000 → effectively zero tax.
- Income ₹12,00,001 → no rebate → roughly ₹71,500 tax lands.

Engine reflects this faithfully. (Worth surfacing in the UI when an employee is within ₹500 of the cliff — VPF / NPS contributions can drop them below.)

---

## Configuration snapshot

Every `PayrollRun` row stores the full `StatutoryConfig` as JSON at the moment of initiate. Recompute and FnF re-read this snapshot. So:

- A recompute today gives the same numbers as initiate did last month, even if slabs have changed.
- Different employees within the same run all use the same config — no drift mid-run.
- Audit trail: every paid run carries the exact rates that produced its numbers.

---

## YTD aggregation

Three sources merge into "current employer YTD":

1. **Approved/Paid runs** in the same fiscal year — summed from `payrun_employees` by `GetCurrentEmployerYtdAsync`. Returns gross, taxable gross, and TDS separately.
2. **Opening balances** (`employee_fy_openings`) — pre-system months on the same employer. Gross treated as taxable since no breakdown captured.
3. **Form 12B** (`prior_employer_ytds`) — different employer in the same FY. Adjusted via `PriorEmployerYtdMapper` before passing to the engine.

---

## What the engine does NOT do

- **Old tax regime.** V1 is new regime only.
- **Refunds via payroll.** TDS clamped at zero; over-deducted is recovered via IT return.
- **80C / 80D / HRA exemption.** Not relevant for new regime in v1.
- **Stock options, RSU tax events.** Not modeled.
- **Mid-year regime switch.** A single FY = one regime.
- **Arrears.** `ArrearAmount` field exists, hardcoded zero. Hook for later.

---

## Test coverage

| Layer | Files | Count |
|---|---|---|
| Engine calculators (direct) | 7 test files in `tests/Payroll.Engine.Tests/` | 112 |
| Application services / handlers | `tests/Payroll.Application.Tests/` | 143 |
| Infrastructure (Postgres) | `tests/Payroll.Infrastructure.Tests/` | 6 |

Total: 261. CI gate.

---

## Open items

- **#7 PT global toggle** — see `docs/follow-ups/2026-05-25-007-pt-global-toggle.md`. Engine already exempts when no slab matches the state, so realistic case is covered; a tenant-level off switch is product-decision territory.
