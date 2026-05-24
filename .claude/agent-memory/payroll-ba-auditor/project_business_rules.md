---
name: project-business-rules
description: All statutory rules, validation invariants, workflow rules, and security rules discovered in code
metadata:
  type: project
---

## Giving (Donations) Module Rules
- Module is fully functional — NOT premium-locked in Zoho Payroll
- Routing invariant: `#/donations` always redirects to `#/loans` via Ember `beforeModel`/`redirect` hook. Programmatic nav required: `router.transitionTo('donations')`. Do NOT replicate this bug.
- Campaign participation is two-stage: pledge (employee commits intent) then contribution (actual payroll deduction). Pledge ≠ contribution.
- 80G exemption types (from `GET /api/v1/donations/editpage`): `donation_100_percent_exemption`, `donation_50_percent_exemption`, `none`. Stored as enum in campaign record.
- Campaign end_date: month-year granularity only (no day). Exact API date format unconfirmed — all tested formats (ISO8601, MM/YYYY, YYYY/MM, "May 2026") returned validation error.
- Liability tracking rule: Zoho UI warns "donor is liable until PAN is remitted to NGO" — implies contribution is held as pending liability until remittance confirmed.
- IT calculation integration: 80G donation amount factors into TDS computation for the employee in the same pay period.
- Campaign states: Active, Completed (from filter options; no explicit Draft state observed).
- Campaign is org-wide: all employees can pledge; there is no employee-group targeting per campaign.
- No NGO verification: any organization name can be entered; Zoho does not validate against 80G-registered NGO list.
- Donation reports: `reports.employee-donation-summary` and `reports.employee-donation-details` routes exist.

## Documents Module Rules
- Two folder types only: `org_public_folder` (org-wide, e.g., offer letters, policies) and `payroll_employee_folder` (per-employee, e.g., KYC docs).
- Upload model: ZIP archive containing PDFs. Each PDF named by Zoho employee entity ID (18-digit string), NOT employee code or PAN.
- File constraint: PDFs only inside ZIP. ZIP max size: 50MB. No single-file per-employee upload UI — batch-first model.
- Employee visibility is folder-level, not document-level: `shared_public: true` = all employees see the folder in portal. Default is `true` on creation.
- RBAC: `shared_to` array for specific user/role targets. Admin role: mandatory. Manager: optional. Not surfaced in creation dialog — only in Edit flow.
- Storage quota: 1GB per 100 employees (tenant-level, not per-folder).
- Folder nesting: supported by data model (`parent_folder_id`, `depth`) but UI is flat (no sub-folders shown in audit).
- Payslips are NOT stored in Documents module — managed via Pay Runs subsystem with separate storage.
- Form 16 is NOT stored in Documents module — managed via Taxes & Forms > Form 16 module.
- System-generated documents (ECR, challans, Form 24Q) each managed in their own modules; Documents module = user-uploaded files only.
- Document expiry: `expiry_date` field on document entity drives email reminders. Two recipient classes: (1) employee (their own doc expiry), (2) HR users (all docs, auto-notified by role).
- Expiry reminders: configurable in Settings > Employees > Document. On-expiry + configurable before-expiry intervals. Multiple before-expiry reminders allowed.
- No version control: re-upload of same employee ID filename behavior (replace vs append) unconfirmed from UI.
- No e-signature: e-sign is absent from all three evidence vectors (Ember routes, API endpoints, UI). Zoho Sign not integrated. Defer to v2. Design `signature_request_id` as nullable column for future migration.
- Three top-level route contexts: `documents.folder` (all), `documents.organization-folder` (org-only), `documents.employee-folder` (employee-only). Use route separation for access control.
- Folder pagination: default 100 docs per page, sorted by `created_time` ascending.
- Folder sub-structure: API `GET /api/v1/documents?folder_id={id}` returns `parent_folders`, `folders`, `documents` — tree navigation supported.

