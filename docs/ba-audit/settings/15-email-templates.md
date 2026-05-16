# Settings > Email Templates

## URL
`#/settings/email-templates`

Template editor URL pattern: `#/settings/email-templates/edit?type={template_type}`

## Purpose
Manage email notification templates sent to employees for payslip issuance events. Templates are pre-built with HTML formatting and merge fields (placeholders). Each template can be customised — subject line and body.

## Page Layout
Simple two-column table. Header row: Template Type | More Actions. Clicking a template name or "Edit" link opens the template editor.

Quick link in header: "Configure Sender Email Preferences" → navigates to `#/settings/email-preference`.

---

## Template List

| # | Template Type | Type Code | Description |
|---|--------------|-----------|-------------|
| 1 | Payslip Notification | `payslip_notification` | Sent when employees are paid in a regular payroll run |
| 2 | Payslip Notification (For Portal Disabled Employees Only) | `payslip_notification_portal_disabled` | Alternate notification for employees without portal access |
| 3 | Off Cycle & One-Time Payrolls Payslip Notification | `special_payroll_payslip_notification` | Sent when employees are paid via off-cycle or one-time payrolls |
| 4 | Full & Final Settlement Payslip Notification | `final_settlement_payslip_notification` | Sent when an employee's final settlement is processed |

## Actions
| Action | Element | Behavior |
|--------|---------|----------|
| Edit | Link per row | Navigates to template editor for that template type |
| Configure Sender Email Preferences | Header link | Navigates to `#/settings/email-preference` |

---

## Template Editor (`/edit?type={type}`)

### Editor Page Title
"Edit template for {Template Name}"

### Header Notice
> "Emails will be sent from primary contact by default. You can change the Sender address by changing the primary contact in Settings > Preferences > Sender Email Preferences."

### Fields

| Field | Type | Notes |
|-------|------|-------|
| Subject | Text input | Pre-filled with default subject. Supports placeholder variables (e.g., `%PayPeriodMonth%`) |
| Body | Rich text editor (WYSIWYG, iframe) | Full HTML email body with pre-built layout |

### Rich Text Editor Toolbar
| Tool | Type |
|------|------|
| Headings | Dropdown (heading level selector) |
| Bold | Button |
| Italic | Button |
| Underline | Button |
| Strikethrough | Button |
| Font Family | Dropdown (default: Arial) |
| Font Size | Dropdown |
| Indent | Button |
| Outdent | Button |
| Create Link | Button |
| Insert Image | Button |
| Insert HTML | Button |
| Insert Placeholders | Button (opens placeholder picker dropdown) |

### Insert Placeholders — Available Placeholders

**EMPLOYEE group:**
| Placeholder Label | Variable |
|------------------|---------|
| Name | `%EmployeeName%` |
| Pay period | `%PayPeriodMonth%` |
| Pay date | (mapped in template) |
| Payroll Type | (mapped in template) |
| Portal Web Link | `%PaySlipURL%` |

**ORGANIZATION group:**
| Placeholder Label | Variable |
|------------------|---------|
| Name | `%CompanyName%` |
| User | `%PrimaryContactName%` |
| Email | (org email) |
| Portal App Store Link | (iOS link) |
| Portal Play Store Link | (Android link) |

### Default Template Content (Payslip Notification)

**Default Subject:** `Payslip for the month of %PayPeriodMonth%`

**Default Body placeholders used:**
- `%CompanyName%` — company name in header
- `%EmployeeName%` — employee greeting
- `%PayPeriodMonth%` — bold pay period reference
- `%PaySlipURL%` — "View Payslip" CTA button URL
- `%PrimaryContactName%` — sign-off name

**Email Structure:**
1. Header: Company name on teal border card
2. Body: "Hi {EmployeeName}," greeting + pay period message
3. CTA: "View Payslip" button (blue, links to portal)
4. Footer: "Cheers, {PrimaryContactName} | {CompanyName}"
5. Disclaimer: "Note: This is a system generated mail. Please do not reply."

### Buttons
| Button | Action |
|--------|--------|
| Save | Saves customised subject + body |
| Cancel | Returns to Email Templates list without saving |
| Back (← arrow) | Returns to Email Templates list |

---

## Business Rules

1. **4 fixed templates** — no ability to add or delete templates; only edit existing ones.
2. **Template scope is org-wide** — all employees in the org receive the same template content.
3. **Portal Disabled template** — separate template for employees who cannot access the ESS portal (payslip must be emailed directly as there is no portal link).
4. **Sender address** — configured separately in Sender Email Preferences; cannot be changed per template.
5. **Placeholder substitution** — variables in `%VariableName%` format are replaced at send time with real values.

## Cross-Module Impact

| Template | Triggered By |
|----------|-------------|
| Payslip Notification | Payroll Run → Pay action (regular payroll) |
| Portal Disabled Notification | Same trigger, but only for employees with portal disabled |
| Off Cycle Notification | Off Cycle / One-Time Payroll → Pay action |
| F&F Settlement Notification | Full & Final Settlement processing |

## Observations & Notes

1. **No "Send Test Email" button** visible in editor — no way to preview the rendered email before saving.
2. **No custom templates** — fixed 4 types. Cannot add event-based templates (e.g., salary revision notification, loan approval).
3. **Portal Disabled variant** is important — distinguishes employees with ESS portal access from those without; different UX for email-only payslip delivery.
4. **HTML template in iframe** — rich WYSIWYG editor renders the email preview live.
5. For our build: Email notifications need 4 similar template types with placeholder substitution. Template storage: HTML body + subject string per template type per tenant. Sender configuration is separate.

## Screenshots
- `docs/ba-audit/settings/screenshots/15-email-templates.png`
- `docs/ba-audit/settings/screenshots/15-email-template-editor.png`
