# TEST_PLAN.md — Granular QA Checklist

> Derived from AUDIT_PLAN.md. Every item = one atomic check a human tester ticks off, with exact expected outcome. UI-only (no API shortcuts). Maps 1:1 to Phase-2 Playwright tests.
> Roles UI-reachable: **SuperAdmin** (`admin@payroll.local`), **OrgAdmin** (provisioned). Other 4 roles → GAPS.
> Statutory assertions: compute from rates shown in UI (Statutory/Tax pages), not engine fixtures.

---

## A. AUTH & SESSION

### A1. Login
- [ ] `/login` renders email + password fields + submit button
- [ ] Blank email → submit blocked, "Enter a valid email"
- [ ] Malformed email ("foo") → "Enter a valid email"
- [ ] Blank password → "Required"
- [ ] Wrong password for real user → "Invalid credentials or server error.", stays on /login
- [ ] Unknown email → same error (no enumeration difference)
- [ ] Valid SuperAdmin creds → redirect to `/platform/orgs`
- [ ] Valid OrgAdmin creds → redirect to `/dashboard`
- [ ] Token persisted; reload keeps session
- [ ] 6+ rapid bad logins → rate limited (5/min/IP) — flag if not observable in UI

### A2. Forgot / set password
- [ ] `/forgot-password` blank email → "Required"
- [ ] Malformed email → "Invalid email"
- [ ] Valid existing email → "If that email exists, a reset link has been sent."
- [ ] Nonexistent email → identical success message (no enumeration)
- [ ] Reset email arrives in MailHog with set-password link
- [ ] `/set-password` with no token/email params → error "Invalid or missing link parameters…"
- [ ] Password < 8 chars → rule error shown
- [ ] Password missing uppercase → specific error
- [ ] Password missing lowercase → specific error
- [ ] Password missing digit → specific error
- [ ] Password missing special char → specific error
- [ ] confirmPassword mismatch → error
- [ ] 8–11 char valid-pattern password → DIVERGENCE check (Zod 8 vs Identity 12): record which wins
- [ ] Valid new password → "Password set successfully…" + login link
- [ ] Login with new password → succeeds

### A3. Guards & redirects
- [ ] Anonymous → `/dashboard` redirects to `/login`
- [ ] Anonymous → `/platform/orgs` redirects to `/login`
- [ ] SuperAdmin → `/dashboard` redirects to `/platform/orgs`
- [ ] OrgAdmin → `/platform/orgs` redirects to `/`
- [ ] Logout from sidebar → returns to `/login`, session cleared, back-button cannot re-enter app
- [ ] Bad route `/zzz` → redirects to `/`

---

## B. PLATFORM ADMIN (SuperAdmin)

### B1. Tenants list `/platform/orgs`
- [ ] Header "Organisations" + subtitle visible
- [ ] "+ Provision New Organisation" button present
- [ ] Empty DB → "No organisations yet. Provision the first one."
- [ ] Columns: Name, Slug, Status, Created
- [ ] Active tenant → green "Active" badge
- [ ] Row click → `/platform/orgs/{id}`

### B2. Provision org `/platform/orgs/new`
- [ ] Blank displayName → "Required"
- [ ] Typing displayName auto-fills slug (lowercase, hyphenated)
- [ ] Manually editing slug stops auto-derive
- [ ] Slug with uppercase/space → invalid (regex)
- [ ] Slug < 3 chars → invalid
- [ ] Blank adminEmail → required
- [ ] Malformed adminEmail → invalid
- [ ] displayName 101–200 chars → DIVERGENCE check (FE 200 / BE 100): record outcome
- [ ] Valid submit → 201, redirect to list, new org appears
- [ ] Duplicate slug → 409 "That slug is already taken. Choose a different one."
- [ ] Welcome email for adminEmail lands in MailHog with set-password link

### B3. Org detail `/platform/orgs/:id`
- [ ] Shows DisplayName, Slug (mono), Schema, AdminEmail, Created (en-IN)
- [ ] Active org shows "Suspend Org" (red) + "Resend Setup Email"
- [ ] Suspend → status flips to red "Suspended", button becomes "Activate Org"
- [ ] Suspended org: provisioned OrgAdmin login → 403 (verify on /login)
- [ ] Resend Setup Email on active org → "Email Sent ✓", new email in MailHog
- [ ] Resend on suspended org → error (blocked)
- [ ] Activate → status back to green "Active"

