---
name: project-audit-status
description: Track which modules and pages have been audited vs pending, per session
metadata:
  type: project
---

## Module Checklist

### Completed (Session 1 — 2026-05-14)
- [x] Authentication & Authorization
  - [x] Login Page (/login)
  - [x] Forgot Password Page (/forgot-password)
  - [x] Set Password Page (/set-password?token=&email=)
  - [x] Auth API controller (POST /api/auth/set-password, POST /api/auth/forgot-password)
  - [x] OpenIddict token endpoint (POST /connect/token)
  - [x] Role constants (SuperAdmin, OrgAdmin, HRManager, PayrollManager, FinanceViewer, Employee)
  - [x] JWT structure (sub, email, role, tenant_id, tenant_slug, exp)
  - [ ] MFA — NOT implemented (no code found)
  - [ ] Token refresh flow — not yet audited in detail
  - [ ] Token revocation — interface exists (ITokenRevocationService)

- [x] Platform Admin (SuperAdmin only)
  - [x] Tenants List Page (/platform/orgs)
  - [x] Provision New Organisation Page (/platform/orgs/new)
  - [x] Organisation Detail Page (/platform/orgs/:id)
  - [x] Suspend/Activate Org actions
  - [x] Resend Setup Email action

- [x] Tenant/Organisation Setup (partial — DB schema only, no UI yet)
  - [x] Schema-per-tenant provisioning (CreateTenant creates schema + seeds user)
  - [ ] Company Profile UI — NOT IMPLEMENTED
  - [ ] Fiscal Year configuration UI — NOT IMPLEMENTED
  - [ ] Pay Schedule UI — NOT IMPLEMENTED
  - [ ] Statutory Registration Numbers UI — NOT IMPLEMENTED

- [x] Employee Master (basic creation only)
  - [x] Employees List Page (/employees)
  - [x] Create Employee inline form (on same page)
  - [ ] Employee Detail/Edit Page — NOT IMPLEMENTED
  - [ ] Bank Details capture — domain fields exist, no UI
  - [ ] Aadhaar capture — domain field exists, no UI
  - [ ] UAN capture — domain field exists, no UI
  - [ ] ESI IP Number capture — domain field exists, no UI
  - [ ] PF Opt-Out toggle — domain field exists, no UI
  - [ ] PWD flag — domain field exists, no UI
  - [ ] Date of Leaving / termination — domain field exists, no UI
  - [ ] Employee status transitions — no UI

- [x] Org Structure
  - [x] Branches (/org/branches) — List + Create
  - [x] Departments (/org/departments) — List + Create (no parent dept hierarchy in UI)
  - [x] Designations (/org/designations) — List + Create
  - [x] Cost Centres (/org/cost-centres) — List + Create

### Pending (Not Yet Audited)
- [ ] Salary Structure (components exist in DB, no UI built)
- [ ] Employee Salary Structure assignment (entity in DB, no UI)
- [ ] Payroll Run (entity in DB with full state machine, no UI)
- [ ] TDS / Income Tax module
- [ ] Provident Fund module
- [ ] ESI module
- [ ] Professional Tax module
- [ ] LWF module
- [ ] Payslips
- [ ] Reports & Analytics
- [ ] Compliance Calendar
- [ ] Audit Logs UI
- [ ] Settings & Configuration / Statutory Toggles (entity in DB, no UI)
- [ ] Notifications & Emails (MailHog running, email sent on provision)
- [ ] Data Import/Export
- [ ] User Management & RBAC (POST /api/users exists, no list/edit UI)

### Zoho Payroll Reference Product Audit (Session 2 — 2026-05-15)
- [x] Dashboard — Onboarding / Getting Started state (fresh org, 2/7 steps complete)
  - [x] Global header elements documented
  - [x] Left sidebar navigation — all 10 items documented
  - [x] 7-step onboarding checklist — all steps and sub-items documented
  - [x] Step 4 statutory sub-items: EPF, ESI, LWF, PT with routes
  - [x] Additional notable features section
  - [x] Help & resources panel
  - [x] Support contact cards
  - [x] Employee portal mobile app promo
  - [x] Global footer
  - [x] System maintenance banner
  - [x] Console errors logged (CSP violation, TransitionAborted, avatar 400)
  - [ ] Operational dashboard (post-setup state) — NOT YET AUDITED
  - [ ] Approvals accordion sub-items — NOT YET AUDITED
  - [ ] Instant Helper walkthrough content — NOT YET AUDITED
  - [ ] Step 2–6 "Complete Now" destination routes — NOT YET AUDITED (click audit needed)

Key findings:
- Zoho Payroll frontend is Ember.js (hash-based routing, vendor bundle confirms)
- PT is scoped per work location (not org-wide) — important for our PT implementation
- Step 7 (Prior Payroll) auto-marks complete for fresh orgs — optional/skippable step
- Dashboard in onboarding state shows NO payroll data — pure setup orchestration surface
- "Approvals" in sidebar is an accordion with hidden sub-items

