# AUDIT_PLAN.md — Indian Payroll SaaS Full Application Audit

> **Purpose:** Phase 0 deliverable. A complete map of the *actual application source* (not the Zoho reference in `docs/ba-audit/`). Every route, role, form, table, calculation, modal, state transition, permission gate, displayed number, and visual risk area. This feeds Phase 1 (TEST_PLAN.md) and Phase 2 (Playwright specs).
>
> **App under test:** web `http://localhost:5173`, API `http://localhost:5000` (full Docker stack up).
> **Stack:** .NET 8 + React 19 + TS + PostgreSQL 16 (schema-per-tenant) + Redis + Hangfire + MinIO + MailHog. New tax regime ONLY.

---

## 0. TEST ENVIRONMENT & CREDENTIALS

| Item | Value | Notes |
|---|---|---|
| Web URL | `http://localhost:5173` | Vite dev frontend |
| API URL | `http://localhost:5000` | OpenIddict token at `/connect/token` |
| MailHog UI | `http://localhost:8025` | Catch set-password / welcome / payslip emails (read reset links here) |
| SuperAdmin email | `admin@payroll.local` | **Seeded via `SUPERADMIN_EMAIL` env var** — NOT UI-creatable |
| SuperAdmin password | in `.env` `SUPERADMIN_PASSWORD` | env-seeded; **GAPS: UI-INACCESSIBLE SETUP** |
| Tenant admin (OrgAdmin) | created by provisioning an org | first password set via emailed link (MailHog) |

**Critical setup chain (all UI except the seed):** SuperAdmin (seeded) → log in → provision org (creates OrgAdmin, no password) → MailHog welcome email → set-password link → OrgAdmin logs in → onboarding checklist → employees → pay runs.

---

## 1. COMPLETE SITEMAP

### 1.1 Public / unauthenticated
| Route | Component | Purpose |
|---|---|---|
| `/login` | LoginPage | OAuth2 password grant → `/connect/token` |
| `/forgot-password` | ForgotPasswordPage | Request reset email (no enumeration) |
| `/set-password` | SetPasswordPage | Requires `?token=&email=`; sets/resets password |
| `/` | RootRedirect | → `/login` (anon), `/platform/orgs` (SuperAdmin), `/dashboard` (tenant) |
| `*` | Navigate | Catch-all → `/` |

### 1.2 Platform admin (SuperAdmin only — `RequireSuperAdmin`)
| Route | Component | Purpose |
|---|---|---|
| `/platform` | → `/platform/orgs` | index redirect |
| `/platform/orgs` | TenantsPage | List tenants |
| `/platform/orgs/new` | ProvisionOrgPage | Create tenant + OrgAdmin |
| `/platform/orgs/:id` | OrgDetailPage | Suspend/activate/resend setup email |

### 1.3 Tenant app (auth + non-SuperAdmin — `RequireAuth` + `RequireTenantUser` + `AppLayout`)
| Route | Component | Purpose |
|---|---|---|
| `/dashboard` | DashboardPage | KPIs + setup checklist |
| `/employees` | EmployeesPage | Employee list |
| `/employees/import` | ImportEmployeesPage | XLSX bulk import |
| `/employees/new` | AddEmployeeWizard | Step 1 (create) |
| `/employees/:id/wizard/:step` | AddEmployeeWizard | steps: `salary`, `personal`, `payment` |
| `/employees/:id` | EmployeeDetailPage | 5 tabs |
| `/employees/:id/exit/initiate` | ExitInitiationPage | Offboarding |
| `/pay-runs` | PayRunsPage | Run Payroll + History tabs |
| `/pay-runs/:id` | PayRunDetailPage | 3 tabs |
| `/pay-runs/:id/fnf` | FnfSettlementPage | Final settlement |
| `/settings` | SettingsHomePage | hub (9 links, 3 groups) |
| `/settings/work-locations` | WorkLocationsPage | + form page (create/edit) |
| `/settings/departments` | DepartmentsPage | modal CRUD |
| `/settings/designations` | DesignationsPage | modal CRUD |
| `/settings/business-units` | BusinessUnitsPage | modal CRUD |
| `/settings/org-profile` | OrgProfilePage | logo + company details |
| `/settings/pay-schedule` | PaySchedulesPage | work week / pay date |
| `/settings/salary-components` | SalaryComponentsPage | 6 tabs, 5 add-modals |
| `/settings/salary-structures` | SalaryStructuresPage | list |
| `/settings/salary-structures/new` | SalaryStructureBuilderPage | builder |
| `/settings/salary-structures/:id/edit` | SalaryStructureBuilderPage | builder (edit) |
| `/settings/statutory` | StatutoryComponentsPage | 5 tabs: EPF/ESI/PT/LWF/Bonus |
| `/settings/tax-details` | TaxDetailsPage | TAN/AO code/deductor |

