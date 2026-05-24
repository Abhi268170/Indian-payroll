# Zoho Payroll Settings — BA Audit Index

**Org:** lerno | **App:** payroll.zoho.in | **Audit completed:** 2026-05-15
**Total pages audited:** 33 Settings pages across 3 nav groups

---

## ORGANISATION SETTINGS

| # | Page | File | URL | One-Line Summary |
|---|------|------|-----|-----------------|
| 01 | Organisation Profile | [01-org-profile.md](01-org-profile.md) | `#/settings/orgprofile` | Company name, logo, address, industry, date format; Org ID 60071806579 |
| 02 | Branding | [02-branding.md](02-branding.md) | `#/settings/branding` | Appearance (Dark/Light pane) + Accent colour (5 options); changes apply to all Zoho Finance apps |
| 03 | Work Locations | [03-work-locations.md](03-work-locations.md) | `#/settings/worklocations` | Physical office locations with state; state locked post-creation for statutory compliance; triggers LWF config |
| 04 | Departments | [04-departments.md](04-departments.md) | `#/settings/departments` | Create/import departments; Name + Code + Description (250 char); CSV/TSV/XLS import |
| 05 | Designations | [05-designations.md](05-designations.md) | `#/settings/designation` | Simple designation list; Name only; no description or code |
| 06 | Subscriptions | [06-subscriptions.md](06-subscriptions.md) | `#/settings/subscriptions` | Plan details (14-day trial); feature list (25 Core + 5 Automation + 1 Premium); 0 employees used |
| 07 | Users | [07-users.md](07-users.md) | `#/settings/users` | Invite users by Name/Email/Role; 3 roles: Admin/Manager/Reimbursements & POI Reviewer |
| 08 | Roles | [08-roles.md](08-roles.md) | `#/settings/roles` | Full RBAC matrix: 5 Employee sub-modules, Payroll (APPROVE+PAY), Loan (RECORD DISBURSEMENT), 22 Settings toggles, 9 Report types, protected data access |
| 09 | Tax Details | [09-tax-details.md](09-tax-details.md) | `#/settings/taxdetails` | PAN (AAAAA0000A), TAN (AAAA00000A), AO Code (4-part), Tax Payment Frequency (Monthly, locked); Deductor: Employee or Non-Employee |
| 10 | Pay Schedule | [10-pay-schedule.md](10-pay-schedule.md) | `#/settings/payschedule` | Work week (Mon–Fri default), Salary Calculation Method (Actual/Fixed days), Pay Date (Last day or 1–30) |

---

### SETUP & CONFIGURATIONS

| # | Page | File | URL | One-Line Summary |
|---|------|------|-----|-----------------|
| 11 | Statutory Components | [11-statutory-components.md](11-statutory-components.md) | `#/settings/statutory-component/epf` | EPF/ESI/PT/LWF/Statutory Bonus config; 5 tabs; EPF number format, ESI wage threshold ₹21,000, Kerala PT half-yearly slabs |
| 12 | Salary Components | [12-salary-components.md](12-salary-components.md) | `#/settings/salary-components/earnings` | 4 tabs: 14 Earnings (33 earning types), 2 Deductions, 1 Benefit (VPF), 5 Reimbursements; immutability after employee assignment |

---

### EMPLOYEE PORTAL & CLAIMS

| # | Page | File | URL | One-Line Summary |
|---|------|------|-----|-----------------|
| 13 | Employee Portal | [13-employee-portal.md](13-employee-portal.md) | `#/settings/portal/preferences` | ESS portal enable/disable, banner message with expiry, contact email, document visibility; Web Tabs (subscription) |
| 14 | Claims & Declarations | [14-claims-declarations.md](14-claims-declarations.md) | `#/settings/preferences/fbp` | 4 sub-tabs: FBP (empty), Reimbursement Claims (empty), IT Declaration (Locked + regime switch + TDS override checkboxes), POI (Locked) |

---

### CUSTOMISATIONS

| # | Page | File | URL | One-Line Summary |
|---|------|------|-----|-----------------|
| 15 | Email Templates | [15-email-templates.md](15-email-templates.md) | `#/settings/email-templates` | 4 fixed templates (Payslip/Portal-Disabled/Off-Cycle/F&F); WYSIWYG editor with merge fields; placeholders: Employee Name, Pay Period, Portal URL, Company Name |
| 16 | Sender Email Preferences | [16-sender-email-preferences.md](16-sender-email-preferences.md) | `#/settings/email-preference` | Configure From address; public domain override to Zoho mailer; verification required; ₹0 cost |
| 17 | Salary Templates | [17-salary-templates.md](17-salary-templates.md) | `#/settings/salary-templates` | Reusable CTC structures; two-panel builder; Basic (50% CTC) + Fixed Allowance (residual) always present |
| 18 | PDF Templates | [18-pdf-templates.md](18-pdf-templates.md) | `#/settings/templates/regular-payslip` | 7 regular payslip variants + F&F template + 3 letter templates (Salary Cert, Revision, Bonus); per-template display preferences |
| 19 | Reporting Tags | [19-reporting-tags.md](19-reporting-tags.md) | `#/settings/advanced-reportingtags` | Custom employee classification dimensions; 3-step wizard; up to 59 tags; cross-Zoho-app sharing |

---

### AUTOMATIONS

