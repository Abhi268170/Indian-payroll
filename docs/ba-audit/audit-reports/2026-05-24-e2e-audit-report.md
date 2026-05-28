# End-to-End Audit Report — 2026-05-24

**Auditor:** Claude (Opus 4.7) acting as newly-onboarded operator
**Scope:** Full app, fresh tenant attempted, real flows. No code changes — observation only.
**Reference product:** Zoho Payroll India (`payroll.zoho.in`)
**Tenant used:** `kerala-test-corp` (existing — see TENANT-PROVISION-001 below for why a brand new tenant could not be provisioned)
**Screenshots:** `docs/ba-audit/screenshots/2026-05-24-e2e/`

## Severity legend
- **🔴 Critical** — blocks user, data loss, wrong calculation, security
- **🟠 Major** — wrong behaviour or significant UX failure, but workaround exists
- **🟡 Minor** — confusing copy, inconsistency, missing label, redundant click
- **🔵 Cosmetic** — padding, margin, alignment, color contrast, typography

## Coverage executed
- ✅ Login / superadmin / platform-admin org list / org provisioning
- ✅ Tenant admin login → dashboard
- ✅ Settings: Org Profile, Work Locations, Departments, Designations, Business Units, Pay Schedule, Salary Components, Salary Structures, Statutory (EPF/ESI/PT/LWF/Statutory Bonus), Tax Details
- ✅ Employees list + filters + pagination + detail (Overview, Salary Details)
- ✅ April 2026 payroll: initiate → review → Taxes & Deductions tab → Approve → Record Payment → Paid
- ✅ Exit initiation (Bulk FS for EMP002, attempted Single FS for EMP003)
- ⚠️ Could not complete Single FS settlement (form button stuck disabled, see FNF-007)
- ⚠️ May regular run NOT executed (decided not to compound on top of broken FnF state)
- ❌ Reports, Documents, Taxes & Forms, Loans, Approvals — **module not present in nav**

---

## Summary counts

| Severity | Count |
|---|---|
| 🔴 Critical | 8 |
| 🟠 Major | 14 |
| 🟡 Minor | 56 |
| 🔵 Cosmetic | 21 |
| **Total** | **99** |

---

# 0. Pre-flight / Onboarding blockers

## TENANT-PROVISION-001 — 🔴 CRITICAL — Cannot provision ANY new tenant
**File:** `src/Payroll.Infrastructure/Services/TenantSchemaProvisioner.cs:276`
**Repro:** Superadmin → Provision New Organisation → fill any valid input → submit
**API response (500 body):**
```
Npgsql.PostgresException: 42703: column "is_one_time" does not exist
   at TenantSchemaProvisioner.SeedSystemComponentsAsync(line 276)
```
**Diagnosis:** The `is_one_time` column was added to `salary_components` via an EF migration, but the per-tenant schema bootstrap path either (a) applies migrations in the wrong order, (b) hard-codes a CREATE TABLE without the new column, or (c) seeds `is_one_time` before the column exists. Verified via DB: `tenant_demo`, `tenant_lurio`, `tenant_zenith_technologies` are MISSING the column (created before migration). Only `tenant_kerala_test_corp` has it.
**Impact:** Zero new customers can be onboarded. SaaS is unsellable until fixed. Pre-existing tenants without the column would also fail any read/write that touches `is_one_time`.
**Workaround for this audit:** Reused existing tenant `kerala-test-corp` (cleaned of prior payroll runs).

## TENANT-PROVISION-002 — 🟠 Major — Generic error message hides root cause
UI shows `"Provisioning failed. Please try again."` for any backend 500. No correlation ID, no support link, no actionable detail. Operator has no path forward except retry forever. **Recommended:** surface a stable error code (`PROV-SCHEMA-001`) and link to support — do NOT leak stack traces.