---

## C. DASHBOARD (OrgAdmin)

### C1. KPI tiles
- [ ] 4 tiles render: Active Employees, Current Period, Pay Run Status, Last Paid Run
- [ ] New tenant, 0 employees → tiles dimmed (opacity-70)
- [ ] After adding N active employees → Active Employees shows exact N, not dim
- [ ] No outstanding run → Pay Run Status "good" tone (green icon)
- [ ] Outstanding run exists → "warn" tone + AlertCircle icon
- [ ] Setup complete + 0 history → Last Paid Run shows "—"

### C2. Setup checklist
- [ ] Card shows `{completed}/{total}` and % progress bar
- [ ] 9 steps listed (org-profile, tax-details, work-locations, org-structure, pay-schedule, statutory, salary-structure, deductor-employee, first-employee)
- [ ] Pending step → Circle icon; complete step → Check + "Completed" badge
- [ ] "Apply defaults" appears only on work-locations / org-structure / salary-structure
- [ ] "Apply defaults" click → disabled + "Applying…" → step completes, progress increments
- [ ] Primary CTA text matches current step
- [ ] pay-schedule shows "Locks after 1st run" / "Locked" badge appropriately
- [ ] Card disappears when setupComplete=true
- [ ] Welcome banner greets by name; dismiss persists (localStorage), gone on reload
- [ ] Preflight warning banner shows amber + Configure link when applicable

### C3. Dashboard cards
- [ ] Process Pay Run card appears when period set + no outstanding run; subtitle shows active count
- [ ] Click → navigates to pay-runs
- [ ] Last Paid Run card shows count + formatINR net pay when history exists

---

## D. SETTINGS — ORG SETUP

### D1. Org Profile
- [ ] Blank Company Name → required
- [ ] Blank Company PAN → required
- [ ] Invalid PAN ("ABC") → format error (regex AAAAA9999A)
- [ ] Valid PAN accepted
- [ ] GSTIN invalid format → backend error surfaced
- [ ] PIN non-6-digit → error
- [ ] Industry dropdown has 14 options
- [ ] State dropdown has 35 options
- [ ] Save valid → "Organisation profile saved" toast
- [ ] Reload page → saved values persist
- [ ] Logo upload PNG ≤2MB → "Logo uploaded", logo renders
- [ ] Logo upload >2MB or non-PNG/JPEG → "Logo upload failed. Must be PNG/JPEG under 2 MB."
- [ ] Logo Replace + Delete work

### D2. Work Locations
- [ ] Empty → "No work locations yet" + Add button
- [ ] Create: blank Name → required
- [ ] Create: no State → required
- [ ] Create: PIN non-6-digit → error
- [ ] Valid create → card appears with name/address/employee count (0)
- [ ] Edit: State field read-only (cannot change)
- [ ] Edit: PT Registration Number field present (≤50)
- [ ] Kebab → Mark Inactive → "Inactive" badge
- [ ] Delete unused location → confirm "Delete "{name}"? This cannot be undone." → removed
- [ ] Delete location with employees → blocked, "Cannot delete — employees are assigned…"

### D3. Departments / Designations / Business Units
- [ ] Department: blank name → required; create → row appears
- [ ] Department: Code ≤20, Description ≤250 enforced
- [ ] Department delete when assigned → blocked with message
- [ ] Designation: blank name → required; create → appears
- [ ] Designation delete when assigned → blocked
- [ ] Business Unit: blank name → required; Description ≤500
- [ ] Business Unit delete when assigned → blocked
- [ ] Each modal: ESC closes, backdrop click closes, Cancel closes without saving

### D4. Pay Schedule
- [ ] Deselect all work-week days → Save disabled
- [ ] Select ≥1 day → Save enabled
- [ ] Salary Calc = FixedDays → Fixed Working Days field appears (1–31)
- [ ] Fixed days 0 or 32 → invalid
- [ ] Pay Date = SpecificDay → day dropdown 1–30
- [ ] First Payroll: set Month only → Year required (and vice-versa)
- [ ] Preview shows 3 upcoming pay dates (working-day adjusted)
- [ ] Save → "Pay schedule saved"
- [ ] After first processed run → work-week + calc method disabled (opacity-60); pay date still editable