## Authentication & Security Rules
- Password policy: min 8 chars, must contain uppercase, lowercase, digit, special character (enforced both client-side Zod and server-side FluentValidation)
- Forgot password: always returns success response regardless of email existence (no user enumeration)
- Set password: requires token + email query params; invalid/missing shows error state before any API call
- JWT expiry: checked client-side via user.exp * 1000 > Date.now()
- 401 response: auto-logout via Axios interceptor
- SuperAdmin role: routed to /platform/orgs on login; all other roles go to /employees
- RequireSuperAdmin guard: non-SuperAdmin attempting /platform/* routes get redirected to /employees

## Tenant Provisioning Rules
- Slug validation: ^[a-z0-9][a-z0-9\-]{1,61}[a-z0-9]$ (3–63 chars, no leading/trailing hyphens)
- Slug is CRITICAL for SQL safety: it becomes the PostgreSQL schema name (CREATE SCHEMA sql). Regex is the injection guard.
- Schema name derivation: tenant_{slug.replace('-', '_')} — e.g., slug "acme-corp" -> schema "tenant_acme_corp"
- Slug auto-derived from display name on frontend (downcased, non-alnum replaced with hyphens, leading/trailing stripped)
- Slug is manually editable and overrides auto-derivation once touched
- Duplicate slug: 409 Conflict returned
- Admin email: sent a welcome email with set-password link on tenant creation
- Resend Setup Email: blocked if already active/completed (DomainException -> 409)
- Tenant status: Active (default on create), Suspended (via Suspend action)
- DisplayName max: 100 chars (server), 200 chars (client — MISMATCH)

## Employee Rules
- PAN format: ^[A-Z]{5}[0-9]{4}[A-Z]{1}$ (validated both client Zod and server FluentValidation)
- PAN stored AES-256 encrypted (column: encrypted_pan)
- Aadhaar stored AES-256 encrypted; always masked in API output (XXXX-XXXX-NNNN)
- Bank account + IFSC stored AES-256 encrypted
- Minimum age: 18 years old at date of joining (server-side: DateOfBirth < UtcNow - 18 years)
- Employee Code: unique per tenant (DB unique index on tenant_id + employee_code)
- Employee Code max: 20 chars (client), 50 chars (server — MISMATCH)
- Status defaults to Active on creation
- Soft delete: all org entities support soft delete (is_deleted + deleted_at + deleted_by)
- PF Opt-Out: boolean flag on employee (not yet in UI)
- IsPWD: Person with Disability flag (tax implication under old regime — note: V1 is new regime only)

## PayPeriod Rules
- Indian fiscal year: April (month 4) to March (month 3)
- FY2026 = April 2025 to March 2026
- FY computed as: Month >= 4 ? Year : Year - 1
- MonthsRemainingInFiscalYear: April = 12, March = 1 (used for TDS projection)
- PayPeriod stored as two integers (pay_period_year, pay_period_month) — NOT as a date range

## Payroll Run State Machine
Pending -> Processing -> Draft -> Finalised
                      -> Failed (from any non-Finalised state)
Finalised -> Draft (via Unlock with reason — requires unlock_reason text)
- VariableInputsFileKey: MinIO S3 key for uploaded variable inputs file (immutable audit artifact)
- employee_count: set when moving to Draft (number of employees processed)
- failure_reason: set on Failed transition (max 2000 chars)
- unlock_reason: set when re-opening a Finalised run (max 2000 chars)
- Payroll run is effectively immutable once Finalised — unlock creates an audit trail

## Salary Component Rules
- Formula types: Fixed (fixed_amount), PercentOfBasic, PercentOfGross, PercentOfCTC (percentage)
- Fixed amounts: numeric(18,4) precision
- Percentages: numeric(7,4) precision
- Component code: unique per tenant (DB index)
- is_system_component: flag for system-seeded components (not user-deletable, presumably)

## Statutory Module Rules
- Modules: PF, ESI, PT, LWF, TDS
- Each tenant can toggle each module on/off independently
- One toggle record per module per tenant (UNIQUE constraint)

## Organisation Profile / Settings Rules (from Zoho reference audit)
- Business Location (country) is immutable after org creation — stored at provisioning, shown as disabled field
- Organisation Address = Primary Work Location ("Head Office") address — the two are linked
- Filing Address must be one of the active Work Locations (not free-text) — constrained by `GET /api/v1/worklocations?filter_by=Status.Active`
- Filing Address deep-linkable via query param `?change_filing_address=true`
- Date Format is org-wide — applies to all payslip PDFs, reports, statutory forms; default `dd/MM/yyyy` (Indian standard)
- Field Separator (`.`, `-`, `/`) applies to date formatting across all documents; default `/`
- Logo appears on Payslip and TDS Worksheet; constraints: PNG/JPG/JPEG, max 1 MB, preferred 240x240 @ 72 DPI
- Industry field is one of 30 fixed options — informational in v1 (no confirmed statutory logic dependency)
- Public domain email (gmail.com etc.) triggers automatic sender override to Zoho's relay address for deliverability
- State list must use 36 current Indian states/UTs — exclude legacy "Daman and Diu" (merged into "Dadra and Nagar Haveli and Daman and Diu" in 2020)
- Fiscal Year, PAN, TAN, statutory registration numbers are NOT on org profile page — belong in Tax Details / Statutory Components settings
- No UI built yet for toggle management

## Tax Details / TDS Configuration Rules (from Zoho reference audit — Session 4)
- PAN format enforced: `AAAAA0000A` (5 alpha + 4 numeric + 1 alpha). Error: "Enter a valid PAN." — same message for empty and format-invalid (no distinction).
- TAN format: `AAAA00000A` (4 alpha + 5 numeric + 1 alpha). Marked optional in UI but functionally required for Form 24Q filing and challan generation.
- AO (Assessing Officer) code: 4-segment segmented input (AAA/AA/000/00) = Area Code / AO Type / Range Code / AO Number. Optional in UI, required for TDS return filing.
- Tax Payment Frequency: hardcoded "Monthly" — NOT configurable from UI. Statutory rule: TDS due by 7th of following month; March exception: 30th April.
- Deductor Type: "Employee" (lookup from active Employee Master) vs "Non-Employee" (free-text). Conditional form: Non-Employee adds Designation field and enables Father's Name.
- Deductor's Father's Name: disabled in Employee mode (expected auto-fill from Employee record); enabled in Non-Employee mode.
- Save is always enabled (no disabled state) — validation fires on submit, not on blur.
- Validation error pattern: top-of-form banner "Oops! Looks like you missed something..." with a list of errors, dismissible with X button.
- Saving with valid PAN+TAN but no Deductor selected returns HTTP 200 — Deductor is optional at settings level.
- Deductor Name employee dropdown: live-search autocomplete against `GET /api/v1/autocomplete/employee?filter_by=Status.Active&employee_type=employee&module=Payroll&search_text=`.
- Save API: `PUT /api/v1/settings/incometaxdetails`. Load API: `GET /api/v1/settings/incometaxdetails`.

## Org Structure Rules
- Branch requires a state (IndianState enum) — critical for PT/LWF calculations which are state-specific
- Department supports parent_department_id (hierarchical) in DB — NOT exposed in UI yet
- All org entities: soft delete with audit trail

## Data Integrity Notes
- No FK constraints visible in migration between employees and departments/designations/branches/cost_centres
  (references are stored as UUIDs but no FOREIGN KEY constraint defined in migration)
- payroll_runs index is NOT unique on (tenant_id, pay_period_year, pay_period_month) — multiple runs per period possible (needed for retry on failure)
- audit_logs has no FK to any entity table — loose coupling, entity can be deleted and log remains

## Employee Module Rules (from Zoho reference audit — Sessions 5–6)

### Salary Structure
- CTC is the anchor: all non-residual components are % of CTC or % of Basic
- Fixed Allowance = Monthly CTC − sum(all other component monthly amounts). This is an invariant enforced by the engine.
- Salary components once associated with any employee: only Name and Amount/% can be changed; type and calculation method locked
- Changes to org-level components only apply to new employees (not retroactively to existing)
- Add Earning dropdown in edit form shows only: active components NOT already in this employee's structure
- Two-phase salary wizard save: Statutory → Salary Structure → Benefits (each section has its own Save & Continue)

### ESI
- ESI wage ceiling: ₹21,000/month gross. Employees earning > ₹21,000 not ESI-eligible.
- ESI eligibility is runtime-computed per pay run — not stored on employee. Recomputed monthly.
- If org has ESI configured but employee gross > ₹21,000: no ESI deduction for that employee.

### Proration
- Mid-month join: salary prorated at pay run time (not at employee creation). DOJ stored; engine prorates on run.
- Proration formula: `Monthly × (Days worked / Total calendar days in month)`. Days basis (calendar vs working) configurable per pay schedule.

### Prior Employer YTD
- TDS formula with prior YTD: `Projected Annual Tax − Prior Employer TDS − Current Employer TDS so far = Remaining TDS`
- Remaining TDS spread over remaining months in FY
- Standard Deduction (₹75,000 new regime) claimed once per FY across both employers — prior employer field captures whether they claimed it
- Multiple prior employer records per (employee, FY) must be supported

### Exit / F&F
- Exit reasons: Terminated By Employer, Termination By Death, Termination by Disability, Resigned By Employee
- Reason drives gratuity eligibility (5-year service rule; Terminated By Employer = eligible regardless of tenure)
- Gratuity = `(Last Basic + DA) × 15/26 × Years`. Cap: ₹20,00,000 tax-free.
- Leave encashment calc: `EL balance × (Basic / 26)`. Tax-free up to ₹25,00,000 (private sector, FY2024-25+)
- F&F timing: regular schedule OR custom date (admin's choice at exit initiation)
- Personal email must be captured at exit for post-exit Form 16 delivery (work email deactivated)

### Import
- 19 import types in 5 groups. Prior Employer YTD importable as "Previous Employment Details" CSV.
- Salary Revision importable via CSV (critical for bulk hike cycles).
- Work Location cannot be created via import — must exist in Settings first.
- Designation and Department CAN be created via import (or inline during wizard).

### IT Declaration
- Default regime in Zoho: `tax_regime=with_exemptions` = OLD regime. Our build: new regime ONLY.
- IT Declaration locked by default per employee per FY. Admin unlocks or submits on behalf.
- POI (Proof of Investments) = separate workflow from IT Declaration — independent lock/unlock.
- Benefits in Zoho = `deduction_type=pre-tax`. Same entity as deductions, differentiated by flag.

### Compliance Gaps Found
- No Aadhaar field in Zoho Payroll employee entity — notable gap vs some Indian HR systems
- No Employment Type field (Contractor vs Permanent) — all employees treated as permanent salaried
- ESI not configured in lerno org → no test data for ESI flows; need employee with ≤ ₹21,000 gross

## Pay Runs Rules (from Zoho reference audit — Session 7)

### State Machine
- States: Ready → Draft → Payment Due → Paid
- "Payment Due" is a distinct state between approval and payment recording — NOT "Finalised"
- Our codebase `PayrollRunStatus` enum is missing: Payment Due (approved-not-paid intermediate state)
- Transitions: Ready+[Create Run] → Draft; Draft+[Submit and Approve] → Payment Due; Payment Due+[Record Payment] → Paid
- Reversal: Paid → Draft ("Revert to Draft" action — reason text required, audit logged)
- Draft → Ready: possible ("Delete Pay Run") — only in Draft state, cannot delete once approved

### Run Types (8 confirmed from Payroll History filter)
1. Regular — standard monthly payroll
2. Past — back-dated payroll for prior periods
3. Final Settlement — F&F for exited employees (single)
4. One Time Payout — ad-hoc payment outside regular cycle
5. Off Cycle — off-schedule payroll (supplemental run)
6. Bulk Final Settlement — F&F for multiple exits in one run
7. Resettlement — correction/revision to a previously paid run
8. (implied) All — filter default

### Approval Gate Rules
- Hard block: any employee in MISSING status → "Please complete your pending tasks to approve this payroll" toast; approval rejected
- Soft warning: employees with PAN missing → warning shown in Pending Tasks but does NOT block approval
- Pending Tasks section shows count of blocked employees before approval
- All MISSING employees must be either: (a) added to payroll (profile completed), or (b) skipped — before approval is possible

### Skip Employee Rules
- Skip is permanent for the pay cycle (cannot un-skip once confirmed)
- Skip reason is mandatory (free-text, API rejects special characters including hyphens — alphanumeric only)
- Skipped employees remain visible in pay run tables with "Skipped" badge + reason (audit trail)
- Bulk skip available: select-all checkbox → "Skip" in bulk action bar
- API: `PUT /api/v1/payrollruns/{run_id}/employees/skip?notes={reason}&pay_joinee_arrear_later={bool}`
- `pay_joinee_arrear_later=true`: skipped new joiner's prorated pay treated as arrear in next run
- `pay_joinee_arrear_later=false`: pay is forfeited for this period

### LOP (Loss of Pay)
- Input: per-employee, per-run — entered in employee split panel (not pre-stored)
- Effect: triggers live proration recalculation displayed immediately in split panel
- Field: numeric (days), 0 = no LOP (default)
- No configurable basis (calendar vs working days) visible in this dialog — may be org-level config

### TDS Override
- Admin can override engine-computed Income Tax per employee per run
- Override reason: mandatory free-text field
- Override is stored per employee per run (not permanent — scoped to this run only)
- Override visible in employee split panel "Income Tax" row

### Post-Approval Locks
- After approval (Payment Due state): Reimbursements locked, IT Declaration locked, POI (Proof of Investments) locked
- Pay run itself becomes read-only — component amounts cannot be changed
- Only "Record Payment" and "Download Bank Advice" are active in Payment Due state

### Bank Advice
- Generated post-approval (Payment Due state)
- Downloaded as file (format not confirmed — likely CSV or Excel)
- Used by finance team for bank transfer instructions

### Payslips
- Generated when run reaches Paid state (after Record Payment)
- Delivery: email notification to employees (portal link or direct PDF — configurable)
- Download available from Pay Run Summary page (admin side)
- Payslip shows: Gross, Deductions breakdown, Net Pay, YTD figures

### Professional Tax
- PT label includes work location in brackets: e.g., "KL Professional Tax (Head Office)"
- Confirms PT is scoped per work location (not org-wide)
- KL PT showed ₹0.00 for EMP001 (₹70k/month) — possible config gap in test org; KL PT should be ₹200/month for salary > ₹20k
- PT slab must be configured per state per work location before payroll run

### Salary Component Invariant (confirmed in Pay Run)
- Fixed Allowance = Monthly CTC − sum(all other component monthly amounts)
- EMP001 (₹70,000): Basic ₹39,998 + HRA ₹15,999 + Fixed Allowance ₹14,003 = ₹70,000
- Basic = ~57% of monthly CTC (not 50%) — Zoho default is 50%, but user had edited to 57% at structure setup

## Statutory Compliance Rules (from Zoho reference audit — Sessions 9–10)

### EPF (Provident Fund)
- EPF Number format: `AA/AAA/0000000/XXX` (state code / establishment type / 7-digit serial / extension) — regex enforce
- Employee contribution: 12% of PF Wage (basic + DA or gross, depending on config)
- Employer EPS: 8.33% of PF Wage, capped at ₹15,000 wage ceiling = max ₹1,250/month
- Employer EPF: remainder of 12% after EPS = (12% − 8.33%) × min(wage, 15,000) = ₹1,150 at ceiling
- EDLI: 0.5% of PF Wage capped at ₹15,000 (employer cost, paid to EPFO)
- EPF Admin Charges: 0.5% of PF Wage capped at ₹15,000 (employer cost)
- ECR (Electronic Challan cum Return) = monthly file to EPFO portal; format documented in `docs/ba-audit/compliance/74-epf-ecr-challan.md`
- LOP and PF interaction: two options (reduce PF wage or not); configurable at org EPF settings level
- PF Wage ceiling can be set at ₹15,000 or actual wage — configurable option in EPF settings
- UAN: 12-digit numeric, EPFO-issued, portable across employers. Field conditional on EPF being configured.

### ESI (Employee State Insurance)
- ESI Number format: `00-00-000000-000-0000` (17 chars including hyphens) — regex enforce
- Employee ESI rate: 0.75% of gross pay
- Employer ESI rate: 3.25% of gross pay
- ESI eligibility ceiling: ₹21,000/month gross. Employees above this ceiling: not eligible.
- Contribution periods: H1 = April–September; H2 = October–March. Once enrolled for a period, cannot stop mid-period.
- ESI is computed on gross, not basic — unlike PF which may be on limited wage
- No dedicated ESI challan screen in Zoho — admin downloads ESI Monthly Summary report from Reports Centre

### Professional Tax (PT)
- PT is per work location (state-specific, not org-wide)
- Auto-provisioned from org address state when org is created
- Kerala slabs (Half Yearly, effective 01/04/2026):
  | Income Range | PT Amount |
  |-------------|-----------|
  | < ₹11,999 | ₹0 |
  | ₹12,000 – ₹17,999 | ₹120 |
  | ₹18,000 – ₹29,999 | ₹180 |
  | ₹30,000 – ₹44,999 | ₹300 |
  | ₹45,000 – ₹59,999 | ₹450 |
  | ₹60,000 – ₹74,999 | ₹600 |
  | ≥ ₹75,000 | ₹1,250 |
- Max PT per year in all states: ₹2,500 (constitutional limit)
- PT challan = generated from PT Summary report + manual submission to state treasury
- Employee-level PT toggle exists (opt individual employees out of PT if applicable)

### LWF (Labour Welfare Fund)
- Kerala LWF: ₹50 employee + ₹50 employer, Monthly, Disabled by default
- LWF amounts are fixed (NOT percentage) — state-mandated fixed sums
- Not all states have LWF (e.g., Delhi, Bihar do not)
- LWF is per state, configurable per org via Statutory Components settings
- Deduction frequency varies by state (Monthly / Bi-Annual / Annual)

### TDS / Form 24Q
- TDS liabilities only generated from pay run approval onwards (not historical)
- Feature-flag: "Taxes & Forms" must be explicitly enabled via banner (not in default sidebar nav)
- TDS liabilities tab: Unpaid / Paid — transitions via Record Challan action
- Record Challan modal fields: Paid Amount (required), Penalty (optional), Interest (optional), Challan Number (required), BSR Code (required), Paid Date (required)
- BSR Code = Basic Statistical Return code assigned to TDS payment bank branch
- Challan Number = unique per TDS payment transaction
- Due dates: Q1=31 Jul, Q2=31 Oct, Q3=31 Jan, Q4=31 May
- Monthly TDS due: 7th of following month (March: 30th April)
- Form 24Q format: FVU (File Validation Utility) text file — admin downloads and uploads to TRACES manually (no NSDL API integration in Zoho)
- Responsible Person entity: separate from Employer; has own PAN, designation, contact details
- "Did you file Form 24Q in previous quarter?" flag: first-time filer indicator (affects Q1 return format)
- New regime slabs FY2026-27:
  | Income Range | Rate |
  |-------------|------|
  | 0 – 4,00,000 | 0% |
  | 4,00,001 – 8,00,000 | 5% |
  | 8,00,001 – 12,00,000 | 10% |
  | 12,00,001 – 16,00,000 | 15% |
  | 16,00,001 – 20,00,000 | 20% |
  | 20,00,001 – 24,00,000 | 25% |
  | Above 24,00,000 | 30% |
- Standard Deduction: ₹75,000 (FY2025-26 onwards, new regime)
- Rebate u/s 87A: ₹60,000 for taxable income ≤ ₹12,00,000 (new regime)
- Surcharge cap: 25% for new regime (NOT 37%); 37% applies only to old regime

### Form 16
- Part A: TRACES-issued, downloaded by employer from TRACES, uploaded to Zoho. Zoho does NOT generate Part A.
- Part B: Zoho-generated from pay run data (all months in FY)
- Merged PDF: Part A + Part B combined per employee per FY
- Tax Deductor gate: TAN + employer PAN + Deductor name ALL required before Form 16 generation can start
- Deductor immutability invariant: once Form 16 generation starts for a FY, Tax Deductor record is locked for that FY
- Assessment Year = Financial Year + 1 (FY2025-26 → AY2026-27)
- Employee PAN gate: per-employee — no PAN = cannot generate Form 16 for that employee
- Part A matching: done by employee PAN (TRACES Part A PAN must match Zoho employee profile PAN)
- PDF password: employee's PAN (AES encryption)
- Status state machine: Not Generated → Part A Uploaded → Generated → Signed → Published (Emailed)
- Status is monotonically increasing — no regression
- Portal visibility: employee can see Form 16 only after admin "Publishes" (status ≥ Published)
- Statutory deadline: Rule 31 — Form 16 must be issued by 31 May after end of FY
- Digital signature: DSC (Class 2/3, .pfx format) or e-Sign (Aadhaar OTP via MEITY-licensed ESP)
- Unsigned distribution: permitted by Zoho but non-compliant with CBDT electronic issuance mandate
- v1 recommendation: unsigned distribution; DSC and e-Sign deferred (`// DEFERRED: digital-signature`)
- Bulk operations (generate/sign/email/download-ZIP): must run as background jobs (Hangfire) — cannot be synchronous for large employee counts
- File naming: `Form16_{PAN}_{FY}.pdf` (e.g., `Form16_ABCPM1234A_FY2025-26.pdf`)
- MinIO path: `form16/{tenant_id}/{employee_id}/{fiscal_year}/form16.pdf`
- Audit log: every Form 16 download (who, when, employee_id, FY) must be logged

## Employee Portal + Email Templates Rules (from Zoho reference audit — Session N+2)

### Employee Portal
- Portal is enabled by default — ACTIVE state on fresh org
- Banner message requires expiry date if entered — prevents stale announcements; date picker constraint
- Portal contact email ≠ sender email: contact info shown to employees for queries; sender address configured in Sender Email Preferences (separate setting)
- "Show documents in employee portal" is a global gate — documents not visible in portal unless this toggle is ON AND document is in an Employee Folder (org-level folders always visible to admin but not portal employees unless toggle on)
- Web Tabs: custom external URL embeds; no payroll data dependency; useful for L&D/policy links

### Email Templates
- 4 trigger-based email templates only (no manual send from template page):
  1. `payslip_notification` — fires on Record Payment for regular pay run
  2. `payslip_notification_portal_disabled` — fires on Record Payment for employees without portal access
  3. `special_payroll_payslip_notification` — fires on Off Cycle + One-Time Payout payment
  4. `final_settlement_payslip_notification` — fires on F&F Settlement recording
- Subject default: `"Payslip for the month of %PayPeriodMonth%"` — placeholder syntax `%PlaceholderName%`
- Sender must be verified (email verification flow) before emails send from custom address
- Public domain senders (gmail.com, yahoo.com, etc.) auto-routed through `message-service@mail.zohopayroll.in`
- Custom email templates: NOT supported — only the 4 system templates; no "create new template"

### Claims & Declarations Settings
- IT Declaration is LOCKED by default per org — admin must "Release IT Declaration" before employees can submit
- POI (Proof of Investments) is LOCKED by default — released independently of IT Declaration
- POI "Process from March" (default): approved POI amounts factored into TDS from March payroll — aligns with CBDT guidance for Q4 recalculation
- "Allow employees to switch tax regime" exists in Zoho but is OLD REGIME gateway — **DEFERRED for v1** (new regime only); mark `// DEFERRED: old-regime` if encountered
- "Allow TDS modification to exceed calculated amount" — admin override for arrear/correction scenarios; default unchecked
- Mandatory POI attachment: if enabled, employees must upload document proof (not just declare amount) — our v1 should support this as an org-level config flag
- Mandatory reviewer comment for partial POI approval: audit trail for approved-less-than-declared amount

### Compliance Calendar
- `#/compliance-calendar` → 404 (page not found) — NO compliance calendar in Zoho Payroll
- Statutory due dates are NOT tracked in-app in Zoho Payroll
- For our build: compliance calendar is a build opportunity (not a Zoho reference feature)

## Direct Deposits / Payment Processing Rules
- Zoho Payments Payout: ₹3.00/employee/pay run + 18% GST = ₹3.54 per employee per run
- ICICI Bank integration: paid plan only — not available on trial
- HSBC Bank integration: available on trial plan
- v1 approach: manual bank file download (Bank Advice file from Pay Run Summary) — no direct deposit integration needed for v1
- Direct deposit integrations require bank account verification on the org side (not just employee side)

## Settings / Customisation Rules
- Salary Templates: pre-configured salary structures for fast employee onboarding; NOT the same as Salary Components (components = building blocks; templates = pre-assembled structures)
- PDF Template default: exactly one template must be DEFAULT at all times; setting new default is atomic
- Automation uses Zoho Deluge scripting language — proprietary; NOT JavaScript or Python
- Daily automation caps: Custom Functions 1000/day, Webhooks 1000/day, Email Alerts 500/day
- Contractor module: disabled by default; when enabled, contractors share employee role permissions; org folder docs visible to contractors
- Loan types NOT in Settings: loan types/templates created in Loans module (Manage Loans dialog), NOT in Settings > General > Loans; Settings > Loans = custom field configuration only (max 59 custom fields)
- Data Backup: on-demand only (no auto-schedule); CSV format; sent to admin email; backup history table
- Reporting Tags: custom labels for cross-cutting report segmentation (e.g., by project, cost centre)

## Rules Confirmed / Discovered — Session N+3 (2026-05-16)

### TDS / IT Declaration Lock
- IT Declaration LOCKED by default = all TDS = ₹0 for all employees. Employer is liable under Section 201(1A) for non-deduction.
- Arjun Mehta estimated TDS (if unlocked): ₹18,250 annual tax + ₹730 (4% cess) = ₹18,980 / ₹1,582 per month (based on ₹7,65,000 taxable income, new regime)
- Taxable income ≤ ₹7,00,000 → Rebate u/s 87A applies (₹0 tax). Arjun = ₹7,65,000 → exceeds threshold → NO rebate.
- "Allow employees to switch tax regime" CHECKED in demo org — old regime switching enabled. DEFERRED for v1.

### TAN / Tax Deductor Clarification
- TAN MUMR12345A IS configured in Settings > Taxes. Form 16 error "Tax Deductor not found" is due to missing Deductor NAME (responsible person) — NOT missing TAN.
- To unblock Form 16 and Form 24Q: navigate Settings > Taxes, scroll to Tax Deductor Details, select admin user as Deductor.

### Proration — Calendar Days Confirmed
- LOP proration uses CALENDAR days (not working days). Formula: Component × (Payable Days / Calendar Days in Month).
- Evidence: May 2026, 31 calendar days. Arjun LOP 2 days → Actual 29 days. Basic ₹40,000 × 29/31 = ₹37,419 (observed ₹37,417 — rounding truncation).
- Fixed Allowance is NOT prorated (observed: ₹13,100 in LOP month, same as full month amount).

### Skipped Employees — Composite Gate
- Employee skipped from pay run if ANY of these fields are missing: DOB, Father's Name, Personal Email, Permanent Address, Bank Account, Salary Structure.
- NOT a PAN-only gate. Priya (no PAN) is included; Vikram (missing DOB, Father's Name, Personal Email, Address) is skipped.

### Kerala PT Half-Yearly
- PT deductions only in September and March pay runs. May PT = ₹0 (correct — not a deduction month).
- Arjun PT (September): ₹600 (₹60k–₹74,999 slab). Priya PT (September): ₹180 (₹18k–₹29,999 slab).

### EPF Configuration
- "Included in Salary Structure" = employer PF contributions embedded in CTC. No additional employer cost above CTC/net pay shown in payroll cost.
- ECR is ₹0 in demo org (all employees EPF disabled). A nil ECR may still be required if org registered with EPFO.
- NCP Days in ECR = LOP Days for each employee.

### Taxes & Forms Sub-Navigation (Final)
- Only 4 items: TDS Liabilities, Challans, Form 24Q, Form 16.
- EPF, ESI, PT, LWF NOT in Taxes & Forms — accessible only via Reports > Statutory Reports.
- Challans page: Unassociated / Associated tabs. "New" button to record ITNS 281 challan. ITNS 281 fields: BSR Code, Challan Date, Challan Serial Number, Amount, Nature of Payment, Month.

### Loan Perquisite
- Formula: Monthly Perquisite = Opening Balance × SBI MCLR / 12.
- Exemption: aggregate outstanding < ₹2,00,000 OR medical loan (Rule 18 IT Rules).
- Both demo loans qualify for exemption individually (₹50,000 < ₹2L; ₹1,00,000 < ₹2L).
- SBI MCLR rate configuration location not found in Settings — may be auto-maintained by Zoho.
- "Income Tax Rules, 2026" citation in UI is likely errata for "IT Rules, 1962, Rule 15(5)".

### RBAC (Confirmed Roles)
- 3 system roles (cannot delete): Admin (full access), Manager (all except org settings), Reimbursements and POI Reviewer (approvals only).
- New Role button = create custom role.
- Single role per user (no multi-role assignment).

### PDF / Email Templates
- 7 payslip templates (Elegant = default), 1 FnF Settlement template, 3 letter templates.
- 4 email notification templates (Payslip, Portal-Disabled Payslip, Off Cycle Payslip, FnF Payslip).
- Portal-disabled employees receive payslip as PDF attachment; portal-enabled employees receive a link.

### Pay Run Approval Workflow
- Configurable: Simple (any authorized user), Multi-Level (all approvers must approve sequentially), Custom (criteria-based routing).
- Default for demo org: not explicitly configured (behaves as Simple Approval for single-admin org).

### Integrations (Zoho Apps)
- Zoho People: LOP sync + employee data sync.
- Zoho Books: payroll journal sync (auto-creates journal entries post finalization).
- Zoho Expense: expense submission route for reimbursements (richer than built-in portal).
- Zoho Analytics (Beta): custom dashboards from payroll data.
- None connected in demo org.

### Direct Deposits (Confirmed)
- Zoho Payments Payout: ₹3.00 + 18% GST = ₹3.54 per employee per run.
- ICICI Bank: paid plan only (blocked in trial).
- HSBC Bank: available in trial (no restriction shown).

### Prior Payroll Onboarding Mislead
- Dashboard onboarding Step 7 (Prior Payroll) shows "Completed" on page visit even if not actually configured.
- "Enable Prior Payroll" button in Prior Payroll settings — not clicked in demo org.
- Prior payroll data (YTD from previous FY) not entered — TDS may not account for prior employer income.

### Onboarding Completeness
- 5/7 onboarding steps complete in demo org.
- Step 4 (Statutory Components) and Step 5 (Salary Components) NOT actually complete.
- Step 7 (Prior Payroll) shows "Completed" but not configured (onboarding marks on visit, not on action).
