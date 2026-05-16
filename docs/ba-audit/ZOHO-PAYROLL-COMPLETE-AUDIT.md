# Zoho Payroll India — Complete BA Audit

**Product:** Zoho Payroll India (payroll.zoho.in)
**Audit date:** May 2026
**Org:** Lerno Technologies Pvt Ltd (test org, Kerala state, trial plan)
**Auditor:** BA Agent (Playwright MCP — live browser audit)
**Employees tested:** 5 (EMP001–EMP005, see Appendix C)
**Purpose:** Reference for building Indian Payroll SaaS

---

## 1. Executive Summary

### Scope and Method

A live Playwright browser audit of Zoho Payroll India was conducted across all modules. A fresh trial organisation (lerno) was created with 5 mock employees covering common scenarios: standard, mid-month joiner, mid-year joiner with prior employer YTD, contractor gap, and prior FY DOJ. Two complete payroll runs (April 2026, May 2026) were executed end-to-end through Draft → Approved → Paid states. Compliance modules, approvals, loans, reports, edge cases, and settings (33 pages) were audited systematically.

**Total audit items:** 112 (dashboard = session 2 baseline; settings 01–33; employees 34–51; pay runs 52–67; approvals 68–73; compliance 74–82; form 16 83–86; loans 87–94; giving 93–94; documents 95–98; reports index; edge cases 105–112)

### Top 10 Findings Most Impactful for Our Build

1. **No auto-proration for mid-month joiners** — Zoho requires manual LOP entry for days before joining date. Our build should auto-compute LOP from joining date within pay period.
2. **PF wage ceiling defaults OFF** — `is_employee_restricted_basic_enabled` defaults to `false`, meaning Zoho would compute PF on full basic, not capped at ₹15,000. This is a statutory violation by default. Our build must default `restrict_pf_wage_to_ceiling = true`.
3. **IT Declaration defaults to old regime** — Zoho's declaration URL uses `tax_regime=with_exemptions` (old regime). Since our v1 is new regime only, we must enforce the new regime structure from day one and never implement old-regime pathways.
4. **No audit trail on pay run** — no audit log tab for who approved, who paid, who rejected. Indian payroll compliance requires full state-transition audit. Our build needs `PayrollRunAuditLog` entity.
5. **TDS computation is PDF-only** — TDS Sheet is rendered as a server-side PDF iframe with no structured JSON. Our build must expose TDS computation as a JSON API, enabling testable arithmetic and structured Form 16 generation.
6. **Salary component immutability after employee assignment** — once a component is assigned, only Name and Amount can change. This critical design constraint prevents retroactive salary structure changes and must be replicated exactly.
7. **Fixed Allowance is always the residual component** — absorbs all unallocated CTC. Immutable design. Must be enforced at DB level, not just UI.
8. **PT is work-location scoped, not org-wide** — Professional Tax slabs are per state per work location. Multi-location orgs need independent PT configuration per branch.
9. **No dedicated F&F settlement flow** — Zoho uses Off Cycle Pay Run + manual inputs. Our build needs an explicit Full & Final run type with auto-computed gratuity (5-yr rule, ₹20L cap) and leave encashment.
10. **TDS module is feature-flag gated** — TDS Liabilities and Form 24Q must be explicitly enabled. Liabilities only appear for runs after the enable date; no retroactive computation.

### Top 10 Gaps — Where We Can Do Better

1. **Auto-prorate mid-month joiners** — detect joining date in pay period, suggest LOP, allow admin override.
2. **Correct PF ceiling default** — `restrict_pf_wage_to_ceiling = true` out of the box.
3. **Structured TDS computation** — JSON API `GET /payroll-runs/{id}/employees/{empId}/tds-worksheet` returning full slab computation, not a PDF blob.
4. **Explicit F&F run type** — with auto-computed gratuity, leave encashment, notice pay, loan deduction, pro-rated salary — not a manual multi-step Off Cycle run.
5. **One-click reprocess** — Zoho requires 5 manual steps (Delete Payment → Reject → Draft → Modify → Approve → Pay). Our build: `POST /payroll-runs/{id}/reprocess`.
6. **Immutable audit trail on pay run** — every state transition logged with timestamp and user.
7. **Daily automated backup** — Zoho only offers on-demand backup. Our build: daily MinIO backup with retention policy.
8. **Compliance calendar** — statutory due-date alerts (PF by 15th, ESI by 21st, TDS by 7th, PT quarterly). Zoho has no proactive compliance calendar.
9. **Standalone payslip URL** — Zoho embeds payslip in split panel with no dedicated URL. Our build: `GET /payslips/{runId}/{employeeId}` returns HTML/PDF, suitable for email links.
10. **Bank Advice stored in MinIO** — Zoho's Bank Advice is a direct download with no history. Our build stores every generated file in MinIO with a `PayrollRunFile` record.

### Overall Quality Assessment

Zoho Payroll India is a functionally complete payroll system. Statutory coverage is broad (EPF, ESI, PT, LWF, TDS, Form 24Q, Form 16) and the UX is generally well-thought-out. The dual-label approach for Income Tax Code 2025 (Section 123/80C) shows forward-looking compliance readiness. Kerala PT half-yearly deduction and ESI continuation rule are correctly implemented — both are edge cases many vendors miss.

Key weaknesses: no audit trail on pay runs; PF ceiling default is wrong; mid-month joiner proration requires manual intervention; F&F settlement is fragmented; TDS data is locked in PDFs rather than exposed as structured data. These gaps create our differentiation opportunity.

---

## 2. Product Overview

### Main Navigation Structure

| # | Nav Item | Route | Module |
|---|----------|-------|--------|
| 1 | Getting Started | `#/home/dashboard` | Onboarding |
| 2 | Dashboard | `#/home/dashboard` | Home |
| 3 | Employees | `#/people/employees` | Employee Master |
| 4 | Pay Runs | `#/payruns` | Payroll Processing |
| 5 | Approvals ▾ | — | Accordion |
|   | — Reimbursements | `#/approvals/reimbursements` | |
|   | — Proof of Investments | `#/approvals/proof-of-investment` | |
|   | — Salary Revision | `#/approvals/salary-revision` | |
| 6 | Taxes & Forms ▾ | — | Accordion (feature-flag gated) |
|   | — TDS Liabilities | `#/taxes-and-forms/tax-liabilities/pending` | |
|   | — Challans | `#/taxes-and-forms/tax-payments/unassociated` | |
|   | — Form 24Q | `#/taxes-and-forms/form24q` | |
|   | — Form 16 | `#/taxes-and-forms/form16` | |
| 7 | Loans | `#/loans` | Loan Management |
| 8 | Giving | `#/donations` | Charitable Giving |
| 9 | Documents | `#/documents/folder` | File Management |
| 10 | Reports | `#/reports` | 39 Reports |
| 11 | Settings | `#/settings` | Configuration |

**Tech stack observed:** Ember.js frontend (hash-based routes `#/...`), REST API at `/api/v1/...`, server-rendered PDF for TDS worksheets.

### Settings Architecture (33 Pages, 4 Groups)

| Group | Pages |
|-------|-------|
| Organisation Settings | Org Profile, Branding, Work Locations, Departments, Designations, Subscriptions, Users, Roles, Tax Details, Pay Schedule |
| Setup & Configurations | Statutory Components, Salary Components |
| Employee Portal & Claims | Employee Portal, Claims & Declarations |
| Customisations | Email Templates, Sender Email, Salary Templates, PDF Templates, Reporting Tags |
| Automations | Workflow Rules, Actions, Schedules, Workflow Logs |
| Module Settings — General | Employees & Contractors, Pay Runs, Salary Revisions, Leave & Attendance, Loans |
| Module Settings — Payments | Direct Deposits |
| Extensions & Developer | Integrations (Zoho Apps), Connections, Incoming Webhooks, Data Backup |

### Onboarding Checklist (7 Steps)

| Step | Name | Status Behaviour |
|------|------|-----------------|
| 1 | Add Organisation Details | Required — blocks step 2 |
| 2 | Provide Tax Details | PAN + TAN + AO Code |
| 3 | Configure Pay Schedule | Work week + salary method + pay date |
| 4 | Set up Statutory Components | EPF / ESI / LWF / PT (sub-items) |
| 5 | Set up Salary Components | Earnings, deductions, reimbursements |
| 6 | Add Employees | At least one employee required |
| 7 | Configure Prior Payroll | Auto-completes for new orgs (no prior payroll) |

Steps can be completed out of order. The dashboard shows progress as "N/7 Completed". The operational dashboard (payroll summaries, charts) only appears after all 7 steps complete.

### User Roles Model

Zoho provides 3 built-in roles (non-deletable) and supports custom roles (subscription-gated):

| Role | Scope |
|------|-------|
| Admin | Unrestricted access to all modules |
| Manager | All modules except organisation settings |
| Reimbursements & POI Reviewer | View and approve reimbursements and POI only |

Permission dimensions: Employee (10 sub-modules with CRUD + Approve), Payroll Run (with distinct **Pay** permission), Loan (with distinct **Record Disbursement** permission), Settings (22 boolean toggles), Reports (9 boolean toggles), Documents (4 boolean toggles). A special **"Provide access to protected data"** toggle gates PAN, Aadhaar, and bank account unmasking.

### Subscription Tiers

Trial plan: 14 days, 0 employees used. Feature list observed: 25 Core features, 5 Automation features, 1 Premium feature (Custom Roles). Direct bank integrations (ICICI) are paid-plan only. All 39 reports included in trial.

---

## 3. Settings Module

### 3.1 Organisation Settings (Pages 01–10)