| # | Page | File | URL | One-Line Summary |
|---|------|------|-----|-----------------|
| 20 | Workflow Rules | [20-automations.md](20-automations.md) | `#/settings/automation/workflows` | Event-driven rules for Pay Runs/Employee/Loan/Reimbursement Claim; 1,000 custom functions + webhooks/day; 500 email alerts/day |
| 21 | Actions | [20-automations.md](20-automations.md) | `#/settings/automation/actions/alerts` | Pre-built action components: Email Alerts, Webhooks, Custom Functions (Deluge), Field Updates; 4 sub-tabs |
| 22 | Schedules | [20-automations.md](20-automations.md) | `#/settings/automation/schedules` | Periodic Deluge script tasks; run at defined time intervals |
| 23 | Workflow Logs | [20-automations.md](20-automations.md) | `#/settings/automation/logs/alerts` | Audit trail for all automation executions; 5 sub-tabs; filter by Status/Module/Date Range; exportable |

---

## MODULE SETTINGS

### GENERAL

| # | Page | File | URL | One-Line Summary |
|---|------|------|-----|-----------------|
| 24 | Employees & Contractors | [24-employees-contractors.md](24-employees-contractors.md) | `#/settings/employee/contractor` | Contractor module enable toggle; Custom Fields (0/59 used; 18 data types); Custom Button, Validation Rules, Record Locking, Related List sub-tabs |
| 25 | Pay Runs | [25-payrun-approvals.md](25-payrun-approvals.md) | `#/settings/payrun/custom-approval/list` | Pay run approval workflow: Simple / Multi-Level / Custom; additional sub-tabs for Custom Button, Record Locking, Related List |
| 26 | Salary Revisions | [26-salary-revision-approvals.md](26-salary-revision-approvals.md) | `#/settings/salary-revision/custom-approval/list` | Salary revision approval: same 3-tier workflow as Pay Runs |
| 27 | Leave & Attendance | [27-leave-attendance.md](27-leave-attendance.md) | `#/settings/holiday-leave/enable-module` | Blocked until Pay Schedule configured; governs attendance cycle, LOP deduction, leave types |
| 28 | Loans | [28-loans.md](28-loans.md) | `#/settings/loan/custom-field/list` | Loan module custom fields (0/59); Custom Button, Validation, Record Locking, Related List sub-tabs |

---

### PAYMENTS

| # | Page | File | URL | One-Line Summary |
|---|------|------|-----|-----------------|
| 29 | Direct Deposits | [29-direct-deposits.md](29-direct-deposits.md) | `#/settings/direct-deposit` | 3 salary disbursement channels: Zoho Payments Payouts (₹3/emp/run + GST), ICICI Bank (paid plan only), HSBC Bank |

---

## EXTENSIONS & DEVELOPER DATA

### INTEGRATIONS

| # | Page | File | URL | One-Line Summary |
|---|------|------|-----|-----------------|
| 30 | Integrations — Zoho Apps | [30-integrations-zoho.md](30-integrations-zoho.md) | `#/settings/integrations/zoho` | 4 Zoho integrations: People (employee/LOP sync), Books (accounting journal sync), Expense (claim reimbursement), Analytics BETA (custom BI) |

---

### DEVELOPER DATA

| # | Page | File | URL | One-Line Summary |
|---|------|------|-----|-----------------|
| 31 | Connections | [31-developer-data.md](31-developer-data.md) | `#/settings/developer-space/connections` | OAuth connections to external REST APIs; for use in Custom Buttons, Schedules, Custom Functions |
| 32 | Incoming Webhooks | [31-developer-data.md](31-developer-data.md) | `#/settings/developer-space/incomingwebhooks` | Receive POST data from external apps; triggers payroll workflow actions |
| 33 | Data Backup | [33-data-backup.md](33-data-backup.md) | `#/settings/data-backup` | On-demand CSV backup (all modules) + Audit Trail backup (incremental ZIP); email delivery; backup history table |

---

## Critical Findings Summary

### Statutory Compliance Flags
1. **PT Kerala = Half-Yearly** — deduction cycle differs from other states; engine must handle non-monthly PT deduction cycles.
2. **ESI threshold ₹21,000** — hardcoded; must be DB-configurable for future threshold changes.
3. **EPF wage cap ₹15,000** — employer contribution restricted to ₹15,000 PF wage; configurable per org.
4. **IT Declaration Lock/Release** — declaration window is a toggle; open/close is admin-controlled, not employee-controlled.
5. **TDS override flag** — allows TDS beyond system-computed amount; must be Admin-role restricted.
6. **New regime only (v1)** — "Allow employees to switch tax regimes" flag must default to disabled for our v1 build.

### Architecture Insights
1. **Salary Component immutability** — once assigned to employee, only Name and Amount changeable; type and statutory treatment frozen.
2. **Fixed Allowance = residual component** — always present in salary structure; absorbs unallocated CTC.
3. **3-tier approval model** — Simple/Multi-Level/Custom applies identically to Pay Runs and Salary Revisions.
4. **Custom field limit** — 59 custom fields per entity (Employee, Loan); model as typed CustomField table.
5. **Deluge lock-in** — all automation scripting uses Zoho's proprietary Deluge. Our build: use C# Hangfire jobs instead.
6. **Direct deposit fee** — ₹3/employee/run via Zoho Payments (in-app); bank API integration is enterprise-tier.

### Missing in Zoho (gaps our build should fill)
1. No scheduled automatic backup — our build: daily automated MinIO backup.
2. No compliance calendar / statutory due-date alerts in Settings.
3. Salary Transfer Letter template (referenced in search index but not in PDF Templates nav — may be gated).
4. YES Bank direct deposit (mentioned in text but not visible in UI — may be removed or hidden).