### 1.4 Route guards
- `RequireAuth`: token≠null + user≠null + `user.exp*1000 > Date.now()`; else → `/login`.
- `RequireSuperAdmin`: above + role includes `SuperAdmin`; else → `/`.
- `RequireTenantUser`: above + role does **not** include `SuperAdmin`; SuperAdmin → `/platform/orgs`. (SuperAdmin has no `tenant_id` claim → tenant queries throw "Tenant context not resolved".)
- Deep-link cross-tenant: PayRunDetailPage / FnfSettlementPage 404-route on not-found.

---

## 2. ROLES & PERMISSION MATRIX

**Enum (`src/Payroll.Domain/Constants/Roles.cs`):** `SuperAdmin`, `OrgAdmin`, `HRManager`, `PayrollManager`, `FinanceViewer`, `Employee`.

**API authorization policies (Program.cs):**
| Policy | Roles satisfying it |
|---|---|
| SuperAdmin | SuperAdmin |
| OrgAdmin | OrgAdmin |
| HRManager | HRManager, OrgAdmin |
| PayrollManager | PayrollManager, OrgAdmin |
| FinanceViewer | FinanceViewer, PayrollManager, OrgAdmin |
| Employee | Employee, HRManager, PayrollManager, OrgAdmin |

**Gated endpoints (sample):**
- `/api/tenants/*` → SuperAdmin
- `/api/users` → OrgAdmin
- `/api/v1/onboarding/seed-defaults/{step}` → OrgAdmin
- `/api/v1/payroll-runs/{id}/bank-advice`, `/bank-advice/download`, `/export/payroll-details`, `/export/tds-breakup` → FinanceViewer
- `/hangfire` dashboard → SuperAdmin
- `/connect/token` → AllowAnonymous; validates creds + tenant active (SL-002)

**CONFIRMED — only 2 of 6 roles are UI-reachable.** Verified: zero frontend references to `/api/users`, no users/team/invite route or nav entry, no `HRManager`/`PayrollManager`/`FinanceViewer` strings anywhere in `web/src`. `UsersController` (POST, OrgAdmin policy) exists API-side but **no frontend is wired to it**. Therefore:
- **UI-reachable roles:** `SuperAdmin` (env-seeded), `OrgAdmin` (created by org provisioning).
- **UI-INACCESSIBLE roles:** `HRManager`, `PayrollManager`, `FinanceViewer`, `Employee` — cannot be created or assigned through any UI flow. Their permission boundaries are a **product GAP**, not a testable surface. → GAPS.md "UI-INACCESSIBLE SETUP".

**Permission tests that ARE runnable (UI):**
- SuperAdmin visiting `/dashboard` → bounced to `/platform/orgs`.
- OrgAdmin (tenant user) hitting `/platform/orgs` directly → bounced to `/`.
- Anonymous deep-link to any protected route → `/login`.
- Suspended-tenant OrgAdmin login → 403.
- All payroll ops run as OrgAdmin (the only tenant role obtainable); FinanceViewer-gated exports succeed for OrgAdmin (policy includes OrgAdmin). The 403-as-wrong-role path is UI-untestable (no second tenant role).

---

## 3. EVERY FORM (FIELDS · VALIDATION · STATES)

### 3.1 Login (`/login`)
- `username` (email): required, valid email ("Enter a valid email").
- `password`: required, min 1 ("Required").
- Submit → `/connect/token` (grant_type=password). Success: store token, redirect by role. Error: "Invalid credentials or server error." Suspended tenant → 403.
- Rate limit: 5/min/IP on auth endpoints.

### 3.2 Forgot password (`/forgot-password`)
- `email`: required, valid email.
- Always shows "If that email exists, a reset link has been sent." (no enumeration). Reset email lands in MailHog; link valid 1 hour.