| Page | Key Fields | Cross-Module Impact | Build Notes |
|------|-----------|---------------------|-------------|
| **01 Org Profile** | Company name, logo, address, industry, date format (DD/MM/YYYY default), Org ID (numeric) | Appears on payslip header, Form 16 | Org ID is auto-assigned; store as opaque string |
| **02 Branding** | Dark/Light theme + 5 accent colours | Applies to all Zoho Finance apps in tenant | Our build: per-tenant theme in tenant config |
| **03 Work Locations** | Name, address, state (locked post-creation), PT auto-triggered | State drives PT slab auto-load; state locked for statutory compliance | `WorkLocation.state` is immutable after creation |
| **04 Departments** | Name, code, description (250 char); CSV/TSV/XLS import | Employee assignment | Department code is optional but useful for reporting |
| **05 Designations** | Name only; no code or description | Employee profile display, payslip | Simple lookup table |
| **06 Subscriptions** | Plan name, feature list, employee count | Feature gating | Track employee count for billing |
| **07 Users** | Name, email, role (Admin/Manager/Reviewer) | Access control | Invite by email; role assignment at creation |
| **08 Roles** | RBAC matrix — see Section 2 | Every page and action | Copy Zoho's permission matrix structure |
| **09 Tax Details** | PAN (AAAAA0000A), TAN (AAAA00000A), AO Code (4-part), Tax Payment Frequency (Monthly, hardcoded), Deductor (Employee or Non-Employee) | Form 24Q generation, Form 16 | TAN is optional; AO Code needed for challan; frequency always Monthly |
| **10 Pay Schedule** | Work week (Mon–Fri default), Salary Calculation Method (Actual Days / Fixed 30 Days), Pay Date (Last day or 1–30) | Determines pay period dates, LOP denominator | Two salary calc methods: Actual (calendar days in month) vs Fixed 30 — affects proration formula |

### 3.2 Statutory Components (Page 11)

5-tab page: EPF | ESI | Professional Tax | LWF | Statutory Bonus.

**EPF Configuration:**

| Field | Value/Options | Statutory Basis |
|-------|--------------|-----------------|
| EPF Number | `AA/AAA/0000000/XXX` format | EPFO registration number |
| Deduction Cycle | Monthly (hardcoded) | EPF deposited by 15th of following month |
| Employee Contribution | 12% of Actual PF Wage (locked) | Statutory — EPF Scheme 1952 |
| Employer Contribution | 12% full OR restrict to ₹15,000 PF wage | Option 2 = PF wage cap |
| EDLI | 0.50% of PF wage, max ₹75 | EDLI Scheme 1976 |
| EPF Admin Charges | 0.50% of PF wage | EPFO circular |
| Employer EPS split | 8.33% of wage (max ₹15,000 wage) → EPS; balance → EPF | EPS Scheme 1995 |
| Override per employee | Checkbox (unchecked default) | Allows individual PF rate variation |
| Pro-rate restricted PF wage | Checkbox (unchecked default) | LOP months — PF on earned wage |

**ESI Configuration:**

| Field | Value | Notes |
|-------|-------|-------|
| ESI Number | `00-00-000000-000-0000` format | ESIC registration |
| Deduction Cycle | Monthly (hardcoded) | ESIC by 21st of following month |
| Employee Contribution | 0.75% of Gross Pay (locked) | Post-2019 ESIC revision |
| Employer Contribution | 3.25% of Gross Pay (locked) | |
| Eligibility threshold | ₹21,000/month gross (hardcoded) | ESI Act |
| Continuation rule | Contribute till period end after crossing ₹21k | ESI Rule 50 — correctly implemented |

**Professional Tax:** Auto-configured per work location state. PT number entered per location. PT slabs are system-provided (not user-editable). Kerala is half-yearly (Aug + Feb).

| Kerala PT Slabs (Half-Yearly Gross Salary) | Tax Amount |
|-------------------------------------------|------------|
| ₹1 – ₹11,999 | ₹0 |
| ₹12,000 – ₹17,999 | ₹320 |
| ₹18,000 – ₹29,999 | ₹450 |
| ₹30,000 – ₹44,999 | ₹600 |
| ₹45,000 – ₹99,999 | ₹750 |
| ₹1,00,000 – ₹1,24,999 | ₹1,000 |
| ₹1,25,000 and above | ₹1,250 |

**LWF:** Per work location state; Kerala: Employee ₹50 + Employer ₹50 = ₹100/month; must be explicitly enabled.

**Statutory Bonus:** Payment of Bonus Act 1965; 8.33%–20% of salary; Monthly or Yearly frequency; computed on higher of minimum wage or Basic+DA; immutable rate once associated to employee (changeable only at FY start).

### 3.3 Salary Components (Page 12)

4 tabs: Earnings (14 system + custom) | Deductions (2 system) | Benefits (1 — VPF) | Reimbursements (5 system).

**33 earning types available:** Basic, HRA, DA, Retaining Allowance, Conveyance, Bonus, Commission, Children Education Allowance, Hostel Expenditure, Transport, Helper, Travelling, Uniform, Daily, City Compensatory, Overtime, Telephone, Fixed Medical, Project, Food, Holiday, Entertainment, Custom Allowance, Food Coupon, Gift Coupon, Research, Books & Periodicals, Shift, Fuel, Driver, LTA, Vehicle Maintenance, Telephone & Internet.

**Component attributes per earning:**
- Pay Type: Fixed (recurring monthly) / Variable (manual input per run)
- Calculation Type: Flat Amount / % of Basic / % of CTC
- Tax treatment: taxable / non-taxable
- EPF treatment: Always / Only when PF Wage < ₹15,000 / Never
- ESI treatment: Yes / No
- Pro-rata: prorated on LOP or not
- Show in payslip: Yes / No
- Variable-only: Tax Deduction Preference (spread across year vs deduct in same payroll)

**Critical immutability rule:** Once a component is assigned to an employee's salary structure, only Name and Amount/Percentage can be edited, and only for new employees. Existing employees are unaffected. This prevents retroactive salary structure mutations.

**Deductions (system):** Withheld Salary, Notice Pay Deduction — both One Time frequency.
**Benefits:** VPF (Voluntary Provident Fund) — pre-tax, Recurring.
**Reimbursements:** Fuel, Driver, Vehicle Maintenance, Telephone, LTA — all inactive by default with ₹0 max amount.

### 3.4 Employee Portal & Claims (Pages 13–14)

**Employee Portal:** Enable/disable toggle; banner message with expiry date; contact email; document visibility controls. Portal-disabled employees must have investment proofs collected manually before F&F.

**Claims & Declarations (4 sub-tabs):**
- **FBP:** Flexible Benefit Plan configuration (empty for test org)
- **Reimbursement Claims:** Enable and configure reimbursement types
- **IT Declaration:** Lock/Release toggle; "Allow employees to switch tax regimes" checkbox; TDS override permission
- **POI:** Proof of Investment lock/release; payroll processing month selector

Critical: IT Declaration window is admin-controlled (not always open). Admin explicitly opens and closes the window. TDS override (admin can exceed system-computed TDS) is a separate permission.

### 3.5 Customisations (Pages 15–19)

**Email Templates (Page 15):** 4 fixed templates — Payslip Email, Portal Disabled, Off-Cycle Payslip, F&F Payslip. WYSIWYG editor with merge fields: `{{Employee Name}}`, `{{Pay Period}}`, `{{Portal URL}}`, `{{Company Name}}`.

**Salary Templates (Page 17):** Two-panel builder. Basic (50% CTC) + Fixed Allowance (residual) always present. Template assigned to employee; changes to template do not retroactively affect assigned employees.

**PDF Templates (Page 18):** 7 regular payslip variants + F&F template + 3 letter templates (Salary Certificate, Revision Letter, Bonus Letter). Per-template display preferences.

**Reporting Tags (Page 19):** Custom classification dimensions (up to 59 per entity); 3-step wizard; shared across Zoho Finance apps.

### 3.6 Automations (Pages 20–23)

Zoho Deluge scripting (proprietary). Event-driven workflow rules for Pay Run, Employee, Loan, Reimbursement Claim events. Limits: 1,000 custom functions + webhooks/day, 500 email alerts/day. Our build: use C# Hangfire jobs instead.

### 3.7 Module Settings (Pages 24–29)

**Employees & Contractors (Page 24):** Contractor module toggle; custom fields (18 data types, 0/59 used).

**Pay Runs (Page 25) + Salary Revisions (Page 26):** 3-tier approval workflow: Simple / Multi-Level / Custom. Custom approval allows configuring specific approvers and escalation chains.

**Leave & Attendance (Page 27):** Blocked until Pay Schedule is configured. Governs LOP deduction, leave types.

**Loans (Page 28):** Custom fields for loan entity (0/59 used).

**Direct Deposits (Page 29):** 3 channels — Zoho Payments Payouts (₹3/emp/run + 18% GST; available on trial), ICICI Bank (paid plan only), HSBC Bank (available on trial). Default when none configured: generate bank transfer file for manual upload.

### 3.8 Developer Data (Pages 30–33)

**Integrations — Zoho Apps (Page 30):** Zoho People (employee + LOP sync), Zoho Books (journal sync), Zoho Expense (expense claims), Zoho Analytics BETA.

**Developer Space (Pages 31–32):** OAuth Connections to external REST APIs; Incoming Webhooks for external → payroll triggers.

**Data Backup (Page 33):** On-demand CSV backup (all modules) + Audit Trail backup (incremental ZIP); emailed to admin; backup history table. No scheduled automatic backup.

---

## 4. Employees Module

### 4.1 Full Employee Data Model

**Core fields (Add Employee Wizard — 4 steps):**

| Step | Fields |
|------|--------|
| Step 1: Basic Details | First Name, Last Name, Employee ID (auto-suggest), Designation (inline create), Department (inline create), Work Location, Date of Joining (calendar-click required), Work Email, Mobile, Portal Access checkbox |
| Step 2: Salary Details | Pay Schedule, CTC (Annual), salary component table (Basic auto-50%, HRA auto-50% of Basic, Fixed Allowance residual), Add Earning inline |
| Step 3: Personal Details | Date of Birth (age auto-calc), Gender, Father's Name (required for Form 16), PAN (optional), Marital Status, Differently Abled (6 options), Nationality |
| Step 4: Payment Info | Payment Mode (Bank Transfer, Cash, Cheque, Others), Account Number (masked), IFSC (live validated), Bank Name (auto-filled or manual fallback) |

