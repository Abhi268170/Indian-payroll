# BA Audit — Master Index

## Session Reports

| Session | Date | Module(s) | Status |
|---------|------|-----------|--------|
| Session 1 | 2026-05-14 | Auth, Platform Admin, Employee Master, Org Structure | Complete |
| Session 2 | 2026-05-15 | Zoho Payroll Reference — Dashboard (Onboarding state) | Complete |
| Session 3 | 2026-05-15 | Zoho Payroll Reference — Settings > Organisation Profile + sidebar nav inventory | Complete |
| Session 4 | 2026-05-15 | Zoho Payroll Reference — Settings > Tax Details (PAN, TAN, AO code, Deductor) | Complete |
| Sessions 5–6 | 2026-05-15 | Zoho Payroll Reference — Employees Module (items 34–51) | Complete |
| Sessions 7–8 | 2026-05-15 | Zoho Payroll Reference — Pay Runs Module (full lifecycle, 16 files) | Complete |
| Sessions 9–10 | 2026-05-15 | Zoho Payroll Reference — Statutory Compliance + Form 16 (items 74–86) | Complete |
| Session N | 2026-05-15 | Zoho Payroll Reference — Approvals Module (items 68–73) | Complete |

---

## Audit Progress

### Completed

- Authentication & Authorization (Login, ForgotPassword, SetPassword)
- Platform Admin (Tenants List, Provision Org, Org Detail)
- Employee Master (List + Create — partial; many fields not yet in our UI)
- Org Structure (Branches, Departments, Designations, Cost Centres)
- Zoho Payroll Reference — Dashboard (Onboarding / Getting Started state)
- Zoho Payroll Reference — Settings > Organisation Profile (full field audit + sidebar nav inventory)
- Zoho Payroll Reference — Settings > Tax Details (PAN, TAN, AO code, Deductor Type conditional form)

- **Zoho Payroll Reference — Employees Module (20 files, items 34–51 + addendum)** → [employees/00-employees-index.md](employees/00-employees-index.md)
  - Employee List (empty + populated, 21 columns, 8 views, import/export)
  - Add Employee Wizard (4 steps: Basic Details, Salary Details, Personal Details, Payment Info)
  - Employee Profile (5 tabs: Overview, Salary Details, Investments, Payslips & Forms, Loans)
  - Salary Structure + Component Actions (Add Earning, Scheduled Earning, Benefit, Deduction, Donation)
  - 5 Mock Employees Created: EMP001–EMP005
  - Scenario Coverage: Mid-month join (EMP002), Mid-year join + prior YTD (EMP003), Contractor gap (EMP004), Prior FY DOJ (EMP005)
  - Salary Revision (direct edit + dated revision route investigation)
  - Exit Process form (route, fields, exit reasons, F&F timing)
  - F&F Settlement (design-level)
  - Bulk Import (19 import types across 5 groups)
  - Settings > Salary Components (4 tabs: 14 earnings + 2 deductions + 1 benefit + 5 reimbursements)

- **Zoho Payroll Reference — Pay Runs Module (16 files, items 52–67)** → [pay-runs/](pay-runs/)
  - Pay Runs List (Ready state, New dropdown 8 run types, Payroll History link)
  - Add Employees gate (MISSING vs WITHOUT_PAN variants, bulk skip with `pay_joinee_arrear_later`)
  - Pay Run Preview / Draft (per-employee split panel: LOP, Earnings, TDS override, Deductions)
  - Overall Insights (component drill-down via `/payruns/insights/{id}/earnings/{componentId}`)
  - Approve Payroll dialog (hard block on MISSING, soft warning on PAN-missing)
  - Pay Run Summary: Payment Due state + Paid state (Bank Advice, Record Payment, revert)
  - Payroll History (8 filter types)
  - Full state machine confirmed: Ready → Draft → Payment Due → Paid
  - All dialogs: Skip Employee (single + bulk), Approve, Record Payment, Delete Recorded Payment
  - One Time Payout + Off Cycle Pay Run creation dialogs
  - EMP001 April 2026 breakdown: Basic ₹39,998 | HRA ₹15,999 | Fixed ₹14,003 = ₹70,000
  - Per-state row kebab matrix documented (Draft:6, Approved:variable, Paid:2)