### Zoho Payroll Reference Audit (Session 3 — 2026-05-15)
- [x] Settings > Organisation Profile (`#/settings/orgprofile`)
  - [x] All form fields documented (logo, name, business location, industry, date format, field separator, address, filing address, contact info)
  - [x] Industry dropdown — all 30 options captured
  - [x] Date Format dropdown — all 15 options captured (with live date preview)
  - [x] Field Separator dropdown — all 3 options captured (`.`, `-`, `/`)
  - [x] State dropdown — all 37 entries captured (incl. legacy "Daman and Diu" duplication flagged)
  - [x] Filing Address modal — structure, constraint, API call documented
  - [x] Contact Information section — email display, sender override logic documented
  - [x] Console errors documented (TransitionAborted, CSP violation, avatar 400)
  - [x] Full Settings sidebar navigation inventory — 33 sub-pages across 9 sections in 3 groups
  - [ ] Branding page — NOT YET AUDITED
  - [ ] Email Preferences page — NOT YET AUDITED

Key findings from Session 3:
- Organisation Address auto-creates "Head Office" Work Location — address and work location are linked
- Filing Address is constrained to active Work Locations (live API lookup: `GET /api/v1/worklocations?filter_by=Status.Active`)
- Date Format uses Java/CLDR token format (EEE, MMMM, dd) — live preview in dropdown
- NO fiscal year, PAN, TAN, statutory registration numbers on this page — those are in Taxes and Statutory Components
- Daman & Diu appears twice in state list (legacy + merged UT) — compliance concern for PT slabs
- Public domain email triggers automatic sender override to `message-service@mail.zohopayroll.in`
- Full sidebar: 33 distinct settings routes confirmed

### Zoho Payroll Reference Audit (Session 4 — 2026-05-15)
- [x] Settings > Tax Details (`#/settings/taxes`)
  - [x] Section 1: Organisation Tax Details — PAN, TAN, AO code (4-segment), Tax Payment Frequency (read-only)
  - [x] All tooltip texts captured (AO code, Tax Payment Frequency)
  - [x] Section 2: Tax Deductor Details — Employee vs Non-Employee conditional form fully documented
  - [x] Employee mode: Deductor Name combobox (live-search from Employee Master), Father's Name disabled
  - [x] Non-Employee mode: Deductor Name free-text, Father's Name enabled, Designation field appears
  - [x] Save button behaviour — always enabled; validates on submit; banner error pattern
  - [x] PAN validation: empty + invalid format both show "Enter a valid PAN." (same message)
  - [x] Successful save with valid PAN+TAN (HTTP 200 confirmed via network log)
  - [x] API contracts: GET + PUT `/api/v1/settings/incometaxdetails`, GET `/api/v1/autocomplete/employee`
  - [ ] TAN format-invalid test — NOT YET DONE
  - [ ] Father's Name auto-population (no employees in test org) — NOT YET VERIFIABLE
  - [ ] Role-based access — NOT YET TESTED

Key findings from Session 4:
- Tax Payment Frequency is hardcoded "Monthly" — non-configurable. Potential gap for quarterly-eligible deductors.
- Saving with missing TAN/AO code/Deductor returns HTTP 200 — no downstream guard visible at config time.
- AO code segmented 4-part input (AAA/AA/000/00) matches Income Tax Dept format exactly.
- Deductor Type switching causes full DOM re-render (not just enable/disable) — Designation field appears only in Non-Employee mode.
- Validation banner pattern: "Oops! Looks like you missed something..." — same for empty and invalid PAN.

### Zoho Payroll Reference Audit (Sessions 5–6 — 2026-05-15)
- [x] Employees Module — COMPLETE (20 files: items 34–51 + 41a + index)
  - See `docs/ba-audit/employees/00-employees-index.md` for full detail
  - [x] Employee List — empty state + populated state (21 columns, 8 views, import/export)
  - [x] Add Employee Wizard — all 4 steps fully documented
  - [x] Employee Profile — all 5 tabs (Overview, Salary Details, Investments, Payslips & Forms, Loans)
  - [x] Salary Structure Actions — Add Earning, Scheduled Earning, Benefit, Deduction, Donation
  - [x] 5 Mock Employees Created — EMP001–EMP005 (all active in system)
  - [x] Mid-Month Join scenario (EMP002 — proration rules)
  - [x] Mid-Year Join + Prior Employer YTD (EMP003 — TDS integration)
  - [x] Contractor Gap documented (EMP004 — no Employment Type field in Zoho)
  - [x] Prior FY DOJ (EMP005 — works without issue)
  - [x] Salary Revision — both mechanisms (direct edit + dated revision)
  - [x] Exit Process form — all fields, Reason for Exit options, F&F timing
  - [x] F&F Settlement — design level
  - [x] Bulk Import — all 19 import types across 5 groups documented
  - [x] Settings > Salary Components — 4 tabs, 14 earnings + 2 deductions + 1 benefit + 5 reimbursements
  - [ ] IT Declaration form (full) — DEFERRED (page load failure; requires complete employee profile)
  - [ ] Salary Revision dated UI — DEFERRED (trigger requires active pay run)
  - [ ] F&F Settlement page — DEFERRED (exit not submitted to preserve EMP001)