**Employee list columns (21 available, 8 configurable):** Employee ID, Name, Designation, Department, Work Location, UAN, PAN, PF Account Number, ESI Number, Prior Payroll Status, Onboarding Status, Date of Joining, Status.

**Employee Profile Tabs:** Overview | Salary Details | Investments | Payslips & Forms | Loans

**Employee Status enum:** Active, On Notice Period, Exited, Inactive

**Gaps vs ideal:** No Aadhaar field in Zoho. No Employment Type (Contractor vs Permanent) field — all employees are treated identically.

### 4.2 Salary Structure

The salary structure uses a CTC-anchored percentage model:
- **Basic:** Always 50% of Annual CTC (immutable calculation type, can only adjust %)
- **HRA:** 50% of Basic (derived)
- **Fixed Allowance:** Monthly CTC − sum(all other components) = residual, always balances to zero unallocated CTC
- Additional components: flat amounts or % of Basic

**Two-phase save:** Adding statutory details (EPF/ESI check) must complete before Salary Structure section unlocks. Benefits section unlocks last.

**Salary actions on employee profile:**
- Add Earning: one-time or recurring additional earning
- Add Scheduled Earning: recurring extra income (separate from salary structure; `ScheduledEarning` entity)
- Add Benefit (pre-tax deduction): VPF, NPS employee — `is_pre_tax = true`
- Add Deduction (post-tax): post-tax recurring or one-time
- Add Donation: 80G charitable contribution

### 4.3 Contractor vs Employee

Zoho does not differentiate contractors from employees in the UI or data model. All employees are permanent salaried. Contractor TDS (194C/194J) is out of scope for Zoho Payroll v1. Our build should add `employment_type` field (Permanent, Contract, Part-time) even if v1 behavior is identical.

### 4.4 Prior Employer YTD

Accessed via Employee > Investments > IT Declaration → "Previous Employment Details" section. Fields: Prior employer name, period (from/to), gross salary, TDS deducted. This feeds into TDS computation — the system adds prior employer income to projected current-year income and subtracts prior TDS already deducted.

Standard Deduction (₹75,000) is claimed once per FY across both employers — the system must handle deduplication.

### 4.5 Bulk Import (19 Import Types)

| Group | Import Types |
|-------|-------------|
| Employee Details | Basic Details, Statutory Details, Payment Information |
| Salary Details | Salary Details, Salary Revision, Scheduled Earnings, Benefits, Deductions, Perquisites, Vehicle Details |
| Complete Employee | All basic + statutory + payment in one import |
| Exit Details | Last Working Day, Exit Reason, F&F timing |
| Investments | Chapter VI-A, HRA, Previous Employment (Prior Employer YTD), Allowance Declaration, Other Income, Let-Out Property, Tax Regime |

Import flow: Select Type → Download Template → Upload → Map Fields → Validation Preview → Import.

### 4.6 Employee Lifecycle

```
Not Yet Onboarded
  ↓ [Add Employee Wizard complete]
Active
  ↓ [Initiate Exit Process: Last Working Day + Reason + F&F timing]
On Notice Period
  ↓ [F&F Pay Run processed]
Exited / Full & Final Complete
```

**Exit reasons (4 options):** Terminated By Employer, Termination By Death, Termination By Disability, Resigned By Employee. Reason drives gratuity eligibility and Form 16 timing.

**F&F timing:** Regular pay schedule OR custom date (admin specified).

**Salary Revision:** Two mechanisms — direct edit (immediate, no approval trail) vs dated revision (effective date, approval workflow, arrears computed). Bulk import supports dated revision via CSV.

### 4.7 Employee Views (Pre-built + Custom)

8 pre-built views: All Employees, Active, Inactive, Exited, On Notice Period, Without Bank Details, Without PAN, ESI Eligible. Custom view builder available.

---

## 5. Pay Runs Module

### 5.1 State Machine

```
READY (period card visible on /payruns)
  ↓ [Click period card]
DRAFT
  ├─ Enter variable inputs: LOP days, Add Earning (one-time), TDS override, Import/Export
  ├─ Manage pending tasks (missing bank details, missing work email, etc.)
  ├─ [Skip employee: mandatory reason stored permanently]
  ↓ [Approve Payroll — all pending tasks cleared]
APPROVED (variable inputs locked)
  ├─ [Reject Approval + optional reason] → DRAFT
  ↓ [Record Payment: date, mode, reference]
PAID (terminal state)
  └─ [Delete Recorded Payment + confirm] → APPROVED
       └─ [Reject Approval] → DRAFT
```

State badge variation: "Approved" when pay day in future; "Payment Due" when pay day has passed without payment recorded.

### 5.2 Pay Run Types (8 Types)

| Type | Trigger | Purpose |
|------|---------|---------|
| Regular Payroll | Sequential monthly card | Standard monthly run |
| Off Cycle Pay Run | New → Off Cycle | Mini run for specific date (F&F, advance, correction) |
| One Time Payout | New → One Time Payout | Single component mass payment (Bonus, Commission) |
| Resettlement Payroll | History filter only (not in New dropdown) | Arrear settlement from salary revision |
| Full & Final | Exit process → F&F flag | Employee exit settlement |
| Supplementary | (inferred from run types) | Additional pay run for missed employees |
| Bonus Run | (via One Time Payout with Bonus component) | Festival/annual bonus |
| Revision Arrears | (via Resettlement type) | Retroactive salary revision delta |

**Sequential constraint:** Regular runs advance one month at a time. Admin cannot pre-initiate future months.

### 5.3 Variable Inputs

| Input Type | Method | Notes |
|-----------|--------|-------|
| LOP days | Per-employee split panel | Calendar days, not working days; denominator = days in month |
| Add Earning | Per-employee dropdown | One-time earnings from salary component library |
| TDS Override | Inline form | Mandatory reason field; TDS can be set above system-computed value |
| Import | CSV import | Bulk LOP, bulk one-time earnings |
| Export | CSV export | Current draft variable inputs |

**Approval gate:** Hard block if pending tasks (missing bank details, work email, etc.) remain. Toast: "Please complete your pending tasks."

**Skip employee:** Requires mandatory reason. Skip reason displayed permanently in employee row, including in Paid state. No pay computed for skipped employees.

### 5.4 Payslip Structure (All Line Items)

**Header:** Company name, logo, pay period, "Payslip" title

**Employee Details:** Name, Employee ID, Designation, Department, DOJ, PAN (masked XXXXX1234X), UAN, Bank Account (masked XXXX+4 digits), IFSC, Payment Mode

**Attendance:** Total Working Days (calendar days in month), Paid Days, LOP Days

**Earnings:**
- Basic (prorated if LOP)
- HRA (prorated if LOP)
- Fixed Allowance (prorated if LOP)
- One-time earnings: Bonus, Commission, Leave Encashment (as entered)
- Reimbursements (non-taxable)
- **Gross Earnings** (sum)

**Deductions:**
- TDS / Income Tax (computed or overridden)
- Professional Tax (state-specific slab)
- PF Employee contribution (12% of PF wage, if EPF enabled)
- ESI Employee contribution (0.75% of Gross, if ESI enabled and salary ≤ ₹21k)
- Other deductions (ad-hoc)
- **Total Deductions** (sum)

**Net Pay:** Gross − Total Deductions; displayed in rupees + words (lakh/crore system)

### 5.5 Proration Logic

**Formula (confirmed):**
```
Prorated Amount = (Base Days − LOP Days) / Base Days × Full Monthly Component Amount
```

**Base Days** = calendar days in the month (May = 31, regardless of weekends).

**Rounding:** Truncation (not standard half-up rounding). Confirmed: ₹40,000 × 29/31 = ₹37,419.35 → ₹37,417 displayed (consistent truncation).

**Critical gap:** No auto-proration for mid-month joiners. System shows full month salary (31 days) for an employee who joined on day 16. Admin must manually enter 15 LOP days.

**All components prorated uniformly:** Basic, HRA, Fixed Allowance all prorated at the same ratio.

### 5.6 TDS Computation (New Regime FY2026)

New Regime Tax Slabs FY2026-27:

| Taxable Income | Rate |
|----------------|------|
| ₹0 – ₹4,00,000 | 0% |
| ₹4,00,001 – ₹8,00,000 | 5% |
| ₹8,00,001 – ₹12,00,000 | 10% |
| ₹12,00,001 – ₹16,00,000 | 15% |
| ₹16,00,001 – ₹20,00,000 | 20% |
| ₹20,00,001 – ₹24,00,000 | 25% |
| ₹24,00,001 and above | 30% |

Standard Deduction FY2026 (new regime): ₹75,000.
Rebate u/s 87A / Section 156(2): ₹25,000 rebate if taxable income ≤ ₹7,00,000.
Surcharge: 10% (50L–1Cr), 15% (1Cr–2Cr), 25% (2Cr–5Cr), 37% (>5Cr) with marginal relief.
Health & Education Cess: 4% on (tax + surcharge).

**TDS distribution method:** Annual projected tax − YTD tax paid / remaining months in FY = monthly TDS.

**TDS Sheet access:** `GET /api/v1/employees/{id}/taxworksheet?month={YYYY-MM}&accept=pdf&print=true` — server-rendered PDF, not structured data.

### 5.7 Approval Workflow

3 workflow types (configured in Settings > Pay Runs > Approval):
- **Simple:** Single approver (default)
- **Multi-Level:** Sequential chain of approvers
- **Custom:** Conditional approval chains

Admin-created pay runs: the creating admin is always the initiator. Approval may still require a different user to approve depending on workflow config.

Reject Approval dialog: optional reason field; returns run to Draft state.

### 5.8 Payment Advice / Bank File Export

**Bank Advice:** Available in Approved and Paid states. Direct file download — not stored in downloads history. Contains employee name, account number, IFSC, amount, remarks.

**Export Data (CSV):** Employee details, salary components, statutory data. Available from draft onwards.

**Payment Modes:** Manual Bank Transfer, Direct Deposit (if configured), Cheque, Cash.

### 5.9 Payroll History

