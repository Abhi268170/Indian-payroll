# UF-47: Mark Pay Run as Paid

**Module:** Pay Runs > [Month] Pay Run > Record Payment / Mark as Paid
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 pay run — already PAID (observed in this state)
**App State Before:** May 2026 = PAID; prior steps reconstructed from UI state

---

## Overview

"Mark as Paid" (or "Record Payment") is the final step in the pay run cycle. It:
1. Records that salary has been disbursed to employees
2. Generates payslips and makes them available to employees
3. Freezes pay run as immutable
4. Updates TDS liability records
5. Makes bank transfer file available

---

## Entry Point

After pay run is reviewed and approved:
Pay Run Summary page → "Record Payment" button (or "Mark as Paid")

**Button state:**
- Enabled: When pay run is in Approved / Ready-to-Pay state
- Disabled: When pay run is in Draft or already PAID

---

## Record Payment Flow (Expected)

### Step 1: Click "Record Payment"
Modal opens with payment details.

### Step 2: Payment Details Form

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Payment Date | Date | Yes | Actual date salaries were disbursed; defaults to pay schedule date |
| Payment Mode | Dropdown | Yes | Bank Transfer / Cheque / Cash |
| Remarks | Text | Optional | e.g., "NEFT batch May 2026" |

### Step 3: Confirm
- "Record Payment" button in modal
- System transitions pay run to PAID state

### Step 4: Post-Payment
- Payslips available in employee portal
- Bank Advice downloadable (UF-51)
- TDS liability table updated

---

## Payment Date vs Pay Schedule Date

The **Pay Schedule** (configured in Settings) defines the expected pay date (e.g., 29th of each month).

| Date | Description |
|------|-------------|
| Pay Schedule Date | Configured expected pay date (e.g., 29th May) |
| Actual Payment Date | Date admin records payment (may differ from schedule) |

**Observed:** May 2026 pay run — Payslip shows Pay Date: 29/05/2026. This matches the configured pay schedule, suggesting payment was recorded on schedule.

---

## Bank Transfer File

After "Mark as Paid":
- "Download Bank Advice" button activates (UF-51)
- Contains: Employee name, bank account, IFSC, amount
- Format: Typically Excel or CSV for bank upload
- Used to initiate NEFT/RTGS batch transfer from employer's bank

**Workflow:**
1. Admin marks pay run as paid in Zoho
2. Downloads bank advice file
3. Uploads to bank's corporate net banking portal
4. Bank processes NEFT/RTGS to individual employee accounts
5. Employees receive salary in bank account

**Note:** Zoho Payroll does NOT directly initiate bank transfers — it only generates the instruction file. Bank transfer is outside Zoho's scope.

---

## Undo Mark as Paid

From UF-46, the PAID state dropdown contains "Delete Recorded Payment":
- This appears to revert the payment recording
- Pay run returns to a pre-payment state
- Payslips may become unpublished (need confirmation)
- TDS liability records may revert

**Business risk:** If payslips have been downloaded or emailed, un-paying could cause confusion. Admin must communicate to employees.

---

## Payslip Availability Timeline

| Event | Timing |
|-------|--------|
| Admin marks as paid | T (payment recorded date) |
| Payslips visible in portal | T + near-immediate |
| Payslip email sent | T + configured delay or manual trigger |
| Payslip downloadable by employee | T + near-immediate |

---

## Business Rules
1. "Mark as Paid" is irreversible without "Delete Recorded Payment"
2. Payment date recorded is for audit; actual bank transfer is external
3. All employees in the pay run (Active status) get payslips — Skipped do not
4. Payslip PDF locks to the finalized pay data (not recalculated later)
5. TDS liability for the month locks at this point

## Gaps / Observations
- "Record Payment" modal not directly observed (May already PAID)
- "Delete Recorded Payment" exact behavior (payslip recall, TDS revert) not confirmed

## Open Questions
- [ ] Can payment date be backdated (e.g., pay run finalized on June 5 but payment date set to May 31)?
- [ ] What happens to payslips if "Delete Recorded Payment" is used after employees have downloaded them?
- [ ] Is there an auto-send email option when pay run is marked as paid?
- [ ] If payment is by cheque for some and bank transfer for others, can mixed payment modes be recorded per employee?