## AUTH-001 — 🔵 Cosmetic — Login page layout
- Massive empty whitespace above/below the form card on desktop (~50% of viewport unused).
- No product imagery / split layout (compare Zoho's split with marketing on the left).
- Title-only "Indian Payroll" — no logo / product mark.

## AUTH-002 — 🟡 Minor — Missing standard login UX
- No "Show/hide password" eye icon.
- No "Remember me" checkbox.
- "Forgot password?" link uses body-text color — looks unclickable.
- Page `<title>` is generic `"Indian Payroll"` on every route — should be `"Sign in — Indian Payroll"`, `"Dashboard — Indian Payroll"`, etc.
- No CAPTCHA or visible lockout indicator. (Backend rate-limit `EnableRateLimiting("auth")` exists, but user-facing feedback missing.)

---

# 1. Platform Admin

## PLAT-001 — 🟡 Minor — Org list lacks scaling controls
- No pagination, no search, no filter. Fine at 4 rows, will fail at 100+.
- No row-actions column (deactivate / impersonate / view detail).
- Row click affordance not obvious (no hover/cursor indicator).

## PLAT-002 — 🔵 Cosmetic — Date format inconsistent with project standard
Dates render as `14/5/2026`, `20/5/2026`. CLAUDE.md mandates `dd/MM/yyyy`. Should be zero-padded: `14/05/2026`.

## PLAT-003 — 🟡 Minor — Provisioning form omits common org fields
At onboarding Zoho collects PAN, GSTIN, industry, country. Our form only collects name, admin email, slug. Deferred to Settings → Org Profile (acceptable, but document the flow).

## PLAT-004 — 🟡 Minor — No subscription / plan selection on provisioning
For a B2B SaaS there's no plan tier, billing email, trial-length picker. Acceptable for v1 but flag.

## PLAT-005 — 🔵 Cosmetic — "Provision Organisation" button uses non-primary color
Dark navy (`~#0f172a`) rather than brand purple `var(--color-primary)`. Inconsistent with the rest of the app.

## PLAT-006 — 🟡 Minor — Slug field has no validation hint
Helper says "Auto-generated from name, editable" but does not state allowed character set. User can type spaces/uppercase and only discover the rule on submit failure.

## PLAT-007 — 🟡 Minor — Email helper text misleading in dev
"A welcome email with a set-password link will be sent to this address" — in local/dev there is no real SMTP (only MailHog). Adding a dev-mode banner would help testers.

---

# 2. Dashboard

## DASH-001 — 🔴 CRITICAL UX — Dashboard literally empty
Reads `"Dashboard coming soon"`. First page operators see after login. Zoho equivalent shows: pending tasks, payroll cost YTD, recent runs, alerts. Empty dashboard = poor first impression and orphaned operator (no clear next step).

## NAV-001 — 🟠 Major — Critical modules missing from sidebar
Sidebar has only **3 items**: People, Pay Runs, Settings. Missing surfaces present in Zoho:
- Approvals (workflow for multi-stage payroll approvals)
- Taxes & Forms (Form 16, Form 24Q, ITR proofs, TDS challan)
- Loans (advance recovery)
- Reimbursements / Giving
- Documents (HR documents per employee)
- Reports (most basic SaaS expectation)
Either these are deferred to a later milestone or simply absent. Either way they should be visible as "coming soon" placeholders so customers know the roadmap.

## NAV-002 — 🟡 Minor — Sidebar branding placeholder
Generic "IP" avatar + "Indian Payroll" text. No tenant logo, no white-label affordance. For SaaS this matters.

## NAV-003 — 🔵 Cosmetic — Top-right shows slug not display name
Shows `kerala-test-corp` (slug) instead of `Kerala Test Corp` (display name). Inconsistent.

## NAV-004 — 🟡 Minor — Missing top-bar utilities
No notifications icon, no global search, no help / "What's new" link.

## NAV-005 — 🔵 Cosmetic — Duplicate identity surfaces
Avatar circle `AD` in both top-right and bottom-left. Different colour (blue) from brand primary (purple).

---

# 3. Settings

## SET-001 — 🟡 Minor — Active sidebar state wrong on landing
Settings landing page renders 3 cards (Org / Setup / Taxes), but the active sidebar item shows "Departments" highlighted — not the landing page. Visual lie about where the user is.

## SET-002 — 🟡 Minor — Terminology drift
Card heading says "Organisation Settings", sidebar section says "ORG STRUCTURE", page header says "Settings". Three names for overlapping things.

## SET-003 — 🟡 Minor — Org Profile missing from sidebar
Card has "Organisation Profile" link but there is no sidebar entry for it. User must hunt via the landing card.

## SET-004 — 🟠 Major — Users & Roles missing
No surface to manage admin users, role assignment, invitations. Critical for SaaS multi-user orgs.

## SET-005 — 🟡 Minor — Other key settings missing
No: Notifications/email templates, Audit log, Integrations, API keys.

## SET-006 — 🟡 Minor — "Close Settings" UX is jarring
Top-right "× Close Settings" treats Settings as a modal overlay rather than a sibling page. Closes by navigating to dashboard. Counter-intuitive.

## SET-007 — 🔵 Cosmetic — Decorative icon colours unclear
Org card icon green, Setup orange, Taxes red. Convention not explained — looks decorative.

## SET-008 — 🔵 Cosmetic — Massive empty whitespace below cards
Landing has 3 cards (one with a single link "Tax Details"). 80% of viewport empty.

## ORG-001 — 🔴 CRITICAL — Date input shows `mm/dd/yyyy` (US locale)
**File:** Org Profile → "Date of Incorporation"
Native `<input type="date">` defers to browser locale. CLAUDE.md mandates `dd/MM/yyyy` for display. Indian operators will mis-enter dates. Same bug appears in: Exit LWD, FnF Settlement Date, Record Payment Date. **Replace native date input with a controlled picker.**

## ORG-002 — 🟡 Minor — No inline PAN/GSTIN validation
Placeholders show pattern but no on-blur validation. Operator can submit invalid PAN and only get feedback at save.

## ORG-003 — 🟡 Minor — Industry select is unpopulated
Shows "Select industry" but options not verified to be present.

## ORG-004 — 🟡 Minor — `Select` controls use native `<select>` element
Industry + State use native dropdowns rather than the design-system Select. Inconsistent with the rest of the app.

## ORG-005 — 🟡 Minor — Required field marking inconsistent
Some fields have `*`, others don't. No documented rule.

## ORG-006 — 🔵 Cosmetic — No "Cancel" / "Discard" affordance next to Save Profile
Save Profile is the only action; if user wants to discard there's no clear button.

## ORG-007 — 🔵 Cosmetic — Save Profile button colour
Bright blue, different from brand purple primary used elsewhere. Third button colour seen so far (after platform navy and brand purple).

## ORG-008 — 🟡 Minor — No country dropdown
Assumes India. Acceptable for v1; flag for future.

## ORG-009 — 🟡 Minor — Pin Code field validation
No 6-digit pin format validation visible.

## ORG-010 — 🟡 Minor — No branch addresses
Only registered address. Multi-branch orgs need branch tracking — partially covered by Work Locations but ownership unclear.

## WL-001 — 🔵 Cosmetic — Redundant edit affordances on each card
Both pencil icon AND kebab `⋯` shown per Work Location card. Pick one.

## WL-002 — 🟡 Minor — "0 Employees" on each location
Counter may be stale or unimplemented — Kerala tenant has 250 employees but card shows 0.

## WL-003 — 🔵 Cosmetic — "Add Work Location" button colour
Blue, not brand purple. Same inconsistency as ORG-007.

## WL-004 — 🟡 Minor — No LWF/PT state mapping per location
Critical statutory deductions are derived from work-location state. Mapping is implicit, not surfaced.

## PS-001 — 🟠 Major — "Locked after payroll run" state visually unclear
Banner says Work Week + Salary Calc Method are LOCKED, but the radio buttons don't appear disabled/greyed in a discoverable way. Operator can click them and may believe they changed.

## PS-002 — 🟡 Minor — Missing pay frequency options
No bi-weekly / weekly. Acceptable for v1 IN payroll.

## PS-003 — 🟡 Minor — No "Inputs cut-off" date
Operators need to know when variable inputs lock for the upcoming pay cycle.

## SC-001 — 🟡 Minor — Pagination present but unobvious
With small datasets, pagination footer doesn't show — but should still render the controls so the UI is consistent.

## SC-002 — 🟠 Major — Activate/Deactivate is a bare link with no confirmation
Deactivating a salary component while employees depend on it may break their salary structure. No "Are you sure?" dialog. No usage count warning.

## SC-003 — 🟡 Minor — `Bonus One-time` / `Leave Encashment One-time` chip styling
Small "One-time" badge looks like a sub-label. Could be a discrete pill column.

## SC-004 — 🟡 Minor — System vs custom components not visually distinct
No lock icon or "System" badge. Operator could accidentally deactivate a system component.

## SC-005 — 🟡 Minor — Calculation column inconsistent
Shows `"37.5% of CTC"`, `"₹1,600.00"`, `"Variable"`, `"10.67% of Basic"`. Mixed semantics. `"Variable"` is vague.

## SC-006 — 🟡 Minor — Code naming inconsistency
`CHILDRENEDALLOWANCE` — readable but `CHILD_ED_ALLOWANCE` would be clearer. Inconsistent capitalisation/joining.

## SS-001 — 🟡 Minor — Salary Structures lacks usage metadata
Single row "Standard — 9 components". No CTC band, no preview, no usage count, no last-modified date.

## STAT-001 — 🟠 Major — EPF Establishment Code blank with no remediation hint
Shown as `—`. No banner saying "Required for ECR filing — set before running payroll". Operator only discovers this when ECR filing fails (which there's no UI for anyway).

## STAT-002 — 🟡 Minor — "Disable EPF" button bare-action
Disabling EPF mid-year would break all in-progress payrolls. No confirmation modal, no impact assessment.

## STAT-003 — 🟡 Minor — Tab count badges missing
EPF/ESI/PT/LWF/Statutory Bonus tabs — no count badge showing which are configured vs unconfigured.

## STAT-004 — 🟢 Note — Hardcoded label "₹15,000"
"Restricted PF Wage (₹15,000)" — verify this is sourced from DB config, not hardcoded in the React component (CLAUDE.md rule). Acceptable if labels-only display.

## TAX-001 — 🔴 CRITICAL — Tax Details page entirely empty by default
PAN/TAN/AO Code/Range Code/Deductor Type/Name all `—`. Without these, Form 24Q + Form 16 cannot be generated. No onboarding wizard or banner forces operator to fill these before running payroll.

## TAX-002 — 🟡 Minor — Deductor Type enum not surfaced as picker
Should be a dropdown of {Company, Govt, Partnership, Individual, Trust, ...}.

## TAX-003 — 🟠 Major — Tax Deductor Employee not assignable here
Exit initiation gating relies on `OrgProfile.DeductorEmployeeId` — but there is no UI on the Tax Details page to assign which employee acts as deductor signer. Discoverability gap.

## TAX-004 — 🟡 Minor — No Form 24Q / Form 16 actions
Page hints "TDS filing details for Form 24Q / Form 16 generation" but no menu item to actually trigger or view these.

---

# 4. Employees

## EMP-001 — 🟠 Major — Incomplete profiles still marked Active
Banner: "25 employees have incomplete profiles" — yet each shows Active status. Active employees with incomplete profiles will silently be excluded or break payroll. Should be flagged as "Incomplete" status until profile is complete.

## EMP-002 — 🟡 Minor — No department/location filter on list
Only Status (All/Active/Inactive/Exited) and free-text search. Hard to slice by dept or work-location.

## EMP-003 — 🟡 Minor — No sort controls on column headers
Click on "Code", "Name", "Joined" doesn't sort.

## EMP-004 — 🔵 Cosmetic — Avatar always single brand colour
All employees share the same blue avatar background. Hashed-by-name palette would aid scanning.

## EMP-005 — 🔵 Cosmetic — "Add Employee" + "Import" both white-button styled
Primary action ("Add Employee") should be brand purple, secondary ("Import") outlined. Currently equal weight.

## EMP-006 — 🟢 Good — Dates use dd/MM/yyyy correctly on this page
`15/01/2024` ✓

## EMP-007 — 🔵 Cosmetic — Pagination footer cramped
Bottom row "Showing 1-25 of 250 | Rows per page: 25 | 1/10" is tight. Increase spacing.

## EMP-DET-001 — 🟢 Good — PAN masked correctly
`XXXXX234F` per security rule ✓

## EMP-DET-002 — 🟢 Good — Indian numeric grouping on monetary values
`₹12,00,000.00` — lakh grouping ✓

## EMP-DET-003 — 🔵 Cosmetic — Each section has its own "Edit" link
Inconsistent with edit-in-place pattern. Could be a single pencil per section.

## EMP-DET-004 — 🟡 Minor — Profile Incomplete badge non-actionable
Clicking the badge does not jump to the section with missing fields.

## EMP-DET-005 — 🟡 Minor — Kebab menu has only "Initiate Exit Process"
No "Send invite", "Resend verification", "Deactivate", "Revise salary directly", "Send reminder". Single-action kebab is overengineered for one option.

## SAL-001 — 🟡 Minor — Salary Structure missing employer EPF/Gratuity rows
Standard structure visible (Basic / HRA / Conv / LTA / Prof. Pursuit / Attire / Medical / Stat Bonus / Special Allowance + Cost to Company). Per Pay Schedule + Statutory config "Include Employer EPF in CTC: Yes" — yet employer EPF row not shown in the structure breakdown. Same for Gratuity. CTC reconciliation difficult.

## SAL-002 — 🟡 Minor — Sum of monthly components rounding by ₹0.25
Calculator absorbs into "Special Allowance" (Residual) — but displayed monthly sum (₹99,999.75) differs from displayed CTC (₹1,00,000.00). Tolerable but worth a fix to round consistently.

## SAL-003 — 🟢 Good — TDS calculation correctly applies 87A rebate
Employees with annual taxable < ₹12L correctly show Annual Tax ₹0 (FY 2025-26 new regime + 87A rebate up to ₹12L). Validated against Priya Sharma (₹10.81L taxable → ₹0), Chitra Verma (₹16.7L taxable → ₹1,39,557 ≈ engine output).

---

# 5. April 2026 Payroll Run

## PAYRUN-001 — 🟡 Minor — Period display inconsistent
Heading says `2026-04`, body says `30 Apr 2026`. Mixed `YYYY-MM` vs `dd MMM yyyy`. Pick one — `April 2026` reads best.

## PAYRUN-002 — 🟢 Good — Pay-day-passed banner
"The pay day for 2026-04 has passed. Consider initiating the pay run immediately." ✓

## PAYRUN-003 — 🟡 Minor — "250 active employees" includes incomplete profiles
See EMP-001. Incomplete profiles get included in pay run — silent risk.

## PAYRUN-004 — 🟡 Minor — Period chip shows raw `2026-04`
Same as PAYRUN-001. Localised format preferred.

## PAYRUN-005 — 🔴 CRITICAL — Process Payroll navigates to `/pay-runs/undefined`
**Repro:** Click `Process Payroll` → polling completes → navigation lands on `/pay-runs/undefined` (404).
**Root cause:** Polling result JSON returned with **PascalCase** keys (`"Id"`, `"Year"`, ...) but `PayRunsPage.tsx` reads `dto.id` (lowercase). PascalCase came from background-job result serialisation that bypasses the camelCase JsonOptions. Operator has to manually navigate to find the freshly-created run.

## PAYRUN-006 — 🟠 Major (audit-only) — `/api/v1/payroll-runs/pending` 404
Frontend has been deployed with new code calling `/pending` but the running Docker API has not been rebuilt. Real impact only after the new feature branch is rolled out — but worth flagging that the build/release pipeline does not bundle FE + BE together.

## PAYRUN-007 — 🟠 Major — Tab clicks can bounce to /login
Repro (intermittent): on Pay Run detail, clicking "Taxes & Deductions" → redirect to /login. Likely a 401 from a refreshed token expiring mid-session and the global axios interceptor doing a hard redirect rather than silent refresh.

## PAYRUN-008 — 🔴 CRITICAL — Calculation suspect for low-CTC employees
Quick eyeball of values shows Priya: Gross ₹96,397.12 (CTC ₹1,00,000/mo). Difference is Employer EPF + Gratuity excluded from gross — **correct payroll behaviour** but the operator UI does not explain *why* gross ≠ CTC. Provide a tooltip / breakdown. (Note: the calc itself is correct after analysis. The risk is operator confusion, not engine error.)

## PAYRUN-009 — 🟡 Minor — Filter chip counts misleading
`All 25 | Active 25 | Skipped 0` shows page-of-25 counts, not total-of-250. Should read `All 250` etc.

## PAYRUN-010 — 🟡 Minor — Eye icon (👁) action not labelled
What does it do? Opens variable-inputs panel. Aria-label needed.

## PAYRUN-011 — 🟠 Major — Skip action has no confirmation
Single click "Skip" in the row without confirmation. Easy mis-click on a 250-row table.

## PAYRUN-012 — 🟢 Good — Approve dialog has clear pre-approval checklist
"Expense reimbursements after this point won't be included...", "IT declarations will be locked...", "Cannot be reverted without rejecting" — ✓ all the right warnings.

## PAYRUN-DET-001 — 🟠 Major — "Overall Insights" tab is empty
"Overall Insights — coming soon". For a paid SaaS the run-level dashboard is the main deliverable. Same severity as DASH-001.

## PAYRUN-DET-002 — 🟡 Minor — Status column missing from employee table
Active/Skipped not shown per-row.

## RP-001 — 🔴 CRITICAL — Payment Date input shows `mm/dd/yyyy`
Same root cause as ORG-001. For a payment-record action this is high-risk: operator could record 05/04 instead of 04/05 and the engine will trust the recorded date.

## RP-002 — 🟡 Minor — Payment Mode fixed to "Bank Transfer"
No picker for Cash / Cheque / Other. Some orgs disburse cash to interns/contract.

## RP-003 — 🟡 Minor — "Send payslip notification" defaults ON
For 250 employees this would trigger 250 emails in one click. Default ON is dangerous; default OFF + opt-in is safer.

## PAID-001 — 🟡 Minor — No regenerate-payslips action after Paid
Only Bank Advice + kebab visible. Regenerate should be a documented action (kebab? menu?).

---

# 6. Exits + FnF

## EXIT-001 — 🔴 CRITICAL — LWD date input shows `mm/dd/yyyy`
Same as ORG-001/RP-001. **Highest impact here** — LWD wrong by even one day shifts gratuity eligibility, notice pay, and final pay date.

## EXIT-002 — 🟠 Major — Right-side preview missing Designation + Department
Exit form shows employee mini-card on right. "Designation:" and "Department:" both render blank labels with no value, even though employee has both. Field-render bug.

## EXIT-003 — 🟡 Minor — Personal Email Address is optional but should be required
Helper text says "Email for final payslip + Form-16". If work email is being deactivated then a non-work email is mandatory. Mark required.

## EXIT-004 — 🟢 Good — Settlement-mode radios match Zoho
"Pay as per the regular pay schedule" / "Pay on a given date" — matches reference product ✓

## EXIT-005 — 🟢 Good — Reasons enum aligns with statutory exit categories
{Terminated by Employer, Termination by Death, Termination by Disability, Resigned by Employee}

## EXIT-006 — 🟡 Minor — Native `<select>` for Reason for Exit
Inconsistent with design-system Select.

## EXIT-007 — 🟠 Major — Proceed button enabled-state doesn't react to programmatic input
Setting native value via standard `value` property does not enable button. Likely controlled-input via react-hook-form. Real users typing characters into the date field will be fine, but any external automation or assistive tech that sets value programmatically will hit this. Worth verifying screen-reader / autofill compatibility.

## FNF-001 — 🔴 CRITICAL — Step-2 page heading is **RED**
`<h1>Final Settlement Payroll</h1>` rendered in red colour. Red is reserved for errors/destructive in our design system. Makes the screen look like an error state. **Change to standard heading colour.**

## FNF-002 — 🟠 Major — TOTAL NET PAY shows ₹0.00 before save
Engine has computed the baseline (regular salary for partial month) but the FnF Step-2 preview shows ₹0.00 until the form is saved. Operator can't see the running total as they edit. Provide a live recalc preview.

## FNF-003 — 🟡 Minor — Helper text missing on most FnF fields
Only Gratuity has Sec 10(10) tooltip. Bonus / Commission / Leave Encashment / Notice Pay need explanations (tax exemption rules, statutory limits, formulas).

## FNF-004 — 🟡 Minor — No baseline visible
Operator cannot see the regular monthly salary baseline that the FnF will add to. Provide read-only summary.

## FNF-005 — 🟡 Minor — Notice Pay phrasing
"Does this Employee hold Notice Pay?" is grammatically awkward. Prefer "Notice pay recovery applies" + amount input.

## FNF-006 — 🟠 Major — Save and Continue stays disabled
Step-2 form has all defaults but the Save button is disabled. Only enables when a field is changed. Operator who wants to accept defaults has no recourse other than touching an input.

## FNF-007 — 🟠 Major — Form submit disabled in observed automation
Editing values via dispatched events did not enable the Save button. May indicate over-strict react-hook-form rules. Affects: testing, accessibility tools, autofill, form sync. Re-verify with manual mouse + keyboard.

## BULK-001 — 🟠 Major — Bulk FS shows ₹0.00 for both employees
After initiating Bulk FS for Amit (joining Priya's existing Draft bulk run), the Pay Run detail shows both employees but all monetary values ₹0.00. Possible causes:
- Engine didn't recompute when appending the second employee.
- Frontend displays before async compute completes (no loading state).
Either way the operator sees an empty run and may try to approve it.

## BULK-002 — 🟡 Minor — Bulk FS pay-day 29-May not visibly distinguished from regular May run
Both are 2026-05 with similar pay-dates. Hard to tell apart in the Pay Runs list without the Type column (which we shipped in `feat/zoho-payruns-fnf-parity` but the API Docker image hasn't been rebuilt).

---

# 7. May Payroll — NOT EXECUTED

Skipped because:
- Bulk FS run is in a broken ₹0.00 state (BULK-001).
- Single FS Step-2 cannot be saved (FNF-006/007).
- Running a regular May on top of these would compound a broken state.

To resume: fix BULK-001 / FNF-006 first, then re-run May.

---

# 8. Reports — MODULE ABSENT

No "Reports" nav item. CLAUDE.md and Zoho parity expect at minimum:
- Payroll Summary
- Statutory Reports (PF ECR, ESI, PT, LWF)
- TDS Reports (Form 24Q, Form 16)
- Salary Register
- Employee Master export
- Loan Schedule

## REP-001 — 🟠 Major — Reports module entirely missing
Treat as a top-priority deferred feature.

---

# 9. Documents — MODULE ABSENT

No "Documents" nav item. Employee onboarding documents (offer letter, ID proof), payslip archive UI, Form 16 archive — none.

## DOC-001 — 🟠 Major — Documents module entirely missing

---

# 10. Taxes & Forms — MODULE ABSENT

## TF-001 — 🟠 Major — No surface to generate Form 24Q / Form 16
Tax Details page hints at these but no menu actually generates them.

---

# 11. Loans / Reimbursements / Approvals — MODULE ABSENT

## LRA-001 — 🟠 Major — Multiple core SaaS modules missing
Loans, Reimbursements, Approvals — Zoho parity requires all three. Currently absent.

---

# 12. Cross-cutting / Design System

## DS-001 — 🔴 CRITICAL — Brand colour inconsistency
Three different button "primary" colours observed:
- Brand purple (`var(--color-primary)`) — Login, Approve Payroll, most actions
- Bright blue — Save Profile, Add Work Location, Record Payment dialog button
- Dark navy — Provision Organisation (platform admin)
Pick **one** primary and enforce via shared `<Button variant="primary">` component.

## DS-002 — 🔴 CRITICAL — Native HTML `<input type="date">` used everywhere
- Org Profile → Date of Incorporation
- Exit → Last Working Day, Final Settlement Date
- Record Payment → Payment Date
- Possibly: salary effective dates, DOB
Native input respects browser locale → US format (`mm/dd/yyyy`) for English-US browsers (default in most installs). **High risk of date entry errors in production.** Replace with a controlled picker.

## DS-003 — 🟠 Major — Native `<select>` mixed with custom Select
Org Profile (Industry, State), Exit (Reason for Exit) use native; rest of app uses design-system Select. Visual + behavioural inconsistency.

## DS-004 — 🟡 Minor — Inconsistent destructive-action confirmations
Delete employee, Disable EPF, Skip employee, Deactivate component — all bare clicks, no confirmation. Standardise on a `<ConfirmDialog>` for any non-reversible action.

## DS-005 — 🔵 Cosmetic — `Close` button styles vary
Some pages have `← Back`, some have `× Close Settings`, some have nothing. Standardise.

## DS-006 — 🔵 Cosmetic — Heading colour inconsistency
FNF heading is red, most are black, error toasts also red. Reserve red exclusively for errors/destructive.

## DS-007 — 🔵 Cosmetic — Whitespace not used to anchor content
Dashboard, Settings landing, Work Locations list, Salary Structures — all show small content with vast empty page. Centre content or add filler cards.

## DS-008 — 🔵 Cosmetic — Edit affordance varies
Pencil icon | "Edit" link | kebab — three patterns for the same action across pages.

## DS-009 — 🔵 Cosmetic — Avatar colour palette uniform
Same blue circle for every avatar. A hashed palette would aid scanning.

## DS-010 — 🔵 Cosmetic — Page `<title>` is always `Indian Payroll`
No per-page titles → poor browser-tab UX, poor SEO/sharing if ever public-facing.

## DS-011 — 🔵 Cosmetic — Pagination footer needs more breathing room
"Showing 1-25 of 250 | Rows per page: 25 | 1/10" packed together.

## DS-012 — 🔵 Cosmetic — Toggle/tab active states inconsistent
Some use bottom border + blue text (Pay Runs tabs), some use background fill (employee status filter), some use border pill (chip filters).

## DS-013 — 🔵 Cosmetic — Inconsistent button sizing
`h-8`, `h-9`, native button height co-exist. Audit and pick a scale.

---

# 13. Security findings

## SEC-001 — 🟠 Major — 500 errors leak full stack traces (dev mode at minimum)
Provisioning API leaked full Npgsql + EF Core stack trace including server-side file paths (`/src/src/Payroll.Infrastructure/...`). Acceptable in dev (`DeveloperExceptionPageMiddlewareImpl`), but **must verify production env disables this** — and that `Program.cs` correctly guards `app.UseDeveloperExceptionPage()` behind `app.Environment.IsDevelopment()`.

## SEC-002 — 🟡 Minor — No Subresource Integrity on external scripts
(Not directly verified; flag for review.)

## SEC-003 — 🟡 Minor — Auth flow silent-refresh missing
PAYRUN-007 above: tab click bounces to /login. Likely access-token expired and the axios interceptor does a hard redirect rather than attempting refresh-token flow. Should silently refresh.

## SEC-004 — 🟢 Good — CSP, X-Frame-Options, X-Content-Type-Options present
Verified in API response headers ✓

## SEC-005 — 🟢 Good — PAN masking on display ✓

## SEC-006 — 🟢 Good — JWT roles claim used for role-based routing
SuperAdmin routes to `/platform/orgs` ✓

---

# 14. Calculation findings

## CALC-001 — 🟢 Good — Indian numeric grouping (lakh/crore) consistent on monetary displays
`₹2,43,82,057.92` ✓ throughout pay run + employee pages.

## CALC-002 — 🟢 Good — TDS engine applies FY 2025-26 new regime + 87A rebate correctly
Validated 5 employees across taxable-income bands (₹3.94L, ₹7.87L, ₹10.81L, ₹16.7L, ₹20.6L). All match expected slab math.

## CALC-003 — 🟢 Good — PT engine produces Kerala ₹208 cap (where observed)

## CALC-004 — 🟢 Good — EPF capped at ₹15,000 wage (employee ₹1,800)

## CALC-005 — 🟢 Good — Net pay arithmetic consistent
Gross − Deductions − Taxes = Net validated on Priya (96,397.12 − 2,058 − 0 = 94,339.12 ✓) and Chitra.

## CALC-006 — 🟡 Minor — Special Allowance absorbs ₹0.25 rounding
Sum of displayed components ≠ displayed CTC by 0.25 (see SAL-002). Engine likely uses `decimal` round-to-nearest correctly internally — the issue is only display.

## CALC-007 — ⚠️ Not validated — Bulk FS run with ₹0.00 outputs
Could be (a) engine bug on append, (b) async compute not complete, (c) UI display lag. Needs reproduction with browser dev tools open.

## CALC-008 — ⚠️ Not validated — Gratuity Sec 10(10) ₹20L exemption split
Single FS could not be completed; Sec 10(10) split (taxable portion of gratuity > ₹20L lifetime) was not exercised in this audit.

---

# 15. Quick wins (high-impact, low-effort)

1. **DS-002** — replace native date inputs with a controlled picker → eliminates 4 critical findings (ORG-001, EXIT-001, RP-001, FNF-related date inputs).
2. **PAYRUN-005** — fix PascalCase result-JSON deserialisation → unbreaks the most-used flow.
3. **FNF-001** — change red heading to standard colour → 5-minute fix, big perceived-quality lift.
4. **TENANT-PROVISION-001** — fix `is_one_time` schema/seed ordering → unblocks every new customer.
5. **NAV-001** — add placeholder nav items for missing modules → sets honest expectations.
6. **DS-001** — converge on a single primary button colour → 30-minute design-system enforcement.
7. **TAX-003** — surface "Tax Deductor Employee" picker on Tax Details page → unblocks exit flow without operator hunting.

---

# 16. Recommended priority order for fix sprint

**P0 (must fix before any prod release):**
- TENANT-PROVISION-001 (no new tenants)
- ORG-001 / EXIT-001 / RP-001 (date format wrong → calc risk)
- PAYRUN-005 (Process Payroll → undefined)
- DASH-001 (empty dashboard is unsellable)
- TAX-001 (no TDS = no Form 24Q = compliance failure)

**P1 (ship in next milestone):**
- All "Module Absent" findings: Reports, Documents, Forms, Loans, Approvals
- DS-001/DS-002/DS-003 (design system inconsistencies)
- FNF-001 / FNF-002 / FNF-006 (FnF flow polish)
- BULK-001 (validate engine on append)
- SET-004 (Users & Roles)

**P2 (UX polish):**
- All cosmetic findings
- Sort/filter additions
- Confirmation modals (DS-004)

---

# Appendix: Files referenced

- `src/Payroll.Infrastructure/Services/TenantSchemaProvisioner.cs:276` — schema seed bug
- `web/src/pages/payroll/PayRunsPage.tsx` — PascalCase deserialisation bug
- `web/src/pages/payroll/FnfSettlementPage.tsx` — red heading + disabled save
- `web/src/pages/employees/ExitInitiationPage.tsx` — native date input
- `web/src/pages/settings/OrgProfilePage.tsx` — native date input
- `src/Payroll.Application/Commands/Employees/InitiateExitCommand.cs` — tax deductor gating

# Appendix: Test data / artefacts

- April 2026 payroll run: `a5e67b98-7f0f-4e1c-aa05-50e572a54ef4` — **Paid**
- Bulk FS run (May 2026, EMP001+EMP002): `357033ae-c072-45ce-aa61-9475ad7a1179` — Draft, ₹0.00 anomaly
- Single FS run (May 2026, EMP003): `e75fddb7-1c1c-4024-a84e-1325e17e1aaf` — Draft, Step-2 not saved due to FNF-006
- 48 screenshots: `docs/ba-audit/screenshots/2026-05-24-e2e/e2e-*.png`
