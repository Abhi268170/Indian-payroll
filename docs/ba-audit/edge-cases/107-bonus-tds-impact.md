# Edge Case > Annual Bonus TDS Impact

## Scenario
Add a one-time bonus of ₹2,00,000 to EMP003 (Vikram Nair) in the June 2026 pay run as an ad-hoc earning. Observe TDS spike and document how Zoho computes TDS on bonus.

## Steps to Reproduce
1. Navigate to Pay Runs > New (to create June 2026 run)
2. In variable inputs, add Bonus earning for EMP003
3. Compare TDS before and after bonus addition
4. Finalize run and observe TDS on payslip

## Expected Behaviour (statutory rule)
**TDS on Bonus — Annualisation Method:**
Per CBDT Circular No. 04/2024 and standard payroll practice:
- Employer must estimate total taxable income for the FY at each payroll month
- For a mid-year bonus: annualise the bonus, add to projected annual salary, compute annual tax, then distribute remaining tax equally over remaining months
- **Formula**: `Monthly TDS = (Annual Tax on [Annual Basic Salary + Bonus + Declarations]) - Tax Already Paid) / Remaining Months`

For EMP003 (Vikram Nair, CTC ₹25,00,000/year, New Regime):
- Basic annual salary: ~₹14,35,484 (57.14% of ₹25L)
- Bonus: ₹2,00,000
- Annual taxable income (approx): ₹16,35,484 (after standard deduction ₹75,000) = ₹15,60,484
- Under New Regime FY2026-27 slabs:
  - Up to ₹3,00,000: Nil
  - ₹3,00,001–₹7,00,000: 5% = ₹20,000
  - ₹7,00,001–₹10,00,000: 10% = ₹30,000
  - ₹10,00,001–₹12,00,000: 15% = ₹30,000
  - ₹12,00,001–₹15,00,000: 20% = ₹60,000
  - ₹15,00,001+: 30% on ₹60,484 = ₹18,145
  - **Total Annual Tax**: ~₹1,58,145
  - **Monthly TDS (without bonus)**: ~₹13,178

After adding ₹2,00,000 bonus in June (month 3):
  - Annual taxable with bonus: ~₹17,60,484
  - Tax on ₹17,60,484: ₹30%: ₹60,484 + additional ₹2L @ 30% = ₹60,000 more
  - Total annual tax: ~₹2,18,145
  - Tax already paid (April+May): ~₹26,356
  - Remaining tax: ₹1,91,789
  - Remaining months: 10 (June to March)
  - **June TDS spike**: ₹19,179/month (increased from ₹13,178)

## Actual Zoho Behaviour

### Blocker: EMP003 Onboarding Incomplete
EMP003 (Vikram Nair) was skipped in the May 2026 pay run with status "Onboarding incomplete":
```json
{ "payment_status": "skipped", "notes": "Onboarding incomplete" }
```
EMP003 has no bank details, work email, or complete statutory information. This means:
- EMP003 cannot participate in any payrun until onboarding is completed
- The bonus TDS scenario **cannot be tested for EMP003 in the current org state**

### Variable Pay Types Confirmed (from `/api/v1/payroll/meta`)
The following variable/ad-hoc earning types are available for June pay run:
| Earning Name | Earning Type | Earning ID |
|-------------|-------------|------------|
| Overtime Allowance | overtime | 3848927000000032481 |
| Gratuity | gratuity | 3848927000000032480 |
| **Bonus** | **bonus** | **3848927000000032471** |
| Commission | commission | 3848927000000032472 |
| Leave Encashment | leave_encashment | 3848927000000032479 |
| Notice Pay | notice | 3848927000000032482 |
| Hold Salary | hold_salary | 3848927000000032483 |

**Bonus earning ID**: `3848927000000032471` — correctly configured as a variable pay type

### May 2026 Payrun TDS Observation
From actual May 2026 payrun data for EMP001 (Arjun Mehta, CTC ₹9,45,000):
- Income Tax deduction: **₹0**
- Annual CTC: ₹9,45,000; monthly gross: ~₹65,484
- Annual gross (approx): ~₹7,85,808
- Under New Regime FY2026-27: Annual tax on ₹7,85,808 minus standard deduction ₹75,000 = ₹7,10,808
  - 0–3L: Nil; 3L–7L: ₹20,000; 7L–7.10808L: 10% × ₹10,808 = ₹1,081
  - Annual tax: ~₹21,081
  - Monthly TDS: ₹1,757

**The actual TDS was ₹0 in May 2026.** This is anomalous — either:
1. Zoho is not yet computing TDS (trial period, incomplete setup)
2. Some exemption applies (rebate under Section 87A would apply if annual tax ≤ ₹25,000 — which it does here at ₹21,081 < ₹25,000 threshold)

**Correct statutory treatment**: Under new Section 156(2) [formerly 87A rebate], a resident individual with total income up to ₹7,00,000 gets a full rebate under new regime. EMP001's taxable income (~₹7,10,808) is slightly above ₹7,00,000, so partial rebate or no rebate applies. The ₹0 TDS may indicate Zoho is applying the ₹7L boundary rebate calculation, but this needs verification.

## Screenshots
- None captured for this scenario (EMP003 was skipped/incomplete)

## Gap / Bug / Surprise
1. **BLOCKER**: EMP003 cannot be used — onboarding is incomplete. The bonus TDS scenario cannot be live-tested in this org.
2. **TDS = ₹0 for EMP001** despite income slightly above ₹7L — needs investigation to confirm if this is Section 87A/156(2) rebate correctly applied, or a TDS configuration gap.
3. **No TDS for any employee in May 2026** (total taxes = ₹0) — suspect TDS computation may be disabled or incorrectly configured in this trial org.
4. Zoho's TDS computation method (annualise vs spread) could not be directly observed without a live bonus entry being processed.

## How We Should Build This
- TDS computation: Use annualisation method per CBDT guidelines
- For one-time bonus: Add bonus to total projected annual income for the FY, recompute total annual tax, subtract tax already deducted, spread remaining tax over remaining months
- Section 87A / Section 156(2) rebate: Apply automatically when net taxable income ≤ ₹7,00,000 (New Regime) — rebate = actual tax liability, capped at ₹25,000
- Marginal relief: If income is just above ₹7L, compute marginal relief (ensure tax + surcharge doesn't exceed income above ₹7L)
- Store per-run TDS computation detail: annual income projected, tax computed, tax already deducted, months remaining, monthly TDS — as immutable audit artifact
- Make TDS computation visible in payslip detail view with breakdown
