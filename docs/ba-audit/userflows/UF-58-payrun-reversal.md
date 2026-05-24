# UF-58: Pay Run Reversal

**Module:** Pay Runs > [Month] Pay Run > Reversal / Delete Recorded Payment
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 PAID pay run; "Delete Recorded Payment" option confirmed in header dropdown
**App State Before:** May 2026 pay run PAID

---

## Pay Run Reversal Overview

A pay run reversal is distinct from a revision:
- **Revision** = Correct specific amounts in a finalized run (forward correction)
- **Reversal** = Undo the finalization of a pay run (revert to editable state)

---

## "Delete Recorded Payment" — Observed Option

From UF-50 (pay run header dropdown in PAID state):
| Action | Description |
|--------|-------------|
| Delete Recorded Payment | Appears to revert PAID state |

This is the only reversal mechanism observed in Zoho Payroll.

---

## What "Delete Recorded Payment" Does (Expected Behavior)

### Likely Behavior:
1. Pay run reverts from PAID → Approved (or Draft)
2. Payment date is cleared
3. Payslips are "unpublished" from employee portal
4. Bank advice file generation may be disabled
5. TDS liability record for the month reverted

### What It Does NOT Do:
- Does NOT delete the pay run itself (data preserved)
- Does NOT reverse actual bank transfers (those are outside Zoho's scope)
- Does NOT revert EPF ECR once submitted to EPFO

---

## Full Reversal Scenario

**Scenario:** May 2026 pay run marked as PAID, but admin discovers a critical error affecting all employees (e.g., wrong pay schedule date, all LOP days wrong).

**Steps:**
1. Navigate to May 2026 PAID pay run
2. Click header dropdown → "Delete Recorded Payment"
3. Confirm deletion warning
4. Pay run reverts to editable state
5. Admin corrects the errors
6. Re-processes and re-finalizes the pay run
7. "Record Payment" again with correct payment date

---

## Risk Analysis — Reversal After Employee Download

| Risk | Impact |
|------|--------|
| Employees already downloaded payslip | Outdated payslip in employee's possession |
| Salary already credited to bank | Bank transfer cannot be recalled via Zoho |
| TDS liability record reverted | Form 24Q data temporarily incorrect |
| PF challan already paid | Cannot be recalled; must adjust in next month's ECR |
| Statutory compliance gap | If reversal takes multiple days: compliance timing risk |

**Critical note:** If salary has been physically transferred to employee bank accounts, reversal in Zoho does NOT reverse the bank transfer. Admin must separately handle bank-side recall (complex, bank-dependent process).

---

## Reversal vs Cancellation

| Action | Effect |
|--------|--------|
| Delete Recorded Payment | Reverts to editable state; data preserved |
| Cancel Pay Run | Cancels the entire run; may delete data |

Whether Zoho has a "Cancel Pay Run" option distinct from "Delete Recorded Payment" is not confirmed.

---

## ESI/PF Reversal Implications

### If PF Challan Already Paid (EPFO)
- EPFO challan cannot be reversed
- Excess PF paid will be adjusted in next month's ECR
- ECR revision via EPFO Unified Portal may be needed

### If ESI Challan Already Paid (ESIC)
- Similar to PF — ESIC challan adjustments in subsequent period
- Contact ESIC regional office for large errors

### If TDS Already Deposited (ITNS 281)
- TDS challan paid to Income Tax cannot be reversed
- Excess TDS: Refund claim filed by employer via Form 26B or adjusted in next challan

---

## Business Rules
1. "Delete Recorded Payment" reverts PAID state — use only before bank transfer initiated
2. If bank transfer already done: use Revision Run to correct (do NOT un-pay)
3. Reversal window: Best done same day as finalization (before external actions)
4. Statutory challans (PF/ESI/TDS) paid externally cannot be recalled via Zoho
5. After reversal: Re-process run, re-finalize, re-record payment

## Gaps / Observations
- "Delete Recorded Payment" exact behavior not tested — did not click (risk of disrupting demo org)
- Whether "Cancel Pay Run" exists as a separate option not confirmed
- Post-reversal state (Approved vs Draft vs editable) not confirmed

## Open Questions
- [ ] After "Delete Recorded Payment," does the run go to Draft or Approved state?
- [ ] Are payslips automatically unpublished from employee portal after reversal?
- [ ] Is there a confirmation dialog warning about irreversible bank transfers?
- [ ] Can admin partially reverse a pay run (revert only for specific employees)?
