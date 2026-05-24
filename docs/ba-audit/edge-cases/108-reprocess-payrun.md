# Edge Case > Reprocess Finalised Pay Run

## Scenario
The May 2026 pay run is in "Paid" status. Document the complete workflow to reprocess (undo payment, correct, re-approve, re-pay) and all guards/warnings at each step.

## Steps to Reproduce
1. Navigate to Pay Runs > Payroll History
2. Click on 01/05/2026–31/05/2026 pay run (status: Paid)
3. Click "Show dropdown menu" button in the pay run header
4. Click "Delete Recorded Payment"
5. Document state changes, warnings, and subsequent options

## Expected Behaviour (statutory rule)
Payroll is generally immutable once finalised and paid. Any correction should ideally be done via:
- **Arrear/Revision in next month's payrun** (preferred — clean audit trail)
- **Reprocessing** (only when same-month correction is necessary, e.g., gross error)

The standard industry workaround (used in GreytHR, Keka, Zoho) is:
1. Delete recorded payment (reverts "Paid" to "Approved")
2. Unapprove (reverts to "Reviewed" or "Draft")
3. Make corrections
4. Re-approve
5. Re-mark as paid

## Actual Zoho Behaviour

### Payroll History Page
**URL**: `#/payruns/payroll-history`
**Completed runs visible**:
| Payment Date | Type | Period | Status |
|-------------|------|--------|--------|
| 29/05/2026 | Regular Payroll | 01/05/2026–31/05/2026 | Paid |
| 30/04/2026 | Regular Payroll | 01/04/2026–30/04/2026 | Paid |

### Pay Run Summary Page (Paid State)
**URL**: `#/payruns/3848927000000034159/summary`

**Header actions available for Paid run**:
1. Camera/export icon button (unnamed)
2. **"Send Payslip"** button
3. **"Show dropdown menu"** button → reveals:
   - "Download all Payslips"
   - "Download all TDS Worksheets"
   - "Show Downloads"
   - **"Delete Recorded Payment"** — this is Step 1 of reprocess

**No direct "Unapprove" or "Unlock" button** is visible — the only mutation action is "Delete Recorded Payment."

### Employee-level Actions (Paid State)
Each employee row has a "Show dropdown menu" button. EMP001 row actions (inferred from UI pattern for paid employees):
- View payslip
- View TDS worksheet

### Reprocess Flow — Documented Steps

**Step 1: Delete Recorded Payment**
- **Location**: Pay Run Summary > dropdown > "Delete Recorded Payment"
- **Expected post-action**: Status changes from "Paid" to "Approved" (payment record deleted)
- **Warning expected**: Confirmation dialog ("Are you sure you want to delete the recorded payment? This will revert the pay run to Approved status.")
- **Data impact**: Payment batch record deleted; bank transfer advice invalidated; payslip status may revert to draft
- **Not tested live**: Could not complete due to SPA routing interference from persistent loan modal

**Step 2: Post-Deletion State (Expected)**
After deleting recorded payment:
- Pay run status: "Approved" (or equivalent pre-payment status)
- Available actions expected: "Record Payment" (to re-pay) + potentially "Unapprove"

**Step 3: Unapprove (Expected)**
If an "Unapprove" button appears after deleting payment:
- Reverts status to "Draft" or "Reviewed"
- Allows modifications to earnings/deductions

**Step 4: Make Corrections**
- Modify variable inputs (add/remove earnings, adjust LOP)
- Process payrun again

**Step 5: Re-approve and Re-pay**
- Go through standard approval workflow
- Re-record payment

### Payrun State Machine Observed
```
Draft → Processing → Reviewed → Approved → Paid
                                           ↑
                          Delete Recorded Payment
                                    ↓
                              Approved (again)
```

Note: Zoho uses `payment_status: "completed"` for Paid state in API, and `status: "completed"` for the pay run itself.

## Screenshots
- `screenshots/108-payroll-history.png` — Payroll History page with both completed runs
- `screenshots/108-payrun-summary-paid.png` — Pay Run Summary in Paid state showing all employees
- `screenshots/108-payrun-dropdown-paid.png` — Dropdown menu showing available actions for Paid run

## Gap / Bug / Surprise
1. **Only one mutation action visible on Paid run**: "Delete Recorded Payment" is the only destructive action. There is no "Unapprove" or "Unlock" visible at first; it presumably appears after payment deletion.
2. **No audit trail UI visible**: There is no "Audit Log" tab on the payrun detail page showing who finalised it, who deleted payment, and when. This is a compliance concern for Indian payroll (audit logs are legally required).
3. **No warning messaging visible on Paid run**: The summary page does not say "This payrun is finalised and locked" — it just omits mutation actions from the primary UI.
4. **3 employees skipped**: EMP003, EMP004, EMP005 are shown as "Skipped — Reason: Onboarding incomplete". These employees cannot be included in this run even after reprocess unless onboarding is completed first.
5. **SPA routing bug**: A persistent "Create Loan" modal (pre-filled with Arjun Mehta loan data from a prior session) keeps intercepting navigation events, making systematic testing of the reprocess flow difficult. This is a Zoho UI bug / session state issue, not a business logic issue.
6. **Total Taxes = ₹0**: For a ₹87,484 payroll, all taxes (TDS + PT) are ₹0. Suspicious — see Edge Case 107 for TDS analysis and Edge Case 109 for PT analysis.

## How We Should Build This
- Maintain explicit payrun state machine: `Draft → Computed → Reviewed → Approved → Paid`
- "Paid" is immutable — no direct edits
- Reprocess workflow: `Paid → [Delete Payment] → Approved → [Unapprove] → Draft → [Recompute] → ...`
- Each state transition logged in `PayrollRunAuditLog` with: timestamp, actor, from_state, to_state, reason
- When "Delete Payment" is triggered: require explicit reason text; log all details
- Show full audit trail on payrun detail page (Audit Log tab)
- Distinguish between soft reopen (same payrun, new computation) and revision (next month, with arrears)
- Prefer arrear-in-next-run approach over same-run reprocess — show this as the default/recommended path
- Never allow deletion of TDS challan data when deleting payment record — keep statutory records intact