- **Zoho Payroll Reference — Statutory Compliance + Form 16 (items 74–86)** → [compliance/](compliance/) + [form16/](form16/)
  - 74: EPF ECR challan — configuration form (10 fields), ECR via Reports
  - 75: EPF UAN management — conditional field gated on EPF org config
  - 76: ESI challan — 0.75%/3.25% rates, ₹21k ceiling, contribution period rule
  - 77: PT challan — Kerala slabs (7 slabs, Half Yearly, max ₹2,500/yr)
  - 78: LWF challan — Kerala ₹50/₹50 monthly, disabled by default
  - 79: TDS + Form 24Q — TDS liabilities (Unpaid/Paid tabs), Record Challan modal (6 fields), Form 24Q Q1 FY2026-27, FVU text file download, Preferences (Employer + Responsible Person)
  - 80: Form 16 Part A — TRACES-sourced, employer upload, 4-step flow, Tax Deductor gate
  - 81: Form 16 Part B — Zoho-generated, full structure (Sec 17(1), Sec 10, Std Deduction ₹75k, Sec 87A rebate ₹60k, new regime slabs, surcharge cap 25%)
  - 82: Form 16 bulk generate — 4-step flow, DSC/e-Sign, bulk email (PAN password), bulk ZIP
  - 83: Form 16 list view — pre-generation state, FY selector, Tax Deductor gate
  - 84: Form 16 generate flow — 4 prerequisites, step-by-step, status state machine
  - 85: Form 16 employee view — portal access, PDF download, PAN-as-password, re-send
  - 86: Form 16 digital signature — DSC vs e-Sign vs unsigned; v1 recommendation: unsigned fallback

- **Zoho Payroll Reference — Loans Module (items 87–94)** → [loans/](loans/)
  - 87: Loans list empty state — Manage Loans dialog, loan type creation (name + perquisite rate), no system presets
  - 88: Create Loan form — 8 fields (5 visible + 3 conditional on date); Repayment section triggered by Disbursement Date selection; perquisite exemption per Rule 15(5) IT Rules 2026; disbursement out-of-band (tooltip explicitly states)
  - 89: Loan Approval — NOT FOUND; no approval workflow; loan immediately "Open" on save; Approvals module covers Salary Revision/POI/Reimbursements only
  - 90: Loan Disbursement — "Record Repayment" modal (Amount/Date/Payment Mode); date constraints: > loan start AND not in future; Payment Mode: Cheque/Cash/Bank Transfer/Others
  - 91: Loans List populated — 7 columns; status filter (Open/Paused/Closed/All); both test loans created: LOAN-00001 (₹50k, Personal, EMP001) + LOAN-00002 (₹1L, Emergency 6%, EMP003); "View Loan Repayments" → separate tracking page
  - 92: Repayment Schedule — inline on loan detail (pre-EMI empty state); Loan Repayments page at `#/loans/repayments` with Month/Method/Employee filters
  - 93: Loan Deduction in Pay Run — not directly observed (EMI start 01/07/2026, future); auto-deduction from EMI Start Date pay run; perquisite (non-exempt) adds to TDS taxable income
  - 94: Loan Foreclosure — no dedicated Foreclose action; lifecycle via More dropdown: Edit Loan / Pause Instalment Deduction / Delete Loan; Pause modal: Immediately/On Scheduled Month + Resume On date + Reason; Delete: simple Yes/No confirm