Key findings from Sessions 5–6:
- IT Declaration URL `tax_regime=with_exemptions` = OLD regime — our build is new regime only
- Benefits in Zoho = pre-tax deductions (`deduction_type=pre-tax`) — same entity, different flag
- No Employment Type field (Contractor vs Permanent) in Zoho Payroll
- No Aadhaar field in Zoho Payroll employee entity
- 21 customizable columns in Employee List (UAN, PAN, PF A/C, ESI No, Prior Payroll Status)
- Salary Revision importable via bulk CSV (critical for hike cycles)
- IFSC lookup must fail gracefully — all 5 test mock IFSCs failed lookup
- ESI ceiling ₹21,000 — none of test employees eligible; need sub-₹21k employee for ESI testing
- Fixed Allowance = Monthly CTC − sum(all other components) — invariant enforced by Zoho engine
- Import Data modal has 19 import types including "Previous Employment Details" (prior employer YTD)

### Zoho Payroll Reference Audit (Session 7 — 2026-05-15)
- [x] Pay Runs Module — COMPLETE → `docs/ba-audit/pay-runs/52-payrun-module.md`
  - [x] Pay Runs List page — Ready state, "New" dropdown (Regular + 3 special types), Payroll History link
  - [x] Add Employees gate — MISSING vs WITHOUT_PAN filter variants, single + bulk skip dialog
  - [x] Pay Run Preview / Draft — per-employee split panel (LOP, Earnings, TDS override, Deductions)
  - [x] Overall Insights — component drill-down via `#/payruns/insights/{run_id}/earnings/{component_id}`
  - [x] Approve Payroll dialog — Pending Tasks hard-block (MISSING employees), PAN-missing soft warning
  - [x] Pay Run Summary (Payment Due state) — Bank Advice file download, Record Payment dialog
  - [x] Pay Run Summary (Paid state) — Payslip download, email delivery, revert to draft
  - [x] Payroll History — 8 filter types documented
  - [x] Full state machine confirmed: Ready → Draft → Payment Due → Paid
  - [x] April 2026 run executed end-to-end (EMP001 + EMP002 paid; EMP003/004/005 skipped)

Key findings from Session 7:
- "Payment Due" is a distinct state between Approval and Record Payment — NOT in our codebase (we have no equivalent)
- 8 payroll history filter types vs our 2-value enum (Regular, FullAndFinal) — significant gap
- LOP (Loss of Pay) is per-employee per-run input, not a pre-stored value — entered in split panel
- TDS override: admin can override engine Income Tax with mandatory reason; stored per employee per run
- PAN missing = soft warning only (does NOT block approval); MISSING status = hard block
- Skip Employee: permanent for pay cycle; `pay_joinee_arrear_later` bool param (arrear in next run)
- Skip notes API rejects hyphens (HTTP 400) — alphanumeric only
- Post-approval locks: Reimbursements, IT Declaration, POI all locked
- Fixed Allowance residual invariant confirmed: Basic ₹39,998 + HRA ₹15,999 + Fixed ₹14,003 = ₹70,000 for EMP001
- KL PT labeled "KL Professional Tax (Head Office)" — work-location specific (confirms PT model)
- Bank Advice file generated post-approval for bank operations
- PayRunEmployee junction entity needed: employee_id, pay_run_id, status (Included/Skipped/Missing), lop_days, tds_override_amount, tds_override_reason, gross_pay, net_pay, skip_reason, pay_arrear_later

### Zoho Payroll Reference Audit (Session 8 — 2026-05-15) — Pay Runs Deep Dive
- [x] Pay Runs Module — FULLY COMPLETE → 16 files (53–67) + index in `docs/ba-audit/pay-runs/`
  - [x] 53: Period selection, auto-creation, empty state ("You deserve a break today!")
  - [x] 54: Variable inputs — LOP proration, Add Earning (Bonus/Commission/Leave Encash), TDS override + mandatory reason, Import (5 types) + Export (2 types), Cancel confirmation dialog
  - [x] 55: Draft review screen — all 3 tabs, pending tasks section, per-row kebab (6 options in Draft)
  - [x] 56: Payslip split panel (read-only post-approval), TDS Sheet PDF iframe, Download Payslip dialog (password protection default ON)
  - [x] 57: Proration — LOP formula confirmed (calendar days denominator, truncation rounding), mid-month joiner NOT auto-prorated (confirmed with EMP002 — 31 days shown despite joining 16 May)
  - [x] 58: F&F gap analysis — EMP005 skipped (onboarding incomplete), no dedicated F&F flow observed in Zoho
  - [x] 59: Approval flow — hard block on pending tasks, Skip Employee dialog (mandatory reason), Approve dialog, Draft → Approved transition
  - [x] 60: Post-approval — Approved state kebab (Reject Approval), Record Payment dialog, Paid state, Delete Recorded Payment dialog (Paid → Approved reversal), full state machine documented
  - [x] 61: Bank Advice (direct download, no history), Export Data (Payroll + Comparison), Downloads panel (empty — Bank Advice not tracked)
  - [x] 62: Full payslip field inventory, TDS Sheet API endpoint confirmed, new regime slabs FY2026 documented
  - [x] 63: Send Payslip (bulk + individual), informational dialog, no scheduling (immediate send)
  - [x] 64: Payroll History table (4 columns, type filter, row-click nav), June run not yet appearing (system timing)
  - [x] 65: Delete Recorded Payment flow (Paid → Approved), per-state row kebab matrix (Draft:6, Approved:variable, Paid:2)
  - [x] 66: One Time Payout dialog (component + date), Off Cycle Pay Run dialog (single date), Resettlement Payroll type noted
  - [x] 67: Overall Insights — Employee Breakdown metrics, Statutory Summary ("No data" — PF/ESI not configured), Payment Mode Summary, Component Wise Breakdown with per-component deep links
  - [x] 00: Master index created