### D5. Tax Details
- [ ] PAN shown read-only (from org profile) or "Set in Company Profile" link if missing
- [ ] TAN ≤10, auto-uppercase
- [ ] AO Area Code ≤3 uppercase; AO Type dropdown 10 options; Range ≤3 numeric; AO Number ≤5 numeric
- [ ] Deductor Type dropdown 7 options
- [ ] Deductor Type ≠ Company → Father's Name + Designation fields appear
- [ ] Tax Deductor Employee searchable dropdown (active employees); shows "required for exit initiation" if unset
- [ ] Save → "Tax details saved", persists on reload

---

## E. SETTINGS — STATUTORY

### E1. EPF
- [ ] Employee Contribution shows "12% of PF Wage (statutory)", read-only in edit
- [ ] Employer rate selector: "12% of Actual PF Wage" / "12% of Restricted PF Wage (₹15,000)"
- [ ] Include Employer EPF in CTC toggle
- [ ] LOP options: Pro-rate Restricted PF Wage, Consider salary on LOP
- [ ] Save → "EPF settings saved"
- [ ] Disable EPF → confirmation modal → "EPF disabled"; re-enable works

### E2. ESI
- [ ] Employee 0.75%, Employer 3.25%, cycle Monthly displayed
- [ ] Info box: "≤ ₹21,000/month gross; PWD ≤ ₹25,000/month"
- [ ] Establishment Code editable; Notified Area checkbox
- [ ] Save persists

### E3. Professional Tax
- [ ] Per-state rows for states with work locations
- [ ] "Add PT Number" → modal single field → saves, "PT number updated"
- [ ] View Slabs → read-only table (Min/Max Gross, PT Amount, Frequency, Gender if any)
- [ ] Revise Slabs → effective date req, frequency dropdown, editable rows, Add Row, per-row delete
- [ ] Save slabs → "PT slabs updated"; note about historical retention shown

### E4. LWF
- [ ] Per-state rows (12 LWF states) with amount/% + frequency + threshold
- [ ] Enabled/Disabled badge
- [ ] Toggle Enable/Disable → "LWF status updated"

### E5. Statutory Bonus
- [ ] Disabled → info + "Enable Statutory Bonus"
- [ ] Enabled → shows rate, payout mode
- [ ] Bonus Rate < 8.33 or > 20 → invalid (step 0.01)
- [ ] Payout Mode Yearly → Payout Month dropdown (12 months)
- [ ] Save → "Bonus configuration saved"; Disable works

---

## F. SALARY COMPONENTS & STRUCTURES

### F1. Components list
- [ ] 6 tabs: All/Earnings/Deductions/Reimbursements/Benefits/Corrections — filter correctly
- [ ] Add dropdown: 5 options
- [ ] Columns: Name (+payslip subtitle), Code, Category, Calculation, Status, Actions
- [ ] Calculation renders correctly ("50% of Gross", "Fixed ₹5,000", "Residual CTC")

### F2. Add Earning modal
- [ ] Blank Name → required; Name in Payslip auto-fills from Name
- [ ] Earning Type dropdown 32 options
- [ ] Pay Type Monthly → Formula Type radio (Fixed/%Basic/%Gross/%CTC)
- [ ] Formula Fixed → Amount required >0
- [ ] Formula % → Percentage 0–100 required
- [ ] One-Time checked → Pro-rata disabled, formula forced Fixed
- [ ] EPF checked → EPF Inclusion Rule dropdown appears
- [ ] Create → appears in Earnings tab
- [ ] ESC/backdrop/Cancel close without saving

### F3. Add Deduction / Benefit / Reimbursement / Correction
- [ ] Deduction: Frequency options (EveryMonth/OnceAYear/Quarterly/HalfYearly); One-Time forces EveryMonth
- [ ] Benefit VPF → Percentage 1–100 required
- [ ] Benefit NPS → Govt Sector checkbox
- [ ] Reimbursement: Name + Amount
- [ ] Correction: Formula Type + %/Amount

