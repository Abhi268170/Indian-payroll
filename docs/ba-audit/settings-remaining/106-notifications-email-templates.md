# Item 106: Notifications & Email Templates

**URL:** `https://payroll.zoho.in/#/settings/email-templates`
**Module:** Settings > Customisations > Email Templates
**Related:** Sender Email Preferences (`#/settings/email-preference`)
**Audit Date:** 2026-05-15

---

## Screenshots

- `screenshots/106-email-templates-list.png` — Email Templates list (4 templates)
- `screenshots/106-email-template-editor.png` — Template editor (Payslip Notification)
- `screenshots/106-sender-email-preferences.png` — Sender Email Preferences

---

## Email Templates — Landing Page

**URL:** `#/settings/email-templates`

### Template List

| # | Template Type | Query Param `type` | Trigger Description |
|---|---------------|---------------------|---------------------|
| 1 | Payslip Notification | `payslip_notification` | Sent when employees are paid (regular pay run) |
| 2 | Payslip Notification (For Portal Disabled Employees Only) | `payslip_notification_portal_disabled` | Sent to employees without portal access — includes payslip as attachment or download link |
| 3 | Off Cycle & One-Time Payrolls Payslip Notification | `special_payroll_payslip_notification` | Sent when Off Cycle or One-Time Payroll payment is made |
| 4 | Full & Final Settlement Payslip Notification | `final_settlement_payslip_notification` | Sent when F&F Settlement is processed |

### Actions

| Action | Description |
|--------|-------------|
| Template Name link | Opens editor at `#/settings/email-templates/edit?type={type}` |
| More Actions dropdown | Per-template: Edit (inferred) |
| Configure Sender Email Preferences | Button/link → navigates to `#/settings/email-preference` |

---

## Template Editor

**URL:** `#/settings/email-templates/edit?type=payslip_notification`

### Editor Fields

| Field | Type | Default Value | Notes |
|-------|------|---------------|-------|
| Subject | Text input | `Payslip for the month of %PayPeriodMonth%` | Supports placeholder variables using `%PlaceholderName%` syntax |
| Body | Rich text editor (WYSIWYG) | (default template body) | Toolbar with Headings dropdown + font selector (Arial) |

### Toolbar Controls

| Control | Type | Options |
|---------|------|---------|
| Headings | Dropdown | Heading 1–6, Normal text |
| Font family | Dropdown | Arial (default, others available) |
| Insert Placeholders | Button | Opens placeholder picker — injects `%PlaceholderName%` at cursor |

### Placeholder System

**Syntax:** `%PlaceholderName%` — used in Subject and Body fields.

**Known placeholders (inferred from default subject):**
- `%PayPeriodMonth%` — the pay period month name (e.g., "April 2026")

**Business rule:** Placeholders are substituted at email send time with employee-specific or payroll-specific data. Admin cannot break the system by using invalid placeholders — they will render as-is or be empty.

### Sender Info Banner

> "Emails will be sent from primary contact by default. You can change the Sender address by changing the primary contact in Settings > Preferences > Sender Email Preferences."

### Actions

| Action | Behavior |
|--------|----------|
| Save | Saves template with current subject + body |
| Cancel | Discards changes, returns to template list |

---

## Sender Email Preferences

**URL:** `#/settings/email-preference`

### Current State (lerno org)

| Field | Value |
|-------|-------|
| Name | abhijithss2255 |
| Email | abhijithss2255@gmail.com |
| Role | PRIMARY |
| Verification Status | Unverified (Resend Email) |

### Public Domain Override

**Warning displayed:**
> "If you send emails from public domain, they are likely to land in the Spam folder. So, if emails are sent with any of the following email addresses in the From field, emails will be sent from message-service@mail.zohopayroll.in. If you still want to send emails using the public domain, Change Setting"

**Override behavior:**
- gmail.com, yahoo.com, outlook.com, and similar public domains → emails sent FROM `message-service@mail.zohopayroll.in`
- Business/custom domains (e.g., company.com) → emails sent from the configured sender address
- "Change Setting" link: opt-out of override (accept deliverability risk)

### "EMAILS ARE SENT THROUGH" block

- Label: "Email address of Zoho Payroll"
- Value: `message-service@mail.zohopayroll.in`

### Actions

| Action | Description |
|--------|-------------|
| Add Sender | Adds a new sender email address |
| Unverified (Resend Email) | Per-sender: resends verification email |
| Change Setting | Opts out of public domain override |

### Sender Verification

**Business rule:** Sender email must be verified before emails are sent from that address. Until verified, Zoho overrides with `message-service@mail.zohopayroll.in` regardless of domain type.

---

## Business Rules

1. **4 email templates — all trigger-based:** templates are auto-sent on specific payroll events; no manual send from here.
2. **Portal vs non-portal split:** "Payslip Notification (For Portal Disabled Employees Only)" ensures every employee receives their payslip — portal users get a link; non-portal users get a direct download link or attachment.
3. **Off Cycle + One-Time share one template:** the same template `special_payroll_payslip_notification` applies to both Off Cycle Pay Runs and One-Time Payouts.
4. **Subject line supports placeholders:** `%PayPeriodMonth%` and other dynamic variables; system substitutes at send time.
5. **Sender email must be verified:** unverified sender = Zoho's own email used.
6. **Public domain auto-override:** Gmail/Yahoo senders are silently switched to Zoho's `message-service@mail.zohopayroll.in` to avoid spam folders.
7. **No HTML template visible in innerText** — WYSIWYG editor body is not inspectable via DOM text extraction; assumed to be an iframe or contenteditable with default HTML content.

---

## Cross-Module Impact

- Email Templates → Pay Runs: `payslip_notification` fires on "Record Payment" for regular pay runs
- Email Templates → Pay Runs (Off Cycle): `special_payroll_payslip_notification` fires on Off Cycle payment
- Email Templates → Exit/F&F: `final_settlement_payslip_notification` fires when F&F settlement is recorded
- Email Templates → Sender Prefs: sender address shown in `From:` field of all outgoing emails
- Sender Prefs → RBAC: "Manage Users" permission covers sender management (who can add/remove senders)

---

## Open Questions

- [ ] What placeholders are available in the body — full placeholder list not captured (Insert Placeholders dropdown not opened)?
- [ ] Does the Payslip Notification (Portal Disabled) send the payslip PDF as an attachment or a one-time download link?
- [ ] Can custom email templates be created (beyond the 4 system templates)?
- [ ] Are there email templates for other events (IT Declaration deadline, POI submission reminder, salary revision approval)?
- [ ] What is the "Configure Failure Preferences" option visible in Automation > Actions — does it relate to email delivery failures?
- [ ] MailHog: in our self-hosted build, do these email triggers work with MailHog SMTP? (All outgoing SMTP should route through MailHog in dev.)
