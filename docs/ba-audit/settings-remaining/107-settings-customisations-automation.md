# Item 107: Settings — Customisations, Automation & Remaining Modules

**URLs:** Multiple (see sections)
**Module:** Settings > Customisations | Automations | Module Settings | Extensions
**Audit Date:** 2026-05-15

---

## Full Settings Sidebar Inventory

This is the complete settings sidebar structure as discovered from the DOM:

### ORGANISATION SETTINGS

| Section | Sub-item | URL |
|---------|---------|-----|
| Organisation | Profile | `#/settings/orgprofile` |
| Organisation | Branding | `#/settings/branding` |
| Organisation | Work Locations | `#/settings/work-locations` |
| Organisation | Departments | `#/settings/departments` |
| Organisation | Designations | `#/settings/designations` |
| Organisation | Subscriptions | `#/settings/subscription-details` |
| Users and Roles | Users | `#/settings/users-roles/users` |
| Users and Roles | Roles | `#/settings/users-roles/roles` |
| Taxes | Tax Details | `#/settings/taxes` |
| Setup & Configurations | Pay Schedule | `#/settings/pay-schedules` |
| Setup & Configurations | Statutory Components | `#/settings/statutory-details/list` |
| Setup & Configurations | Salary Components | `#/settings/salary-components/earnings` |
| Setup & Configurations | Employee Portal | `#/settings/portal/preferences?configure_emails=true` |
| Setup & Configurations | Claims and Declarations | `#/settings/preferences/fbp` |
| Customisations | Email Templates | `#/settings/email-templates` |
| Customisations | Sender Email Preferences | `#/settings/email-preference` |
| Customisations | Salary Templates | `#/settings/salary-templates` |
| Customisations | PDF Templates | `#/settings/templates/regular-payslip` |
| Customisations | Reporting Tags | `#/settings/advanced-reportingtags` |
| Automations | Workflow Rules | `#/settings/automation/workflows` |
| Automations | Actions | `#/settings/automation/actions/alerts` |
| Automations | Schedules | `#/settings/automation/schedules` |
| Automations | Workflow Logs | `#/settings/automation/logs/alerts` |

### MODULE SETTINGS

| Section | Sub-item | URL |
|---------|---------|-----|
| General | Employees & Contractors | `#/settings/employee/contractor` |
| General | Pay Runs | `#/settings/payrun/custom-approval/list` |
| General | Salary Revisions | `#/settings/salary-revision/custom-approval/list` |
| General | Leave & Attendance | `#/settings/holiday-leave/enable-module` |
| General | Loans | `#/settings/loan/custom-field/list` |
| Payments | Direct Deposits | `#/settings/direct-deposit` |

### EXTENSIONS & DEVELOPER DATA

| Section | Sub-item | URL |
|---------|---------|-----|
| Integrations | Zoho Apps | `#/settings/integrations/zoho` |
| Developer Data | Connections | `#/settings/developer-space/connections` |
| Developer Data | Incoming Webhooks | `#/settings/developer-space/incomingwebhooks` |
| Developer Data | Data Backup | `#/settings/data-backup` |

---

## Customisations — Detailed

### Branding

**URL:** `#/settings/branding`

| Field | Type | Options | Notes |
|-------|------|---------|-------|
| Appearance | Radio | Dark Pane \| Light Pane | Theme mode |
| Accent Color | Color picker | Blue (default) | Accent colour for UI elements |

**Note:** "These preferences will be applied across Zoho Finance apps." — branding is org-wide, not app-specific.

### Salary Templates

**URL:** `#/settings/salary-templates`

**Empty state:** "You haven't created any salary templates yet."

**Description:** "Create salary templates for commonly used salary structures and assign them to employees."

**Feature benefits listed:**
1. **Design** — multiple salary structures per designation
2. **Duplicate** — clone template to create variants
3. **Save Time** — assign predefined templates when onboarding employees

**Action:** "Create Salary Template" button — links to template creation form (not audited — empty state org)

**Business purpose:** Pre-defined salary structures (component mix + amounts) that can be applied to new employees without manually entering all components each time. Critical for bulk onboarding.

### PDF Templates

**URL:** `#/settings/templates/regular-payslip`

**Sub-nav:**