### F4. Salary structure template builder `/settings/salary-structures/new`
- [ ] Builder renders component composition UI
- [ ] Residual-CTC component behavior (one residual)
- [ ] Live CTC breakdown updates as components change
- [ ] Save → appears in `/settings/salary-structures` list
- [ ] Edit existing → loads values, saves changes
- [ ] (Spec thin in audit — verify fields during test, log gaps)

---

## G. EMPLOYEES

### G1. List `/employees`
- [ ] Empty → "No employees yet. Add your first employee."
- [ ] Add Employee disabled until departments+designations+work-locations+salary-structure exist; tooltip lists missing
- [ ] Columns: Employee, Code, Department, Location, Type, Joined (dd/MM/yyyy), Status
- [ ] Status tabs All/Active/Inactive/Exited filter + reset to page 1
- [ ] Search by name → filters; by code → filters; by email → filters
- [ ] No match → "No employees match this filter."
- [ ] Incomplete profile → "Incomplete" badge + banner with count
- [ ] Pagination: page size persists (localStorage); next/prev bounded
- [ ] Row click → `/employees/{id}`

### G2. Wizard Step 1 Basic
- [ ] Blank First/Last Name → required
- [ ] Blank Work Email → required; malformed → error
- [ ] Mobile non-10-digit → error
- [ ] Missing Gender/DoJ/DoB/Employment Type → required
- [ ] Missing Department/Designation/Work Location → required
- [ ] +New Department modal inline → creates + selects
- [ ] +New Designation / Business Unit inline works
- [ ] Duplicate email → DomainException error shown
- [ ] Employee Code blank → auto-generated
- [ ] Valid → Save and Continue → Step 2 (salary)

### G3. Wizard Step 2 Salary
- [ ] Annual CTC blank or 0 → invalid (>0)
- [ ] CTC entered → monthly breakdown shown
- [ ] Template select → pre-fills components + statutory checkboxes
- [ ] % override 0–100 step 0.5; fixed step 100
- [ ] Reset override → reverts to template default; button hidden if unchanged
- [ ] Add Earning → component + formula type + amount/% conditional
- [ ] Add Benefit → fixed ₹/month
- [ ] Preview sections: deductions, take-home, employer contributions, benefits, CTC footer — numbers reconcile (sum of components = CTC)
- [ ] Save and Continue → Step 3

### G4. Wizard Step 3 Personal
- [ ] Blank Father's Name → required
- [ ] PAN invalid format → error; valid accepted
- [ ] Aadhaar non-12-digit → error
- [ ] Personal Email malformed → error
- [ ] PIN non-6-digit → error
- [ ] Residential State 28+ options
- [ ] Save → Step 4

### G5. Wizard Step 4 Payment
- [ ] Only Bank Transfer enabled; others "Coming soon" disabled
- [ ] Bank mode → Account Holder/Bank Name/Account Type/Account Number/Confirm/IFSC required
- [ ] Account Number masked (password field)
- [ ] Confirm Account ≠ Account → mismatch error
- [ ] IFSC ≠ 11 chars → error
- [ ] Save and Finish → `/employees/{id}` detail

### G6. Employee detail
- [ ] Header: avatar, name, code, status badge, Incomplete badge if applicable
- [ ] 5 tabs: Overview, Salary Details, Tax, Investments, Payslips & Forms
- [ ] Overview edit-in-place: Basic, Personal, Statutory, Payment sections save
- [ ] PAN displayed masked (XXXX-XXXX-1234); Aadhaar masked; account masked
- [ ] Salary tab: Annual CTC, monthly, component table (monthly/annual); Revise button
- [ ] Revise → wizard salary step with ?revise=1; Save returns to detail
- [ ] Tax tab: FY dropdown; months/gross/TDS/PF opening edit + save
- [ ] Investments → "Coming soon"
- [ ] Payslips tab: table period/generated/net/download/published
- [ ] Kebab "Initiate Exit Process" visible only if Active + no exit
- [ ] After exit scheduled → kebab "Cancel Exit Process"

