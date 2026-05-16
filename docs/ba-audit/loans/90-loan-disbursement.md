# Item 90: Loan Disbursement

**URL / Navigation Path:** No dedicated disbursement URL
**Module:** Loans
**Entry Point:** Loan Detail page → "Record Repayment" button (which also covers pre-payment recording)

---

## Purpose

Document how loan disbursement works — how and when the loan principal reaches the employee.

---

## Key Finding: Disbursement is Out-of-Band

**Zoho Payroll explicitly does NOT disburse loan amounts through the payroll run.**

The Loan Amount field tooltip states verbatim:
> "The loan amount will not be paid as a part of the pay run. You need to pay the amount to your employee separately."

This means:
- The employer must transfer the loan amount to the employee via external means (bank transfer, cheque, cash, etc.)
- Zoho Payroll tracks the loan for EMI deduction purposes only
- No "Disburse" button or payment action exists in the UI

---

## Record Repayment Modal (Manual Pre-Payment)

URL state: `#/loans/{id}?record_payment=true`

This modal is used when an employee repays outside the payroll cycle (e.g., cash repayment, early partial payment). It is NOT the disbursement flow.

**Modal Structure:**

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Repayment Amount | Number (₹) | Yes | max = remaining principal amount | Input has HTML `max` attribute set to current remaining amount |
| Repayment Date | Date picker (dd/MM/yyyy) | Yes | Must be > loan start date AND not in the future | Error: "Please make sure pre-payment is not a date in the future." / "Prepayment date must be greater than loan start date and loan payment date." |
| Payment Mode | Combobox | Yes | One of: Cheque, Cash, Bank Transfer, Others | Ember ac-box combobox |
| Remaining Amount | Display (read-only) | — | Auto-updates as amount field changes (live preview) | Shows ₹X after entry |

**Payment Mode Options:** Cheque, Cash, Bank Transfer, Others

**Actions:** Record Repayment (submit) | Cancel

**Validation:**
- "Please make sure pre-payment is not a date in the future." — repayment date is in the future
- "Prepayment date must be greater than loan start date and loan payment date." — repayment date is before disbursement date

**Constraint discovered during audit:** For loans with a future disbursement date (e.g., 01/06/2026 when today is 15/05/2026), no manual repayment can be recorded since any valid date (after disbursement) is in the future.

---

## Screenshot

- `screenshots/90-record-repayment-modal.png` — Record Repayment modal

---

## EMI Deduction via Pay Run (Normal Flow)

For the normal EMI deduction flow:
1. Loan is created with Disbursement Date and EMI Start Date
2. When payroll is run for the month that includes the EMI Start Date, the EMI amount is automatically deducted from the employee's net pay
3. The repayment schedule table on Loan Detail is populated with each deducted instalment
4. Status bar shows % of loan repaid

This flow was not directly observable in the current session (both loans have future EMI start dates of 01/07/2026, and no June/July 2026 pay run was created).

---

## Disbursement Date Business Rules

1. Must be in a non-completed pay period (future pay period)
2. Must be after the org's first pay period start date
3. The date is informational only — no payment is triggered in the system

---

## Cross-Module Impact

- EMI deduction appears as a salary deduction component in the pay run
- Perquisite (if non-zero rate, non-exempt) adds to taxable perquisite for TDS
- Loan closing date is auto-calculated = EMI Start Date + (N-1) months where N = number of instalments

---

## Open Questions

- [ ] Where does the EMI deduction appear in the payslip? (As a deduction line item? What label?)
- [ ] Is there a separate "Loan Disbursement" accounting entry or journal creation?
- [ ] Is the disbursement date used for any TDS or income tax calculation?
- [ ] Can the disbursement date be backdated to a historical pay period?
