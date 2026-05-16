# UF-48: New Joiner Proration in Pay Run

**Module:** Pay Runs > Employee Summary > New Joiner Proration
**Tested:** 2026-05-16
**Mock Data Used:** Demo employees — Date of Joining observed on profiles
**App State Before:** May 2026 pay run PAID

---

## New Joiner Proration Overview

When an employee joins mid-month, their salary for that month is prorated based on:
- Working days from joining date to month end
- Calendar days in the month

**Formula:** `Monthly Salary Component × (Working Days in Month / Calendar Days in Month)`

OR some systems use: `Monthly Salary Component × (Working Days / Working Days in Month)`

---

## Proration Methods

### Method 1: Calendar Days (Zoho Default — Observed)
`Prorated Amount = Component × (Actual Days Present / Calendar Days)`

**Evidence from UF-50:** Arjun's LOP proration uses calendar days:
- May: 31 calendar days
- Payable Days: 31, LOP: 2, Actual Payable: 29
- Basic: ₹40,000 × 29/31 = ₹37,419 ≈ ₹37,417

This confirms Zoho uses **calendar days** for proration (not working days).

### Method 2: Working Days
`Prorated Amount = Component × (Days Worked / Working Days in Month)`

This is NOT what Zoho uses (based on LOP observation).

---

## New Joiner Proration Scenarios

### Scenario 1: Joined 16th May 2026 (mid-month)
- Calendar days in May: 31
- Days from 16th to 31st: 16 days
- Proration fraction: 16/31
- Basic ₹40,000: Prorated = ₹40,000 × 16/31 = ₹20,645

### Scenario 2: Joined 1st May 2026 (first day)
- Days: 31/31 = Full month
- No proration needed

### Scenario 3: Joined 31st May 2026 (last day)
- Days: 1/31
- Basic ₹40,000: Prorated = ₹40,000 × 1/31 = ₹1,290

---

## Pay Run Handling of New Joiners

### Auto-Detection
When pay run is processed for the month:
1. System checks Date of Joining for each employee
2. If DOJ is within the pay month: applies proration fraction
3. If DOJ is before the pay month start: full month salary

### New Joiner in Pay Run Summary

**Expected display in Pay Run Summary:**
| Employee | Status | Days | Gross |
|----------|--------|------|-------|
| [New Employee] | Active | 16/31 | ₹X (prorated) |

**Payslip will show:**
- Payable Days: 31
- LOP Days: 0
- Actual Payable Days: 16 (joining from 16th)
- Each component × 16/31

---

## Joining Mid-Pay-Period

If the pay schedule is non-monthly (e.g., weekly or bi-weekly):
- Proration uses pay period days, not calendar month days
- Demo org: Monthly pay schedule (29th pay date) — uses calendar month

---

## PF Proration for New Joiners

- PF deduction starts from month of joining
- If joined 16th May: PF deducted on prorated basic (16/31 of basic)
- PF wage = min(prorated basic, ₹15,000) — cap applies to prorated amount
- EPF ECR shows NCP Days = 15 (days before joining) for the first month

---

## PT Proration for New Joiners

- PT slab determined by full-month salary (not prorated) — per RBI/PT act interpretation
- Some states use prorated salary for slab; others use full month salary
- Zoho likely uses full-month salary for PT slab determination
- Actual PT amount: Flat amount per slab (not prorated) — deducted in full if joining month is a PT month

---

## Demo Org Employee Joining Dates

| Employee | DOJ | May 2026 Pay Status |
|----------|-----|---------------------|
| Arjun Mehta | Before May 2026 | Full month (29/31 after LOP) |
| Priya Sharma | Before May 2026 | Full month |
| EMP003 (Vikram) | Unknown | Skipped (onboarding incomplete) |
| EMP004 (Aisha) | Unknown | Skipped |
| EMP005 (Rahul) | Unknown | Skipped |

No mid-month joiner to observe in demo state.

---

## Business Rules
1. Proration uses calendar days (not working days) — confirmed from LOP data
2. All fixed components prorated using same fraction (Days Present / Calendar Days)
3. Variable components not prorated (flat amounts entered by admin)
4. PF computed on prorated basic; cap applies to prorated amount
5. TDS computed on annualized prorated salary for the month
6. DOJ must be correctly entered in employee profile for auto-proration

## Gaps / Observations
- No mid-month joiner in demo org to directly observe proration in pay run
- PT behavior for new joiners in Kerala not confirmed
- TDS annualization for partial-year joiners not directly tested

## Open Questions
- [ ] For a new joiner in November (5 months remaining in FY), does TDS annualize over 5 months or 12?
- [ ] Can admin override the prorated amount for a specific employee?
- [ ] How does the system handle salary revision mid-month (also a proration scenario)?
- [ ] Is there a "Days Present" field that admin can edit, or is it derived from DOJ only?
