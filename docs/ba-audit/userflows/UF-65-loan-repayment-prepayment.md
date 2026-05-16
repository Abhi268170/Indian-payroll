# UF-65: Loan Repayment and Prepayment

**Module:** Loans > Loan Detail > Record Repayment
**Tested:** 2026-05-16
**Mock Data Used:** LOAN-00001 (Arjun, ₹50,000, 0 EMIs paid)
**App State Before:** Loans list page; LOAN-00001 detail panel observed

## Steps Executed
1. Observed LOAN-00001 detail panel (from UF-63)
2. Identified "Record Repayment" button in detail panel
3. Identified repayment schedule section (empty — no EMIs paid yet)
4. Documented expected repayment and prepayment flows

---

## Record Repayment Button

### Location
Loan detail panel → primary action button at bottom: "Record Repayment"

### Purpose
Allows admin to manually record a repayment outside of the regular payroll deduction cycle. Used when:
- Employee pays back a lump sum in cash/bank transfer
- Employer wants to record an ad-hoc payment that didn't come through payroll
- Adjusting loan balance after an error

---

## Record Repayment Form (Expected)

### Fields
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Repayment Date | Date | Yes | Date of payment |
| Amount | Currency (₹) | Yes | Amount repaid |
| Payment Mode | Dropdown | Yes | Cash / Bank Transfer / Salary Deduction |
| Remarks | Text | Optional | Note about this repayment |

### Actions
| Button | Behavior |
|--------|----------|
| Save | Records the repayment; reduces outstanding balance |
| Cancel | Discards, returns to loan detail |

---

## Repayment Schedule Section (Currently Empty)

### Table Headers (from UF-63)
| Column | Notes |
|--------|-------|
| Instalment Date | Date of each EMI |
| EMI | Amount of each instalment |
| Total Amount Repaid | Running total repaid |
| Remaining Amount | Outstanding after this EMI |
| Action | Likely: Edit / Delete this instalment record |

### Empty State Message
"The employee is yet to pay the first instalment through Zoho Payroll."

**First instalment:** 01/07/2026 → Not yet due as of 2026-05-16.

---

## Prepayment (Foreclosure)

**Prepayment** = paying back more than the EMI amount in a given month (reducing outstanding faster).

**Foreclosure** = paying back the full outstanding balance at once (closing the loan early).

### Prepayment via "Record Repayment"
1. Admin clicks "Record Repayment"
2. Enters amount > EMI (e.g., ₹20,000 instead of ₹5,000)
3. System reduces outstanding balance by ₹20,000
4. Remaining instalments recalculate:
   - Old: 10 EMIs of ₹5,000 = ₹50,000
   - After ₹20,000 prepayment: Outstanding = ₹30,000 → 6 EMIs of ₹5,000

**OR** the closing date remains fixed and the final instalment is a different amount.
(Exact recalculation behavior not confirmed — need to test.)

### Foreclosure via Record Repayment
1. Admin enters full outstanding balance (₹50,000 for LOAN-00001)
2. Loan status changes to "Closed"
3. No further EMI deductions in pay runs

---

## Regular EMI Deduction vs Manual Repayment

| Method | When Used | Effect |
|--------|-----------|--------|
| Payroll deduction (auto) | Standard monthly EMI via pay run | Deducted from net pay; appears on payslip |
| Manual Record Repayment | Cash payment outside payroll | Admin-entered; reduces balance; does NOT appear on payslip |

When both happen in same month (e.g., payroll deduction + manual cash payment):
- Outstanding balance reduces by both amounts
- Remaining schedule recalculates

---

## Repayment Impact on Perquisite

For loans WITH a non-zero perquisite rate:
- Perquisite is computed on OPENING BALANCE for the month
- When EMI is deducted: balance reduces for the NEXT month's perquisite
- Prepayment reduces future months' perquisite faster

For LOAN-00001 (0% perquisite, exempt): No perquisite regardless of repayment schedule.

---

## Business Rules
1. Outstanding balance = Loan Amount − Total Repaid
2. Monthly EMI deduction from payroll auto-reduces outstanding balance
3. Manual repayment can supplement or replace payroll deduction
4. Loan closes when outstanding balance = ₹0
5. Closed loans remain in system for audit — not deleted
6. FnF outstanding balance recovery closes the loan immediately

## Gaps / Observations
- "Record Repayment" form not filled/submitted (no actual repayment to record)
- Prepayment recalculation logic (remaining EMI count vs closing date) not confirmed
- 🟡 Test with first EMI in July 2026 pay run to see repayment schedule populate

## Open Questions
- [ ] After a prepayment, does the system reduce the number of remaining EMIs or keep the same number with smaller final EMI?
- [ ] Can admin delete a previously recorded repayment (to correct an error)?
- [ ] Is there a partial waiver / write-off functionality for loans (e.g., employer forgives part of the loan)?
- [ ] If loan is foreclosed mid-month, is the perquisite for that month prorated?