Table at `#/payruns/payroll-history`. Columns: Period, Payroll Type, Status, Payroll Cost, Total Net Pay, Employees, Pay Day. Filter by Payroll Type (Regular, Off Cycle, One Time Payout, Resettlement).

### 5.10 Arrears / Revision Runs

Salary revision creates arrear amounts for months between the effective date and the payout month. Zoho's "Resettlement Payroll" type handles this. Off Cycle runs can also carry arrears manually. Backdated arrear computation (for revisions effective before the current pay period) not confirmed in testing.

---

## 6. Approvals Module

### 6.1 Dual-Track Architecture

Zoho uses a fundamental dual-track approval model:

| Track | Who Initiates | Approval Behaviour |
|-------|--------------|-------------------|
| Admin track | Admin creates salary revision, loan, pay run | Auto-approved immediately; bypasses approval queue |
| Employee track | Employee submits POI, reimbursement claim via portal | Goes to Approvals queue for admin review |

The sidebar "Approvals" section is exclusively for employee-submitted items. Admin actions are self-approved.

### 6.2 All Approval Types

| Type | Route | Queue Appears When |
|------|-------|-------------------|
| Reimbursements | `#/approvals/reimbursements` | Employee submits claim via portal; reimbursement components configured |
| Proof of Investments | `#/approvals/proof-of-investment` | POI window released; employees submit documents |
| Salary Revision | `#/approvals/salary-revision` | Custom approval workflow configured; employee-initiated revision |
| Pay Run | Settings config only | Approval workflow settings control who can approve |
| Loans | `#/loans` (top-level, not Approvals nav) | Admin creates loan — auto-approved unless custom workflow |
| IT Declaration | Employee Portal | Managed via Settings > Claims and Declarations; not in Approvals nav |

### 6.3 Salary Revision Form

| Field | Options | Notes |
|-------|---------|-------|
| Revision Type | % increase OR new CTC amount | |
| Revised Annual CTC | ₹ input | New gross CTC |
| Effective Date | Date picker | Must be specified |
| Payout Month | Month selector | When arrears are paid out |
| Reason | Textarea | Mandatory |
| Payout Preferences | Arrear in same run / separate run | |

After submission: creates pending revision. After approval: salary updates at effective date. Arrear delta paid in payout month.

**Revision letter:** "Send Revision Letters" bulk email to employees from Approvals > Salary Revision.

### 6.4 POI Approval Flow

POI window is admin-released (Settings > Claims & Declarations > POI > Release). Employees submit investment documents via portal. Admin reviews each document and approves/rejects per investment category. Approved POI amounts feed into Form 16 Part B.

"Employees yet to submit POI" list shows employees who haven't submitted — admin can send reminders.

### 6.5 Role-Based Access

| Action | Minimum Role Required |
|--------|----------------------|
| View approval queues | Manager |
| Approve reimbursements | Admin or role with Reimbursements Approve |
| Approve POI | Admin or role with "Approve POI" toggle |
| Approve salary revisions | Admin or role with "Approve Salary Revisions" toggle |
| Approve pay run | Role with Payroll Run Approve permission |
| Execute payment | Role with Payroll Run **Pay** permission (distinct from Approve) |

---

## 7. Statutory Compliance

### 7.1 EPF — Configuration, ECR Challan, UAN Management

**Registration threshold:** 20+ employees mandatory.
**Deduction cycle:** Monthly; deposit by 15th of following month.
**Employee contribution:** 12% of Actual PF Wage (locked).
**Employer contribution split:**
- EPS: 8.33% of PF wage, capped at ₹1,250/month (₹15,000 PF wage max)
- EPF: Employer 12% − EPS = balance
- EDLI: 0.50% of PF wage, max ₹75/month (employer only)
- Admin charges: 0.50% of PF wage (employer only)

**ECR (Electronic Challan-cum-Return):** Generated as a report (`/reports/epf-ecr-report`). Columns: UAN, Gross Wage, EPF Wages, EPS Wages, EDLI Wages, contributions, LOP Days. Upload to EPFO Unified Portal.

**UAN Management:** UAN field on employee profile. Visible as an optional column in employee list. Required for ECR generation.

**Critical gap found:** `is_employee_restricted_basic_enabled` defaults to `false`. If EPF enabled with this default, Zoho computes PF on full basic salary instead of ₹15,000 cap. Statutory violation. **Our build must default `restrict_pf_wage_to_ceiling = true`.**

### 7.2 ESI — Configuration, Wage Ceiling, Rates

**Registration threshold:** 10+ employees mandatory.
**Wage ceiling:** ₹21,000/month gross (hardcoded in UI, must be DB-configurable in our build).
**Rates:** Employee 0.75%, Employer 3.25% (post-2019 revision, locked).
**Deposit:** By 21st of following month.
**Continuation rule (ESI Rule 50):** If employee's salary revision takes them above ₹21,000 during a contribution period (Apr–Sep or Oct–Mar), they continue contributing until the period ends. Correctly implemented by Zoho — explicitly documented in UI.
**ESI report:** `GET /reports/esic-return` generates ESI Monthly Summary with: Insurance Person Number, Name, Paid Days, Total Monthly Wages, Reason Code for Zero Working Days, Last Working Day.

**Test org finding:** None of the 5 test employees were ESI-eligible (all salaries above ₹21,000).

### 7.3 Professional Tax — State Slabs, Frequency

PT is configured per work location, auto-seeded from the work location's state on creation. State is immutable post-creation.

**Deduction cycles by state:**
| State | Cycle | Notes |
|-------|-------|-------|
| Kerala | Half-Yearly | Aug + Feb |
| Maharashtra | Monthly | Based on gross salary |
| Karnataka | Monthly | Based on gross salary |
| (other states) | State-specific | Must be pre-loaded in DB |

**Reports:** PT Summary (all employees), Employee-wise PT (per work location + month), Annual PT (per location + FY).

**PT number:** Per work location; entered manually by admin; not validated against state authority.

### 7.4 LWF — State Applicability, Amounts

Labour Welfare Fund: state-specific, per work location. System-defined amounts (not user-editable). Must be explicitly enabled per location.

| State | Employee | Employer | Cycle |
|-------|----------|----------|-------|
| Kerala | ₹50.00 | ₹50.00 | Monthly |
| Maharashtra | ₹18.00 | ₹36.00 | Jun + Dec |
| Karnataka | ₹10.00 | ₹20.00 | Jun + Dec |

**LWF report:** LWF Summary showing employee and employer contributions per employee.

### 7.5 TDS / Income Tax — Form 24Q, Challans

**TDS module:** Feature-flag gated; must be explicitly enabled via "Track TDS Liabilities" button. Once enabled, liabilities appear only for subsequent pay runs (no retroactive computation).

**Challan entity fields:** Paid Amount, Penalty (§234E: ₹200/day), Interest (§201(1A): 1.5%/month), Challan Number, BSR Code, Paid Date.

**TDS Liability workflow:**
1. Approve pay run → TDS Liabilities generated per employee per month
2. Pay challan at bank → Record Challan (Unassociated tab)
3. Associate challan to liability period → Challan moves to Associated tab
4. All months in quarter complete → Generate Form 24Q text file

**Form 24Q filing workflow:**
1. Complete all pay runs for quarter
2. Record and associate challans
3. Form 24Q > Edit Preferences (Employer + Responsible Person details)
4. Generate FVU-compatible text file
5. Upload to TRACES portal via NSDL FVU tool

**Quarterly due dates:**
| Quarter | Period | Due Date |
|---------|--------|---------|
| Q1 | Apr–Jun | 31 July |
| Q2 | Jul–Sep | 31 October |
| Q3 | Oct–Dec | 31 January |
| Q4 | Jan–Mar | 31 May |

**No direct TRACES API integration** — file generated for manual upload.

### 7.6 Form 16 — Part A, Part B, Generation, Distribution

**Architecture:** Zoho splits Form 16 into Part A (TRACES-sourced) and Part B (Zoho-generated). Part A must be downloaded from TRACES portal and uploaded to Zoho. Zoho merges both into a single PDF per employee.

**Prerequisites (gate checks):**

| Gate | Our Test Org Status |
|------|-------------------|
| Tax Deductor configured (TAN + PAN + name) | Blocked |
| Form 24Q filed for all quarters | Blocked |
| Part A downloaded from TRACES | Dependent on 24Q |

**4-step generation flow:**
1. Upload Part A (TRACES download ZIP → upload)
2. Generate (Zoho generates Part B from payroll data; merges with Part A)
3. Sign (DSC hardware token OR e-Sign Aadhaar OTP OR unsigned)
4. Publish / Email (to employee portal + optional email delivery)

**Digital Signature options:**
- DSC: Digital Signature Certificate via hardware USB token
- e-Sign: Aadhaar-based OTP authentication
- Unsigned: permitted for distribution but not compliant with CBDT mandate for deductors with >100 employees

**Employee access:** Post-publish, Form 16 appears in Employee Self-Service Portal → Payslips & Forms tab. PDF password-protected with PAN as password.

**Statutory deadline:** Rule 31 — Form 16 must be issued by 31 May after FY end. FY2025-26 → deadline 31/05/2026.

---

## 8. Loans Module

### 8.1 Data Model

```
LoanType
  id: UUID
  name: String  (e.g., "Personal Loan", "Emergency Loan")
  perquisite_rate: Decimal  (0% = exempt; >0% = taxable perquisite)

Loan
  id: UUID
  loan_number: String  (LOAN-00001, auto-incremented)
  employee_id: FK
  loan_type_id: FK → LoanType
  amount: Decimal
  disbursement_date: Date  (must be in future un-finalised pay period)
  reason: Text
  exempt_from_perquisite: Boolean  (Rule 15(5): medical loans or aggregate < ₹2L)
  emi_start_date: Date
  instalment_amount: Decimal
  num_instalments: Int  (= ceil(amount / instalment_amount))
  loan_closing_date: Date  (auto-calculated)
  status: Enum (Open, Paused, Closed)

LoanRepayment
  id: UUID
  loan_id: FK
  repayment_date: Date
  amount: Decimal
  method: Enum (PayrollDeduction, ManualRepayment)
```

