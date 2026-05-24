# Pay Runs > Payslip Email Distribution

## URL / Navigation Path

`https://payroll.zoho.in/#/payruns/{id}/summary` (Paid state)

Trigger points:
- Page header: "Send Payslip" button (bulk — all non-skipped employees)
- Per-row kebab: "Send Payslip" (individual employee)

## Purpose

Distributes payslips to employees via email. Both bulk (all employees) and individual (per employee) sending are supported. The action is immediate — no scheduling or deferred delivery observed.

## Send Payslip — Bulk (Page-Level Button)

### Trigger

"Send Payslip" button in page header (right of the download icon button, left of the kebab).
Available in: Paid state. (Not observed in Draft or Approved state.)

### Behaviour on Click

A small informational dialog appears:

> "Payslips will be emailed to all employees of this pay run."

- No confirmation required (no "Are you sure?" prompt — the dialog IS the confirmation)
- A button: "Send" (or "OK" — exact label not captured in accessibility tree, but confirmed from screenshot)
- A button: "Cancel"

On Send:
- API call triggered immediately
- All non-skipped employees receive payslip via email
- Skipped employees are excluded automatically
- No success/failure per-employee feedback observed (may be a background job)

### Email Content (inferred from Zoho convention)

- Subject: "Your payslip for [Month Year] — [Company Name]"
- Body: Brief message + attached payslip PDF
- Attachment: Payslip PDF (password-protected if configured in Settings)
- Sender: Configured in Settings (Zoho-managed email or custom SMTP)

## Send Payslip — Individual (Per-Row Kebab)

### Trigger

Row kebab (three-dot) on employee row > "Send Payslip"
Available in: Paid state only. (In Draft/Approved, row kebab does not show "Send Payslip.")

### Behaviour

Similar to bulk send but scoped to one employee. No separate dialog captured — may be an immediate action with a toast confirmation, or the same informational dialog scoped to that employee.

## Fields

| Field | Type | Notes |
|-------|------|-------|
| No input fields | — | Send action has no configurable fields in the UI |
| Email recipients | System-computed | Employee email from employee profile |
| Password protection | System config | Set in Settings > Payslip distribution preferences |

## Buttons & Actions

| Action | State | Trigger | Pre-condition | Post-behaviour |
|--------|-------|---------|---------------|----------------|
| Send Payslip (bulk) | Paid | Page header button | At least 1 non-skipped employee | Informational dialog → Send triggers email |
| Send Payslip (individual) | Paid | Row kebab > Send Payslip | Employee has email on profile | Sends payslip to that employee |
| Cancel (dialog) | Paid | Dialog Cancel | — | Dismisses dialog; no emails sent |

## Conditional Logic

- "Send Payslip" page button is only visible in **Paid state**. Not available in Draft or Approved.
- Per-row "Send Payslip" is available in Paid state in the row kebab.
- Skipped employees are automatically excluded from bulk send.
- If an employee has no email on profile, the behaviour is unknown (not tested) — may silently skip or show an error.

## Cross-Module Links

- Employee profile → email address used for sending
- Settings > Email configuration → SMTP settings, sender name/address
- Settings > Payslip preferences → password protection settings

## Key Observations for Our Build

1. **No confirmation dialog for bulk send** — the informational dialog ("Payslips will be emailed to all employees") serves as both notice and confirmation. This is acceptable UX for payslip sending (non-destructive action). Replicate this pattern.
2. **No scheduling** — Zoho sends immediately on click. Our build should support both immediate send AND scheduled delivery (e.g., "Send on pay day at 9 AM"). Scheduled delivery is a differentiating feature.
3. **No per-employee email preview** — admin cannot preview what the email looks like before sending. Our build should offer: "Preview email" for a sample employee before bulk send.
4. **Email from MailHog in dev** — our dev environment uses MailHog for email capture (docker-compose). All payslip emails in dev/staging should route to MailHog. Never send real emails in non-prod environments.
5. **Password protection config** — Zoho lets admin configure payslip password protection at the org level. Our build should support this via Settings. Password convention: DOB in DDMMYYYY format is standard for Indian payslips.
6. **Bulk send as background job** — for large orgs (500+ employees), bulk payslip generation + email would be slow if synchronous. Use Hangfire background job: `SendPayslipJob` that generates PDFs in parallel (bounded concurrency), stores in MinIO, then sends emails via SMTP. Return a job ID immediately; show progress in Downloads panel.
7. **Email audit log** — log each sent email: `EmployeeId`, `RunId`, `SentAt`, `SentTo (email)`, `Status (Sent/Failed)`. Required for compliance and re-send capability.

## Screenshots

- `screenshots/63-send-payslip-dialog.png` — "Send Payslip" informational dialog

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
