# Item 93: Loan Deduction in Pay Run

**URL / Navigation Path:** Pay Run module (separate module)
**Module:** Loans → Pay Runs integration

---

## Purpose

Document how loan EMI deductions appear and are processed within the payroll run cycle.

---

## Current Session Limitation

The two test loans (LOAN-00001, LOAN-00002) both have EMI Deduction Start Date of **01/07/2026**. The current session's pay runs cover up to May 2026. No June or July 2026 pay run was created during this audit session.

As a result, the loan deduction in pay run flow could NOT be directly observed in the UI.

The following is based on:
1. Loan creation confirmation messages
2. Zoho Payroll product behaviour patterns inferred from the form
3. The "Loan Repayment Summary" state on the loan detail page

---

## Expected Flow (Based on System Design Evidence)

### When Does Loan Deduction Occur?

The EMI deduction is triggered when a pay run is created/finalized for a pay period that **on or after** the loan's EMI Deduction Start Date.

**Evidence:** The loan detail page shows:
- "Next Instalment Date: 01/07/2026" — the payroll run for July 2026 will process the first deduction
- "The employee is yet to pay the first instalment through Zoho Payroll." — confirms no deduction processed yet

### Pay Run Integration Points

1. **Pay Run Initiation** — When payroll is initiated for July 2026 onwards, Zoho Payroll automatically includes active loans with EMI start date ≤ pay period
2. **Loan Deduction Component** — EMI amount appears as a deduction line item on the employee's salary calculation
3. **Payslip** — EMI deduction expected to appear as a separate deduction line (label: likely "Loan EMI" or loan name)
4. **Post-Processing** — After pay run finalization, the repayment schedule table on Loan Detail is populated

### Repayment Schedule Population

After a pay run processes the EMI:
- A new row appears in the Loan Detail repayment table
- Row columns: INSTALMENT DATE | EMI amount | TOTAL AMOUNT REPAID (cumulative) | REMAINING AMOUNT | ACTION
- Amount Repaid and Remaining Amount in the summary section update accordingly
- Progress bar updates

### Perquisite Calculation (Non-Exempt Loans)

For Loan 2 (Emergency Loan, 6% perquisite rate, not exempt):
- The loan perquisite (notional interest benefit) is calculated as a taxable perquisite
- Rule: Perquisite = Outstanding loan balance × Applicable rate (e.g., SBI rate or loan type rate) / 12 per month
- This perquisite adds to the employee's gross taxable income for TDS
- Form 12BA / Form 16 will include this perquisite value

For Loan 1 (Personal Loan, 0% rate, exempt):
- No perquisite tax impact
- Exempt checkbox was checked at creation

---

## Loan EMI as Deduction in Pay Slip

Expected payslip structure (not directly observed):

| Section | Component | Amount |
|---------|-----------|--------|
| Deductions | Loan EMI — Personal Loan | ₹5,000 |
| | (or Loan EMI — [Loan Name]) | |

**Note:** The EMI deduction reduces net pay. It does NOT affect gross salary or CTC.

---

## Variable vs Fixed EMI

The EMI Instalment Amount is fixed at loan creation. However, the system supports:
- **Edit Loan** — allows changing instalment amount (which would recalculate remaining instalments)
- **Pause Instalment Deduction** — suspends EMI for specified period
- **Manual prepayment** (Record Repayment) — reduces outstanding balance, potentially shortening tenure

---

## Key Business Rules

1. EMI deductions are automatic — no manual step required in pay run once loan is configured.
2. Deduction begins from EMI Start Date and continues until all instalments are processed.
3. Perquisite for non-zero, non-exempt loans is taxable — must be reported in TDS calculations.
4. If employee is not part of a pay run in a given month (e.g., LOP, unpaid leave), the loan EMI behaviour is unknown (needs investigation).
5. Final instalment may differ from regular EMI if loan amount is not exactly divisible.

---

## Cross-Module Impact

- Pay Run → Loan: triggers EMI deduction processing
- Loan → TDS: perquisite for non-exempt loans adds to taxable income
- Loan → Payslip: EMI appears as deduction line
- Loan → Loan Detail: repayment schedule populated after pay run

---

## Open Questions

- [ ] Does the EMI deduction appear in the payslip under "Deductions" section? What is the exact label?
- [ ] Can the payroll admin exclude/skip a specific loan EMI for a given pay run?
- [ ] What happens if an employee's net pay is less than the EMI amount? Is partial deduction made?
- [ ] Is the EMI deduction shown in the "Review Payroll" step before finalization?
- [ ] How is the loan perquisite value reported in Form 12B/Form 16?
- [ ] If a pay run is revised (after finalization), does the loan repayment record get reversed?
- [ ] Is there an annual loan summary report available?
