# UF-70: ESI Return and Challan

**Module:** Taxes & Forms > ESI
**Tested:** 2026-05-16
**Mock Data Used:** Demo org; all employees ESI-ineligible (salary > ₹21,000)
**App State Before:** May 2026 pay run PAID; ESI registration number configured

---

## ESI Overview

ESI = Employees' State Insurance. Governed by the ESI Act, 1948 and administered by ESIC (Employees' State Insurance Corporation).

### Applicability
- Establishments with 10+ employees (some states: 20+)
- Employees earning ≤ ₹21,000 gross per month
- ₹25,000 threshold for persons with disability

### Contribution Rates (FY2026-27)
| Party | Rate | Basis |
|-------|------|-------|
| Employee | 0.75% | ESI Wages (Gross Salary) |
| Employer | 3.25% | ESI Wages (Gross Salary) |
| Total | 4.00% | — |

**ESI Wage** = Gross Salary (all earnings including basic, HRA, allowances, bonus, OT)
- Excludes: Washing allowance, Annual bonus under Payment of Bonus Act, Retrenchment compensation

### Wage Ceiling
- Monthly ESI wage ceiling: ₹21,000
- Employee earning > ₹21,000 → Not covered by ESI
- Once covered, continues until wage exceeds ₹21,000 for 2 consecutive contribution periods

---

## ESI Contribution Periods

| Contribution Period | Payment Due Date |
|--------------------|-----------------|
| April – September | 15th of following month |
| October – March | 15th of following month |

**Contribution period ≠ deduction period.** Deduction is monthly; ESIC return filing is half-yearly.

---

## Current Demo Org State

### Employee Salary vs ESI Threshold
| Employee | Gross Salary | ESI Eligible |
|----------|-------------|--------------|
| Arjun Mehta | ₹65,484 | No (> ₹21,000) |
| Priya Sharma | ₹22,000 | No (> ₹21,000) |

**Result:** No ESI deductions in demo org. ESI reports will show ₹0.

---

## ESI in Zoho Payroll — Navigation

### Taxes & Forms > ESI (Expected Sections)
1. **ESI Contributions** — Monthly summary of employee/employer contributions
2. **ESI Challan** — Generate challan for payment
3. **ESI Return** — Half-yearly return (Form 5)

### ESI Settings Configuration
- **ESI Registration Number**: Format `12-34-56789-000-0000` (17-char ESIC code)
- **ESI Applicable**: Toggle per employee
- **ESI Wage Component**: What counts as ESI wage

---

## ESI Monthly Deduction Flow

### Step 1: Pay Run Processing
When ESI-eligible employee's pay run is processed:
- System computes ESI wage = Gross Salary (capped at ₹21,000)
- Employee deduction = 0.75% × ESI wage
- Employer contribution = 3.25% × ESI wage
- Deduction appears on payslip under Statutory Deductions

### Step 2: ESI Challan Generation
After monthly pay run finalized:
1. Navigate to Taxes & Forms > ESI
2. Select month
3. Click "Generate Challan" or "Download ESI Challan"
4. Challan shows: Total employee deduction + Employer contribution
5. Pay via ESIC portal (https://www.esic.in) by 15th of following month

### Step 3: ESI Return (Half-Yearly — Form 5)
Filed twice yearly:
- April–September period → File by 11th November
- October–March period → File by 12th May

**Form 5 contains:**
- Employer establishment details
- Employee-wise: IP number (Insurance Number), name, wage, contributions
- Total contributions for the period

---

## ESI Reports Available (from UF-79)

| Report | Content |
|--------|---------|
| ESI Summary Report | Month-wise ESI contributions per employee |
| ESI Monthly Contribution Report | Detailed per-employee ESI breakdown for one month |

---

## ESI Statutory Reference

| Provision | Reference |
|-----------|-----------|
| Applicability | Section 2(12), ESI Act 1948 |
| Contribution rates | Section 40, ESI Act + ESIC Circular |
| Wage definition | Section 2(22), ESI Act |
| Return filing | Regulation 26, ESI (General) Regulations 1950 |

---

## Business Rules
1. ESI applies only if employee gross ≤ ₹21,000/month
2. Once an employee crosses ₹21,000, ESI stops from the next contribution period (not immediately)
3. New joiners earning ≤ ₹21,000 are auto-enrolled
4. Employee ESI = 0.75%; Employer ESI = 3.25%
5. ESI wage = total gross earnings (not just basic)
6. Challan due: 15th of following month
7. Return (Form 5): Half-yearly

## Gaps / Observations
- Taxes & Forms > ESI URL not confirmed — could not navigate (all employees ineligible; page may show empty)
- ESI challan generation steps not directly observed
- Form 5 export not tested
- 🟡 ESI registration number format confirmed in Settings but employee-level ESI toggle not verified

## Open Questions
- [ ] When ESI is configured for an eligible employee, where does the employer ESI contribution appear — in payslip deductions or separately?
- [ ] Can Zoho auto-detect when employee salary crosses ₹21,000 and disable ESI?
- [ ] Does Zoho generate Form 5 (half-yearly return) directly, or just contribution data?
- [ ] Is there an IP (Insurance Number) field on the employee profile?
