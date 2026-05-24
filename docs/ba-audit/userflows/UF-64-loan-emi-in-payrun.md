# UF-64: Loan EMI in Pay Run

**Module:** Pay Runs > Employee Summary > Deductions > Loan EMI
**Tested:** 2026-05-16
**Mock Data Used:** LOAN-00001 (Arjun, ₹50,000, first EMI due 01/07/2026)
**App State Before:** May 2026 pay run PAID; Arjun's first loan EMI not yet due

## Steps Executed
1. Observed May 2026 pay run summary — no loan EMI visible (first EMI: 01/07/2026)
2. Observed Taxes & Deductions tab: Benefits = ₹0.00 ("There are no deductions present")
3. Documented expected loan EMI flow for June/July 2026

---

## Loan EMI Deduction Timing

### LOAN-00001 (Arjun Mehta)
| Field | Value |
|-------|-------|
| Loan Amount | ₹50,000 |
| Instalment Amount | ₹5,000/month |
| Disbursement Date | 01/06/2026 |
| First Instalment Date | 01/07/2026 |
| Closing Date | 30/04/2027 |
| Instalments | 10 |
| Status | Open |

**EMI deduction begins:** July 2026 pay run (first pay run with pay day on or after 01/07/2026).

In May 2026 pay run (pay day 29/05/2026): EMI = ₹0 (correct — disbursement date is 01/06/2026, not yet disbursed).

---

## How Loan EMI Appears in Pay Run

### In Pay Run Summary (Taxes & Deductions Tab)
When EMI is active, it appears under "Benefits" section (counter-intuitive naming — "Benefits" in Zoho's terminology includes all employer-employee non-statutory deductions):

**Expected Taxes & Deductions tab (July 2026 — with EMI):**
| Tax Name | Paid By Employer | Paid By Employee |
|----------|-----------------|-----------------|
| Income Tax | ₹1,582.00 | ₹1,582.00 |
| KL Professional Tax (Head Office) | ₹0.00 | ₹750.00 |

**Benefits section:**
| Benefit Name | Employer's Contribution | Employee's Contribution |
|-------------|------------------------|------------------------|
| Personal Loan (LOAN-00001) | ₹0.00 | ₹5,000.00 |

### In Employee Payslip
The EMI appears as a deduction line:
```
(-) Deductions:
  Taxes:
    Income Tax: ₹1,582
    KL Professional Tax: ₹750 (September month)
  Benefits:
    Personal Loan (EMI): ₹5,000
Total Deductions: ₹7,332
Net Pay: ₹70,000 - ₹7,332 = ₹62,668
```
(Note: PT deduction in September only per Kerala half-yearly cycle)

---

## Loan EMI Auto-Deduction Rules

1. System checks each employee's active loans before processing each pay run
2. If pay run pay date ≥ loan's first instalment date → EMI is auto-deducted
3. EMI amount = configured instalment amount (₹5,000 for LOAN-00001)
4. Principal only deducted (no interest on 0% perquisite loans)
5. Outstanding balance reduces by instalment amount each month

### Paused Loans
If "Pause Instalment Deduction" is activated on LOAN-00001:
- July 2026 pay run: EMI = ₹0 (paused)
- Outstanding balance remains ₹50,000
- Remaining instalments do NOT increase — missed instalment adds to end or creates a balloon
- (Exact behavior of paused loans not confirmed — need to test)

### Loan Closure
When outstanding balance reaches ₹0:
- Final pay run deducts the last instalment
- Loan status changes from "Open" to "Closed"
- No further deductions in subsequent pay runs

---

## LOAN-00002 (Vikram Nair) — EMI Problem

Vikram Nair's loan (₹1,00,000 Emergency Loan) cannot deduct EMI because:
- Vikram is SKIPPED from pay runs (Onboarding incomplete)
- No pay run means no deduction
- Loan outstanding remains at ₹1,00,000 indefinitely

**This is a compliance/financial risk:** The employer has disbursed ₹1,00,000 but cannot recover it through payroll deduction until Vikram's onboarding is complete.

---

## Taxability of Loan EMI

The loan EMI deduction itself is NOT taxable income and NOT a deduction from taxable income:
- EMI is a principal repayment — not salary, not tax-deductible
- The perquisite value (if applicable) IS added to taxable income — see UF-66

**For LOAN-00001 (0% perquisite, exempt):**
- EMI of ₹5,000 deducted from net pay
- No perquisite added to taxable income
- TDS base unchanged by EMI

---

## Business Rules
1. EMI auto-deducts based on loan's first instalment date vs pay run pay day
2. EMI appears under "Benefits" category in Taxes & Deductions tab (Zoho terminology)
3. Loan EMI is NOT taxable income — it is principal recovery
4. If employee salary < EMI amount: system behavior undefined (likely deducts full available salary?)
5. FnF settlement deducts full outstanding balance (not just one EMI)

## Gaps / Observations
- July 2026 pay run (first EMI month) not yet accessible — date-gated
- Paused loan behavior not tested
- What happens when net pay < EMI amount: not tested
- 🟡 Mark for future session: run July 2026 pay run to test EMI deduction

## Open Questions
- [ ] If net pay in a month is less than the EMI amount, does the system: (a) deduct partial EMI, (b) deduct full EMI creating a negative net, or (c) skip EMI for that month?
- [ ] Can admin override the EMI amount for a specific month?
- [ ] If a loan is paused for 2 months, do the missed 2 EMIs get added to the end (loan extended) or does the final instalment increase?
- [ ] When the last EMI is deducted, does the system automatically close the loan?
