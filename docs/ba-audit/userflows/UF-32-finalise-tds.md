# UF-32: Finalise TDS

**Module:** Taxes & Forms > TDS / Employee Investments
**Tested:** 2026-05-16
**Mock Data Used:** Arjun's IT Declaration locked; all TDS = ₹0
**App State Before:** TDS = ₹0 for all employees in all processed pay runs

## Steps Executed
1. Observed TDS Sheet for Arjun (May 2026 pay run) via cross-origin iframe screenshot
2. Identified IT Declaration LOCKED as root cause of ₹0 TDS
3. Documented TDS finalization flow

---

## TDS Finalization Concept

In Zoho Payroll, "finalising TDS" refers to the process of locking the income tax declaration and TDS computation for the financial year, typically done in February-March before the final pay run of the year.

### Why TDS Finalization Is Needed
- Throughout the year, TDS is deducted based on projected annual income (declaration)
- In the final month (March), actual YTD income is known
- TDS may need adjustment: if total TDS deducted < actual tax liability → remaining TDS due in March
- Admin "finalises" by confirming the March TDS computation is correct before processing the last pay run

---

## Current TDS State

### Root Cause of ₹0 TDS (Discovered)
From `#/settings/preferences/it-declaration`:
**"IT Declaration is Locked"** — the org-wide declaration release has not been triggered.

**Consequence:** Zoho Payroll does not compute TDS for employees whose IT declaration has not been submitted/released. TDS = ₹0 for all months.

**Statutory risk (🔴):** If Arjun's taxable income exceeds the exemption threshold (₹4,00,000 in new regime for FY2026-27 after standard deduction), TDS must be deducted. Not deducting TDS makes the employer liable under Section 201 of Income Tax Act for:
- Default in TDS deduction: Interest @ 1% per month for the period of non-deduction
- Default in TDS deposit: Interest @ 1.5% per month
- Penalty under Section 271C: Equal to the amount of TDS not deducted

### Arjun's Estimated TDS (if declaration was active)
- Annual Gross: ₹70,000 × 12 = ₹8,40,000
- Less: Standard Deduction (new regime): ₹75,000
- Taxable Income: ₹7,65,000
- New Regime Tax (FY2026-27):
  - ₹0 up to ₹4,00,000 = ₹0
  - ₹4,00,001 to ₹8,00,000 @ 5% = ₹4,00,000 × 5% = ₹20,000
  - Remaining ₹3,65,000 within ₹8L slab covered
  - Wait: ₹7,65,000 falls in ₹4L-₹8L slab
  - Tax = (₹7,65,000 − ₹4,00,000) × 5% = ₹3,65,000 × 5% = ₹18,250
  - Health and Education Cess: 4% of ₹18,250 = ₹730
  - Total Annual Tax: ₹18,980
  - Monthly TDS: ₹18,980 / 12 = ₹1,582/month

This matches the earlier estimate — Arjun should be paying ~₹1,582/month TDS, but is currently paying ₹0.

---

## TDS Finalization Steps (Expected Flow)

### Step 1: Release IT Declaration
1. Navigate to `#/settings/preferences/it-declaration`
2. Click "Release IT Declaration"
3. Employees submit declarations via portal
4. Admin may submit on behalf of employees

### Step 2: Monthly TDS Computation
After declaration is submitted:
- TDS for each remaining month = (Annual Tax − YTD TDS Already Deducted) / Remaining Months
- TDS appears in TDS Liabilities page
- TDS is deducted in each monthly pay run

### Step 3: Year-End Reconciliation
In February/March:
1. Admin reviews TDS Liabilities for Q4
2. Verifies all declarations are finalized and POI is verified
3. March pay run TDS may be higher to account for any shortfall
4. March TDS computation is "finalized" when the March pay run is approved

### Step 4: File Form 24Q Q4
After March pay run:
1. Generate Form 24Q text file for Q4 (Jan-Mar)
2. Validate and upload to TRACES
3. Issue Form 16 by 15th June

---

## TDS Computation Formula (New Regime FY2026-27)

```
Annual Taxable Income = Gross Annual Salary 
                       − Standard Deduction (₹75,000)
                       − Employer NPS contribution (80CCD(2), up to 10% of basic)
                       − Other allowed deductions (new regime allows very few)

Annual Tax = Apply new regime slabs:
  ₹0 – ₹4,00,000 → 0%
  ₹4,00,001 – ₹8,00,000 → 5%
  ₹8,00,001 – ₹12,00,000 → 10%
  ₹12,00,001 – ₹16,00,000 → 15%
  ₹16,00,001 – ₹20,00,000 → 20%
  ₹20,00,001 – ₹24,00,000 → 25%
  > ₹24,00,000 → 30%

Rebate u/s 87A: If taxable income ≤ ₹7,00,000 → Tax = 0
  (Arjun: ₹7,65,000 > ₹7,00,000 → No rebate)

Cess: 4% of tax after rebate

Monthly TDS = (Annual Tax − TDS Already Deducted) / Remaining Months in FY
```

---

## Business Rules
1. TDS computation begins only after IT Declaration is released and submitted
2. Monthly TDS = rolling forward calculation (adjusts each month based on YTD)
3. No negative TDS allowed in a month (if over-deducted, excess is adjusted forward)
4. In March: if total tax < total TDS deducted → refund to employee; if > → additional deduction
5. Without PAN: TDS rate is 20% flat (no slab benefit) — Priya's PAN is missing, but salary is below threshold so ₹0 TDS

## Gaps / Observations
- 🔴 IT Declaration not released — employer is liable for TDS default for Arjun (₹1,582/month × months elapsed)
- No TDS finalization wizard or checklist visible in current UI state
- "Finalise TDS" as a discrete UI action not found — likely embedded in year-end pay run process

## Open Questions
- [ ] Is there an explicit "Finalize TDS" button or workflow in Zoho Payroll?
- [ ] If admin releases IT Declaration mid-year (e.g., June), does the system retroactively compute TDS for prior months?
- [ ] Can TDS be computed without IT Declaration (using default new regime assumptions)?
- [ ] Where does the employer enter Form 12B (prior employer TDS) to adjust the TDS computation?