- **Zoho Payroll Reference — Settings > Statutory Components (items 95–99)** → [statutory-components/](statutory-components/)
  - 95: EPF Settings — 10-field config form; Employee 12% (fixed); Employer 12% or restricted to ₹15k; 4 checkboxes (employer/EDLI/admin/override); LOP config (2 sub-options); View Splitup (EPS 8.33%+EPF 3.67%); Preview EPF Calculation tool
  - 96: ESI Settings — ESI Number only field; Employee 0.75% + Employer 3.25% hardcoded; ₹21k eligibility ceiling noted; contribution period rule documented (Apr–Sep / Oct–Mar)
  - 97: PT Settings — location-scoped; Kerala: Half Yearly; 7-slab table; Revise Slabs form (Effective From date + inline editable slab table + Additional Slab button)
  - 98: LWF Settings — Kerala ₹50/₹50 Monthly; Disabled by default; system-supplied amounts (not user-editable); Enable toggle only
  - 99: Statutory Bonus — Payment Bonus Act 1965; 8.33%–20% rate; Monthly vs Yearly mode; Yearly requires Payout Month + Min Wage table per employment category per state
  - 87: Loans list empty state — Manage Loans dialog, loan type creation (name + perquisite rate), no system presets
  - 88: Create Loan form — 8 fields (5 visible + 3 conditional on date); Repayment section triggered by Disbursement Date selection; perquisite exemption per Rule 15(5) IT Rules 2026; disbursement out-of-band (tooltip explicitly states)
  - 89: Loan Approval — NOT FOUND; no approval workflow; loan immediately "Open" on save; Approvals module covers Salary Revision/POI/Reimbursements only
  - 90: Loan Disbursement — "Record Repayment" modal (Amount/Date/Payment Mode); date constraints: > loan start AND not in future; Payment Mode: Cheque/Cash/Bank Transfer/Others
  - 91: Loans List populated — 7 columns; status filter (Open/Paused/Closed/All); both test loans created: LOAN-00001 (₹50k, Personal, EMP001) + LOAN-00002 (₹1L, Emergency 6%, EMP003); "View Loan Repayments" → separate tracking page
  - 92: Repayment Schedule — inline on loan detail (pre-EMI empty state); Loan Repayments page at `#/loans/repayments` with Month/Method/Employee filters
  - 93: Loan Deduction in Pay Run — not directly observed (EMI start 01/07/2026, future); auto-deduction from EMI Start Date pay run; perquisite (non-exempt) adds to TDS taxable income
  - 94: Loan Foreclosure — no dedicated Foreclose action; lifecycle via More dropdown: Edit Loan / Pause Instalment Deduction / Delete Loan; Pause modal: Immediately/On Scheduled Month + Resume On date + Reason; Delete: simple Yes/No confirm

- **Zoho Payroll Reference — Approvals Module (items 68–73)** → [approvals/](approvals/)
  - 68: Approvals navigation — 3 sub-items (Salary Revision, POI, Reimbursements); Loans and Pay Run approval elsewhere; no global dashboard; no nav badge count
  - 69: Pay Run Approval — settings-level config (Simple/Multi-Level/Custom at `#/settings/payrun/custom-approval/list`); none configured; state machine documented
  - 70: Salary Revision Approval — full form (2 revision types, component table, Effective From + Payout Month); EMP001 revised ₹8,40,000→₹9,45,000 (+13%); admin revision bypasses queue; datepicker required (text input rejected at submit)
  - 71: IT Declaration / POI Approval — 4 view states; unsubmitted list (EMP001+EMP002 Portal Disabled); IT Declaration + POI settings (Locked state, Release, March recalculation, mandatory attachment/comments)
  - 72: Loans — top-level at `#/loans` (not Approvals); Create Loan fields + Rule 15(5) perquisite statutory reference; no loan types configured in test org; no approval workflow for loans
  - 73: Reimbursements — New Claim form (6 table columns + Save & Approve); prerequisite: types must be assigned to salary structure; all test employees lack reimbursement components; Import/Export available

---

- **Zoho Payroll Reference — Settings > Statutory Components (items 95–99)** → [statutory-components/](statutory-components/)