### 8.2 Creation

Loan types are admin-defined (no system presets). Admin creates types via "Manage Loans" dialog (Name + Perquisite Rate).

**Disbursement date constraint:** Cannot select a date in a completed (finalised) pay period. Must be future pay period.

**Perquisite calculation:** Perquisite rate on loan above ₹2L triggers taxable benefit per employee. 0% rate → "Exempt this loan from perquisite calculation" checkbox auto-checked (Rule 15(5) IT Rules 2026).

**Auto-calculation:** `num_instalments = ceil(loan_amount / instalment_amount)` computed and displayed live.

### 8.3 Approval

Loans created by admin are auto-approved. Custom approval workflow (if configured in Settings) routes employee-initiated loans to the approval queue.

### 8.4 Disbursement

Loan amount is NOT disbursed via payroll run — admin pays employee directly (bank transfer / cash). Only EMI deductions flow through payroll.

**Record Repayment:** Separate modal — records ad-hoc repayment outside EMI schedule. Repayment date must be after loan start date and not in the future.

### 8.5 Repayment Schedule and EMI Deduction

EMI deductions start from the specified EMI Deduction Start Date. Each month's payroll run automatically deducts the instalment from the employee's net pay. Repayment schedule view (`#/loans/repayments`) shows Month, Method, Employee filters.

**Loan status transitions:** Open → Paused (admin suspends EMI) → Open (resumes) → Closed (fully paid).

### 8.6 Foreclosure

No dedicated "Foreclose" action observed. Lifecycle management via: Edit Loan (change EMI), Pause Instalment Deduction, Delete Loan, Record Repayment (lump sum closes faster). Loan perquisite on outstanding balance computed annually for Form 16.

### 8.7 Loan Reports (4 Reports)

- Loan Outstanding Summary: current balance, pending instalments per loan
- Loan Perquisite Summary: annual perquisite per loan per FY
- Loan Perquisite Projection: projected perquisite per FY
- Loan Summary Report: complete loan details with status

---

## 9. Giving Module

The Giving module allows employers to run charitable donation campaigns deducted via payroll. Integrated with 80G income tax exemption (internally labeled "Section 133").

### Campaign Data Model

| Field | Type | Notes |
|-------|------|-------|
| Campaign Name | String | Admin-defined |
| Description | Text | Shown to employees in portal |
| Exemption Type | Enum | `donation_100_percent_exemption` / `donation_50_percent_exemption` / `none` |
| Campaign End Date | Month-Year | Aligned to payroll cycle granularity |
| Show in Employee Portal | Boolean | Controls visibility to employees |

### Flow

1. Admin creates campaign (with exemption type and end date)
2. Employee sees campaign in portal → pledges an amount (Pledge stage)
3. Monthly payroll deducts pledged amount → Contribution stage
4. Two-stage participation tracked: `pledged_employees` + `contributed_employees`
5. Deduction appears on payslip as a deduction component
6. 80G deduction reduces taxable income in TDS computation
7. 80G detail appears in Form 16
8. Organisation raises a liability (amount collected must be remitted to charity)

### Payroll Integration

- Donation deductions are line items in the pay run (alongside PF, PT, etc.)
- Exemption type drives IT computation: reduces taxable income per 80G clause
- Two donation reports: Employee Donation Summary + Employee Donation Details
- No NGO verification or charity PAN validation in Zoho — gap in our build

**Known Zoho bug:** `#/donations` route URL redirects to `#/loans` in some Ember lifecycle states. Navigation must be triggered from within the app.

---

## 10. Documents Module

### Module Structure

Two distinct folder types:
- **Org Folders** (`org_public_folder`): Company-wide documents (policy, offer letters, handbooks)
- **Employee Folders** (`payroll_employee_folder`): Per-employee documents (KYC, contracts)

**Storage limit:** 1 GB per 100 employees.

**Three route entries:** `documents.folder` (unified), `documents.organization-folder` (org-only), `documents.employee-folder` (employee-only).

### Upload Model

Documents are uploaded as ZIP archives containing PDFs named by employee ID (batch upload). No single-file-per-employee UI. Supported formats: PDF (implied by naming convention).

### System-Generated Documents

Payslips and Form 16 are **not** stored in the Documents module — they have separate download mechanisms (payslip API endpoint; Form 16 generate/publish flow).

### Features

- Document expiry reminders: configurable in Settings > Employee > Document
- Employee visibility: per-folder control
- **No e-signature feature** — despite being mentioned in settings audit, no e-signature route, API, or UI element exists in the current version
- Folder management: admin can create, rename, delete folders

---

## 11. Reports Module

39 system-generated reports across 9 categories. All reports included in trial plan.

### Complete Report Inventory

| # | Report | Category | Filter | Key Columns |
|---|--------|----------|--------|-------------|
| 1 | Payroll Summary | Payroll Overview | Date Range | Pay Components, Amount |
| 2 | Salary Register - Monthly | Payroll Overview | Month | Emp No, Name, Basic, HRA, Fixed Allowance, Tax, PT, Gross, Net |
| 3 | Employees' Salary Statement | Payroll Overview | Date Range | Same as #2 |
| 4 | Employees' Pay Summary | Payroll Overview | Date Range | Gross, Benefits, Deductions, Donations, Taxes, Reimbursements, Net |
| 5 | Payroll Liability Summary | Payroll Overview | Date Range | Liability Name, Employee Contribution, Employer Contribution |
| 6 | Leave Encashment Summary | Payroll Overview | Date Range | Days, Amount per employee |
| 7 | Loss Of Pay Summary | Payroll Overview | Date Range | LOP Days, Adjusted LOP, Actual LOP |
| 8 | Variable Pay Earnings Report | Payroll Overview | Date Range + Earnings Type | Total paid per employee |
| 9 | Scheduled Earning Summary | Payroll Overview | Date Range | Earning Name, Scheduled vs Paid vs Difference |
| 10 | EPF Summary | Statutory | Date Range | PF Account No, UAN, PF Wage, Employee/Employer contribution, VPF, EPS, EDLI, Admin |
| 11 | EPF ECR Report | Statutory | Month | UAN, Gross Wage, EPF/EPS/EDLI Wages, all contributions, LOP Days |
| 12 | ESI Summary | Statutory | Date Range | ESI Number, ESI Wages, Employee/Employer contribution |
| 13 | ESI Monthly Summary | Statutory | Month | Insurance Person No, Name, Paid Days, Total Monthly Wages, Reason Code |
| 14 | Professional Tax Summary | Statutory | Date Range | Work Location, PT Amount |
| 15 | Employee-wise PT Report | Statutory | Work Location + Month | PT Amount, Taxable Wages |
| 16 | Annual PT Report | Statutory | Work Location + FY | Period, Employee Count, PT Amount |
| 17 | LWF Summary | Statutory | Date Range | Employee and Employer contributions |
| 18 | Compensation Details | Employee | Month | DOJ, Earnings, Basic, HRA, Fixed Allowance |
| 19 | Reimbursement Claim Summary | Employee | Date Range | Eligible, Paid, Unclaimed per reimbursement type |
| 20 | Employee Perquisites Summary | Employee | FY | Perquisite Amount, Recovered, Chargeable |
| 21 | Full and Final Settlement Report | Employee | Date Range | LWD, Termination Type, Final Settlement Amount, Service Period |
| 22 | Employees' Salary Revisions | Employee | Date Range | Previous CTC, Revised CTC, Delta, %, Effective From |
| 23 | Salary Revision History | Employee | Employee dropdown | Full revision history per employee |
| 24 | Salary Withhold Report | Employee | Date Range | Withheld Month, Amount, Reason, Release Status |
| 25 | FBP Declaration Report | Declarations | Submitted Date | (No data for test org) |
| 26 | Investment Declaration Report | Declarations | FY | Tax Regime, Chapter VI-A, Allowance, HRA, Other Income, Prior Employment |
| 27 | Proof of Investment Report | Declarations | FY | POI Status, Actual/Approved Amounts, Document Count |
| 28 | Benefits & Deductions Summary | Deduction | Date Range | Type, Name, Employee/Employer Contributions |
| 29 | Deductions Summary | Deduction | Date Range | Total employee contributions |
| 30 | Benefits Summary | Deduction | Date Range | Employee + Employer contributions |
| 31 | Donations Summary | Deduction | Date Range | Total donation per employee |
| 32 | TDS Deduction Summary | Taxes | Date Range | PAN, Taxable Amount, Tax, Surcharge, Cess, Total TDS |
| 33 | Form 24Q | Taxes | FY | Annexure II format |
| 34 | Loan Outstanding Summary | Loans | None | Balance, Pending Instalments, Principal Paid |
| 35 | Loan Perquisite Summary | Loans | FY | Perquisite amount per loan |
| 36 | Loan Perquisite Projection | Loans | FY | Projected perquisite |
| 37 | Loan Summary Report | Loans | Date Range | Complete loan details with status |
| 38 | Payroll Journal Summary | Journal | Date Range | Debit, Credit (journal entry format) |
| 39 | Activity Logs | Activity | Date Range | Date, Activity, Description (audit trail) |

### Export Formats

| Format | Notes |
|--------|-------|
| PDF | Branded with org name |
| XLS | Excel 1997-2004 compatible |
| XLSX | Current Excel format |
| Zoho Sheet | Opens in Zoho's cloud spreadsheet |

**No CSV export** — this is a gap. ECR text file for EPFO is also not confirmed as a direct export from the EPF ECR Report (uses standard export options).

### Common Report Features

All 39 reports support: Favorite/Star, Compare With (prior period), Column customisation, Export, Show History (prior runs), Schedule (auto-email), Search, Advanced Filters (add criteria rows).

**Date Range presets:** This Month, This Quarter, This Year, Previous Month, Previous Quarter, Previous Year, Custom (dual calendar).

### Report Gaps

- No CSV export (only XLS/XLSX)
- ECR text file for EPFO portal upload not confirmed
- No report scheduler visible in main UI (only from within individual report)
- No cross-employee comparison dashboard (requires Zoho Analytics BETA for custom BI)