### 3.3 Set password (`/set-password?token=&email=`)
- Precondition: token+email in query; else error "Invalid or missing link parameters…".
- `newPassword`: min 8, ≥1 upper, ≥1 lower, ≥1 digit, ≥1 special.
- `confirmPassword`: must match.
- Success: "Password set successfully…" + link to login. Errors: per-rule messages; expired/invalid token → "Failed to set password. The link may have expired."
- **Note divergence:** Identity policy = RequiredLength 12; set-password Zod = min 8. Verify which wins (test a 8–11 char password).

### 3.4 Provision org (`/platform/orgs/new`)
- `displayName`: required; FE max 200, BE max 100 (**divergence — test 101–200 chars**).
- `slug`: required, regex `^[a-z0-9]+(-[a-z0-9]+)*$`, 3–63 chars; auto-derived from displayName until manually edited.
- `adminEmail`: required, valid email, max 320.
- Success → 201, redirect to `/platform/orgs`. Slug conflict → 409 "That slug is already taken." Creates: tenant row, PG schema `tenant_{slug_with_underscores}`, OrgAdmin (no pwd), welcome email.

### 3.5 Add Employee wizard — Step 1 Basic (`/employees/new`)
Fields: First Name (req, ≤100), Middle (≤100), Last (req, ≤100), Work Email (req, email, ≤255, **immutable after create**), Employee Code (≤20, auto-gen if blank), Mobile (`^\d{10}$`), Gender (req enum), DoJ (req date), DoB (req date), Employment Type (req enum: FullTime/PartTime/Contract/Intern), Department (req FK, +New modal), Designation (req FK, +New modal), Work Location (req FK, auto-first), Business Unit (FK, +New modal), Is Director (bool), Enable Portal Access (bool). Uniqueness: email + code (DomainException). → Step 2.

### 3.6 Step 2 Salary (`/employees/:id/wizard/salary`)
- Annual CTC (req, >0, step 1000) → live monthly breakdown.
- Salary Template dropdown (active only).
- 4 statutory checkboxes (EPF/ESI/PT/LWF), pre-filled from template.
- Per-component override (in-place): % (0–100, step 0.5) or fixed (step 100); Reset reverts to template default.
- Add Earning (component dropdown + formula type Fixed/PercentOfCTC/PercentOfBasic/PercentOfGross + amount/% conditional).
- Add Benefit (component + fixed ₹/month).
- Live preview: deductions, take-home, employer contributions, benefits, CTC footer. Buttons: Save and Continue / Save (if revise) / Skip.

### 3.7 Step 3 Personal (`/employees/:id/wizard/personal`)
- Father's Name (req, ≤150 — needed for payroll), PAN (`^[A-Z]{5}[0-9]{4}[A-Z]$`, encrypted), Aadhaar (`^\d{12}$`, encrypted, masked XXXX-XXXX-1234), Personal Email (email), Differently Abled Type (enum None/Visual/Hearing/Locomotive/Other), Is PWD (bool), Address1/2 (≤200), City (≤100), Residential State (IndianState enum), PIN (`^\d{6}$`).

### 3.8 Step 4 Payment (`/employees/:id/wizard/payment`)
- Payment Mode radio: Bank Transfer (only enabled), Direct Deposit/Cheque/Cash (disabled "Coming soon").
- If bank: Account Holder Name (req, ≤150), Bank Name (req, ≤150), Account Type (enum Savings/Current/Salary), Account Number (password-masked, req, ≤20, encrypted), Confirm Account Number (must match), IFSC (req, length 11, mono uppercase, encrypted).

### 3.9 Employee exit (`/employees/:id/exit/initiate`)
- Last Working Day (req date, ≥ today−30, ≤ today+5yr), Reason (enum Resigned/Terminated/Death/Disability), Settlement Mode radio (RegularSchedule | CustomDate→shows Final Settlement Date ≥ LWD), Personal Email (email), Notes (≤2000). Submit → creates EmployeeExit + FnF PayrollRun; routes to `/pay-runs/{id}/fnf` (single) or `/pay-runs/{id}` (bulk).