- **Zoho Payroll Reference — Payslips Module (item 100)** → [payslips/](payslips/)
  - 100: No standalone Payslips route; accessed via Employee Profile ("Payslips & Forms" tab) + Pay Run Summary; 7 PDF templates; FY filter; View modal (iframe API); Download dialog (optional password ≥12 chars); TDS Sheet View; "Send Payslip" bulk email with portal/email delivery split; auto-send on pay run payment

- **Zoho Payroll Reference — Reports Module (item 101)** → [reports/](reports/)
  - 101: 39 system reports across 9 categories; export: PDF/XLS/XLSX/Zoho Sheet; criteria builder (Payroll Status/Dept/Designation/Work Location filters); Favorites/Shared/Scheduled sub-views; Zoho Analytics integration (separate subscription)

- **Zoho Payroll Reference — User Management & RBAC (item 102)** → [user-management/](user-management/)
  - 102: 3 roles (Admin/Manager/Reimbursements+POI Reviewer); Invite User form (Name/Email/Role); custom role creation with 100+ permission checkboxes across Employee/Payroll/Loan/Approvals/Settings/Reports/Documents/TaxForms; "Protected data" toggle for PAN/Aadhaar/bank; Leave+Attendance unchecked in Manager by default

---

- **Zoho Payroll Reference — Employee Portal + Email Templates + Settings (items 105–107)** → [settings-remaining/](settings-remaining/)
  - 105: Employee Portal Settings — Preferences (Enable Portal toggle, Banner Message with expiry date, Portal Contact Info with Manage Contacts panel, Document Management toggle) + Web Tabs (empty, for external URL embeds)
  - 106: Email Templates — 4 templates (Payslip Notification, Portal Disabled variant, Off Cycle/One-Time, F&F Settlement); editor with Subject (default `%PayPeriodMonth%`), Rich text body, Insert Placeholders; Sender Email Preferences: abhijithss2255@gmail.com Unverified PRIMARY, public domain auto-override to `message-service@mail.zohopayroll.in`
  - 107: Full Settings sidebar inventory (31 links); Branding (Dark/Light pane, Accent color — applies across Zoho Finance); Salary Templates (empty, for pre-configured salary structures); PDF Templates — 7 regular (Elegant=DEFAULT) + F&F Settlement + 3 letter templates; Reporting Tags (empty); Automation: Workflow Rules/Actions (Alerts/Webhooks/Custom Functions/Field Updates)/Schedules/Logs — usage caps: Custom Functions 1000/day, Webhooks 1000/day, Email Alerts 500/day; Employees & Contractors sub-tabs (Contractor toggle + Custom Fields/Buttons/Validation/Record Locking/Related List); Direct Deposits — Zoho Payments ₹3/employee/run, ICICI (paid plan), HSBC; Claims & Declarations: FBP/Reimbursements (blocked — no components configured), IT Declaration (Locked + "Allow switch regime" checkbox — DEFERRED v1), POI (Locked, "Process from March", mandatory attachment + reviewer comment options); Data Backup (on-demand CSV to email); Compliance Calendar → 404 (NOT PRESENT in Zoho Payroll)