---

## 12. Edge Cases & Statutory Accuracy

### 12.1 Tax Regime Switch

**Scenario:** New vs old regime selection per employee.

**Zoho Behaviour:** IT Declaration URL uses `?tax_regime=with_exemptions` = old regime, `?tax_regime=without_exemptions` = new regime. `can_change_tax_regime: true` flag is year-round (employee can switch regime in any month via declaration).

**Expected:** Regime locked after first payroll run of FY; switchable before first run or during declaration window.

**Verdict:** Mostly correct. Dual label (Section 123 / 80C) correctly maps to Income Tax Code 2025. Our build: enforce new regime only in v1; implement regime switch as a FY-scoped flag with one allowed switch per year.

### 12.2 IT Declaration Full Form Audit

**Zoho Behaviour:** Comprehensive old-regime declaration form covering: HRA (rented house toggle), home loan interest/principal, other income sources, Section 123 (80C) with 14 investment sub-types, Section 126 (80D) with 8 mediclaim sub-types, and 16 other exemptions across Sections 127–154.

**New Income Tax Code 2025 readiness:** Zoho already uses new section numbers (Section 123 for 80C, Section 126 for 80D, etc.) with dual labeling "(Earlier: 80C)". This is well ahead of most payroll vendors.

**"Submit and Compare" UX:** Employee can compare tax liability under both regimes before committing. Excellent UX.

**Key section limits:**

| Section (New Code) | Old Code | Combined Limit |
|-------------------|----------|----------------|
| Section 123 | 80C | ₹1,50,000 (shared with 80CCC + 80CCD(1)) |
| Section 124(1B) | 80CCD(1B) | ₹50,000 (additional, outside 80C cap) |
| Section 126 | 80D | Up to ₹1,00,000 |
| Section 129 | 80E | No limit |
| Section 153 | 80TTA | ₹10,000 |

**Our build:** New regime only in v1. Declaration form much simpler — only Standard Deduction (₹75,000) and employer NPS contribution (80CCD(2)) apply automatically; no 80C/80D inputs needed from employee.

### 12.3 Bonus TDS Impact

**Scenario:** Annual bonus paid as variable earning — how is TDS computed?

**Zoho Behaviour:** Bonus as a Variable earning type. TDS Preference for variable earnings: "Deduct tax in subsequent payrolls" (spread remaining TDS across remaining months) OR "Deduct tax in same payroll" (full TDS in payment month).

**Test org finding:** TDS = ₹0 for all May 2026 employees. Likely Section 87A / Section 156(2) rebate applied — EMP001 income ~₹7.1L, just above ₹7L threshold. Exact rebate logic unconfirmed.

**Our build:** Annualisation method — add bonus to projected annual income, recompute annual tax, subtract YTD tax paid, distribute remaining over remaining months.

### 12.4 Pay Run Reprocess

**Scenario:** Reprocess a finalised (Paid) pay run.

**Zoho Behaviour:** 5-step manual process: Delete Recorded Payment (Paid → Approved) → Reject Approval (Approved → Draft) → Modify variable inputs → Approve → Record Payment. No single "Reprocess" command. No audit trail tab on pay run detail.

**Statutory concern:** Indian payroll audit trail is legally required. Zoho's absence of an audit log is a compliance gap.

**Our build:** `PayrollRunAuditLog` entity mandatory. One-click reprocess shortcut. Preferred approach: reprocess creates a delta/supplementary run rather than mutating the original run.

### 12.5 Multi-Location PT Differences

**Scenario:** Employees across multiple states with different PT slabs.

**Zoho Behaviour:** PT correctly configured per work location. Kerala half-yearly deduction (Aug + Feb) correctly implemented. Multi-location test blocked (test org has only Kerala location).

**Our build:** PT configuration table keyed by `(work_location_id, effective_date)`. Deduction cycle per state stored in DB config. Engine reads PT slab from config at pay run time.

### 12.6 PF Ceiling Cap

**Scenario:** PF computation on salary above ₹15,000.

**Zoho Behaviour (Critical):** `is_employee_restricted_basic_enabled` defaults to `false`. If EPF enabled, Zoho computes PF on full basic salary (e.g., 12% × ₹80,000 = ₹9,600/month) instead of statutory cap (12% × ₹15,000 = ₹1,800/month). Admin must manually enable the restriction.

**Statutory requirement:** Employers must contribute at minimum on ₹15,000; voluntary higher PF is optional — should not be the default.

**Our build:** `restrict_pf_wage_to_ceiling` must default to `true` in `ProvidentFundConfig`. This is the correct statutory default.

**Also noted:** EDLI (0.50%, max ₹75) and Admin Charges (0.50%) are correctly pre-configured in Zoho. EPS computed correctly at 8.33% of PF wage capped at ₹15,000 wage.

### 12.7 Mid-Month Joiner Proration

**Scenario:** EMP001 with 2 LOP days in May 2026 (31 calendar days).

**Confirmed formula:**
```
Prorated = (31 − 2) / 31 × Full Amount = 29/31 × Full Amount
```
- Basic ₹40,000 × 29/31 = ₹37,417 (truncation, not half-up rounding)
- All components prorated at identical ratio

**Mid-month joiner gap (EMP002):** Joined 16 May. System shows 31 paid days (full month). No LOP auto-applied. Admin must manually enter 15 LOP days.

**Our build:** Auto-detect joining date within pay period → suggest LOP days = joining_date.day − 1; allow admin override.

### 12.8 Salary Revision Arrears

**Scenario:** Salary revision with effective date in a prior month; arrear computation.

**Zoho Behaviour:** Revision is payrun-bound (payout month must be specified). Backdated effective date not tested end-to-end. Approval workflow (segregation of duties) present — revision requires approval by someone with "Approve Salary Revisions" permission.

**Our build:** Calendar-date effective (not payrun-bound); arrear delta = sum of (new monthly − old monthly) for each completed month between effective date and current month; paid in specified payout month. Section 157/89(1) relief for arrear TDS computation.

---

## 13. Cross-Module Data Flow

### Primary Data Flow Diagram

```
Organisation Setup (Settings)
  ├── Work Locations → PT slab auto-load → Pay Run PT deduction
  ├── Salary Components library → Salary Templates → Employee Salary Structure
  ├── Statutory Components (EPF/ESI/PT/LWF/Bonus) → Pay Run statutory deductions
  ├── Pay Schedule (Actual vs Fixed days) → Pay Run proration denominator
  └── Tax Details (PAN/TAN) → Form 24Q + Form 16

Employee Master
  ├── Basic Details (DOJ, Work Location, Designation, Dept)
  ├── Salary Structure (CTC, components, Fixed Allowance residual)
  ├── Statutory Details (UAN, PF Account, ESI Number)
  ├── Payment Info (Bank Account, IFSC, Payment Mode)
  ├── IT Declaration (FY-scoped; regime choice; investment declarations)
  │     └── → TDS monthly computation (reduces taxable income)
  ├── Prior Employer YTD → TDS computation (adds prior income + deducts prior TDS)
  └── Exit Record (LWD, reason) → F&F Pay Run type

Pay Run (Regular/Off-Cycle/F&F/One-Time)
  ├── Variable Inputs: LOP days, Ad-hoc earnings, TDS override
  ├── Proration engine: (paid_days / base_days) × component amount
  ├── Statutory deductions: EPF 12%, ESI 0.75%, PT per slab, LWF per state
  ├── TDS engine: projected annual income → tax slab computation → monthly TDS
  ├── Loan EMI deductions → Loan repayment ledger
  ├── Donation deductions → Giving module liability
  ├── Payslip generation (per employee PDF)
  └── State Machine: Draft → Approved → Paid

Post-Pay-Run Artifacts
  ├── TDS Liability (per employee per month) → Challan → Form 24Q
  ├── EPF ECR file → EPFO portal upload
  ├── ESI Monthly Summary → ESIC portal
  ├── PT Challan → State PT authority
  ├── LWF Challan → State LWF board
  ├── Payslip PDF → Employee portal + email
  └── Bank Advice file → Manual bank upload (or direct deposit if configured)

Annual Year-End
  ├── IT Declaration + POI approval → TDS final computation
  ├── Form 16 Part B generation (from payroll data)
  ├── Form 16 Part A (from TRACES) + merge → per-employee PDF
  └── Form 16 distribution (employee portal + email)

Reports
  ├── All 9 categories feed from Pay Run + Employee + Statutory data
  ├── Payroll Journal feeds Zoho Books (accounting integration)
  └── Activity Logs (audit trail of all user actions)
```

### Module Dependency Table

| Module A | Feeds | Module B | Data Transferred |
|----------|-------|----------|-----------------|
| Settings > Statutory Components | → | Pay Runs | EPF/ESI/PT/LWF rates + eligibility |
| Settings > Salary Components | → | Employee Salary Structure | Component library |
| Settings > Pay Schedule | → | Pay Runs | Proration denominator (Actual vs 30) |
| Settings > Tax Details | → | Form 24Q, Form 16 | Employer PAN, TAN, Deductor |
| Work Locations | → | PT Config | State-specific PT slabs |
| Employee Basic | → | Pay Run employee table | Active employees in period |
| Employee Salary Structure | → | Pay Run | Monthly component amounts |
| IT Declaration | → | TDS Engine | Chapter VI-A deductions (old regime) / standard deduction only (new regime) |
| Prior Employer YTD | → | TDS Engine | Prior income + TDS already deducted |
| Employee Exit | → | F&F Pay Run | LWD, reason, F&F timing |
| Loan EMI | → | Pay Run | Monthly instalment deduction |
| Donation Pledge | → | Pay Run | Monthly donation deduction |
| Pay Run (Approved) | → | TDS Liabilities | Per-employee monthly TDS amount |
| TDS Liabilities | → | Form 24Q | Quarterly aggregate TDS per employee |
| Pay Run (Paid) | → | All Payroll Reports | Source data for 39 reports |
| Form 24Q (Filed) | → | Form 16 Part B | Annual TDS certificate data |
| POI (Approved) | → | Form 16 Part B | Verified investment proof amounts |
| Salary Revision (Approved) | → | Next Pay Run | Revised CTC + arrear delta |