### G7. Import `/employees/import`
- [ ] Download Template → XLSX file
- [ ] Upload non-xlsx → rejected client-side
- [ ] Upload >1000 rows → rejected
- [ ] Valid file → validation summary: ready / skip / error counts
- [ ] Errors present → error table (Row/Employee No/Error) + Download Error Report (CSV)
- [ ] Import button disabled if errors>0 or ready=0
- [ ] Overwrite-existing toggle re-validates counts
- [ ] Commit → "Import Complete" + summary (added/updated/skipped)
- [ ] View Employees → new rows present

### G8. Exit initiation `/employees/:id/exit/initiate`
- [ ] Last Working Day < today−30 → error
- [ ] Last Working Day > today+5yr → error
- [ ] Settlement Mode CustomDate → Final Settlement Date appears, must be ≥ LWD
- [ ] Notes >2000 → error
- [ ] Submit single → routes to `/pay-runs/{id}/fnf`; FnF run created
- [ ] Cancel exit (from detail) → confirm dialog → employee restored Active, FnF run removed

---

## H. PAYROLL RUNS

### H1. Initiate
- [ ] `/pay-runs` Run Payroll tab shows current period + Process Payroll
- [ ] Process Payroll → "Calculating payroll…" spinner, polls
- [ ] On complete → navigates to `/pay-runs/{id}` (Draft)
- [ ] Failure → red error banner

### H2. Detail — Draft
- [ ] Header: period + Draft badge, Employees count, Net Pay, Payroll Cost
- [ ] 3 tabs: Employee Summary, Taxes & Deductions, Overall Insights ("coming soon")
- [ ] Employee Summary columns: Gross, Deductions (PF+ESI+PT+LWF), Taxes, Net, LOP
- [ ] Filter tabs All/Active/Skipped with counts
- [ ] Row expand → component breakdown
- [ ] Skip employee → reason required → row opacity-60 + amber reason; count moves to Skipped
- [ ] Undo skip → returns to Active
- [ ] Pending tasks banner lists blockers (Draft)
- [ ] Import LOP/Earnings/Reimbursements (Draft) + Export
- [ ] Approve Payroll → confirmation (warns irreversible) → status Approved
- [ ] Delete (kebab, Draft) → removes run

### H3. Detail — Approved / Paid
- [ ] Approved: buttons Bank Advice, Record Payment, Reject
- [ ] Edit controls (LOP/earnings/skip/TDS override) hidden/disabled when not Draft
- [ ] Reject → optional reason → reverts to Draft
- [ ] Record Payment → date/mode/reference/notify → status Paid
- [ ] Paid columns: Net Pay, TDS, PF
- [ ] Paid: only Bank Advice + kebab "Delete Recorded Payment"
- [ ] Delete Recorded Payment → reverts to Approved

### H4. Calculations (verify UI vs hand-compute from configured rates)
- [ ] Gross = sum of component amounts (no LOP)
- [ ] LOP>0 → prorated components by (baseDays−LOP)/baseDays, flat/non-prorata unchanged
- [ ] PF employee = min(pfWage, ceiling)×rate from EPF config
- [ ] ESI: employee earning >₹21,000 gross → ESI 0 (exempt); ≤21,000 → 0.75%/3.25%
- [ ] PT matches state slab shown in Statutory PT tab
- [ ] LWF matches state config
- [ ] TDS within rebate (taxable ≤12L) → 0; above → slab-based
- [ ] No-PAN employee → TDS = 20% flat (verify §206AA badge in Taxes tab)
- [ ] Net = Gross − all deductions; matches displayed Net Pay exactly
- [ ] One-time earning → projected once in annual income, not ×months (Taxes tab)

### H5. Taxes tab
- [ ] Columns: Annual Projected, Taxable, Annual Tax Liability, TDS This Month, PAN status
- [ ] Expand → worksheet: projected, std deduction, taxable, slab tax, rebate 87A, surcharge, cess 4%, liability, monthly TDS
- [ ] Worksheet arithmetic internally consistent (slab tax − rebate + surcharge + cess = liability)
- [ ] No-PAN → §206AA amber badge

### H6. Payslip
- [ ] Row Download → PDF downloads
- [ ] Payslip Panel: earnings, benefits, deductions, gross, total deductions, net + net-in-words, bank last-4, IFSC
- [ ] Net in words matches numeric net
- [ ] Send → email to employee (MailHog)
- [ ] Bank Advice → XLSX download