- **Zoho Payroll Reference — Session N+1 Batch (UF-30 to UF-96 + DS-01 to DS-06)** → [userflows/](userflows/)
  - UF-30 to UF-35: POI upload, approve/reject POI, finalize TDS, tax regime switch, FnF exit, Gratuity
  - UF-43 to UF-49: Variable pay, Reimbursements in pay run (config-gated), LOP deep-dive, pay run review/approve, mark as paid, new joiner proration, skipped employees
  - UF-50 to UF-58: Download payslip (full payslip panel documented), bank advice, off-cycle, bonus, arrears, past pay run, reprocess, reversal
  - UF-59 to UF-62: Approvals module list (Reimbursements + Salary Revision pages), reimbursement claim approval, reject, approval history/audit trail
  - UF-63 to UF-67: Create loan, loan EMI in pay run, loan repayment/prepayment, loan perquisite (Rule 15(5)), loan foreclosure
  - UF-68 to UF-72: TDS Liabilities, EPF ECR generation, ESI return/challan, PT challan, LWF challan
  - UF-73 to UF-76: Form 24Q (Q1 FY2026-27), Form 16 prerequisites, Form 16 generate/sign, Form 16 publish/email
  - UF-77 to UF-83: Reports Centre (39 reports), Payroll Summary report, Statutory reports (PT Kerala half-yearly confirmed), Loan reports, Employee reports, Declaration/Deduction reports, Payroll Journal + Activity Logs
  - UF-84 to UF-88: Employee Portal overview (onboarding 5/7 steps), portal payslips/declarations (IT Declaration locked = root cause of ₹0 TDS), Salary Components settings (15 earnings, 3 deductions, 5 reimbursements, 1 benefit), additional settings (PDF Templates, Prior Payroll), employee portal reimbursement submission
  - UF-89 to UF-96: TDS Challans (Unassociated/Associated tabs), POI Approvals, Settings Users & Roles (3 system roles: Admin/Manager/Reimbursements+POI Reviewer), Direct Deposits (Zoho Payments ₹3/emp/run + ICICI + HSBC) + Integrations (Zoho People/Books/Expense/Analytics), PDF Templates (7 payslip + 3 letter templates) + Email Templates (4 notification types), Loans custom fields/validation settings, Tax Details (PAN/TAN/AO Code/Deductor), Pay Run + Salary Revision approval workflows (Simple/Multi-Level/Custom)
  - DS-01 to DS-06: Design System — App shell, form components, table/navigation patterns, modal/drawer patterns, toast/notification patterns, color system and design tokens

---

### Pending

- **IT Declaration form (full)** — deferred from Employee module; requires complete employee profile + active pay run
- **Loans Settings (confirmed)** — loan types created in Loans module (Manage Loans dialog), NOT in Settings; Settings > Loans = custom fields/validation only
- **Giving module lifecycle** — campaign lifecycle (Created → Active → Completed) + employee opt-in flow not fully audited
- **Data Import/Export** — employee import detailed flow (step 2 onwards)
- **Operational Dashboard** — post-setup state with payroll data
- **MailHog verification** — confirm dev SMTP routing works with payslip notification triggers
- **Compliance Calendar** — CONFIRMED NOT PRESENT (`#/compliance-calendar` → 404). Statutory due dates not tracked in-app.
- **Date-gated flows** — UF-43, UF-44, UF-47: require June 2026 pay run (available from 01/06/2026); documented as stubs
- **Settings > Employees & Contractors** — contractor toggle + custom fields not navigated
- **Settings > Leave & Attendance** — separate module (Zoho People integration); not configured
- **Settings > Automations** — Workflow Rules, Actions, Schedules, Logs not navigated
- **Settings > Developer Data** — Connections, Incoming Webhooks, Data Backup not navigated
- **Settings > Organisation Profile** — Branding, Work Locations, Departments, Designations, Subscriptions not navigated in current sessions

---

## Key Architectural Discoveries (Cross-Session)