| Category | Sub-item | URL |
|----------|---------|-----|
| PAYSLIP | Regular Payslips | `#/settings/templates/regular-payslip` |
| PAYSLIP | Final Settlement Payslip | (inferred: `#/settings/templates/final-settlement-payslip`) |
| LETTER TEMPLATES | Salary Certificate | (inferred) |
| LETTER TEMPLATES | Salary Revision Letter | (inferred) |
| LETTER TEMPLATES | Bonus Letter | (inferred) |

**Regular Payslip Templates (7):**

| Template Name | Default? |
|--------------|----------|
| Elegant Template | DEFAULT |
| Standard Template | - |
| Mini Template | - |
| Simple Template | - |
| Lite Template | - |
| Simple Spreadsheet Template | - |
| Professional Template | - |

**Actions per template:** "Set as Default" button (non-default templates only)

**Business rule:** Exactly one template is active as DEFAULT at a time. Setting a new default immediately changes which template is used for payslip generation.

### Reporting Tags

**URL:** `#/settings/advanced-reportingtags`

**Empty state:** "Configure tags" — use tags to segregate/visualize org data for improved decision-making.

**Action:** "+ New Tag" button

**Business purpose:** Custom tags that can be applied to payroll data for cross-cutting reporting (e.g., tag by project, cost centre category, etc.). Feeds into Reports module criteria filtering.

---

## Automations — Detailed

### Workflow Rules

**URL:** `#/settings/automation/workflows`

**Empty state:** "You haven't created any Workflow Rules yet."

**Usage Stats (per day):**
- Custom Functions: 0 / 1000
- Webhooks: 0 / 1000
- Email Alerts: 0 / 500

**Module filter:** "Module: All" — suggests workflows can be scoped to specific modules.

**Description:** "Automate actions based on specific conditions and criteria. Once set up, a Workflow Rule triggers an action when specified criteria are met."

**Action:** "Add Workflow Rule" / "+ Add New" button

**Business purpose:** Event-driven automation (e.g., "When employee join date is entered, send welcome email"; "When salary revision is approved, notify HR manager").

### Actions (Automations sub-section)

**URL:** `#/settings/automation/actions/alerts`

**Sub-tabs:**
| Tab | URL |
|-----|-----|
| Alerts | `#/settings/automation/actions/alerts` |
| Webhooks | `#/settings/automation/actions/webhooks` |
| Custom Functions | `#/settings/automation/actions/customfunctions` |
| Field Updates | `#/settings/automation/actions/fieldupdates` |

**Additional toolbar action:** "Configure Failure Preferences" — manages alert failure behavior.

**Alerts description:**
> "Alerts allow you to set up automated notifications and emails for important payroll events in Zoho Payroll. You can set up Email Alerts and In-App Notifications."

Types: Email Alerts | In-app Notifications

**Business rule:** Actions (Alerts, Webhooks, Custom Functions, Field Updates) are the action templates used by Workflow Rules. Actions are defined independently and then referenced by rules — modular design.

### Schedules

**URL:** `#/settings/automation/schedules`

**Empty state:** "You haven't scheduled any tasks yet."

**Description:** "Automate repetitive payroll tasks with Schedule Tasks. Create predefined tasks using a simple deluge script and schedule them to run at specified time intervals."

**Technology note:** Uses Zoho Deluge scripting language for custom scheduled functions.

**Action:** "Add Schedule" button

### Workflow Logs

**URL:** `#/settings/automation/logs/alerts`

*(Not navigated — captured from sidebar only.)*

---

## Module Settings — Detailed

### Employees & Contractors

**URL:** `#/settings/employee/contractor`

**Sub-tabs (Employees & Contractors section):**
| Sub-tab | URL |
|---------|-----|
| Contractor | `#/settings/employee/contractor` |
| Custom Field | `#/settings/employee/custom-field/list` |
| Custom Button | `#/settings/employee/custom-button/list` |
| Validation Rules | `#/settings/employee/field-validations` |
| Record Locking | `#/settings/employee/record-locking` |
| Related List | `#/settings/employee/related-list` |

**Contractor module (empty state):**

> "Capture all necessary details about your contractors and manage their compensation in this module."

**Action:** "Enable Contractors Module" button

**Contractor enablement notes (displayed on page):**
- **Roles & Permissions:** Employee role permissions also apply to contractors (only relevant permissions)
- **Workflows:** Existing Automation > Workflow Rules automatically apply to contractors
- **Documents:** Org folder documents visible to contractors (same as employees)

**Business impact:** Contractors are a separate legal entity from employees — different TDS rules (194C/194J vs 192B), no PF/ESI applicability, no PT, typically fixed-term engagement. Enabling this module unlocks a contractor management workflow distinct from the regular employee flow.