New key findings from Session 8:
- Mid-month joiner (EMP002, joined 16 May) shows 31 days / full salary — NO auto-proration in Zoho
- LOP proration formula confirmed: (31-2)/31 × component = truncation (not rounding)
- Delete Recorded Payment: confirmation text is "You're about to delete the recorded payment for this pay run. Are you sure you want to proceed?" — Yes/No
- Page kebab in Paid state: 4 options (Download all Payslips, Download all TDS Worksheets, Show Downloads, Delete Recorded Payment)
- Per-row kebab in Paid state: 2 options only (Download Payslip, Send Payslip) — NO Revise Salary
- Statutory Summary empty state has zero explanation text (gap: should explain what config is needed)
- Component drill-down URLs: `#/payruns/insights/{runId}/earnings/{componentId}?override_type=`
- One Time Payout: single component field + date only — cannot bulk-configure amounts in dialog
- Off Cycle Pay Run: single date field only — no period range, no employee selector at dialog stage

### Zoho Payroll Reference Audit (Sessions 9–10 — 2026-05-15) — Statutory Compliance + Form 16
- [x] Compliance Phase 5 — COMPLETE → `docs/ba-audit/compliance/` (files 74–82 + 00-index)
  - [x] 74: EPF ECR challan — configuration form (10 fields), ECR via Reports, sample calc documented
  - [x] 75: EPF UAN management — conditional field (gated on EPF org config), employee statutory edit page
  - [x] 76: ESI challan — config form, 0.75%/3.25% rates, ₹21k ceiling, contribution period rule, no dedicated challan screen
  - [x] 77: PT challan — Kerala slabs (7 slabs, Half Yearly, max ₹2,500/yr), "(Revise)" crashed app (Zoho bug documented)
  - [x] 78: LWF challan — Kerala ₹50/₹50 monthly, disabled by default, state-reference table included
  - [x] 79: TDS + Form 24Q — feature-flag gating, TDS liabilities (Unpaid/Paid tabs), Record Challan modal (6 fields: BSR code, challan number, paid date, paid amount, penalty, interest), Form 24Q Q1 FY2026-27 (status Pending, due 31/07/2026), Preferences form (Employer + Responsible Person fields), "Generate Text File" button (FVU-format), quarterly due dates table, per-month employee breakdown
  - [x] 80: Form 16 Part A — TRACES-sourced, employer upload, 4-step flow, Tax Deductor gate, deductor lock invariant
  - [x] 81: Form 16 Part B — Zoho-generated, full Part B structure (Sec 17(1), Sec 10, Std Deduction ₹75k, Sec 87A rebate ₹60k, new regime slabs, surcharge cap 25%)
  - [x] 82: Form 16 bulk generate — 4-step flow, DSC/e-Sign, bulk email (PAN password), bulk ZIP, state machine

- [x] Compliance Phase 6 — Form 16 module — COMPLETE → `docs/ba-audit/form16/` (files 83–86 + 00-index)
  - [x] 83: Form 16 list view — pre-generation state, FY selector, Tax Deductor gate, instructional 4-step flow, expected list columns/filters/empty states documented
  - [x] 84: Form 16 generate flow — full prerequisites (4 gates: Tax Deductor, Form 24Q, Part A upload, employee PAN), step-by-step (Upload Part A → Generate → Sign → Publish/Email), status state machine, per-step data flows, bulk vs individual, FY scoping, statutory deadlines
  - [x] 85: Form 16 employee view — portal access (post-publish only), PDF download, PAN-as-password, email distribution, admin re-send, year selector, prior employer Form 16 gap, MinIO storage path, RBAC model, audit log requirement
  - [x] 86: Form 16 digital signature — DSC (PKI, Class 2/3, .pfx format, iText 7 library), e-Sign (Aadhaar OTP, MEITY framework, ESPs: NSDL/eMudhra/Digio), Unsigned fallback (non-compliant), comparison table, v1 recommendation (both deferred; unsigned for v1), batch signing via Hangfire