### H7. History
- [ ] History tab columns: Payment Date, Type, Period, Employees, Total Net Pay, "Paid"
- [ ] Type filter (All/Regular/FnF/Bulk FnF) works
- [ ] Empty → "No completed pay runs yet."
- [ ] Row click → detail

---

## I. FNF SETTLEMENT

- [ ] `/pay-runs/:id/fnf` loads for FinalSettlement Draft
- [ ] LOP Days input (min 0)
- [ ] Additional earnings: Bonus, Commission, Leave Encashment, Gratuity (Sec 10(10) hint)
- [ ] Deductions dynamic add/remove (name+amount)
- [ ] Notice Pay: checkbox + Payable/Receivable radio + amount
- [ ] Notes textarea
- [ ] Zero amount for a component → component removed (WI-30)
- [ ] Preview → categorized summary without saving
- [ ] Settlement date editable (Draft only)
- [ ] Save → FnF summary computes net
- [ ] Bulk FnF → employee dropdown, save each

---

## J. CROSS-CUTTING

### J1. Modals/drawers global
- [ ] Every modal closes on ESC
- [ ] Every modal closes on backdrop click (where expected)
- [ ] Drawer/Payslip panel (480/520px) closes on ESC + close button
- [ ] Confirmation dialogs block action until confirmed

### J2. Toasts
- [ ] Success toast top-right, auto-dismiss ~3s
- [ ] Warning ~5s
- [ ] Error → manual dismiss only
- [ ] No silent failures (every failed action shows a message)

### J3. formatINR / dates
- [ ] 1234567.89 → ₹12,34,567.89 (lakh/crore grouping)
- [ ] 0 → ₹0.00
- [ ] Dates display dd/MM/yyyy everywhere

### J4. Visual / layout
- [ ] Sidebar bg #1e293b; active item white text on #2563eb
- [ ] Topbar h-14 white; org badge + avatar initials correct
- [ ] Content bg #f8fafc
- [ ] Long employee name (40+ chars) → no broken layout (truncate/wrap)
- [ ] Large currency in right-aligned column → tabular alignment, no overflow
- [ ] Dense table (25+ rows) → row height consistent, hover works, pagination correct
- [ ] KPI grid responsive 1→2→4 col across widths
- [ ] Empty states centered, don't break layout
- [ ] Error states don't break layout
- [ ] Masked PAN/Aadhaar/account render correctly in detail view

### J5. Edge / negative
- [ ] Back button after form submit → no duplicate / stale state
- [ ] Double-click submit → no duplicate record
- [ ] Income at 87A boundary (taxable exactly 12L) → TDS 0
- [ ] ESI gross exactly 21000 → covered; 21001 → exempt
- [ ] PF wage exactly 15000 vs above → cap applied
- [ ] Gross 0 employee → no crash, zero deductions

---

## K. GAPS (UI-inaccessible — record, do not test as pass/fail)
- [ ] SuperAdmin creation (env-seeded only) → GAPS
- [ ] HRManager/PayrollManager/FinanceViewer/Employee creation (no Users UI) → GAPS; permission-matrix for these 4 roles untestable
- [ ] 1-hour reset-token expiry (no clock control in fast run) → GAPS
- [ ] Rate-limit 5/min observability via UI → GAPS if not surfaced

---

*End TEST_PLAN.md — Phase 1 complete. Proceeding to Phase 2: Playwright specs.*

---

# DEEP COVERAGE ADDENDUM (Round 2)

> Selectors confirmed from source. New spec files: calc, fnf, files, employee-detail, payrun-state, platform-extra.

