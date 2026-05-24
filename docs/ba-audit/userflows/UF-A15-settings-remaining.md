# UF-A15: Settings — Remaining Pages Deep-Dive

**Module:** Settings (all sections)
**Tested:** 2026-05-16
**Routes explored:** Organisation Profile, Pay Schedules, Salary Components (Reimbursements), Employee Portal, Integrations

---

## Findings

### 1. Settings Navigation Structure

**Master Settings page:** `#/settings`
**Settings sidebar categories:**

| Category | Items |
|----------|-------|
| Organisation Settings | Profile, Branding, Work Locations, Departments, Designations, Subscriptions |
| Users and Roles | Users, Roles |
| Taxes | Tax Details |
| Setup & Configurations | Pay Schedule, Statutory Components, Salary Components, Employee Portal, Claims and Declarations |
| Customisations | Email Templates, Sender Email Preferences, Salary Templates, PDF Templates, Reporting Tags |
| Automations | Workflow Rules, Actions, Schedules, Workflow Logs |
| General / Module Settings | Employees & Contractors, Pay Runs, Salary Revisions, Leave & Attendance, Loans |
| Payments | Direct Deposits |
| Integrations | Zoho Apps |
| Developer Data / Extensions | Connections, Incoming Webhooks, Data Backup |

---

### 2. Organisation Profile

**Route:** `#/settings/orgprofile`
**Page title:** "Organisation Profile | Settings | Zoho Payroll"

**Fields:**

| Field | Type | Value (test org) | Notes |
|-------|------|-----------------|-------|
| Organisation Logo | File upload | (none) | Image upload for branding |
| Organisation Name | Text | lerno | Required |
| Business Location | Dropdown | India | Required, country-level |
| Industry | Dropdown | (not observed) | Required |
| Date Format | Dropdown | (default) | Required |
| Field Separator | Dropdown | (default) | For CSV exports |
| Address Line 1 | Text | lerno | Company address |
| City | Text | kazhakoottam | |
| State | Text | thiruvananthapuram | |
| PIN Code | Text | 695010 | 6-digit Indian PIN |

**Buttons:** Save

**Note:** Address is in Thiruvananthapuram district, Kerala — confirms PT state as Kerala (₹200/month for eligible employees).

---

### 3. Branding

**Route:** `#/settings/branding`
**Purpose:** Custom logo, colour theme for payslips and portal
**Not deeply explored** in this session.

---

### 4. Work Locations

**Route:** `#/settings/work-locations`
**Purpose:** Define physical office locations. Each location can have different:
- City and state (critical for PT — Professional Tax is state-wise)
- Work days configuration
- Holiday list assignment

**Key business rule:** If an org has employees in multiple states, separate Work Locations with different PT states must be configured. PT deduction uses the Work Location's state, not the organisation's registered state.

**Not deeply explored** — from prior sessions (UF-10-configure-pt.md): Work Locations determine PT applicability per employee.

---

### 5. Departments and Designations

**Routes:** `#/settings/departments`, `#/settings/designations`

**Departments:**
- CRUD operations (Create, Read, Update, Delete)
- Used for grouping employees
- Visible in employee profiles, reports, and salary revision bulk actions