Key findings from Sessions 9–10:
- Compliance nav is NOT at `#/compliance` — accessed via feature-flag banner ("Track TDS Liabilities and Generate Form 24Q")
- Settings > Statutory Components is at `#/settings/statutory-details/list` (not `/settings/statutory` or `/settings/epf`)
- EPF/ESI not configured in test org (all 5 employees above ₹21k ESI ceiling)
- PT auto-provisioned for Kerala (Head Office) when org address was set — confirms work-location-scoped PT model
- TDS Form 24Q Q1 FY2026-27 auto-created on feature enable; TDS ₹0 because test employees below ₹12L threshold
- Form 24Q text file (FVU format) downloadable from within the Q record — NOT direct TRACES upload (manual step)
- "Did you file Form 24Q in previous quarter?" flag — first-time filer indicator in Preferences
- Form 16: Tax Deductor gate is hard (blocks all generation); "cannot change deductor after generation" = immutability invariant for FY
- Zoho does NOT integrate with TRACES API — Part A is manual download from TRACES + upload to Zoho
- 39 total reports in Reports Centre (8 statutory reports confirmed)
- PT "(Revise)" button crashed Zoho app — documented as Zoho bug; no action needed on our side

### Zoho Payroll Reference Audit (Session N — 2026-05-15) — Approvals Module
- [x] Approvals Module — COMPLETE → `docs/ba-audit/approvals/` (files 68–73 + 00-index)
  - [x] 68: Approvals navigation structure — 3 sub-items only (Salary Revision, POI, Reimbursements)
  - [x] 69: Pay Run Approval settings — Simple/Multi-Level/Custom at `#/settings/payrun/custom-approval/list`; none configured; state machine documented
  - [x] 70: Salary Revision Approval — full form; EMP001 revised ₹8,40,000→₹9,45,000 (+13%); admin bypasses queue; effective date must use datepicker
  - [x] 71: IT Declaration / POI Approval — 4 view states; unsubmitted list; IT Declaration + POI settings (Locked, Release, March recalculation, mandatory attachment/comments)
  - [x] 72: Loans — top-level at `#/loans`; Create Loan form; Rule 15(5) perquisite statutory reference; no loan types in test org; no approval workflow
  - [x] 73: Reimbursements — New Claim form (6 table columns + Save & Approve); prerequisite types must be assigned to salary structure; all test employees lack reimbursement components

Key findings from Approvals session:
- Dual-track approval architecture: admin-initiated = auto-approved; employee-initiated = approval queue
- Only 3 sub-items under Approvals nav — Loans and Pay Run approval are separate module locations
- No approval workflow configured in lerno org (all 3 types exist but none selected)
- Salary revision: admin cannot backdate; text date entry rejected at submit; datepicker required
- Loan perquisite: Rule 15(5) IT Rules reference on form; ₹2,00,000 threshold; medical loans exempt
- Reimbursement: Approved Amount is first-class field (partial approval supported); bill date ≠ claim month
- POI "Yet To Confirm" is distinct from "Approval Pending" — two-stage review
- For v1 (New Regime only): IT Declaration and POI flows have reduced scope

### Zoho Payroll Reference Audit (Session N+1 — 2026-05-15) — Giving + Documents Modules
- [x] Giving Module — COMPLETE → `docs/ba-audit/giving/` (files 93–94 + 00-index)
  - [x] 93: Giving overview — routing bug (`#/donations` → `#/loans`), Ember programmatic nav workaround, empty state, campaign list (All/Active/Completed), API `GET /api/v1/donations?filter_by=Status.Active`, full Ember route map, report routes
  - [x] 94: Giving features — New Campaign form (8 fields), exemption types (100%/50%/none from editpage API), validation codes (4=invalid name, 2=invalid end_date), "Things to Note" panel, cross-module impact, API date format unconfirmed, build observations

- [x] Documents Module — COMPLETE → `docs/ba-audit/documents/` (files 95–98 + 00-index)
  - [x] 95: Documents list & module overview — 3 route contexts (unified/org-only/employee-only), sidebar structure, folder data model (full JSON schema), folder creation forms, RBAC via editpage API, filter system (status + employee), storage limit 1GB/100 employees
  - [x] 96: System-generated docs — payslips NOT in Documents module, Form 16 NOT in Documents module, document API data model, expiry reminder system (employees + HR users, on-expiry + before-expiry configurable), two-audience reminder architecture
  - [x] 97: Upload document — ZIP container (PDFs named by employee ID), 50MB limit, batch-only model, employee visibility at folder level via `shared_public`, no version control, folder ops (kebab: Edit/Delete; heading edit icon), API endpoints documented
  - [x] 98: E-signature investigation — ABSENT (3-vector evidence: no Ember routes, all API endpoints 404, no UI sign actions), build recommendation: defer to v2, design schema with nullable `signature_request_id`

