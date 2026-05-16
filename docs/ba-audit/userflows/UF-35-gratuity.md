# UF-35: Gratuity Computation

**Module:** Salary Components > Gratuity / Employee Exit / FnF
**Tested:** 2026-05-16
**Mock Data Used:** Salary Component "Gratuity" (Variable; Flat Amount; EPF=No; ESI=No; Active)
**App State Before:** Gratuity configured as a variable salary component

---

## Gratuity in Zoho Payroll

### Component Configuration
From Settings > Salary Components > Earnings:
| Field | Value |
|-------|-------|
| Name | Gratuity |
| Earning Type | Gratuity |
| Calculation Type | Variable; Flat Amount |
| Consider for EPF | No |
| Consider for ESI | No |
| Status | Active |

**"Variable; Flat Amount"** means the gratuity amount is entered manually (not auto-calculated from a formula in the salary component settings). The admin inputs the gratuity amount during FnF processing.

---

## Statutory Gratuity — Payment of Gratuity Act, 1972

### Eligibility
- Employee must have completed at least **5 years of continuous service**
- Exception: In case of death or disability, gratuity is payable even without 5 years

### Gratuity Formula
```
Gratuity = (Last Drawn Monthly Salary × 15/26) × Years of Service
```

Where:
- **Last Drawn Monthly Salary** = Basic + Dearness Allowance (DA)
  - Note: HRA, bonus, overtime, and other allowances are NOT included
  - In most private sector companies without DA: Just the Basic salary
- **15** = 15 days of salary
- **26** = 26 working days per month (statutory assumption)
- **Years of Service** = Round down to nearest half-year
  - E.g., 6 years 7 months = 7 years (6.5+ rounds to 7)
  - E.g., 6 years 4 months = 6 years (below 6.5 rounds to 6)

### Practical Examples

**Arjun Mehta — Would be eligible?**
- DOJ: (from profile — not confirmed, but employed for 1+ year in mock data)
- Gratuity requires 5 years → Arjun would NOT be eligible yet
- If Arjun had 5 years: Gratuity = (₹40,000 Basic × 15/26) × 5 = ₹1,15,385

**Vikram Nair:**
- DOJ: 01/01/2025 → As of May 2026 = 1 year 4 months → NOT eligible

---

## Tax Exemption on Gratuity

### For Private Sector Employees (Not Government)
- Exempt from income tax up to the LEAST of:
  1. Actual gratuity received
  2. ₹20,00,000 (₹20 lakh — increased from ₹10L in 2018 notification)
  3. Formula: (Last Drawn Salary × 15/26 × Years of Service)

**Practical implication:** Most employees receive exactly the formula amount → full gratuity is exempt up to ₹20L.

### For Government Employees
- Fully exempt under Section 10(10)(i)

### When Taxable
- Gratuity > ₹20,00,000 → excess is taxable as salary income
- Gratuity paid voluntarily (ex-gratia, not under Gratuity Act) → fully taxable unless covered by other exemptions

---

## How Zoho Payroll Handles Gratuity

### During Regular Pay Runs
Gratuity does NOT appear in regular monthly payslips unless the employer has a gratuity fund scheme where monthly provisioning is done.

### During FnF
1. Admin computes gratuity amount (manually or system-computed)
2. Enters the gratuity amount in the FnF pay run as a "Gratuity" variable component
3. System applies the tax exemption (up to ₹20L) automatically in TDS computation
4. Net gratuity (taxable portion, if any) is added to FnF month's taxable income
5. TDS on FnF pay run adjusts accordingly

### Monthly Provisioning (if configured)
Some companies account for gratuity as a monthly expense:
- Each month: Provision = (Monthly Salary × 15/26) / 12 × 1 month
- This does not appear on employee payslips (employer accounting entry only)
- Payroll Journal would capture this as a liability accrual

---

## Gratuity Trust / Fund
Larger companies maintain a Gratuity Trust Fund (approved by Income Tax dept):
- Employer contributes monthly to the fund
- When employee exits, fund pays the gratuity
- Employer gets tax deduction on contributions (Section 36(1)(v))
- Zoho Payroll may or may not have a direct integration for gratuity fund management

---

## Observed Salary Component: "Gratuity"
The "Gratuity" component in Salary Components Earnings is marked:
- EPF = No (correct — gratuity is not EPF wage)
- ESI = No (correct — gratuity is not ESI wage for payroll purposes)
- Variable; Flat Amount → admin enters the computed amount at FnF time

This design is appropriate: Gratuity is not a regular recurring component — it's a one-time exit benefit.

---

## Business Rules
1. Minimum 5 years continuous service required (exception: death/disability)
2. Formula: Basic × 15/26 × years (rounded to nearest 0.5 year)
3. Maximum tax-exempt: ₹20,00,000
4. Gratuity above ₹20L is taxable in FnF month's income
5. In Zoho: Gratuity is entered as a variable flat amount — system does not auto-calculate from formula (admin must compute and enter)
6. No EPF or ESI on gratuity

## Gaps / Observations
- No auto-calculation of gratuity from DOJ + salary — admin must manually compute and enter
- 🟡 A gratuity calculator / preview in the FnF form would be a useful feature (not confirmed present)
- Gratuity trust integration not investigated

## Open Questions
- [ ] Does Zoho Payroll auto-calculate gratuity based on DOJ and basic salary, or is it always manual entry?
- [ ] Is there a gratuity provisioning/fund management module?
- [ ] For the 5-year eligibility check — does Zoho warn the admin if the exiting employee has less than 5 years?
- [ ] How does the system handle gratuity for employees who joined mid-year (partial year at start)?