| Discovery | Source Session |
|-----------|---------------|
| Dual-track approval: admin-initiated = auto-approved; employee-initiated = approval queue | Approvals |
| Mid-month joiner NOT auto-prorated in Zoho — LOP is manual input | Pay Runs |
| "Payment Due" is a distinct state between Approval and Record Payment | Pay Runs |
| Fixed Allowance = CTC − all other components (residual invariant) | Employees + Pay Runs |
| PT is work-location scoped (not org-wide) | Compliance |
| Zoho does NOT integrate with TRACES API — Form 16 Part A is manual upload | Form 16 |
| Tax Deductor gate hard-blocks all Form 16 generation for FY | Form 16 |
| IT Declaration URL `tax_regime=with_exemptions` = Old Regime — v1 is New Regime only | Employees |
| No Aadhaar field in Zoho Payroll — our build has it (encrypted) | Employees |
| Reimbursement types must be pre-configured and assigned to salary structure before any claim | Approvals |
| Loan types/templates are mandatory prerequisite for loan creation | Approvals |
| EPF UAN field is conditionally shown — gated on EPF org-level configuration | Compliance |
| Daman & Diu appears twice in state list (legacy duplication) — PT compliance concern | Settings |
| Public domain email triggers automatic sender override to Zoho's mail service | Settings |
| ESI rates are fully hardcoded (0.75%/3.25%) — no UI configuration for rates | Statutory Components |
| LWF amounts are system-supplied (not user-editable) — admin can only Enable/Disable | Statutory Components |
| Statutory Bonus base = MAX(Minimum Wage, Basic+DA) — Zoho enforces Payment Bonus Act | Statutory Components |
| PT Deduction Cycle is state-mandated (Kerala=Half Yearly) — not configurable by admin | Statutory Components |
| Statutory Bonus rate lock: percentage can only change at FY start once associated with employees | Statutory Components |
| EPF "Override at employee level" checkbox enables per-employee PF rate customisation | Statutory Components |
| NO Compliance Calendar in Zoho Payroll — `#/compliance-calendar` → 404; statutory due dates not tracked in-app | Settings |
| IT Declaration + POI are LOCKED by default — admin must explicitly "Release" before employees can submit | Settings (Claims) |
| POI approved amounts process from March (default) — matches CBDT Q4 TDS recalculation advisory | Settings (Claims) |
| Contractor module disabled by default — must be enabled; shares employee role permissions when enabled | Settings (Employees) |
| Loan types are NOT in Settings — created from within Loans module (Manage Loans dialog) | Loans |
| Direct Deposit has 3 options: Zoho Payments ₹3/emp/run, ICICI (paid plan only), HSBC (trial OK) | Settings (Payments) |
| Salary Templates are pre-configured salary structures for bulk onboarding — not same as Salary Components | Settings |
| Automation uses Zoho Deluge scripting — cap: Custom Functions 1000/day, Webhooks 1000/day | Settings (Automation) |
| "Allow employees to switch tax regime" checkbox exists in IT Declaration + POI settings — Old Regime gateway | Settings (DEFERRED v1) |
| Taxes & Forms sidebar = TDS only (4 items: TDS Liabilities, Challans, Form 24Q, Form 16) — EPF/ESI/PT/LWF only in Reports | Taxes & Forms |
| TAN confirmed configured as MUMR12345A; "Tax Deductor not found" error is due to missing Deductor Name (person), not TAN | Tax Details (UF-95) |
| IT Declaration LOCKED = root cause of ₹0 TDS for all employees — employer liable under Section 201 | TDS (UF-32, UF-85) |
| Kerala PT Half-Yearly confirmed: deductions only in September and March pay runs | PT (UF-39, UF-79) |
| EPF "Included in Salary Structure" = employer contributions embedded in CTC (no additional employer cost above net pay) | EPF (UF-37) |
| Zoho Payroll has 3 RBAC roles: Admin (full), Manager (no org settings), Reimbursements+POI Reviewer (approvals only) | Users & Roles (UF-91) |
| Direct Deposit: ICICI Bank integration requires paid plan; HSBC available in trial; Zoho Payments ₹3/emp/run + 18% GST | Direct Deposits (UF-92) |
| Zoho integrations: People (LOP sync), Books (journal sync), Expense (reimbursements), Analytics (Beta) — none connected in demo | Integrations (UF-92) |
| Payslip PDF has 7 templates; Elegant = default; FnF has separate template; 3 letter templates (Salary Certificate, Revision Letter, Bonus Letter) | PDF Templates (UF-93) |
| Pay Run approval workflow configurable: Simple (any approver), Multi-Level (all must approve), Custom (criteria-based) | Approval Workflows (UF-96) |
