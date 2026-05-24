# UF-57: Reprocess / Revision Pay Run

**Module:** Pay Runs > [Month] Pay Run > Revision Run
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 PAID pay run; theoretical scenario
**App State Before:** May 2026 pay run PAID (immutable)

---

## Revision Pay Run Overview

Once a pay run is finalized (PAID), it cannot be edited. If an error is discovered — wrong salary, missed variable input, incorrect LOP — the correction is made via a **Revision Pay Run**.

A revision pay run:
- Is a supplementary run for the SAME pay period
- Contains only the correcting differences (delta amounts)
- Generates revised payslips for affected employees
- Updates TDS liability records

---

## When to Use Revision Pay Run

| Scenario | Correction |
|----------|-----------|
| Employee paid wrong salary (too low) | Revision run: Pay difference |
| Wrong LOP days entered (too many) | Revision run: Pay back over-deducted amount |
| Variable input missed (bonus not paid) | Revision run: Pay the missed bonus |
| PF/ESI computed incorrectly | Revision run: Correct deductions |
| Wrong bank account (salary bounced) | Re-initiate bank transfer (not a revision) |

**Not a revision run scenario:**
- Wrong bank account: Correction outside payroll (bank recall + re-transfer)
- Employee exit mid-month: Use FnF pay run, not revision

---

## Creating a Revision Pay Run (Expected Flow)

### Entry Point
Option 1: Pay Runs > [May 2026 PAID run] > Header dropdown > "Create Revision Run"
Option 2: Pay Runs > "Create Pay Run" > Type: "Revision" > Select period: May 2026

### Step 1: Select Base Pay Run
Select the PAID pay run to revise: May 2026 Regular Pay Run

### Step 2: Select Employees to Revise
Only employees where correction is needed:
- Arjun Mehta (if his salary was wrong)

### Step 3: Enter Corrected Amounts
| Field | Original | Corrected | Difference |
|-------|----------|-----------|------------|
| Basic | ₹37,417 | ₹40,000 | +₹2,583 |
| (LOP reversed) | (LOP 2 days) | (LOP 0 days) | +₹2,583 |

### Step 4: System Computes Revised TDS
- Revised annual income → revised TDS
- Additional TDS or TDS refund computed
- Delta TDS included in revision run

### Step 5: Review and Finalize Revision Run
- Admin reviews the correction
- Marks revision run as paid
- Revised payslip generated

---

## Revised Payslip

A revision payslip shows:

```
REVISED PAYSLIP — May 2026
(Revision of Pay Run dated 29/05/2026)

Earnings Adjustment:
  Basic (LOP Reversal): +₹2,583

Deductions Adjustment:
  TDS (revised): −₹XXX

Net Revision Pay: ₹X
```

Or it may show the complete revised payslip (full earnings, not just delta).

---

## Impact of Revision on Statutory Records

### TDS Impact
- May 2026 TDS liability increases or decreases
- Form 24Q Q1: If revision is in April–June: Q1 return reflects revised TDS
- If revision after Q1 return filed: Revised Q1 return required

### PF Impact
- If PF was incorrectly computed: Revision run corrects PF for the month
- ECR for May needs to be re-generated and re-submitted to EPFO
- EPFO revision process: Submit revised ECR online via Unified Portal

### ESI Impact
- If ESI was wrongly deducted: Revision corrects ESI for the month
- ESIC challan may need to be revised

---

## Revision vs New Off-Cycle Run

| Dimension | Revision Run | New Off-Cycle Run |
|-----------|-------------|------------------|
| Purpose | Correct a prior pay run | Pay something new |
| Period | Same period as base run | New period |
| Payslip | Revised payslip for same period | New payslip |
| Statutory impact | Updates prior month records | New month records |
| Use case | LOP error, salary error | One-time bonus, missed employee |

---

## Business Rules
1. Revision run always references a specific PAID pay run
2. Only affected employees are included in revision run
3. Revision payslip supersedes original payslip (or is additive — need confirmation)
4. TDS liability records updated: Prior month's TDS may change
5. ECR for EPFO must be re-filed if PF amounts change

## Gaps / Observations
- Revision run not directly tested — no errors in May pay run to correct
- Whether revision payslip replaces or supplements original not confirmed
- EPFO ECR revision workflow via EPFO portal not explored
- "Delete Recorded Payment" as alternative reversal method not fully tested

## Open Questions
- [ ] Is there a "Revision Run" specific type in Zoho's create pay run flow?
- [ ] Can a revision run be created for a prior financial year's pay run?
- [ ] Does the revised payslip replace or supplement the original in the employee portal?
- [ ] If Q1 Form 24Q is already filed and then a revision is made, does Zoho flag the need for revised return?