### 3.10 Settings forms
- **Org Profile:** Company Name (req ≤200), Legal Name (≤200), Company PAN (req, PAN regex, ≤200), GSTIN (BE regex `^\d{2}[A-Z]{5}\d{4}[A-Z]\d[Z][A-Z\d]$`), Website (≤500), Industry (14-option dropdown), Date of Incorporation, Address1/2 (≤250), City (≤100), PIN (`^\d{6}$`), State (35-option). Logo: PNG/JPEG ≤2MB. Toasts: "Organisation profile saved" / "Failed to save profile".
- **Work Location create:** Name (req ≤150), Address1/2 (≤250), State (req, 35 options), City (≤250), PIN (`^\d{6}$`). **Edit:** State read-only; adds PT Registration Number (≤50). Delete blocked if employees assigned.
- **Department:** Name (req ≤150), Code (≤20), Description (≤250). Delete blocked if assigned.
- **Designation:** Name (req ≤150). Delete blocked if assigned.
- **Business Unit:** Name (req ≤150), Description (≤500). Delete blocked if assigned.
- **Pay Schedule:** Work Week day toggles (≥1 req), Salary Calc radio (ActualDays | FixedDays→Fixed Working Days 1–31), Pay Date radio (LastDay | SpecificDay→1–30), First Payroll Month+Year (both-or-neither). Work week + calc method **locked after first processed run**; pay date editable. Preview shows 3 upcoming pay dates.
- **Tax Details:** TAN (≤10 upper), PAN (read-only from org profile), AO Area Code (≤3 upper), AO Type (10-option), Range Code (≤3 numeric), AO Number (≤5 numeric), Deductor Type (7-option), Deductor Name (≤200), conditional (non-Company) Father's Name/Designation (≤200), Tax Deductor Employee (searchable active-employee dropdown — *required for exit initiation*).

### 3.11 Salary component add-modals (6 tabs: All/Earnings/Deductions/Reimbursements/Benefits/Corrections)
- **Earning:** Name (req ≤200), Name in Payslip (req ≤200, auto-fill), Earning Type (32-option enum), Pay Type (Monthly|FlatAmount), Formula Type (Fixed|%Basic|%Gross|%CTC), amount(>0) / % (0–100) conditional, Is Taxable (def T), Consider for EPF (def F)+EPF Inclusion Rule (Always|OnlyWhenPfWageBelowLimit), Consider for ESI (def F), Pro-rata (def T, disabled if one-time), Show in Payslip (def T), One-Time (bool).
- **Deduction:** Name, Name in Payslip, Frequency (EveryMonth/OnceAYear/Quarterly/HalfYearly), One-Time (forces EveryMonth).
- **Benefit:** Benefit Type (VPF→Percentage 1–100 | NPS→Govt Sector flag | OtherNonTaxable), Name, Name in Payslip, Applicable to All (def T).
- **Reimbursement:** Name, Name in Payslip, Amount.
- **Correction:** Name, Name in Payslip, Formula Type, %/Amount.

---

## 4. DATA TABLES & LISTS

### 4.1 Employees list (`/employees`)
Columns: Employee (avatar+name, "Incomplete" badge), Code (mono), Department, Location, Type (badge), Joined (dd/MM/yyyy), Status (Active/Inactive/Exited badge). Status tabs: All/Active/Inactive/Exited (live, resets to page 1). Search: name/email/code/department (live). Pagination: server-side, page size persisted in localStorage (default 25). Empty states: "No employees yet. Add your first employee." / "No employees match this filter." Row click → detail. Incomplete-profile banner with count. Header: Import + Add Employee (gated by onboarding).

### 4.2 Pay runs (`/pay-runs`)
Two tabs. **Run Payroll:** pending chips (All Pending / Final Settlement / Bulk Final Settlement w/ counts), outstanding run "Continue", Process Payroll card, empty "You deserve a break today!…". **History:** columns Payment Date, Payroll Type, Period, Employees (count, right), Total Net Pay (formatINR, right), Status ("Paid"); type filter dropdown; empty "No completed pay runs yet."

### 4.3 Pay run detail — Employee Summary table
Filter tabs All/Active/Skipped (counts). Expandable rows. Draft columns: Gross Pay, Deductions (PF+ESI+PT+LWF), Taxes (TDS or override), Net Pay, LOP days. Approved/Paid columns: Net Pay, TDS, PF only. Inline: Skip (Draft+Active), Download Payslip. Skipped rows opacity-60 + amber reason. Import/Export dropdown (Draft: LOP/Earnings/Reimbursements import; always Export).

### 4.4 Taxes tab
Expandable per employee. Columns: name/code, Annual Projected Income, Taxable Income, Annual Tax Liability, TDS This Month, PAN status. Expanded worksheet: Annual Projected, Standard Deduction, Taxable Income, Tax (Slab), Rebate 87A, Surcharge, Cess 4%, Annual Tax Liability, TDS This Month. §206AA amber badge if PAN override.