### Custom Fields for Employees

**URL:** `#/settings/employee/custom-field/list`

*(Inferred from nav — not navigated directly.)*

**Purpose:** Add custom fields to Employee profile (e.g., "Passport Number", "Emergency Contact", "Vehicle Number for meal allowance"). Custom fields appear on employee form and can be used in reports and workflows.

### Validation Rules

**URL:** `#/settings/employee/field-validations`

**Purpose:** Custom validation rules on employee fields (e.g., "Date of Joining cannot be before 2000-01-01"; "Salary must be > ₹0"). Admin-defined constraints beyond built-in validations.

### Record Locking

**URL:** `#/settings/employee/record-locking`

**Purpose:** Lock employee records from editing under specified conditions (e.g., "Lock employee record after exit date is set"). Prevents accidental modification of finalized employee data.

### Loans Sub-section

**URL:** `#/settings/loan/custom-field/list`

**Sub-tabs (same pattern as Employees):**
- Custom Fields (Usage: 0/59)
- Custom Button
- Validation Rules
- Record Locking
- Related List

**Business note:** No loan type configuration in Settings — loan types are created from within the Loans module's "Manage Loans" dialog. Settings > Loans is purely for customisation (custom fields, validation, locking).

---

## Payments — Direct Deposits

**URL:** `#/settings/direct-deposit`

### Integration Options (3)

| Integration | Status | Fee / Notes |
|-------------|--------|-------------|
| **Zoho Payments - Payouts** | Available (Set Up Now) | Platform Fee: ₹3.00/employee/pay run (excl. 18% GST). Add funds to Zoho Payments Payout Account; disburse directly to employee bank accounts. |
| **ICICI Bank** | Locked (Trial plan) | "To configure ICICI Bank Integration, upgrade your plan." Bank transfer to employees via ICICI net banking. |
| **HSBC Bank** | Available (Set Up Now) | Transfer salaries directly to employees' bank accounts. |

**How Zoho Payments - Payouts works:**
1. Admin adds funds from verified bank account into Zoho Payments Payout Account
2. On pay day, admin disburses salary from Zoho Payroll directly to all employee bank accounts (single action)

**Business rules:**
- Zoho Payments has a per-employee per-run fee of ₹3 + 18% GST = ₹3.54/employee/run
- ICICI Bank integration is a paid plan feature
- HSBC Bank integration available on trial plan
- These are alternatives to manual "Record Payment" + bank file download workflow

---

## Claims and Declarations Settings

**URL:** `#/settings/preferences/fbp` (base)

### Sub-tabs

| Tab | URL | Status |
|-----|-----|--------|
| Flexible Benefit Plan (FBP) | `#/settings/preferences/fbp` | No active FBP component |
| Reimbursement Claims | `#/settings/preferences/reimbursement` | No active reimbursements |
| Income Tax Declaration | `#/settings/preferences/it-declaration` | IT Declaration is Locked |
| Proof Of Investments (POI) | `#/settings/preferences/proof-of-investment` | POI is Locked |

### FBP Preference

**Empty state message:**
> "Your organisation does not have an active FBP component associated to an employee. Mark a reimbursement as FBP component under Settings > Salary Components > Reimbursements and associate it to the employee's salary."

**Prerequisite:** At least one Reimbursement component must be marked as FBP and assigned to an employee's salary structure.

### Reimbursement Claims Preference

**Empty state message:**
> "Employees can get tax exemptions on producing necessary bills. You can enable a reimbursement component under Settings > Salary Components > Reimbursements and associate it to the employee's salary."

**Prerequisite:** Same as FBP — reimbursement components must be configured.

### IT Declaration Preference

| Field / Action | Type | Notes |
|----------------|------|-------|
| IT Declaration is Locked | Status label | Indicates declarations not yet released to employees |
| Release IT Declaration | Button | Releases IT Declaration for employee submission via portal |
| Allow employees to switch tax regimes | Checkbox | If checked, employees can choose old vs new regime in portal |
| Allow TDS modification to exceed current fiscal year's calculated tax amount | Checkbox | If checked, admin can set TDS higher than the computed amount |
| Save | Button | Saves Other Configurations checkboxes |

