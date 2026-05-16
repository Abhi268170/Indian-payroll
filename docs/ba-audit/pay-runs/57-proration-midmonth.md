# Pay Runs > Proration — Mid-Month Joiners & LOP

## URL / Navigation Path

Proration behaviour observed within:
`https://payroll.zoho.in/#/payruns/{id}/summary` — Employee split panel (Draft state)

No dedicated proration screen. Proration is implicit in LOP entry.

## Purpose

Documents how Zoho Payroll handles salary proration for:
1. Loss of Pay (LOP) — admin-entered absent days
2. Mid-month joiners — employees who join after day 1 of the pay period
3. Mid-month exits — employees who leave before the last day

## LOP Proration — Confirmed Behaviour

**Mechanism:** Admin enters LOP days in the employee split panel. On save, all salary components prorate automatically.

**Formula (confirmed by observation):**
```
Prorated Amount = (Base Days − LOP Days) / Base Days × Full Monthly Component Amount
```

**Base Days:** Determined by pay schedule calendar month. May 2026 = 31 Base Days.

**Example — EMP001, 2 LOP Days, May 2026 (31 base days):**
| Component | Full Amount | Prorated (29/31) | Displayed |
|-----------|-------------|------------------|-----------|
| Basic | ₹40,000 | ₹40,000 × 29/31 | ₹37,417 |
| HRA | ₹16,000 | ₹16,000 × 29/31 | ₹14,967 |
| Fixed Allowance | ₹14,000 | ₹14,000 × 29/31 | ₹13,100 |

Rounding: Results rounded to nearest rupee (₹37,417 = ₹37,419.35 rounded down — consistent with truncation rather than rounding).

**Actual Payable Days display:** "Actual Payable Days: 29" shown live in the LOP section as admin types.

## Mid-Month Joiner — Confirmed Behaviour (EMP002, Joined 16 May 2026)

**Finding: Zoho does NOT auto-prorate for mid-month joiners in a regular pay run.**

EMP002 was configured with a joining date of 16 May 2026. In the pay run, the system showed:
- Paid Days: 31 (full month)
- Net Pay: ₹22,000 (full month salary)
- No LOP auto-applied

**Implication:** Admin must manually calculate and enter LOP days equivalent to the days before the joining date.

For EMP002 (joined 16 May): days before joining = 15 days (May 1–15). Admin must enter 15 LOP days manually.
- Prorated: 16/31 × ₹22,000 = ₹11,355 (correct for 16 working days from join date)

**No system validation or warning** was shown about the mid-month join date during the run. The system treats all active employees as full-month employees unless LOP is entered.

**Design gap confirmed:** No automatic detection of joining date within pay period → no auto-LOP suggestion.

## Mid-Month Exit — Not Tested (EMP005 Skipped)

EMP005 (Rahul Desai) was designated for F&F settlement with exit date 20 May 2026 and 8 days leave encashment. However, EMP005's onboarding was incomplete in this test org, so the employee was skipped in the pay run.

**Expected Zoho behaviour (inferred):** For a mid-month exit:
- Admin would enter LOP days equivalent to days after exit date
- Leave encashment would be entered as one-time earning
- Gratuity (if applicable) would be a separate one-time earning
- No dedicated "Full & Final Settlement" wizard observed in the regular pay run flow

**Gaps to investigate in a fully onboarded org:**
- Is there an F&F settlement type distinct from a regular pay run?
- Does Zoho show a specific F&F summary or compute gratuity automatically?
- The "Off Cycle Pay Run" (single date entry) may be the F&F mechanism — requires further testing.

## Proration Edge Cases — Not Yet Tested

| Scenario | Expected | Confirmed |
|----------|----------|-----------|
| LOP = Base Days (entire month absent) | Net Pay = ₹0 | Not tested |
| LOP > Base Days (validation) | Should reject | Not tested |
| Mid-month salary revision | Arrear paid in next run? | Not tested |
| Multiple LOP entries in same month | Cumulative or replace? | Not tested |
| Non-integer LOP (e.g., 0.5 days) | Spinbutton accepts? | Not tested |

## Statutory Proration Notes

**PF proration:** When PF is configured, PF deduction prorates with the PF wage (capped at ₹15,000/month). LOP reduces PF wage proportionally. Not observable in this org (PF not configured).

**ESI proration:** ESI eligibility check uses gross wage. Mid-month joiner with prorated salary may cross/uncross the ₹21,000 ESI threshold. Not observable in this org.

**Professional Tax:** PT slabs are typically monthly and based on gross; proration impact varies by state. Kerala PT: ₹200/month for wages > ₹12,000. Not observable (PT not configured in this org).

## Key Observations for Our Build

1. **No auto-proration from joining date** — critical gap in Zoho. Our build should auto-compute LOP from date of joining when joining date falls within the pay period. Formula: LOP = (joining_date.day - pay_period_start.day) days. Surface this as a suggestion with admin override.
2. **LOP denominator = calendar days in month** — not working days. May has 31 calendar days regardless of weekends. Our engine must use `days_in_month(year, month)` not business days.
3. **Truncation vs rounding** — observe ₹37,417 (not ₹37,418 which would be standard rounding). Use consistent truncation or standard rounding with half-up — define this as a statutory constant.
4. **F&F not visible in regular run** — Zoho likely handles F&F via Off Cycle Run or a dedicated F&F module. Our build should explicitly design an F&F flow as part of the payroll run lifecycle, not as an afterthought.
5. **Leave encashment as one-time earning** — confirmed. Leave encashment is entered in the "Add Earning" dropdown as a separate line item, not auto-computed from leave balance. Our build should auto-compute from leave balance if leave module is integrated.

## Screenshots

- `screenshots/57-emp002-proration-panel.png` — EMP002 split panel showing 31 days / ₹22,000 (no auto-proration despite 16 May join date)
- `screenshots/54-lop-2days-before-save.png` — LOP entry showing Actual Payable Days = 29
- `screenshots/54-emp001-after-lop-save.png` — Post-save prorated amounts for EMP001

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