### 4.5 Settings lists
Work Locations (cards: name/address/employee count/status + edit + kebab Active-toggle/Delete). Departments (table: Name/Code/Description/Actions). Designations (boxes: name + edit/delete). Business Units (boxes: name/description + edit/delete). Salary Components (table: Name+payslip subtitle/Code/Category/Calculation/Status/Actions). Statutory tabs and PT per-state lists. Each list: empty state, delete-blocked-if-assigned.

---

## 5. CALCULATIONS (formula · inputs · output · rounding · edges)

> Engine is pure/deterministic, all rates from `StatutoryConfig` (DB). All rounding `Math.Round(…, 2, MidpointRounding.AwayFromZero)`. Verify displayed values in the Taxes tab + payslip against hand-computation. FY2026-27 test rates below.

### 5.1 Gross (`GrossCalculator`)
- GrossWage = Σ prorated component amounts. Proration (only if LOP>0 AND !IsFlat AND CalculateOnProRata): `round(Amount × (BaseDays−LOP)/BaseDays, 2)`.
- PFWage = Σ prorated where ConsiderForEpf. FullPFWage = Σ unprorated where ConsiderForEpf. TaxableGross = Σ prorated where IsTaxable. ESIWage = Σ prorated where ConsiderForEsi.
- AnnualProjectedGross = YTDGross + (Gross − OneTime)×MonthsRemaining + OneTime. (One-time projected ×1 not ×N.)
- Edges: LOP=0 full pay; IsFlat/no-prorata not prorated; one-time excluded from ×N projection.

### 5.2 TDS (`TDSCalculator`, new regime 115BAC)
1. No PAN → flat 20% of total projected income, ÷ months (bypasses slabs/rebate/cess).
2. TaxableIncome = max(0, ProjectedGross + priorEmployerYTDTaxable − StandardDeduction).
3. Slabs (FY26-27): 0–4L 0%, 4–8L 5%, 8–12L 10%, 12–16L 15%, 16–20L 20%, 20–24L 25%, 24L+ 30%.
4. Rebate 87A: if TaxableIncome ≤ ₹12,00,000 → rebate = min(taxBeforeRebate, ₹60,000).
5. Surcharge slabs with marginal relief.
6. Cess 4% on (tax+surcharge).
7. MonthlyTDS = max(0, (annualTax − currentYTDTDS − priorYTDTDS) / monthsRemaining).
- StandardDeduction ₹75,000. Known-good: ₹12.75L gross → ₹0 (rebate); ₹15.75L gross → ₹9,100/mo; no-PAN ₹15.75L → ₹26,250/mo.

### 5.3 PF (`PFCalculator`)
- Employee = round(min(pfWage, 15000 if restricted) × 12%). VPF = round(empWage × VPF%/100).
- EPS = min(round(min(empWage,15000) × 8.33%), ₹1,250). EPF employer = round(empWage × 12%) − EPS.
- LOP pro-rate cap option. Known-good: wage 10k → emp 1200, EPS 833, EPF 367. wage 30k restricted → emp 1800, EPS 1249.50, EPF 550.50.

### 5.4 ESI (`ESICalculator`)
- Exempt if !enabled OR isExempt OR gross > limit (21000; PWD 25000). Else emp = round(gross×0.75%), employer = round(gross×3.25%).
- Boundary: 21000 inclusive (covered); 21001 exempt (non-PWD). Known-good: 10k → 75 / 325; 21000 → 157.50 / 682.50.

### 5.5 PT (`PTCalculator`)
- State+frequency+effective-date slab match on gross. Monthly = every month; Annual = only in DeductionMonths; HalfYearlySplit (Kerala) = lookup on half-year gross, floor split, last month absorbs remainder. No slab → exempt. Known-good: MH 8k → 175; MH 12k → 200.

### 5.6 LWF (`LWFCalculator`)
- State lookup; exempt if no state or gross > threshold. Frequency Monthly/Annual(month)/HalfYearly(Jun+Dec). Fixed amounts or percentage (with optional caps). Known-good: MH 25/75 monthly; WB 0.5%/1.5% capped 30/100.

### 5.7 Gratuity (`GratuityCalculator`)
- MonthlyAccrual = round(BasicWage × 15 / 26 / 12). Exempt if disabled or basic ≤ 0. 25000 → 120.19.