Key findings from Giving + Documents session:
- Giving is NOT premium-locked — fully functional in test org
- `#/donations` has persistent routing bug: redirects to `#/loans` via Ember `beforeModel`/`redirect` hook; fix via `router.transitionTo('donations')`
- 80G exemption types: `donation_100_percent_exemption`, `donation_50_percent_exemption`, `none`
- Two-stage participation: pledge (intent) then contribution (actual deduction from payroll)
- Campaign end_date: month-year granularity; exact API date format unconfirmed (all tested formats returned error)
- Documents: ZIP of PDFs named by Zoho entity ID (not employee code) — batch-first upload model
- Folder types: `org_public_folder` (org-wide) vs `payroll_employee_folder` (per-employee)
- `shared_public: true` is default on all created folders
- Storage: 1GB per 100 employees (tenant-level quota, not per-folder)
- Payslips and Form 16 are entirely separate from Documents module (separate storage, separate access)
- E-signature: absent from Zoho Payroll; no Zoho Sign integration found

### Zoho Payroll Reference Audit (Session Reports — 2026-05-15) — Reports Module
- [x] Reports Module — COMPLETE → `docs/ba-audit/reports/` (8 files + 6 screenshots)
  - [x] 00-reports-index.md — Master table of all 39 reports with category, URL pattern, filter type, key columns
  - [x] 99-reports-list.md — Full categorised inventory with per-report descriptions and observations
  - [x] 100-payroll-summary.md — Payroll Summary: two-column aggregate, entity_list param, sections, empty states, Compare With, Export formats
  - [x] 101-employee-earnings.md — Salary Register Monthly + Employees' Salary Statement + Employees' Pay Summary
  - [x] 102-statutory-reports.md — All 8 statutory reports (EPF Summary, EPF ECR, ESI Summary, ESI Monthly, PT Summary, Employee-wise PT, Annual PT, LWF Summary) with full column lists and statutory references
  - [x] 103-custom-report-builder.md — Absence of custom report builder; Customize Report Columns feature; More Filters; Scheduled Reports empty state
  - [x] 104-export-formats.md — PDF/XLS/XLSX/Zoho Sheet; No CSV; No ECR text; No ESIC portal format; Indian number formatting
  - [x] 105-other-report-categories.md — All remaining 30 reports (Payroll Overview sub-reports, Employee/Contractor, Declarations, Deductions, Taxes, Loans, Journal, Activity)

Key findings from Reports session:
- 39 total reports, all System Generated, all available on trial plan — no premium paywall
- App framework confirmed: Ember.js (data-ember-action attributes, hash-based routing)
- Persistent Ember router state bug: clicking buttons while the app has a pending loan creation state redirects to /loans/new — workaround: JS DOM evaluation with setTimeout
- Export formats: PDF, XLS (Excel 1997-2004), XLSX (Excel), Export to Zoho Sheet — NO CSV
- EPF ECR Report: Excel only — no EPFO ECR .txt file export (critical gap for compliance)
- ESI Monthly Summary: Excel only — no ESIC portal format
- "Customize Report Columns" badge feature on statutory reports (12/7/6/5/5/5/4 columns)
- "Compare With" period comparison on Payroll Summary, PT Summary, ESI Summary, LWF Summary
- "Show History" button per report — shows prior report runs
- Scheduled Reports tab exists but empty (schedule from within each report)
- Zoho Analytics integration upsell in sidebar (paid add-on for custom reporting)
- Payroll Journal shows double-entry accounting (Payroll Journal accrual + Wage Payment entries)
- Activity Logs in Reports module (not separate audit module)
- No Leave balance reports — Leave Encashment Summary and LOP Summary only
- No Bank Transfer Advice report in Reports module (generated from Pay Runs)
- Variable Pay Earnings Report: per-earning-component, filtered by compensation_id in URL
- Annual PT Report actual page title is "PT Annual Return Statement" (differs from index name)
- Loan Summary Report actual page title is "Loan Overall Summary" (differs from index name)
- Indian number formatting: lakh-crore notation (₹1,79,484.00) confirmed in all report data

### Zoho Payroll Reference Audit (Session N+2 — 2026-05-15) — Portal + Email + Settings
- [x] Employee Portal Settings — COMPLETE → `docs/ba-audit/settings-remaining/105-employee-portal-settings.md`
  - Preferences: Enable Portal Access (ACTIVE), Banner Message (textarea + expiry date), Portal Contact Info (Manage Contacts panel via `?configure_emails=true`), Document Management toggle (show docs in portal)
  - Web Tabs: empty state; used to embed external URLs (company policies, LMS, etc.) in employee portal nav
- [x] Email Templates — COMPLETE → `docs/ba-audit/settings-remaining/106-notifications-email-templates.md`
  - 4 templates: payslip_notification, payslip_notification_portal_disabled, special_payroll_payslip_notification, final_settlement_payslip_notification
  - Editor: Subject (text, supports `%PlaceholderName%` syntax; default = "Payslip for the month of %PayPeriodMonth%"), Body (WYSIWYG + Headings + Font + Insert Placeholders)
  - Sender Email Preferences: abhijithss2255@gmail.com PRIMARY Unverified; public domain → `message-service@mail.zohopayroll.in` override
