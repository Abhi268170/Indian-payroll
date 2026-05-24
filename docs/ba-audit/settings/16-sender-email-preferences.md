# Settings > Sender Email Preferences

## URL
`#/settings/email-preference`

## Purpose
Configures which email address appears in the "From" field when payroll-related emails (payslips, notifications) are sent to employees. Supports multiple sender addresses with verification. Public domain senders (Gmail, Yahoo, etc.) are automatically overridden by Zoho Payroll's own mailer to avoid spam classification.

## Page Layout
Single page. Header has "Add Sender" button. Body has:
1. Public Domains notice panel
2. Sender table (Name | Email Address | Status | Actions)

---

## Public Domains Notice

**Notice text:**
> "If you send emails from public domain, they are likely to land in the Spam folder. So, if emails are sent with any of the following email addresses in the From field, emails will be sent from message-service@mail.zohopayroll.in. If you still want to send emails using the public domain, Change Setting"

**Label:** "Public Domains" with info icon

**Default sender (override for public domains):** `message-service@mail.zohopayroll.in`

**"Change Setting" link** — allows opting out of the automatic override (accepting spam risk)

---

## Sender Table

### Columns
| Column | Description |
|--------|-------------|
| Name | Display name of the sender |
| Email Address | From address used in emails |
| Status | Verified / Unverified |
| Actions | Edit (pencil), Delete (trash) |

### Current Senders

| Name | Email | Status | Primary |
|------|-------|--------|---------|
| abhijithss2255 | abhijithss2255@gmail.com | Unverified | Yes (PRIMARY badge) |

**Note:** gmail.com is a public domain — this sender would be overridden by `message-service@mail.zohopayroll.in` unless Change Setting is applied.

---

## Add Sender Modal

Triggered by "Add Sender" button.

### Modal Fields

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Name | Text | Yes* | Non-empty | Display name for the sender |
| Email Address | Text/Email | Yes* | Valid email format | From address; must be verified |

### Modal Buttons
| Button | Action |
|--------|--------|
| Save | Adds the sender; triggers verification email to the address |
| Cancel | Closes modal without adding |

*Mandatory fields marked with asterisk in modal footer: "* indicates mandatory fields"

---

## Verification Flow
- After adding a sender email, status = "Unverified"
- Zoho sends a verification email to that address
- Action button shown: "(Resend Email)" — resends verification if not received
- Once verified, status changes to "Verified"
- Only verified senders can be set as Primary

---

## Row Actions
| Action | Icon | Behavior |
|--------|------|----------|
| Edit | Pencil icon | Opens edit modal (Name and Email editable) |
| Delete | Trash icon | Removes sender; cannot delete Primary sender |

---

## Business Rules
1. **Primary sender** — one sender must be designated Primary; this is the default From address for all payroll emails.
2. **Public domain override** — Gmail, Yahoo, Outlook, etc. are overridden to `message-service@mail.zohopayroll.in` to prevent spam. Opt-out via "Change Setting".
3. **Verification required** — email address must be verified before it can be used as From address.
4. **Can't delete Primary** — must designate another sender as Primary first.
5. **Per-template sender** — sender is global (not per-template); same From address used for all 4 email templates.

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| Primary Sender | All payroll notification emails use this as the From address |
| Unverified sender | Emails fall back to Zoho default `message-service@mail.zohopayroll.in` |
| Public domain override | Automatically applies even if sender is verified |

## Observations & Notes
1. **SPF/DKIM alignment** — Public domain override is Zoho's workaround for SPF/DKIM failures when From address is a public email. Custom domain senders (e.g., company@lerno.com) would not be overridden.
2. **Current state is misconfigured** — the primary sender (gmail) is unverified and is a public domain, so all emails currently go from Zoho's own mailer address.
3. For our build: Tenant-level sender email configuration with SMTP or SES integration. Custom domain verification via SPF/DKIM records. Fallback to system sender if none verified.

## Screenshots
`docs/ba-audit/settings/screenshots/16-sender-email-preferences.png`