### 5.8 Net pay (`PayrollEngine`)
- Net = Gross − TDS − PF.Employee − VPF − ESI.Employee − PT − LWF.Employee. No final-sum rounding.

**Worked end-to-end (verify in UI):** CTC 70k (BASIC 28k taxable+EPF, HRA 14k, FIXED 28k taxable), MH, no LOP, PAN, no VPF → Gross 70k, TDS 0 (within rebate), PF emp 1800, ESI 0 (>21k), PT 200, LWF 25 → **Net ₹67,975**.

---

## 6. MODALS · DRAWERS · DIALOGS

| Dialog | Trigger | Blocks? | Notes |
|---|---|---|---|
| +New Department/Designation/Business Unit | wizard Step 1 inline | — | quick-create |
| Delete confirm (settings) | trash icon | yes | "Delete "{name}"? This cannot be undone." |
| EPF disable confirm | Disable EPF | yes | dedicated modal |
| PT View Slabs | View Slabs | no | read-only table |
| PT Revise Slabs | Revise Slabs | yes | effective date, frequency, deduction months, editable rows |
| PT Number | Add PT Number | yes | single field |
| Cancel exit confirm | kebab on exited emp | yes | restores Active, removes FnF run |
| Approve Payroll | Approve | yes | warns irreversible-ish; locks reimbursements/IT declarations |
| Record Payment | Record Payment | yes | date, mode, reference, notify checkbox |
| Reject Approval | Reject | yes | optional reason → reverts to Draft |
| Delete Payment | kebab (Paid) | yes | optional reason → reverts to Approved |
| Skip Employee | Skip (Draft) | yes | reason required |
| Bank Advice | Bank Advice | no | download XLSX (FinanceViewer) |
| Import (LOP/Earnings/Reimb) | Import dropdown | — | file upload, template, error list |
| Export | Export | — | CSV/XLSX selector |
| Payslip Panel | Download/row | no | 520px slide-in; Download + Send + Close |
| 5 salary-component add-modals | Add dropdown | yes | per §3.11 |

**Modal behavior to verify:** ESC closes, backdrop click closes (Modal/Drawer), form-in-modal validates+submits+closes, confirmation truly blocks until confirmed.

---

## 7. STATE TRANSITIONS

### 7.1 Payroll run (`PayrollRunStatus`)
`Draft → Approved` (Approve), `Approved → Draft` (RejectApproval), `Approved → Paid` (RecordPayment), `Paid → Approved` (DeletePayment), `Draft → Deleted` (Delete, Draft only), `* → Failed` (MarkFailed). Immutable once non-Draft: SetLop/AddEarning/AddDeduction/Skip/OverrideTds/UpdateSettlementDate/Delete throw if Status≠Draft. UI hides edit buttons accordingly.

### 7.2 Tenant
Created (IsActive=true) → Suspended (IsActive=false) → Activated. Suspended blocks token issuance + resend-setup-email.

### 7.3 Employee
Active → exit scheduled (dateOfLeaving set, FnF run created) → settled. Cancel exit restores Active. profileComplete flag drives Incomplete badge.

### 7.4 FnF zero-value semantics (WI-30)
Zero amount for a FnF component (bonus/commission/leave encashment/gratuity/notice pay) **deletes** that component.

---

## 8. PERMISSION-GATED ACTIONS (UI)
- Add Employee button: disabled until departments + designations + work-locations + salary-structure exist (onboarding hook); tooltip lists missing prereqs.
- Exit kebab: only if status=Active; Cancel Exit only if dateOfLeaving set.
- Pay schedule work-week/calc: disabled (opacity-60) after first paid run.
- Settings deletes: blocked with error if entity assigned to employees.
- Bank Advice / exports: API FinanceViewer policy (403 if unauthorized).

---

## 9. EVERY PLACE REAL NUMBERS DISPLAY
- Dashboard KPIs: Active Employees count, Current Period, Pay Run Status, Last Paid Run (count + formatINR net).
- Setup checklist: `{completed}/{total}` + % bar.
- Salary Step 2 preview: CTC, monthly, deductions, take-home, employer contributions, benefits.
- Employee detail Salary tab: Annual CTC, monthly, per-component monthly/annual.
- Employee Tax tab: months count, gross, TDS deducted, PF deducted (FY opening).
- Pay run header: Employees, Net Pay, Payroll Cost.
- Employee summary rows: Gross, Deductions, Taxes, Net, LOP / Net, TDS, PF.
- Taxes worksheet: projected income, std deduction, taxable, slab tax, rebate, surcharge, cess, annual liability, monthly TDS.
- Payslip panel: earnings, benefits, deductions, gross, total deductions, net + net-in-words, bank last-4, IFSC.
- Statutory tabs: EPF 12%/15000, ESI 0.75%/3.25%/21000/25000, PT slabs, LWF amounts/%, Bonus 8.33–20%.
- formatINR: Indian lakh/crore grouping, 2 decimals, ₹ prefix. e.g. `1234567.89 → ₹12,34,567.89`. Dates dd/MM/yyyy.