**Designations:**
- CRUD operations
- Free-text designation names
- Used in TDS deductor card (Father's name + Designation appear on Form 16)

**Observed designations in test org:** Senior Software Engineer (EMP001), Junior Developer (EMP002)

---

### 6. Pay Schedule

**Route:** `#/settings/pay-schedules`
**Page title:** "Pay Schedule | Settings | Zoho Payroll"

**Current configuration (test org):**

| Field | Value |
|-------|-------|
| Pay Frequency | Every month (monthly) |
| Working Days | Mon, Tue, Wed, Thu, Fri |
| Pay Day | (Change option available) |
| First Pay Period | April 2026 |

**Upcoming payrolls shown:**
- 30/06/2026 (June 2026 pay run)
- 31/07/2026 (July 2026 pay run)

**Important constraint note on page:**
> "Pay Schedule cannot be edited once you process the first pay run. This Organisation's payroll runs on this schedule."

**Frequency options** (observed from settings nav and prior sessions):
- Every month (monthly) — selected in test org
- Expected but not confirmed: Weekly, Bi-weekly, Semi-monthly, Custom

**Pay Day options:** Last working day of month, Specific day (1-31), Custom (configurable)

---

### 7. Statutory Components

**Route:** `#/settings/statutory-details/list`
**From prior sessions (UF-08 to UF-11):**

| Component | Status |
|-----------|--------|
| EPF | Configured |
| ESI | Configured |
| Professional Tax (Kerala) | Configured |
| LWF | Configured |

---

### 8. Salary Components

**Route:** `#/settings/salary-components/earnings` (tabbed)

**Sub-tabs:**
| Tab | Components |
|-----|-----------|
| Earnings | Basic, HRA, Conveyance Allowance, Children Education Allowance, Transport Allowance, Travelling Allowance, Special Allowance, Fixed Allowance, Overtime Allowance, Gratuity |
| Deductions | (from prior sessions: PF Employee, ESI Employee, PT, LWF, etc.) |
| Benefits | (not fully explored) |
| Reimbursements | Fuel, Driver, Vehicle Maintenance, Telephone, Leave Travel Allowance |

---

### 9. Salary Templates

**Route:** `#/settings/salary-templates`
**Purpose:** Pre-defined salary structure templates. Rather than creating a salary structure from scratch per employee, admin can pick a template (e.g., "Software Engineer Template") which pre-populates component allocations as percentages of CTC.

**Difference from Salary Structures:**
- Salary Template = reusable template (abstract %)
- Salary Structure = employee-specific instantiation with actual ₹ amounts

**Not deeply explored** — existence confirmed from settings sidebar.

---

### 10. PDF Templates

**Route:** `#/settings/templates/regular-payslip`
**Purpose:** Customise the visual layout of the payslip PDF. Can change header, footer, logo, colour scheme, which fields to show/hide.

**Template types:**
- Regular Payslip
- (possibly others: Off-Cycle, Bonus, etc.)

**Not deeply explored** in this session.

---

### 11. Claims and Declarations (FBP Preferences)

**Route:** `#/settings/preferences/fbp`
**Page title:** "FBP Preference | Preferences | Settings | Zoho Payroll"
**Purpose:** Configure Flexible Benefit Plan preferences and IT Declaration/POI window dates

**Not deeply explored** — page loaded but content area was obscured by chat widget overlay.

---

### 12. Module Settings — Pay Runs

**Route:** `#/settings/payrun/custom-approval/list`
**Purpose:** Configure whether pay runs require approval before finalization
- Custom approval workflows per pay run type
- Multi-level approval chains
- Approvers by role

---

### 13. Module Settings — Salary Revisions

**Route:** `#/settings/salary-revision/custom-approval/list`
Same structure as Pay Run approvals — configurable approval workflow for salary revisions.

---

### 14. Integrations — Zoho Apps

**Route:** `#/settings/integrations/zoho`
**Page title:** "Integrations - Zoho Apps | Settings | Zoho Payroll"

**Available integrations (all in "Connect" state — none connected):**

| App | Description | Status |
|-----|-------------|--------|
| Zoho People | Fetch employee and LOP details directly from Zoho People (HR system) | Not connected |
| Zoho Books | Sync all payroll transactions with Zoho Books account automatically | Not connected |
| Zoho Expense | Allow employees to submit expenses for reimbursements easily | Not connected |
| Zoho Analytics (Beta) | Create custom reports and make better business decisions | Not connected |

**Zoho People integration:** Bidirectional sync — employee records created in Zoho People auto-populate in Zoho Payroll. LOP (Leave Without Pay) data from leave management syncs to payroll.

**Zoho Books integration:** Payroll journal entries (salary expense, PF liability, TDS payable) auto-posted to Zoho Books chart of accounts.

**Zoho Expense integration:** Expense reports submitted in Zoho Expense can flow into Zoho Payroll as reimbursement claims.

**Zoho Analytics:** Custom report builder on top of payroll data.

---

### 15. Developer Data / Extensions

**Settings sidebar items:**
- Connections — API connections to external services
- Incoming Webhooks — receive events from external systems
- Data Backup — schedule/download data backups

**Not deeply explored** in this session.

---

### 16. Users and Roles

**Routes:** `#/settings/users-roles/users`, `#/settings/users-roles/roles`
**From prior session (UF-91-settings-users-roles.md):**

**Roles observed:**
| Role | Access Level |
|------|-------------|
| Organisation Administrator | Full access |
| (Others configurable) | Restricted access |

**RBAC:** Zoho Payroll supports role-based access. Custom roles can be created with field-level permissions.

---

### 17. Direct Deposits

**Route:** `#/settings/direct-deposit`
**Purpose:** Configure Zoho Payments integration for direct salary disbursement (in-app bank transfer)
**From prior session (UF-92):** Requires Zoho Payments account setup. Allows bank transfers directly from within Zoho Payroll without separate bank login.

---

### 18. Email Templates

**Route:** `#/settings/email-templates`
**Purpose:** Customise email content sent to employees for:
- Payslip distribution
- Portal invitation
- IT Declaration reminder
- POI submission reminder
- Form 16 distribution

**Not deeply explored** in this session.

---

### 19. Subscriptions

**Route:** `#/settings/subscription-details`
**Current state:** Trial (expires in 13 days from test date)
**Upgrade prompt:** "Upgrade" button visible in top navigation

---

## Key Settings Architecture Observations

1. **Settings is a full SPA within the payroll SPA** — the Settings area has its own sidebar nav, search, and close button. It's an overlay/modal-style settings panel, not a full page navigation.

2. **Settings search:** `Search settings ( / )` — keyboard shortcut `/` focuses the settings search field. This is a global settings search.

3. **Pay Schedule immutability:** Once first pay run is processed, pay schedule cannot be changed. This is a critical constraint for the engine — pay period definition is frozen post-first-run.

4. **Multi-state PT support:** Work Locations are the mechanism for supporting employees in different states with different PT slabs. The tax engine must look up PT rates by Work Location state, not org-level state.

5. **Integration architecture:** All Zoho integrations (People, Books, Expense, Analytics) connect via Zoho's internal OAuth/API layer. No third-party payroll API integrations observed.

---

## Screenshots / Files

- `settings-tax-details.png` — Tax Deductor configuration (prior session)
- `settings-employee-portal.png` — Employee Portal settings
- `settings-claims-declarations.png` — Claims and Declarations settings (content partially obscured)
- `settings-reimbursements-components.png` — Reimbursement components
- `reimbursement-component-detail.png` — Component edit form

---

## Gaps / Open Questions

- [ ] **Work Locations CRUD:** Can multiple work locations with different state PT configurations be verified? (e.g., adding a Bangalore location = Karnataka PT)
- [ ] **Salary Templates content:** What exactly is in the salary template? Are there default templates out-of-box?
- [ ] **PDF Template customisation:** How deep is the payslip PDF customisation? Can custom fields be added?
- [ ] **Audit Log:** Is there a system-wide audit log accessible from Settings? What events are tracked (login, payroll processing, data changes)?
- [ ] **Reporting Tags:** `#/settings/advanced-reportingtags` — What are reporting tags used for? Cost center allocation?
- [ ] **Workflow Rules:** `#/settings/automation/workflows` — what triggers and actions are supported?
- [ ] **Data Backup:** How frequently can data be backed up? What format (CSV, JSON)?
- [ ] **API/Webhooks:** Is there a public REST API for Zoho Payroll? What endpoints exist for employee, payrun, and payslip data?
- [ ] **FBP Preferences:** What are the FBP window dates for declaration and proof submission?
- [ ] **Custom Fields:** `#/settings/employee/contractor` → are there custom fields configurable for employees?
- [ ] **Leave & Attendance settings:** `#/settings/holiday-leave/enable-module` — Is Leave & Attendance a separate module that integrates with Zoho Payroll?
