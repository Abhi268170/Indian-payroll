# Pay Runs > Post-Approval State — Approved & Paid

## URL / Navigation Path

`https://payroll.zoho.in/#/payruns/{id}/summary`
States: Approved | Paid

Same URL for both states. State badge in header indicates current state.

## Purpose

Covers the two terminal states of the pay run lifecycle after approval:
1. **Approved** — payroll is locked, bank transfer preparation in progress, payment not yet recorded
2. **Paid** — payment has been recorded, run is complete

## Approved State

### Header & Status Badge

- Title: "Regular Payroll"
- Badge: "Approved" (green/blue badge)
- In prior audit sessions, also observed "Payment Due" badge — this appears to be an alternate label for the same state (possibly depends on pay day proximity).

### Page Kebab Options (Approved State)

1. **Reject Approval** — reverses approval, returns run to Draft
2. **Show Downloads** — opens downloads panel (download history)

### Reject Approval Dialog

| Field | Type | Required | Content |
|-------|------|----------|---------|
| Dialog title | Heading | — | "Reject Payroll?" |
| Reason | Textarea | No (optional) | Free text reason for rejection |
| Confirm button | Button | — | "Reject" |
| Cancel button | Button | — | Dismisses dialog |

After rejection: state → Draft, variable inputs re-editable, admin must re-approve.

### Record Payment Flow (Approved State)

Available from: button in info card (prominent "Record Payment" or "Mark as Paid" button) OR via page action.

**Record Payment Dialog fields:**

| Field | Type | Required | Options | Behaviour |
|-------|------|----------|---------|-----------|
| Payment Date | Date input | Yes | dd/MM/yyyy | Defaults to configured pay day |
| Payment Mode | Dropdown | Yes | Manual Bank Transfer, Direct Deposit, Cheque, Cash | Determines bank advice relevance |
| Reference Number | Text input | No | Free text | Optional bank transaction reference |
| Notes | Textarea | No | Free text | Internal notes |
| Confirm button | Button | — | "Record Payment" | Triggers state → Paid |
| Cancel button | Button | — | — | Dismisses dialog |

After recording payment: state → Paid (terminal), date/mode/reference stored.

## Paid State

### Header & Status Badge

- Title: "Regular Payroll"
- Badge: "Paid" (green badge)
- Back link: `#/payruns/payroll-history`

### Page Header Buttons (Paid State)

1. **Download button** (icon button, left of Send Payslip) — triggers "Download all Payslips" directly or opens download dialog
2. **Send Payslip** — bulk send payslips to all employees via email
3. **Show dropdown menu (kebab)** — dropdown with 4 options:
   - Download all Payslips
   - Download all TDS Worksheets
   - Show Downloads
   - Delete Recorded Payment

### Info Card Strip (Paid State)

Same fields as Draft/Approved with additions:

| Field | Type | Notes |
|-------|------|-------|
| Period | Read-only | "01/05/2026 - 31/05/2026" |
| Base Days | Read-only | "31 Base Days" |
| Month | Read-only | "May 2026" |
| Payroll Cost | Read-only ₹ | ₹87,484.00 |
| Total Net Pay | Read-only ₹ | ₹87,484.00 |
| Download Bank Advice | Button | Direct file download (no history entry) |
| Pay Day | Read-only | "29 May, 2026" |
| Employees | Read-only | "5 Employees" |
| Skipped | Button | "( 3 Skipped )" — opens skipped panel |
| Taxes, Benefits, Donations, Total Deductions | Read-only ₹ | All ₹0.00 in this org |

### Employee Table (Paid State) — Column Set

| Column | Notes |
|--------|-------|
| Checkbox (select all/per row) | Bulk select for download/send |
| Employee Name (EMP ID) | Clickable — opens read-only payslip panel |
| Paid Days | Finalised paid day count |
| Net Pay ₹ | Final net pay |
| Payslip (View) | Opens read-only payslip split panel |
| TDS Sheet (View) | Opens TDS Sheet PDF iframe modal |
| Payment Mode | e.g., "Manual Bank Transfer" |
| Payment Status | e.g., "Paid on 29/05/2026" (coloured badge) |
| Row kebab | "Download Payslip" \| "Send Payslip" (only 2 options in Paid state) |