---

## 10. FILE HANDLING
- Org logo upload: PNG/JPEG ≤2MB; Upload/Replace/Delete; error "Logo upload failed. Must be PNG/JPEG under 2 MB."
- Employee import: `.xlsx` only, ≤1000 rows; download template; validate (ready/skip/error counts); error report CSV; overwrite-existing toggle; commit; done summary.
- Pay run imports: LOP/Earnings/Reimbursements CSV with templates + error lists.
- Pay run exports: CSV/XLSX; bank advice XLSX; payroll-details + tds-breakup (FinanceViewer).
- Payslip PDF: download per employee + email send (MailHog).

---

## 11. VISUAL / LAYOUT RISK AREAS
- Sidebar `w-56`, bg `#1e293b`, active `#2563eb` (verify white text contrast). Topbar h-14 white, org badge + avatar initials. Content bg `#f8fafc`.
- Long employee names (13px, no wrap) → truncation/overflow.
- Large currency (₹12,34,567.89) in tight right-aligned columns → tabular-nums alignment.
- Dense employee-summary + taxes tables (expandable rows, 50+ rows) → row height, hover, pagination.
- KPI grid responsive 1→2→4 col.
- Setup checklist 9 steps + sub-steps indent.
- Drawer w-[480px] / Payslip panel 520px → overflow on small desktop/tablet.
- Modal max-w-md centered on 360px.
- Toast top-right stacking, auto-dismiss (success/info 3s, warning 5s, error manual).
- Empty states + error states must not break layout.
- Masked fields render correctly (PAN/Aadhaar XXXX-XXXX-1234, account password-masked).
- Design tokens (full hex list captured in dashboard read) — spot-check sidebar/page/badge colors.

---

## 12. EDGE CASES TO COVER
- Empty DB: brand-new tenant, no employees/runs (dim KPIs, empty states, gated Add Employee).
- Max data: many employees, long strings, large numbers, pagination tail.
- Boundary calc inputs: income at/just over 87A limit (12L), ESI 21000/21001, PF at/over 15000, PT slab edges, gross=0.
- No-PAN TDS 20% path.
- Mid-year joiner (prior-employer YTD).
- One-time earning projection (×1 not ×N).
- LOP proration + flat/non-prorata components.
- Back-button after submit; duplicate submission prevention.
- Suspended tenant login block; expired reset token (1h).
- Field divergences flagged: displayName FE 200 / BE 100; password Zod 8 / Identity 12.

---

## 13. KNOWN GAPS (carry to GAPS.md in Phase 2)
- **UI-INACCESSIBLE SETUP:** SuperAdmin only via env seed (`admin@payroll.local`). No UI to create SuperAdmin.
- **CONFIRMED: No Users management UI.** `UsersController` + OrgAdmin policy exist API-side, but no route/nav/page is wired to it. HRManager/PayrollManager/FinanceViewer/Employee are uncreatable via UI → full permission matrix for those 4 roles is UI-inaccessible (test only SuperAdmin vs OrgAdmin).
- Token-expiry tests (1h reset link) not feasible in a fast UI run without clock control — flag.
- Several statutory rates hardcoded in FE display (EPF 12%, ESI 0.75/3.25) vs DB-driven engine — verify consistency, not editability.
- **§5 known-good numbers are Engine.Tests fixtures, illustrative only.** Phase-2 calc assertions must hand-compute from the *live tenant's* configured rates as shown on StatutoryComponents/TaxDetails pages — never assert fixture rates blindly (false-mismatch risk).
- Salary-structure **template builder** (`/settings/salary-structures/new` & `/edit`) under-specified here vs the per-employee salary step (§3.6); fill its field/residual-CTC/live-breakdown spec at the start of Phase 1.

---

*End of AUDIT_PLAN.md — Phase 0 complete. Awaiting approval before Phase 1 (TEST_PLAN.md).*