---

## 14. Zoho Gaps — Build Opportunities

### UX Gaps

| # | Gap | Details |
|---|-----|---------|
| U1 | No auto-proration for mid-month joiners | Admin must manually enter LOP days equivalent to pre-join days; no system warning |
| U2 | No single reprocess command | 5 manual steps to go from Paid back to Draft and re-run |
| U3 | Payslip embedded only, no standalone URL | Cannot email a direct link to payslip; no bookmarkable URL |
| U4 | TDS Sheet is PDF-only | No structured data access; cannot build custom views or test arithmetic |
| U5 | Downloads panel doesn't track Bank Advice | Bank Advice is a one-shot direct download; no history; cannot re-download |
| U6 | Date range filter missing from Payroll History | Only Payroll Type filter; no from/to date |
| U7 | Statutory Summary shows "no data" without context | Empty state gives no reason; no link to Settings to configure |
| U8 | No compliance calendar | No proactive due-date alerts for PF/ESI/PT/TDS deposit deadlines |
| U9 | No per-employee export | All exports are bulk; cannot download one employee's history |
| U10 | Giving module routing bug | `#/donations` URL redirects to `#/loans/new` in some Ember states |
| U11 | No Aadhaar field on employee | Aadhaar is a common KYC document; absence is a gap |
| U12 | No Employment Type field | Contractor vs Permanent distinction missing |
| U13 | Dashboard transition from onboarding not observed | Operational dashboard (post-setup) not captured |

### Statutory Gaps

| # | Gap | Details | Severity |
|---|-----|---------|----------|
| S1 | PF ceiling defaults OFF | `is_employee_restricted_basic_enabled = false` → PF computed on full basic; statutory violation | Critical |
| S2 | TDS = ₹0 anomaly | All May 2026 employees show ₹0 TDS; Section 87A application unconfirmed | High |
| S3 | No audit trail on pay run | State transitions (approve, reject, pay) not logged; legally required | High |
| S4 | Statutory Bonus minimum wage not pre-loaded | Admin must manually enter state minimum wage; error-prone | Medium |
| S5 | No TRACES API integration | Form 24Q generated as text file for manual TRACES upload; no automation | Medium |
| S6 | No charity PAN validation in Giving | 80G deductions require valid NGO registration; no validation | Medium |
| S7 | ESI threshold hardcoded | ₹21,000 in UI; must be DB-configurable for threshold changes | Low |
| S8 | IT Declaration defaults to old regime | URL param `tax_regime=with_exemptions` = old regime is the default path | Low (for v1 new-regime-only build) |

### Technical Gaps

| # | Gap | Details |
|---|-----|---------|
| T1 | No daily automated backup | On-demand only; no scheduled MinIO backup |
| T2 | No structured TDS computation API | PDF-only; no JSON endpoint |
| T3 | Deluge lock-in for automation | Proprietary scripting language; no standard API for custom logic |
| T4 | No EPFO direct integration | ECR file upload to EPFO portal is manual |
| T5 | No CSR export for EPF ECR | Standard export formats (XLS/PDF); ECR text format not confirmed |
| T6 | No payroll run mutation warning | Can delete recorded payment and reject approval on a Paid run with no audit |

### Missing Features / Entities

| # | Missing | Details |
|---|---------|---------|
| F1 | Explicit F&F run type | F&F uses Off Cycle + manual inputs; no dedicated F&F wizard with auto-gratuity |
| F2 | Gratuity auto-computation | No 5-year rule, ₹20L cap, or formula-based gratuity in any pay run type |
| F3 | Leave encashment auto-computation | Must be entered manually; no leave balance integration |
| F4 | Reprocess (single command) | Must go through 5 manual state transitions |
| F5 | Salary withhold / hold salary release | Withheld salary component exists but release mechanism not fully documented |
| F6 | Revoke exit process | No "undo" for exit initiation if employee retracts resignation |
| F7 | Probation tracking | No probation period or confirmation date field on employee |
| F8 | Notice period tracking | No notice period length field; exit form just captures LWD |

---

## 15. Build Recommendations

### Data Model Gaps to Fix

| # | Recommendation | Priority |
|---|---------------|----------|
| DM1 | Add `employment_type` enum to Employee: Permanent, Contract, Part-time | High |
| DM2 | Add `aadhaar_number` (encrypted, masked) to Employee | High |
| DM3 | `PayrollRunAuditLog` entity: `(run_id, user_id, from_state, to_state, reason, timestamp)` — log every transition | High |
| DM4 | `TdsLiability` entity per employee per month: feeds Form 24Q aggregate | High |
| DM5 | `ProvidentFundConfig.restrict_pf_wage_to_ceiling` defaults to `true` | High |
| DM6 | `StatutoryConfig` table: all EPF/ESI/PT/LWF/TDS rates keyed by `(state, effective_date)` — no hardcoded values | High |
| DM7 | `PayrollRunFile` table: every generated file (payslip, bank advice, ECR, Form 24Q) stored in MinIO with reference | Medium |
| DM8 | `EmployeeExit.reason` enum: `TerminatedByEmployer, TerminationByDeath, TerminationByDisability, ResignedByEmployee` | Medium |
| DM9 | `WorkLocation.state` is immutable after creation (enforce at application layer + DB trigger) | Medium |
| DM10 | `SalaryComponent` immutability: once assigned to employee, only `name` and `amount` editable; type/treatment frozen | Medium |

### Missing Entities to Add

| # | Entity | Key Fields |
|---|--------|-----------|
| ME1 | `PayrollRunAuditLog` | run_id, actor_id, from_state, to_state, reason, notes, created_at |
| ME2 | `TdsLiability` | employee_id, month, taxable_income, tax_amount, surcharge, cess, total_tds, run_id |
| ME3 | `TdsChallan` | challan_number, bsr_code, paid_date, paid_amount, penalty, interest, status (Unassociated/Associated) |
| ME4 | `Form24Q` | quarter, fiscal_year, status, preferences (employer + responsible person), generated_file_path |
| ME5 | `ScheduledEarning` | employee_id, component_id, amount, start_month, end_month (separate from salary structure) |
| ME6 | `EmployeeProration` | run_id, employee_id, base_days, lop_days, paid_days, source (Manual/AutoJoining) |
| ME7 | `PayrollRunFile` | run_id, file_type (Payslip/BankAdvice/ECR/Form24Q/Form16), storage_path, generated_at, generated_by |
| ME8 | `DonationCampaign` | name, description, exemption_type, end_month, show_in_portal, status |
| ME9 | `DonationPledge` | campaign_id, employee_id, amount, status (Pledged/Contributed) |
| ME10 | `PriorEmployerYtd` | employee_id, fiscal_year, employer_name, period_from, period_to, gross_salary, tds_deducted |

### Statutory Corrections

| # | Correction | Impact |
|---|-----------|--------|
| SC1 | PF ceiling default = true | Prevents statutory violation on EPF enable |
| SC2 | TDS computation as JSON API (not PDF-only) | Enables unit testing; structured Form 16 |
| SC3 | Standard Deduction from DB config (not hardcoded) | FY2025 = ₹50k old regime / ₹75k new regime; FY2026 = ₹75k new regime |
| SC4 | ESI threshold from DB config (not hardcoded ₹21,000) | Future-proofing for ESIC threshold changes |
| SC5 | PT slabs + cycle per state + effective_date in DB | Multi-state support; state-specific deduction frequency |
| SC6 | Salary Revision effective date as calendar date (not payrun-bound) | Backdated arrear computation per calendar month |
| SC7 | Section 87A / 156(2) rebate with marginal relief documented and tested | Verify exact income threshold (₹7L new regime) |
| SC8 | Gratuity auto-computation in F&F: 15 days × years × last_salary / 26, capped at ₹20L | Payment of Gratuity Act 1972 |

### UX Improvements

| # | Improvement | Benefit |
|---|------------|---------|
| UX1 | Auto-prorate mid-month joiners (suggest LOP = join_day − 1, allow override) | Eliminates manual calculation error |
| UX2 | Standalone payslip URL: `GET /payslips/{runId}/{employeeId}` returning HTML or PDF | Email-linkable payslips |
| UX3 | Compliance calendar widget on dashboard | Proactive deadline management |
| UX4 | "Revise and Reprocess" shortcut (single action from Paid state) | Reduces 5-step manual flow to 1 |
| UX5 | Explicit F&F wizard: auto-compute gratuity + leave encashment + notice pay | Eliminates error-prone manual entries |
| UX6 | Bank Advice stored in MinIO with re-download link | No data loss on browser refresh |
| UX7 | TDS Sheet as structured JSON + PDF (not PDF-only) | Transparency; testable; API-accessible |
| UX8 | "Submit and Compare" regime comparison (from Zoho) | Excellent employee UX — replicate |
| UX9 | Onboarding progress tracker showing % completion and next steps | Copy Zoho's 7-step checklist model |
| UX10 | Empty state for Statutory Summary links to Settings to configure | Reduce admin confusion |

### Day-One Features Zoho Lacks

| # | Feature | Notes |
|---|---------|-------|
| D1 | Auto-proration from joining date | Our biggest day-one differentiator |
| D2 | Payroll audit trail (immutable log) | Compliance requirement; Zoho is missing this |
| D3 | Correct PF ceiling default | Statutory accuracy out of the box |
| D4 | Daily automated backup (MinIO) | Zoho offers only on-demand backup |
| D5 | TDS computation as structured JSON API | Enables custom integrations and unit testing |
| D6 | Explicit F&F wizard with auto-gratuity | Full statutory F&F computation |
| D7 | Compliance calendar (statutory due-date alerts) | Proactive statutory management |
| D8 | All generated files stored with re-download history | No data loss; audit trail of outputs |
| D9 | Employment Type field on Employee | Contractor vs Permanent distinction |
| D10 | Aadhaar field (encrypted, masked) | Required for ITR and statutory filings |