**Skipped employees** remain visible in the table:
- No checkbox
- Name + "(Skipped)" label
- Reason spans in next cell
- No Paid Days / Net Pay / View buttons / Payment Mode / Status / Kebab

### Delete Recorded Payment Dialog (Paid State)

| Field | Type | Content |
|-------|------|---------|
| Dialog text | Paragraph | "You're about to delete the recorded payment for this pay run. Are you sure you want to proceed?" |
| Yes button | Button | Deletes recorded payment; state → Approved |
| No button | Button | Dismisses dialog; stays Paid |

After deleting recorded payment: state reverts to Approved. Admin can re-Record Payment (e.g., with corrected date or payment mode).

## Full State Machine

```
DRAFT
   ↓  [Approve Payroll confirmed]
APPROVED
   ├─ [Reject Approval + optional reason] → DRAFT
   ↓  [Record Payment confirmed]
PAID
   └─ [Delete Recorded Payment + confirm] → APPROVED
```

No state beyond Paid. PAID is the terminal state (no "Archive" or "Close" distinct state observed).

## Conditional Logic

- Variable inputs (LOP, earnings, TDS override) are locked in Approved and Paid states.
- Row kebab options change by state:
  - Draft: 6 options (View Payslip, View TDS Sheet, Skip, Undo Skip, Withold Salary, Revise Salary)
  - Approved: observed only "Reject Approval" at page level; per-row kebab may have fewer options
  - Paid: 2 options per row (Download Payslip, Send Payslip)
- "Download all Payslips" downloads all non-skipped employee payslips as individual PDFs or a ZIP.
- "Download all TDS Worksheets" downloads all TDS worksheets as PDFs.
- "Show Downloads" opens a downloads panel (separate URL query param: `?canShowDownloadHistory=true`) — showed "no files to download" in test (Bank Advice downloads directly without history entry).

## Cross-Module Links

- Bank Advice download → used for initiating manual bank transfers
- Payroll History (`#/payruns/payroll-history`) → Paid runs appear here
- Form 16 module → consumes finalised payroll data for annual TDS certificate
- Reports module → payroll summary, bank transfer report fed by Paid run data

## Key Observations for Our Build

1. **Paid is not truly immutable — Delete Recorded Payment exists** — Zoho provides a reversal mechanism. Our build must support `DELETE /api/payroll-runs/{id}/payment` → status back to Approved. This allows correcting payment recording errors.
2. **No "Revise Payroll" from Paid state** — to re-run calculations, admin must Delete Recorded Payment → Reject Approval → re-enter Draft → modify → re-Approve → re-Record Payment. This is cumbersome. Our build could provide a "Revise and Reprocess" shortcut.
3. **Skipped employees stay visible in Paid state** — important for audit trail. Our employee table must always show skipped employees with their reason, even after payment.
4. **Payment Mode options**: Manual Bank Transfer, Direct Deposit (NEFT/RTGS/IMPS automation), Cheque, Cash. Our build must support all four. Direct Deposit implies integration with a payment gateway or bank API (scope for later phase).
5. **Bank Advice is a direct download** — no history entry. Our build should store Bank Advice in MinIO with a record in the `PayrollRunDownload` table so admins can re-download without regenerating.
6. **"Payment Due" vs "Approved" badge discrepancy** — April run showed "Payment Due", May run showed "Approved" for the same logical state. May indicate: "Payment Due" appears when pay day has passed but payment not recorded; "Approved" when pay day is in the future. Track pay day vs current date for badge logic.
7. **Payment date defaults to configured pay day** — good UX. Our Record Payment dialog should default `payment_date` to the configured pay day from the pay schedule.

## Screenshots

- `screenshots/60-approved-kebab-menu.png` — Page kebab in Approved: Reject Approval | Show Downloads
- `screenshots/60-reject-approval-dialog.png` — "Reject Payroll?" dialog with optional reason
- `screenshots/60-record-payment-dialog.png` — Record Payment dialog
- `screenshots/60-paid-state-summary.png` — Summary page in Paid state
- `screenshots/60-paid-page-kebab.png` — Page kebab in Paid (4 options)
- `screenshots/60-paid-row-kebab.png` — Per-row kebab in Paid: Download Payslip | Send Payslip
- `screenshots/65-paid-kebab-menu.png` — Page kebab open showing all 4 options
- `screenshots/65-delete-recorded-payment-dialog.png` — Delete Recorded Payment confirmation dialog

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