## L. Deep calculation worksheet (calc.spec.ts)
- [ ] Taxes & Deductions tab opens; columns: Employee, Annual Projected, Taxable Income, Annual Tax, TDS This Month, PAN
- [ ] Expand a row → worksheet lines visible: Annual Projected Income, Standard Deduction, Taxable Income, Tax (Slab), Rebate u/s 87A (if>0), Surcharge (if>0), Health & Education Cess (4%) (if>0), Annual Tax Liability, TDS This Month
- [ ] Worksheet arithmetic: TaxableIncome = AnnualProjected − StandardDeduction
- [ ] Worksheet arithmetic: AnnualTaxLiability = (Tax(Slab) − Rebate87A + Surcharge + Cess)
- [ ] Income ≤ ₹12,00,000 taxable → Rebate 87A applied → low/zero tax
- [ ] Mid/high CTC → cess = 4% of (tax+surcharge), verified from displayed values
- [ ] PF employee deduction = 12% of min(PF-wage, 15000) for high-basic employee
- [ ] No-PAN employee → §206AA amber badge shown in PAN column
- [ ] No-PAN employee → annual tax = 20% of projected income (flat)
- [ ] LOP days set on a payable employee → gross prorates down vs full
- [ ] Net = Gross − Deductions − Taxes after LOP (invariant still holds)

## M. FnF settlement (fnf.spec.ts)
- [ ] Active employee detail → kebab → "Initiate Exit Process" visible
- [ ] Exit form: Last Working Day, Reason dropdown (4 options), Settlement Mode radios, conditional Final Settlement Date, Personal Email, Notes
- [ ] Submit disabled until last working day + reason (+ settlement date if CustomDate)
- [ ] "Proceed" → navigates to /pay-runs/:id/fnf (single FnF)
- [ ] FnF page inputs: LOP, Bonus, Commission, Leave Encashment, Gratuity (Sec 10(10) hint), Notice Pay checkbox→Payable/Receivable+amount, dynamic Deductions add/remove, Notes
- [ ] Save and Continue persists; reopening shows saved values
- [ ] Zero-amount deduction removed on save (WI-30)
- [ ] Cancel Exit Process → confirm dialog → employee restored Active, FnF run removed

## N. File upload/download (files.spec.ts)
- [ ] Logo: upload valid PNG ≤2MB → "Logo uploaded" toast, logo renders
- [ ] Logo: wrong type / >2MB → "Logo upload failed. Must be PNG/JPEG under 2 MB."
- [ ] Logo: Replace + Delete work
- [ ] Import: Download Template → .xlsx file downloads
- [ ] Import: upload valid xlsx → validation counts (ready/skip/error) render
- [ ] Import: error rows → "Download Error Report" CSV
- [ ] Import: commit → "Import Complete" + added/updated/skipped summary; employees appear
- [ ] Payslip PDF: download from summary row / panel → application/pdf
- [ ] Bank advice: Approved run → "Bank Advice" → Download → .xlsx

## O. Employee detail tabs + masking (employee-detail.spec.ts)
- [ ] 5 tabs switch: Overview, Salary Details, Tax, Investments, Payslips & Forms
- [ ] PAN renders masked as XXXXX{last4} in Personal section read view
- [ ] Account number renders masked as XXXX{last4} in Payment section
- [ ] Overview Basic section: Edit → change mobile → Save → persists
- [ ] Tax tab: FY dropdown (3 options); Add/Edit opening balances (Months/Gross/TDS/PF) → Save → persists
- [ ] Investments tab: "Coming soon"
- [ ] Payslips tab: "No payslips available yet" before any run

## P. Pay-run state machine (payrun-state.spec.ts)
- [ ] Payable employee run has 0 hard blocks (banner absent or only soft warnings)
- [ ] Draft → "Approve Payroll" → dialog "Submit and Approve" → status badge "Approved"
- [ ] Approved → buttons: Bank Advice, Record Payment, Reject
- [ ] Approved → edit controls (Skip/LOP) hidden (immutability)
- [ ] Record Payment dialog: Payment Date + reference + notify checkbox → "Record Payment" → status "Paid"
- [ ] Paid → only Bank Advice + kebab "Delete Recorded Payment"
- [ ] Delete Recorded Payment → status back to "Approved"
- [ ] Reject (from Approved) → reason → status back to "Draft"
- [ ] Draft kebab "Delete Pay Run" → navigates to /pay-runs

## Q. displayName divergence + role GAPS (platform-extra.spec.ts)
- [ ] Provision org with 101-char displayName → record outcome (FE allows ≤200, BE caps 100): expect rejection/error
- [ ] OrgAdmin direct-URL /platform/orgs → redirected to /
- [ ] GAPS: confirm no UI to create HRManager/PayrollManager/FinanceViewer/Employee