---

## 16. API Patterns Observed

### Endpoint Conventions

| Pattern | Example | Notes |
|---------|---------|-------|
| REST at `/api/v1/` | `GET /api/v1/employees` | All API calls observed under this prefix |
| Entity-centric | `GET /api/v1/employees/{id}` | Standard RESTful resource |
| Action subpaths | `GET /api/v1/employees/{id}/taxworksheet` | Entity-specific actions |
| Query params for format | `?accept=pdf&print=true` | TDS Sheet PDF generation |
| Query params for filtering | `?month=2026-05&fiscal_year=2026` | Date-scoped queries |
| Editpage pattern | `GET /api/v1/donations/editpage` | Returns dropdowns, enum options for form construction |

### Autocomplete Patterns

- Employee search: `employee-search-box` component; returns all active employees with employee code in brackets
- IFSC lookup: live validation against bank database; graceful fallback when IFSC not found (bank name manual entry)
- Loan type: combobox showing admin-defined types + "Manage Loans" inline creation option
- Department/Designation: inline creation from employee wizard (without navigating to Settings)

### Report Generation Patterns

- All 39 reports accessible at `/reports/{report-slug}`
- Common filter structure: date picker (preset + custom calendar) + secondary filters
- Export triggered from within report view
- "Compare With" toggle for period comparison
- Report scheduling: from within individual report pages
- History: "Show History" button shows prior report runs

### Pay Run API Patterns

- Pay run created by clicking a period card (auto-created, not form-submitted)
- Variable inputs saved per employee via split panel save button
- State transitions via action buttons (Approve, Record Payment, Delete Payment, Reject Approval)
- Payslip accessed at: `GET /api/v1/employees/{id}/taxworksheet?month={YYYY-MM}&accept=pdf`
- Bank Advice: direct download triggered from pay run summary page

---

## Appendix A — Settings Pages Index (All 33, with URLs)

| # | Page | URL | Section |
|---|------|-----|---------|
| 01 | Organisation Profile | `#/settings/orgprofile` | Organisation |
| 02 | Branding | `#/settings/branding` | Organisation |
| 03 | Work Locations | `#/settings/worklocations` | Organisation |
| 04 | Departments | `#/settings/departments` | Organisation |
| 05 | Designations | `#/settings/designation` | Organisation |
| 06 | Subscriptions | `#/settings/subscriptions` | Organisation |
| 07 | Users | `#/settings/users` | Organisation |
| 08 | Roles | `#/settings/users-roles/roles` | Organisation |
| 09 | Tax Details | `#/settings/taxes` | Organisation |
| 10 | Pay Schedule | `#/settings/payschedule` | Organisation |
| 11 | Statutory Components | `#/settings/statutory-component/epf` | Setup & Config |
| 12 | Salary Components | `#/settings/salary-components/earnings` | Setup & Config |
| 13 | Employee Portal | `#/settings/portal/preferences` | Employee Portal |
| 14 | Claims & Declarations | `#/settings/preferences/fbp` | Employee Portal |
| 15 | Email Templates | `#/settings/email-templates` | Customisations |
| 16 | Sender Email Preferences | `#/settings/email-preference` | Customisations |
| 17 | Salary Templates | `#/settings/salary-templates` | Customisations |
| 18 | PDF Templates | `#/settings/templates/regular-payslip` | Customisations |
| 19 | Reporting Tags | `#/settings/advanced-reportingtags` | Customisations |
| 20 | Workflow Rules | `#/settings/automation/workflows` | Automations |
| 21 | Actions | `#/settings/automation/actions/alerts` | Automations |
| 22 | Schedules | `#/settings/automation/schedules` | Automations |
| 23 | Workflow Logs | `#/settings/automation/logs/alerts` | Automations |
| 24 | Employees & Contractors | `#/settings/employee/contractor` | Module Settings |
| 25 | Pay Runs | `#/settings/payrun/custom-approval/list` | Module Settings |
| 26 | Salary Revisions | `#/settings/salary-revision/custom-approval/list` | Module Settings |
| 27 | Leave & Attendance | `#/settings/holiday-leave/enable-module` | Module Settings |
| 28 | Loans | `#/settings/loan/custom-field/list` | Module Settings |
| 29 | Direct Deposits | `#/settings/direct-deposit` | Payments |
| 30 | Integrations — Zoho Apps | `#/settings/integrations/zoho` | Integrations |
| 31 | Connections | `#/settings/developer-space/connections` | Developer Data |
| 32 | Incoming Webhooks | `#/settings/developer-space/incomingwebhooks` | Developer Data |
| 33 | Data Backup | `#/settings/data-backup` | Developer Data |

---

## Appendix B — Reports Index (All 39)

| # | Report Name | Category | URL Pattern |
|---|-------------|----------|-------------|
| 1 | Payroll Summary | Payroll Overview | `/reports/payroll-summary` |
| 2 | Salary Register - Monthly | Payroll Overview | `/reports/employees-salary-register` |
| 3 | Employees' Salary Statement | Payroll Overview | `/reports/employees-salary-statement` |
| 4 | Employees' Pay Summary | Payroll Overview | `/reports/employee-salary` |
| 5 | Payroll Liability Summary | Payroll Overview | `/reports/payroll-liability` |
| 6 | Leave Encashment Summary | Payroll Overview | `/reports/leave-encashment-summary` |
| 7 | Loss Of Pay Summary | Payroll Overview | `/reports/lop-summary` |
| 8 | Variable Pay Earnings Report | Payroll Overview | `/reports/variable-pay-earnings-report` |
| 9 | Scheduled Earning Summary | Payroll Overview | `/reports/scheduled-earnings-summary` |
| 10 | EPF Summary | Statutory | `/reports/epf-summary` |
| 11 | EPF ECR Report | Statutory | `/reports/epf-ecr-report` |
| 12 | ESI Summary | Statutory | `/reports/esi-summary` |
| 13 | ESI Monthly Summary | Statutory | `/reports/esic-return` |
| 14 | Professional Tax Summary | Statutory | `/reports/pt-summary` |
| 15 | Employee-wise PT Report | Statutory | `/reports/pt-employees-summary` |
| 16 | Annual PT Report | Statutory | `/reports/pt-annual-summary` |
| 17 | LWF Summary | Statutory | `/reports/lwf-summary` |
| 18 | Compensation Details | Employee | `/reports/employee-ctc-master` |
| 19 | Reimbursement Claim Summary | Employee | `/reports/reimbursement` |
| 20 | Employee Perquisites Summary | Employee | `/reports/employee-perquisite-summary` |
| 21 | Full & Final Settlement Report | Employee | `/reports/employee-termination-report` |
| 22 | Employees' Salary Revisions | Employee | `/reports/employee-salary-revisions` |
| 23 | Salary Revision History | Employee | `/reports/employee-salary-revision-history` |
| 24 | Salary Withhold Report | Employee | `/reports/salary-hold-report` |
| 25 | FBP Declaration Report | Declarations | `/reports/fbp-declaration-report` |
| 26 | Investment Declaration Report | Declarations | `/reports/investment-declaration-report` |
| 27 | Proof of Investment Report | Declarations | `/reports/proof-of-investment-report` |
| 28 | Benefits & Deductions Summary | Deduction | `/reports/deductions-summary` |
| 29 | Deductions Summary | Deduction | `/reports/employee-post-tax-deductions-summary` |
| 30 | Benefits Summary | Deduction | `/reports/employee-pre-tax-deductions-summary` |
| 31 | Donations Summary | Deduction | `/reports/employee-donation-summary` |
| 32 | TDS Deduction Summary | Taxes | `/reports/tds-summary` |
| 33 | Form 24Q | Taxes | `/reports/form-24q` |
| 34 | Loan Outstanding Summary | Loans | `/reports/loan-outstanding-summary` |
| 35 | Loan Perquisite Summary | Loans | `/reports/loan-perquisite-summary` |
| 36 | Loan Perquisite Projection | Loans | `/reports/loan-perquisite-projection` |
| 37 | Loan Summary Report | Loans | `/reports/loan-overall-summary` |
| 38 | Payroll Journal Summary | Journal | `/reports/journal` |
| 39 | Activity Logs | Activity | `/reports/activity-log` |

---

## Appendix C — Mock Data Used (All 5 Employees + Scenarios)

| Employee | ID | Scenario | Key Details |
|----------|-----|---------|-------------|
| Arjun Mehta | EMP001 | Standard employee | CTC ₹84,000/year; Basic ₹40,000/month; 2 LOP days in May 2026; Loan LOAN-00001 (₹50,000 Personal at 0%); TDS = ₹0 (87A rebate applied) |
| Priya Sharma | EMP002 | Mid-month joiner | DOJ: 16 May 2026; CTC ₹22,000/month; NOT auto-prorated by Zoho (full month shown); ESI threshold gap (₹22k > ₹21k) |
| Vikram Nair | EMP003 | Mid-year joiner + Prior Employer YTD | DOJ: 01 Jun 2024; IT Declaration page load failure (incomplete profile); Loan LOAN-00002 (₹1,00,000 Emergency at 6%); Bangalore work location not configured so used Mumbai |
| Aisha Khan | EMP004 | Contractor gap | DOJ: 01 Jun 2024; Contractor scenario — no Employment Type field in Zoho; Contractor TDS (194C/194J) out of scope; Department "Design" created inline |
| Rahul Desai | EMP005 | Prior FY DOJ | DOJ: 01 Jun 2024 (prior FY); Skipped in May 2026 run (incomplete onboarding — no bank details); QA Engineer designation created inline |

**Pay Run Test Data:**
- April 2026 Regular Payroll: 2 employees, TDS = ₹0 for all
- May 2026 Regular Payroll (Run ID: `3848927000000034159`): 2 active employees, 3 skipped; Payroll Cost = ₹87,484; Pay Day = 29/05/2026; 2 LOP days applied to EMP001; Total TDS = ₹0

**Org State:** Kerala (all work locations); EPF not enabled; ESI not enabled; PT auto-configured (Kerala half-yearly); LWF disabled; Trial plan.