**Business rules:**
- IT Declaration is "Locked" by default — admin must explicitly Release it
- When locked, only admin can enter declarations on employee's behalf (Employee Profile > Investments > IT Declaration)
- "Allow switch tax regime" — v1 concern: we build New Regime only; this option enables Old Regime switch — likely deferred for our build
- IT Declaration help document link + IT Declaration ebook download available

### Proof Of Investments (POI) Preference

| Field / Action | Type | Notes |
|----------------|------|-------|
| POI is Locked | Status label | POI not yet released to employees |
| Release Proof Of Investments | Button | Releases POI for employee submission via portal |
| Process payroll with approved POI amount from [Month] | Dropdown | "March" — month from which approved POI amounts start reflecting in TDS calculation |
| Allow employees to switch tax regimes | Checkbox | Same as IT Declaration setting |
| Allow TDS modification during Payroll | Checkbox | Admin can override TDS during pay run |
| Mandate investment proof attachments for POI submission | Checkbox | Employees must upload document proof (not just declare amount) |
| Mandate reviewer comments for partial investment amount approval | Checkbox | Reviewer must add comment when approving less than declared amount |
| Save | Button | Saves Other Configurations |
| View Employees yet to submit POI | Toolbar link | Quick link to POI submission status |

**Business rules:**
1. **POI "Process from March"** — key statutory rule: POI-approved amounts typically reflected from Q4 (January–March). Zoho defaults to March. This aligns with CBDT advisory that employers should accept POI by March for TDS recalculation.
2. **Mandatory attachments** — if enabled, employee cannot submit POI with amount only; must attach scanned document. Critical for 80C, 80D claims.
3. **Partial approval comment mandate** — reviewer must justify partial approval (audit trail for POI disputes).
4. **TDS modification** — allows payroll admin to manually adjust TDS during pay run (e.g., if employee has other income sources).

---

## Data Backup

**URL:** `#/settings/data-backup`

### Sub-tabs

| Tab | Description |
|-----|-------------|
| Backup Data | Full org data export as CSV |
| Backup Audit Trail | Audit trail export (not separately navigated) |

### Backup History Table

| Column | Description |
|--------|-------------|
| BACKUP TIME | When backup was triggered |
| USER NAME | Who initiated the backup |
| FILE TYPE | CSV (inferred) |
| EXPORT STATUS | Success / Failed / In Progress |
| DOWNLOAD LINK | Download completed backup file |

**Actions:**
- "Back Up Data" button — triggers immediate backup; sends CSV to admin email
- "Backup history" accordion — shows past backup attempts

**Business rule:** Backup is CSV export sent to admin email — not an automated scheduled backup (that would require Schedules in Automation). This is on-demand only.

---

## Business Rules Summary

1. **Salary Templates** reduce onboarding time — pre-configured component structures assignable to employees.
2. **PDF Templates** — Elegant is default payslip; admin changes default with "Set as Default"; immediate effect on next payslip generation.
3. **Automations use Deluge** — Zoho's proprietary scripting language; not Python or JS. Custom functions cap at 1000/day.
4. **Contractors module disabled by default** — must be explicitly enabled; shares employee role permissions.
5. **Direct Deposit** pricing: ₹3/employee/run (Zoho Payments); ICICI needs paid plan; HSBC available on trial.
6. **Claims & Declarations locked by default** — IT Declaration AND POI both locked; admin releases separately.
7. **POI reflects from March** — Zoho default aligns with CBDT Q4 TDS recalculation advisory.
8. **No compliance calendar** — `#/compliance-calendar` → 404. Zoho does NOT have a built-in compliance calendar module. Statutory due dates are not tracked in-app.
9. **Data backup is on-demand, CSV** — no auto-scheduled backup in base product.

---

## Open Questions

- [ ] Workflow Rules: what modules/events can trigger a workflow? (Employee Create, Pay Run Approve, Salary Revision, etc.)
- [ ] Custom Fields for Employees: what field types are supported (text, number, date, dropdown, file)?
- [ ] Salary Templates: can templates be version-controlled (e.g., "FY2026 template" vs "FY2027 template")?
- [ ] Zoho Payments Payout: is there a minimum fund balance requirement?
- [ ] HSBC Bank integration: requires account at HSBC or just any HSBC corporate net banking?
- [ ] Data Backup: does "Backup Audit Trail" include all user actions or only payroll events?
- [ ] Compliance Calendar: given `#/compliance-calendar` → 404, how do admins track statutory due dates? (Manual process or Zoho Books integration?)
- [ ] "Allow TDS modification to exceed calculated amount" — is this only for arrear scenarios or general override?