- [x] Settings Full Inventory + Remaining Modules — COMPLETE → `docs/ba-audit/settings-remaining/107-settings-customisations-automation.md`
  - Full settings sidebar: 31 routes confirmed and documented in table
  - Branding: Dark/Light pane + Accent Color (applies across all Zoho Finance apps)
  - Salary Templates: empty; pre-configured salary structures for fast onboarding
  - PDF Templates: 7 regular (Elegant=DEFAULT) + F&F Settlement + 3 letter templates (Salary Certificate, Salary Revision Letter, Bonus Letter)
  - Reporting Tags: empty; custom labels for cross-cutting report segmentation
  - Automation: Workflow Rules / Actions (Alerts+Webhooks+Custom Functions+Field Updates) / Schedules / Logs; usage caps 1000/1000/500 per day; uses Zoho Deluge scripting
  - Employees & Contractors sub-tabs: Contractor (Enable toggle), Custom Field, Custom Button, Validation Rules, Record Locking, Related List
  - General > Loans: `#/settings/loan/custom-field/list` — purely customisation (custom fields 0/59); loan types are NOT here
  - Direct Deposits: Zoho Payments ₹3/emp/run+18% GST; ICICI (paid plan); HSBC (trial OK)
  - Claims & Declarations: FBP (empty — no components), Reimbursements (empty), IT Declaration (Locked; "Allow regime switch" = DEFERRED v1), POI (Locked; "Process from March"; mandatory attachment/reviewer comment options)
  - Data Backup: on-demand CSV to email; backup history table; Backup Audit Trail sub-tab
  - Compliance Calendar: CONFIRMED NOT PRESENT (`#/compliance-calendar` → 404)

Key findings:
- No Compliance Calendar in Zoho Payroll — statutory due date tracking not in-app
- IT Declaration "Allow employees to switch tax regime" = Old Regime gateway — DEFERRED for v1 (new regime only)
- POI "Process from March" = default aligns with CBDT Q4 TDS recalculation advisory
- Loan types NOT in Settings — created from Loans module (Manage Loans dialog)
- Automation uses Deluge (Zoho proprietary scripting), not standard JS/Python
- Direct Deposit is an optional paid add-on (₹3/employee/run) — our v1 will use manual bank file download

### Zoho Payroll Reference Audit — Session N+3 Batch (2026-05-16) — Major Bulk Documentation
- [x] UF-30 to UF-35: POI upload, POI approve/reject, TDS finalization (IT Declaration lock = root cause ₹0 TDS), tax regime switch (old/new), FnF exit process, Gratuity calculation
- [x] UF-43 to UF-49: Variable pay inputs (date-gated), Reimbursements in pay run (config-gated), LOP calculation deep-dive (calendar days confirmed), Pay run review/approve workflow, Mark as paid, New joiner proration (calendar days formula confirmed), Skipped employees (composite onboarding gate)
- [x] UF-50 to UF-58: Download payslip (full panel: Arjun ₹65,484; Payable 29/31 days), Bank advice, Off-cycle pay run, Bonus pay run, Arrears pay run, Past pay run (historical view), Reprocess/revision run, Pay run reversal ("Delete Recorded Payment" mechanism)
- [x] UF-59 to UF-62: Approvals module (Salary Revision + Reimbursements pages empty state), Reimbursement claim approval flow, Reject approval items, Approval history / audit trail
- [x] UF-63 to UF-67: Create loan (LOAN-00001 Arjun ₹50k; LOAN-00002 Vikram ₹1L), Loan EMI in pay run (first EMI July 2026), Loan repayment/prepayment, Loan perquisite (Rule 15(5) SBI MCLR), Loan foreclosure (via Record Repayment)
- [x] UF-68 to UF-72: TDS Liabilities (all ₹0 due to IT Declaration lock), EPF ECR generation (₹0; ECR format documented), ESI return/challan (all employees ineligible), PT challan (Kerala half-yearly; PT Number blank = gap), LWF challan (Kerala ₹4/year employee)
- [x] UF-73 to UF-76: Form 24Q Q1 FY2026-27 (Tax Deductor = Deductor's Name missing, not TAN), Form 16 prerequisites (TAN confirmed MUMR12345A; Deductor Name still missing), Form 16 generate/sign, Form 16 publish/email
- [x] UF-77 to UF-83: Reports Centre (39 reports, 9 categories), Payroll Summary report, Statutory reports (PT Kerala half-yearly confirmed; EPF/ESI ₹0), Loan reports (4 reports), Employee reports (7 reports), Declaration/Deduction reports (7 reports), Payroll Journal (simple Dr/Cr) + Activity Logs
- [x] UF-84 to UF-88: Employee Portal (5/7 onboarding steps; Prior Payroll NOT actually enabled despite showing "Completed"), Portal payslips/declarations (IT Declaration LOCKED = root cause TDS ₹0; "Allow regime switch" = CHECKED), Salary Components settings (full component inventory: 15 earnings, 3 deductions, 5 reimbursements, 1 benefit), Additional settings, Employee portal reimbursement submission flow
- [x] UF-89 to UF-96: TDS Challans (Unassociated/Associated tabs; ITNS 281 workflow), POI Approvals (empty; 2 employees yet to submit; IT Declaration must be released first), Users & Roles (Admin/Manager/Reimbursements+POI Reviewer), Direct Deposits + Zoho Apps Integrations (People/Books/Expense/Analytics), PDF Templates (7 payslip + 3 letter templates; Elegant = default) + Email Templates (4 notification types), Loans custom fields settings (customization only; NOT loan types), Tax Details (TAN MUMR12345A confirmed; Deductor Name = actual block for Form 16), Pay Run + Salary Revision approval workflows
- [x] DS-01 to DS-06: Full Design System documentation — App shell layout, Form components (Indian validations), Table/navigation patterns, Modal/drawer patterns, Toast/notification patterns, Color system and design tokens

Key discoveries from this batch:
- IT Declaration LOCKED = employer liable Section 201 (Arjun TDS ₹18,980/year, ₹1,582/month = ₹0 currently)
- TAN confirmed as MUMR12345A; Form 16 blocked by missing Deductor Name (person), NOT TAN
- Kerala PT Half-Yearly: only September and March pay runs deduct PT
- EPF "Included in Salary Structure" = no additional employer cost above net pay
- Taxes & Forms sidebar = TDS only (4 items); EPF/ESI/PT/LWF accessible only via Reports
- Prior Payroll onboarding step = "Completed" despite NOT being enabled (onboarding marks complete on page visit)
- All reimbursement components INACTIVE with ₹0 max — no claims possible in demo org
- Vikram Nair LOAN-00002 ₹1,00,000 — no EMI recovery while skipped from pay runs (financial risk)
- Pay Run approval: Simple/Multi-Level/Custom configurable in Settings (not configured in demo = Single Admin)
- 102 total files in userflows directory
- Zoho integrations: People (LOP), Books (journal), Expense (reimbursements), Analytics (Beta) — none connected

### Zoho Payroll Reference Audit — FINAL SESSION (2026-05-16) — Mop-Up A1–A15 + Final Summary

- [x] UF-A1: Payslip PDF format (RC4 encryption, bank account unmasked in PDF, structure)
- [x] UF-A2: Bank advice format (11 bank formats, SBI ABSENT, XLS default)
- [x] UF-A3: TDS sheet format (iframe PDF, print-only, no download, "Section 156" error)
- [x] UF-A4: Form 16 generation (4-step: Upload Part A → Generate → Sign → Publish/Email; trial FY dropdown empty)
- [x] UF-A5: Employee portal per-employee toggle (admin email unverified = portal enable blocked)
- [x] UF-A6: Exit / F&F flow (Ember date picker limitation; Tax Deductor exit block; 4 exit reasons)
- [x] UF-A7: IT Declaration + POI (Finance Act 2025 renumbering; CTA wrong regime URL bug; POI approval page)
- [x] UF-A8: Reimbursements (Add Claim form; 5 default components; FBP checkbox; carry-forward)
- [x] UF-A9: Giving / CSR (Section 133 = formerly 80G; 3 exemption types; new campaign form)
- [x] UF-A10: Documents module (1GB/100 emp quota; two folder types; portal visibility gate)
- [x] UF-A11: Statutory bonus (Payment of Bonus Act 1965; eligibility auto-check unconfirmed)
- [x] UF-A12: Pay run per-employee row actions (PAID state: Download/Send Payslip only; 3 skipped = no actions)
- [x] UF-A13: Pay run Overall Insights (Component Wise Breakdown: Basic ₹48,417 + HRA ₹14,967 + Fixed ₹24,100)
- [x] UF-A14: Salary revision approval (empty state; status machine documented from prior sessions)
- [x] UF-A15: Settings remaining pages (full sidebar 30+ items; Pay Schedule immutability; Zoho integrations)
- [x] UF-FINAL-SUMMARY.md: Full audit summary (117 files total; top 10 findings; top 10 gaps; V1 checklist)

## AUDIT STATUS: COMPLETE

Total files produced: 117 (UF-01 to UF-96, DS-01 to DS-06, UF-A1 to UF-A15)

All Zoho Payroll India modules comprehensively audited as of 2026-05-16.

## Next Phase: V1 Product Build

Use audit artifacts to drive requirements for:
- P0: Employee master (PAN mandatory), salary components, pay run engine, TDS new regime, PF/ESI/PT, payslips, bank advice, TDS worksheet (with Download), Form 24Q, employee portal
- P1: Arrears/bonus/off-cycle runs, salary revision approval, loan management, reimbursements, Form 16, LWF, F&F, bulk import, RBAC, audit trail
- Deferred: Old regime IT Declaration, FBP, Zoho integrations, e-sign, analytics

Critical build differentiators vs Zoho:
1. AES-256 PDF encryption (not RC4)
2. Bank account masked in payslip PDF (not just UI)
3. PAN mandatory — blocks payroll inclusion (not soft warning)
4. Correct Section 87A citation in TDS worksheet (not "Section 156")
5. SBI bank advice format supported from V1
6. Compliance calendar built in (Zoho has none)
