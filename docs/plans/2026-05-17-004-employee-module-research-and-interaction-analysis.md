# Employee Module — Research & Interaction Analysis

**Date:** 2026-05-17  
**Sources:** BA audit docs (`docs/ba-audit/employees/`, `userflows/`, `settings/`), domain model inspection, interaction scenario analysis  
**Purpose:** Pre-implementation reference for building the Employee module and its prerequisites

---

## Table of Contents

1. [Prerequisites — Must Build Before Employee](#1-prerequisites--must-build-before-employee)
2. [Employee Data Model — Complete Field Inventory](#2-employee-data-model--complete-field-inventory)
3. [Domain Model Gaps vs Current Implementation](#3-domain-model-gaps-vs-current-implementation)
4. [New Entities Required](#4-new-entities-required)
5. [Add Employee Wizard — Step-by-Step](#5-add-employee-wizard--step-by-step)
6. [Employee List and Profile](#6-employee-list-and-profile)
7. [Special Cases](#7-special-cases)
8. [Bulk Import](#8-bulk-import)
9. [Salary Structure Assignment and Revision](#9-salary-structure-assignment-and-revision)
10. [Employee Exit and Full & Final](#10-employee-exit-and-full--final)
11. [Employee Portal](#11-employee-portal)
12. [Cross-Module Interaction Analysis](#12-cross-module-interaction-analysis)
13. [Recommended Build Order](#13-recommended-build-order)

---

## 1. Prerequisites — Must Build Before Employee

### Hard Prerequisites (Block Specific Operations)

| Prerequisite | What It Blocks | Status |
|---|---|---|
| Work Locations | Cannot add employee without at least one; cannot be created via bulk import | ✓ Done |
| Departments | Can be created inline in wizard; soft prerequisite | ✓ Done |
| Designations | Can be created inline in wizard; soft prerequisite | ✓ Done |
| **Pay Schedule** | Salary Details (Step 2) save is hard-blocked without one | ✗ Not built |
| **Statutory Components settings** | EPF/ESI/PT/LWF checkboxes in wizard only render if org has configured them | ✗ Not built |
| **Salary Components settings UI** | "Add Earning" dropdown in wizard empty without pre-configured components (Basic is auto-present; HRA and others need config) | ✗ Not built |
| Org Profile | Payslip header; not an employee creation blocker | ✓ Done |
| Tax Details (PAN + TAN) | Form 24Q, Form 16 generation; not employee creation | Partially done |

### Runtime Prerequisites (Payroll Run Inclusion)

An employee is silently skipped in a pay run — not errored — if any of these are missing:

| Missing Field | Effect |
|---|---|
| Date of Birth | Skip with reason |
| Father's Name | Skip with reason |
| Personal Email | Skip with reason |
| Permanent Address | Skip with reason |
| Bank Account Details | Skip with reason |
| Salary Structure assignment | Skip with reason |
| PAN | Not a hard skip; triggers TDS at 20% u/s 206AA |

---

## 2. Employee Data Model — Complete Field Inventory

### Step 1 — Basic Details (Mandatory; Creates the Record)

| Field | Type | Required | Validation | Notes |
|---|---|---|---|---|
| First Name | string(100) | Yes | Non-empty | |
| Middle Name | string(100) | No | Free text | |
| Last Name | string(100) | No | Free text | |
| Employee ID (Employee Code) | string(50) | Yes | Unique within tenant | Manual entry; support auto-increment with configurable prefix |
| Date of Joining | DateOnly | Yes | Any calendar date | Gate: must be within a configured financial year |
| Work Email | string(200) | Yes | Email format; unique within tenant | **IMMUTABLE post-save** — used as portal login identifier; show warning before save |
| Mobile Number | string(10) | No | Exactly 10 digits; no country code | |
| Is Director / Person with Substantial Interest | bool | No | Default: false | Statutory: no 87A rebate; different perquisite valuation u/s 17; feeds TDS engine |
| Gender | enum | Yes | Male / Female / Other | |
| Work Location | FK → WorkLocation | Yes | Must pre-exist; cannot create via import | Drives PT state; PT slab = WorkLocation.State |
| Designation | FK → Designation | Yes | Can be created inline during wizard | |
| Department | FK → Department | Yes | Can be created inline during wizard | |
| Enable Portal Access | bool | No | Default: false | If true → sends portal invitation email to WorkEmail |
| Employment Type | enum | No | Permanent / Contract / Probation / Intern | V1: all types processed identically (contractor TDS 194C/J: DEFERRED) |
| Gratuity Calculation Date | DateOnly | No | Import only; defaults to DOJ | Allows prior service credit |

### Step 2 — Salary Details (Gated: Pay Schedule Must Exist)

**Phase 1 — Statutory (only visible if configured at org level):**

| Component | Type | Default | Notes |
|---|---|---|---|
| EPF Enabled | bool | Off | Only shown if org has EPF configured |
| ESI Enabled | bool | Off | Only shown if org has ESI configured |
| Professional Tax Enabled | bool | Auto-on if WorkLocation state has PT | Per Work Location slab |
| LWF Enabled | bool | Off | Only shown if org has LWF configured |

**Phase 2 — Salary Structure:**

| Field | Type | Required | Notes |
|---|---|---|---|
| Annual CTC | decimal | Yes | Anchor; all percentage components derive from this |
| Basic (% of CTC) | decimal | Yes | Defaults to 50%; user edits %; monthly amount is read-only computed |
| HRA (% of Basic) | decimal | No | Defaults 50% Basic (metro); 40% (non-metro); only if HRA component exists |
| Fixed Allowance | decimal | System | Never user-editable; = Monthly CTC − sum(all other monthly components) |
| Additional earning components | per component | No | From "Add Earning" dropdown; only active org-configured components not yet assigned |

**Phase 3 — Other Benefits:**

Benefit Plan selection from org-configured plans. "Proceed" skips this section.

### Step 3 — Personal Details (Skippable; Required for Payroll Inclusion)

| Field | Type | Required for Payroll | Validation | Notes |
|---|---|---|---|---|
| Date of Birth | DateOnly | Yes | dd/MM/yyyy | Missing → payroll skip |
| Father's Name | string(200) | Yes | | Required for Form 16/24Q; missing → payroll skip |
| PAN | string(10) (encrypted) | No | AAAAA0000A format | Missing → TDS at 20% u/s 206AA |
| Aadhaar | string (encrypted) | No | 12 digits | Our addition; display as XXXX-XXXX-1234; full reveal = role + audit log |
| Differently Abled Type | enum | No | None / Visual / Hearing / Speech / Mobility / Other | 80U/80DD higher deduction limit |
| Personal Email | string(200) | Yes | Email format | Different from Work Email; missing → payroll skip; used for F&F payslip |
| Address Line 1 | string(250) | Yes | | Missing → payroll skip |
| Address Line 2 | string(250) | No | | |
| City | string(100) | No | | |
| Residential State | enum IndianState | No | 37 values | NOT PT-determining state (PT = WorkLocation.State) |
| PIN Code | string(6) | No | 6-digit | |
| Marital Status | enum | No | | Import spec field |

### Step 4 — Payment Information (Skippable; Required for Payroll Inclusion)

| Field | Type | Required (if not skipped) | Validation | Notes |
|---|---|---|---|---|
| Payment Mode | enum | Yes | DirectDeposit / BankTransfer / Cheque / Cash | Default: DirectDeposit |
| Account Holder Name | string | Yes (bank modes) | Pre-filled from employee name | |
| Bank Name | string | Yes (bank modes) | Auto-filled from IFSC lookup; manual fallback | |
| Account Number | string (encrypted) | Yes (bank modes) | Entered masked; AES-256 at rest | Display: XXXX + last 4; reveal = role + audit log |
| Re-enter Account Number | string (UI only) | Yes | Must match; not stored separately | |
| IFSC Code | string(11) (encrypted) | Yes (bank modes) | AAAA0000000; lookup populates Bank Name | Lookup failure → manual entry allowed |
| Account Type | enum | Yes (bank modes) | Savings / Current | Default: Savings |

### Additional Fields (Not in Wizard; Set Post-Creation or via Import)

| Field | Type | Notes |
|---|---|---|
| UAN | string(12) | PF Universal Account Number |
| PF Account Number | string | |
| ESI Number | string(17) | Stored as ESICIPNumber in current domain |
| Portal Status | enum | Disabled / InviteSent / Active |
| Onboarding Status | enum | Incomplete / Complete |
| Last Working Day | DateOnly | On EmployeeExit entity, not Employee directly |

### Encryption Summary

| Field | Encryption | Display Pattern |
|---|---|---|
| PAN | AES-256 | Full in admin view; masked in list |
| Aadhaar | AES-256 | XXXX-XXXX-1234; full reveal = role + audit log |
| Account Number | AES-256 | XXXX{last4}; full reveal = role + audit log |
| IFSC | AES-256 | Full display |

---

## 3. Domain Model Gaps vs Current Implementation

### Employee Entity — Missing Fields

| Missing | Reason |
|---|---|
| `WorkEmail` | Core identifier; portal login; immutable |
| `MiddleName` | Wizard has 3-part name |
| `MobileNumber` | Basic contact field |
| `IsDirector` | TDS: no 87A rebate; different perquisite valuation |
| `WorkLocationId` (FK) | Currently stores `WorkState` enum directly; needs FK for PT routing and location-specific statutory config |
| `FatherName` | Form 16/24Q required |
| `PersonalEmail` | Payroll inclusion gate; F&F notifications |
| `AddressLine1`, `AddressLine2`, `City`, `ResidentialState`, `PinCode` | Payroll inclusion gate |
| `DifferentlyAbledType` | 80U/80DD tax impact |
| `PortalAccess` (bool) | Per-employee portal toggle |
| `OnboardingStatus` (enum) | Tracks wizard completion state |
| `GratuityCalculationDate` | Import spec; can pre-date DOJ for prior service credit |
| `MaritalStatus` | Import spec |
| `PfAccountNumber` | Separate from UAN |
| `MiddleName` | 3-part name in wizard |
| `PaymentMode` | Belongs on EmployeeBankDetails sub-entity |

### Enum Corrections

**EmployeeStatus** — rename values:
- Current: `Active, OnNotice, Terminated, Resigned`
- Needed: `Active, OnNoticePeriod, Exited, Inactive`
- Note: `Terminated` and `Resigned` are exit *reasons* (on `EmployeeExit.reason`), not statuses

**EmploymentType** — change values:
- Current: `FullTime, PartTime, Contract, Intern`
- Needed: `Permanent, Contract, Probation, Intern`

**New enums required:**
- `DifferentlyAbledType`: None, Visual, Hearing, Speech, Mobility, Other
- `PaymentMode`: DirectDeposit, BankTransfer, Cheque, Cash
- `ExitReason`: TerminatedByEmployer, TerminationByDeath, TerminationByDisability, ResignedByEmployee
- `FinalPayMode`: RegularSchedule, CustomDate
- `OnboardingStatus`: Incomplete, Complete
- `PortalStatus`: Disabled, InviteSent, Active
- `AccountType`: Savings, Current

### StatutoryToggle — Org-Level Only, Missing Per-Employee Overrides

Current `StatutoryToggle` is keyed by `(TenantId, StatutoryModule)` — org-level only.  
Need separate `EmployeeStatutorySettings` for per-employee EPF/ESI/PT/LWF overrides.

### EmployeeSalaryStructure — Missing Component Breakdown

Currently stores only `AnnualCTC` + dates. No per-employee component breakdown.  
Need `EmployeeSalaryComponent` junction to store per-employee percentage/amount per component.

### SalaryComponent — Missing Fields

| Missing | Reason |
|---|---|
| `IsActive` | Only active components show in "Add Earning" dropdown |
| `IsFixedAllowance` | Fixed Allowance is a special system component |
| `ShowInPayslip` | Component display preference |
| `PfTreatment` | Always / Only when PF wage < ₹15,000 / Never |
| `EsiTreatment` | Yes / No |
| `IsProRated` | Whether component prorates on LOP |
| `PayType` | Fixed (recurring) / Variable (manual input per run) |

### PayrollRun — Missing Fields

- Missing `RunType` enum: Regular, FullAndFinal, OffCycle, Bonus
- Missing `Approved` status in state machine (Draft → Approved → Paid/Finalised)

---

## 4. New Entities Required

| Entity | Purpose |
|---|---|
| `EmployeeBankDetails` | Encrypted payment details (account number, IFSC, bank name, account type, payment mode) |
| `EmployeeStatutorySettings` | Per-employee EPF/ESI/PT/LWF boolean overrides |
| `EmployeeSalaryComponent` | Junction Employee ↔ SalaryComponent; per-employee percentage or flat amount |
| `PaySchedule` | Pay schedule configuration (salary calculation method, pay frequency, pay day) — HARD PREREQUISITE |
| `SalaryTemplate` | Reusable salary structure templates; copied at assignment time |
| `SalaryRevision` | Dated CTC change record (previous_ctc, new_ctc, effective_from, reason, status) |
| `ScheduledEarning` | Org-level master for recurring extras outside CTC |
| `EmployeeScheduledEarning` | Junction Employee ↔ ScheduledEarning |
| `Deduction` | Org-level deduction master (pre-tax flag, amount) |
| `EmployeeDeduction` | Junction Employee ↔ Deduction |
| `PriorEmployerYtd` | Prior employer YTD data for mid-year joiners (gross, TDS, standard deduction claimed, PT paid) |
| `ITDeclaration` | Per-employee per-FY; locked/unlocked/submitted/approved state machine |
| `EmployeeExit` | Exit record (last_working_day, reason, final_pay_mode, personal_email) |
| `FullAndFinalSettlement` | F&F computation linked to exit and pay run |
| `ImportJob` | Generic import pipeline job (type, file, status, row counts) |
| `ImportRow` | Per-row import result (row number, status, error message) |
| `FilterPreset` | Saved employee list views (system views + user-defined) |

---

## 5. Add Employee Wizard — Step-by-Step

### Prerequisites Check Before Opening Wizard

Detect before wizard opens, not at Step 2:
- At least one Work Location must exist → if not, block with redirect to Settings → Work Locations
- A Pay Schedule must exist → if not, show inline warning at start of wizard with link to Settings → Pay Schedules

### Step 1 — Basic Details

- All fields in section 2 Basic Details
- Inline modal flows: "New Designation" (name only); "New Department" (name + optional code)
- Work Email irreversibility warning: shown as field-level warning immediately on input; no confirmation dialog
- "Preview mail" button appears next to Portal Access checkbox once Work Email is filled
- "Save and Continue" creates Employee record; employee appears in list as "Incomplete"

### Step 2 — Salary Details (Two-Phase Save)

**Phase 1:** Statutory checkboxes (only rendered if org has each configured)  
→ Save locks statutory choices; reveals Salary Structure form  
→ If no Pay Schedule: save blocked — "You cannot update salary details without configuring a pay schedule"

**Phase 2:** CTC + component entry  
→ Fixed Allowance auto-computed; non-editable  
→ "Add Earning" shows only active org-configured components not yet assigned  
→ Save locks structure; reveals Other Benefits

**Phase 3:** Benefits (optional; "Proceed" skips)

### Step 3 — Personal Details

"Skip" is valid. But missing any of DOB/FatherName/PersonalEmail/Address causes payroll skip (not wizard failure).

### Step 4 — Payment Information

"Skip" is valid. Missing bank account causes payroll skip. IFSC live lookup with graceful fallback.

### Onboarding Completeness — Payroll Inclusion Gate

Employee skipped in pay run if ANY missing: DOB, FatherName, PersonalEmail, Address, BankAccount, SalaryStructure.  
Missing PAN: not a skip gate — TDS computed at 20% u/s 206AA with a warning in pay run.

---

## 6. Employee List and Profile

### List View

**Default columns:** Employee Name (locked), Work Email, Department, Employee Status (locked)

**All 21 available columns:** Employee Name, Work Email, Department, Employee Status, Cost to Company, Date of Birth, Date of Joining, Designation, ESI Number, Employee ID, Father Name, Gender, Last Working Day, Mobile Number, Onboarding Status, PAN, PF A/C Number, Portal Status, Prior Payroll Status, UAN, Work Location

**System filter views (non-deletable):**
1. All Employees
2. Active Employees (default)
3. Exited Employees
4. Incomplete Employees
5. Portal Enabled Employees
6. Portal Disabled Employees
7. Yet to Accept Portal Invite Employees

**Custom views:** User-created named filters; stored as `FilterPreset` with `is_system = false`.

**Incomplete employee banner:** "You have N incomplete employees." with per-row "Complete now" link → routes to first incomplete wizard step.

### Employee Profile — 5 Tabs

**Tab 1: Overview**

Persistent header: Avatar (initials), Employee ID, Name, Status Badge, Designation  
Header buttons: "Add" split button (Scheduled Earning / Deduction / Benefit / Donation); kebab dropdown

Kebab actions:
- "Delete Employee" — only if no payroll history
- "Initiate Exit Process" — only if at least one completed payroll run; blocked if employee is Tax Deductor

Four editable sections: Basic Information | Statutory Information | Personal Information | Payment Information

**Tab 2: Salary Details**

CTC summary card + "Revise" button. Salary structure table with component breakdown. Pending revision banner if revision is queued.

**Tab 3: Investments**

Sub-tabs: IT Declaration | Proof of Investments. Period selector (FY dropdown). State machine: Locked → Unlocked → Submitted → Approved.

**Tab 4: Payslips & Forms**

FY selector. Payslip rows per month. Form 16 Part A + Part B downloads.

**Tab 5: Loans**

Loan list with outstanding balance. "Create Loan" button.

---

## 7. Special Cases

### 7.1 Mid-Month Joiner

Trigger: `DateOfJoining` is not the 1st of the month.

- Proration happens at pay run time, not at creation time
- Formula: `Monthly Gross × (Paid Days / Denominator)`
- Denominator driven by Pay Schedule: "Actual Days" = calendar days in month; "Fixed 30 Days" = 30 always
- Paid Days = calendar days from DOJ to end of month

Our build improvement: auto-compute proration at pay run time; allow admin to override LOP days.

### 7.2 Mid-Year Onboarding with Prior Employer YTD

Trigger: Employee joins in current FY with prior employment in same FY.

`PriorEmployerYtd` fields:

| Field | Type | Required | Notes |
|---|---|---|---|
| `employer_name` | string | Yes | |
| `period_from` | DateOnly | Yes | Must be in FY; must not overlap current employment |
| `period_to` | DateOnly | Yes | Must be before current employer's DOJ |
| `gross_salary` | decimal | Yes | Total gross paid by prior employer in FY |
| `standard_deduction_claimed` | decimal | No | 0 or ₹75,000; only one employer claims this per FY |
| `professional_tax_paid` | decimal | No | |
| `tds_deducted` | decimal | Yes | TDS deducted by prior employer u/s 192 |
| `other_income` | decimal | No | Interest income declared by employee |

TDS recomputation on save:
```
Projected Annual = prior_gross + (current_monthly_gross × remaining_months)
Tax = new regime slabs applied
Remaining TDS = projected_tax − prior_tds − current_tds_so_far
Monthly TDS (future) = Remaining TDS ÷ remaining months
Standard deduction = ₹0 if prior employer already claimed ₹75,000
```

Saving `PriorEmployerYtd` must trigger TDS recomputation for all FUTURE months. Past months are immutable.

### 7.3 Contractor vs Regular Employee

Employment type `Contract`: V1 behavior identical to Permanent for salary payroll.  
Contractor TDS (194C/194J): `// DEFERRED: contractor-tds-194c`  
Contractors in V1: no PF, no ESI (gate on employment_type in statutory processing).

### 7.4 PF Opt-Out

`epf_enabled` boolean on `EmployeeStatutorySettings`. Admin can disable PF for any employee regardless of salary threshold (statutory gap — warn but don't block). UAN should be captured before disabling.

### 7.5 ESI Eligibility

Runtime-computed per pay run, not stored:
- Eligible if: `employee.esi_enabled AND gross_pay ≤ ₹21,000`
- Rule 50 continuation: once eligible, contributions continue until end of contribution period even if salary exceeds ₹21,000 mid-period
- Rates: Employee 0.75%; Employer 3.25%

### 7.6 Professional Tax

- State determined by `WorkLocation.State` (not employee's residential address)
- Configured per state in Settings → Statutory Components → Professional Tax
- Per-employee toggleable even if org has PT enabled
- Kerala: half-yearly (August + February only)
- PT Number stored per Work Location, not per org

---

## 8. Bulk Import

### Entry Point

Employee List → dropdown caret next to "Add" → "Import Data"

### Import Types (19 total; V1 priority ones)

| Type | Purpose |
|---|---|
| Employee Basic Details | Create/update employees (25 columns) |
| Employee Salary Details | Assign CTC + component percentages |
| Salary Revision | Bulk CTC changes with effective date |
| Previous Employment Details | YTD for mid-year joiners |
| Employee Payment Information | Bank details |
| Employee Statutory Details | EPF/ESI/PT/LWF per employee |

### File Specs

- Formats: CSV, TSV, XLS
- Max size: 5 MB
- Encoding: UTF-8
- Date format in import: `dd-MM-yyyy` (hyphen) — differs from UI display `dd/MM/yyyy` (slash)
- Duplicate handling: Skip or Overwrite (radio; default Overwrite)

### Employee Basic Details Import — 25 Columns

| Column | Mandatory | Notes |
|---|---|---|
| Employee Number | Yes | Unique |
| First Name | Yes | |
| Middle Name | No | |
| Last Name | Yes | |
| Gender | No | Male/Female |
| Employee Status | No | Default Active |
| Date of Joining | Yes | dd-MM-yyyy |
| Gratuity Calculation Date | No | Can pre-date DOJ |
| Last Working Day | No | For exits |
| Designation | No | Must match existing or be created first |
| Work Email | Yes | Unique |
| Department | No | Must match existing |
| Worklocation Name | No | **Must pre-exist — hard constraint** |
| Enable Portal | No | Yes/No |
| Personal Email | No | |
| Father Name | No | |
| Mobile Number | No | |
| Date of Birth | No | dd-MM-yyyy |
| Personal AddressLine1 | No | |
| Personal AddressLine2 | No | |
| Personal City | No | |
| Personal StateCode | No | 2-letter code (TN, KA, MH…) |
| Personal Country | No | |
| Personal PostalCode | No | 6-digit |
| PAN Number | No | AAAAA0000A format validated |

### Validation Rules

1. Work Location must pre-exist; row fails if referenced location not in org
2. Work Email + Employee ID uniqueness enforced per row
3. Salary Revision import: employee must exist; effective date must be current or future pay period
4. Prior Employment: period must be in current FY; must not overlap current employment
5. PAN format: exactly `AAAAA0000A`
6. Date format: `dd-MM-yyyy` (import) vs `dd/MM/yyyy` (UI)
7. Partial success: valid rows imported; error rows shown with reason

### Error Handling

- Row-level errors shown in validation preview before final commit
- Error report CSV: original columns + appended "error" column
- `ImportJob` + `ImportRow` entities track status per row

---

## 9. Salary Structure Assignment and Revision

### Assignment During Wizard

- Salary Template is COPIED at assignment time — template changes never affect already-assigned employees
- If no template: manual CTC + Basic% entry is sufficient (Fixed Allowance absorbs remainder)
- If wizard salary step skipped: employee flagged Incomplete; skipped in pay runs

### Fixed Allowance Invariant

```
Fixed Allowance = Monthly CTC − sum(all other salary component monthly amounts)
```

- Always auto-computed; never user-editable
- Must be enforced by the payroll ENGINE, not just the UI — API bypasses must still produce correct FA
- When CTC changes: percentage-based components scale; flat amounts stay; FA absorbs difference

### Salary Component Immutability

Once a component type is assigned to an employee's structure, the calculation TYPE is locked. Only percentage/amount can be changed — not the formula type (% of CTC vs % of Basic vs flat amount).

### Salary Revision Flow

**Mechanism 1 — Direct Edit (no effective date):**
- Applies to current open pay run if not yet Approved
- Immediate overwrite of current structure
- Warning shown if pay run already in progress

**Mechanism 2 — Dated Salary Revision:**
- Creates `SalaryRevision` record with `previous_ctc`, `new_ctc`, `effective_from`, `reason`
- If `effective_from` is mid-month: engine must split-prorate (old rate × days before; new rate × days from effective date)
- Shows "pending" banner on Salary Details tab until the pay run for that period completes
- Approval workflow: if configured in Settings → revision goes to Approvals module

**SalaryRevision.status state machine:** Pending → Active → Historical

Pay run processor must query pending revisions at COMPUTATION time (not initiation time) to detect mid-month splits.

---

## 10. Employee Exit and Full & Final

### Exit Flow

Entry point: Employee Profile → kebab → "Initiate Exit Process"  
Pre-condition: Employee must have at least one completed payroll run. If not → only "Delete Employee" is shown.  
Tax Deductor block: if employee is the org's Tax Deductor → exit blocked until Tax Deductor is reassigned in Settings → Tax Details.

**Exit form fields:**

| Field | Type | Required | Notes |
|---|---|---|---|
| Last Working Day | DateOnly | Yes | Must be ≥ DOJ |
| Reason for Exit | enum | Yes | TerminatedByEmployer / TerminationByDeath / TerminationByDisability / ResignedByEmployee |
| When to Settle Final Pay | enum | Yes | RegularSchedule / CustomDate |
| Final Pay Date | DateOnly | Conditional | Required if CustomDate |
| Personal Email | string | No | Critical for post-exit Form 16 delivery |
| Notes | text | No | Internal admin notes |

On "Proceed": Employee status → `OnNoticePeriod`; `EmployeeExit` record created; employee stays in regular pay runs until LWD.

### F&F Settlement Computation

F&F is a special pay run type (`PayrollRunType.FullAndFinal`). Treat as part of the regular Month N pay run for the exit month — not a separate payment.

**Earnings:**

| Component | Formula | Tax Treatment |
|---|---|---|
| Prorated salary (final month) | `Monthly Gross × (Days worked / Calendar days in month)` | Fully taxable |
| Earned Leave Encashment | `EL Balance Days × (Basic / 26)` | Taxable during service; up to ₹25L exempt on retirement |
| Gratuity | `(Basic + DA) × 15/26 × Years of service`; cap ₹20L | Up to ₹20L tax-free; excess taxable |
| Notice Pay (if employer waives) | `Monthly Gross × (Unserved notice days / 30)` | Fully taxable |

**Deductions:**

| Component | Computation |
|---|---|
| Notice Pay Recovery | `Monthly Gross × (Short notice days / 30)` if employee short-served |
| Outstanding Loan Balance | Full outstanding balance |
| TDS on F&F | u/s 192 on aggregated F&F taxable income |
| PF / PT / ESI | Normal rules for final month |

**Gratuity eligibility:**
- Minimum 5 years continuous service (ResignedByEmployee)
- No minimum tenure for TerminatedByEmployer, TerminationByDeath, TerminationByDisability
- Gratuity Calculation Date (not DOJ) is the service start anchor

**After F&F finalized:**
- Employee status → `Exited`
- Excluded from all future regular pay runs
- F&F payslip emailed to `PersonalEmail` (NOT WorkEmail — may be deactivated)
- Employee record retained minimum 7 years for statutory compliance
- Form 16 generated at year-end even for exited employees

---

## 11. Employee Portal

### Portal Access Requirements

Both conditions required:
1. Org-level toggle = Active (Settings → Employee Portal → Preferences)
2. Per-employee `PortalAccess = true` (set on employee profile)

On enable: invitation email → WorkEmail → employee creates account → logs in.

Portal access states: `Disabled` → `InviteSent` → `Active`

WorkEmail is the portal login identifier. This is why WorkEmail is immutable post-creation — changing it would orphan the OpenIddict user record.

### What Employees Can Do

| Feature | Available by Default | Admin Action Required |
|---|---|---|
| View/download payslips | Yes | None |
| View TDS worksheet | Yes | None |
| View loans | Yes | None |
| Submit IT Declaration | No | Admin must release IT Declaration window (Settings → Claims & Declarations) |
| Upload Proof of Investments | No | IT Declaration must be released |
| Submit Reimbursement Claims | No | Admin must activate reimbursement component |
| View Form 16 | No | Admin must publish Form 16 |
| View Documents | No | Admin must enable "Show documents" toggle in portal settings |
| Choose Tax Regime | No | V1: off (new regime only) |

### Portal Settings (Settings → Employee Portal → Preferences)

| Setting | Notes |
|---|---|
| Enable Portal Access | Org-wide master switch |
| Banner Message + Display Until | Expiry date required when set |
| Portal Contact Information | Email shown to employees for queries (multiple contacts allowed) |
| Show documents in employee portal | Default off |

### IT Declaration (V1 — New Regime Only)

- Only `without_exemptions` pathway (new regime); do NOT implement old regime
- Under new regime: only NPS employer contribution (80CCD(2)) and standard deduction (₹75,000) affect TDS
- No 80C, no HRA exemption, no LTA in new regime
- "Allow employees to switch tax regimes": off in V1 (single regime only)

### Reimbursement Claim Flow

1. Employee → portal → Submit Claim (type, amount, date, description, receipt upload)
2. Status: Pending
3. Admin reviews in Approvals → Reimbursements
4. Admin approves (full or partial) + selects payout month
5. Included in that month's pay run
6. Status: Approved → Paid

---

## 12. Cross-Module Interaction Analysis

### 12.1 Setup Dependency Chain

```
OrgProfile (PAN) → TaxDetails (TAN) → PaySchedule
                                             ↓
StatutoryConfig (EPF/ESI/PT/LWF) ← WorkLocations (PT state per location)
                                             ↓
               SalaryComponents (Basic auto; HRA and others need config)
                                             ↓
                                    Add Employee (wizard completes)
                                             ↓
                                     First Pay Run
```

If admin goes to "Add Employee" without a Pay Schedule, they hit a hard wall at Step 2 — AFTER already filling Step 1. Detect this before the wizard opens and redirect.

### 12.2 WorkLocation Is More Than a Lookup

WorkLocation.State drives PT — not the employee's residential state. Employee lives in Bengaluru, works from Mumbai office → Maharashtra PT applies.

Consequences:
- Org with employees in 3 states must configure PT for each state separately (Settings → Statutory Components → Professional Tax)
- PT Number is per Work Location
- WorkLocation must pre-exist before bulk import — unlike Departments/Designations which can be inferred

### 12.3 Two-Layer Statutory Toggle

```
Org Level:      EPF = ON  (Settings → Statutory Components)
Employee Level: EPF = OFF (EmployeeStatutorySettings per employee)
```

Wizard only shows EPF checkbox if org-level EPF = ON. Per-employee toggle is set in wizard Phase 1 and editable post-creation from profile → Statutory Information.

Current `StatutoryToggle` entity is org-level only. `EmployeeStatutorySettings` entity is missing and must be created.

### 12.4 Fixed Allowance Invariant — Engine Owns This

The UI computes Fixed Allowance live. But the payroll engine must independently enforce:

```
Fixed Allowance = Monthly CTC − sum(all other component monthly amounts)
```

Any mutation to salary structure (CTC change, component add/remove, percentage change) must re-derive Fixed Allowance. The engine cannot trust the stored FA value — must recompute from components at pay run time.

### 12.5 Salary Revision × Pay Run Timing

Three scenarios the engine must handle:

- **Revision effective 1st of month:** Clean; full month at new CTC
- **Revision effective mid-month:** Engine detects `SalaryRevision` with `effective_from` within current pay period; computes `(days_before × old_daily) + (days_from × new_daily)`
- **Revision submitted while pay run is Draft/Approved:** Revision is queued; current run uses old CTC; next run uses new CTC

Pay run processor must query pending `SalaryRevision` records at COMPUTATION TIME, not initiation time.

### 12.6 Prior Employer YTD → TDS Cascade

```
Projected Annual = prior_gross + (current_monthly_gross × remaining_months)
Total Tax = new regime slabs on projected annual
Remaining TDS = total_tax − prior_tds − current_tds_so_far
Monthly TDS (future) = remaining_tds ÷ remaining_months
Standard deduction = ₹0 if prior employer already claimed ₹75,000
```

Saving `PriorEmployerYtd` must trigger TDS recomputation for all FUTURE months. Past months are immutable (finalised pay runs cannot be changed). Multiple prior employer records per (employee, FY) are valid.

### 12.7 WorkEmail Immutability — Data Integrity Constraint

WorkEmail is the OpenIddict user identity for the employee portal. Changing it would:
- Orphan the OpenIddict user record
- Break active portal sessions
- Misalign past payslip delivery records
- Break audit trail

The API must reject any attempt to modify WorkEmail post-creation with a clear error: "Work email cannot be changed after it has been saved." This is enforced at API layer, not just UI.

### 12.8 Tax Deductor Exit Block — Cross-Module Dependency

Employee exit handler must:
1. Query Settings → Tax Details to check if `employee_id` is the current Tax Deductor
2. If yes → return 422 with message: "Reassign Tax Deductor in Settings → Tax Details before exiting this employee"

This is explicit cross-module coupling between the Employee exit flow and the Tax Details settings.

### 12.9 Onboarding Gate at Pay Run — Not at Creation

Employee creation is lenient. The gate fires at pay run COMPUTE time, not at employee create time. Per-employee skips with per-reason detail appear in pay run summary. Admin can complete onboarding mid-month and manually include skipped employees.

### 12.10 Employee Exit × Regular Pay Run × F&F

```
Month N exit (LWD = 20th):
  Regular pay run Month N: prorates salary for days 1–20
  F&F items: leave encashment + gratuity − notice recovery − loan balance
  Single payment instruction combining both
  
After F&F:
  Status → Exited
  Excluded from all future regular pay runs
  F&F payslip → PersonalEmail (not WorkEmail)
```

F&F payslip goes to `PersonalEmail` because WorkEmail may be deactivated by the time disbursement happens.

### 12.11 Portal Invitation × WorkEmail Immutability

Portal invitation email goes to WorkEmail. Employee creates portal password linked to WorkEmail identity. If WorkEmail could later be changed, the identity is broken. The immutability constraint exists precisely because of this auth dependency.

---

## 13. Recommended Build Order

### Phase 1 — Prerequisites (Build Before Employee Wizard)

1. **Pay Schedule settings** (Settings → Pay Schedules)
   - Hard blocks salary step in employee wizard
   - Configures: pay frequency, pay day, salary calculation method (Actual Days / Fixed 30 Days)
   - Proration formula in the engine depends on this

2. **Statutory Components settings** (Settings → Statutory Components)
   - EPF configuration (employer % / employee %, wage ceiling)
   - ESI configuration (employer % / employee %, wage ceiling)
   - Professional Tax (per state — slab configuration; PT number per Work Location)
   - LWF (per state)
   - Without this: statutory section in wizard is blank; can't test statutory path

3. **Salary Components settings UI** (Settings → Salary Components → Earnings)
   - Refine existing domain entity (add IsActive, IsFixedAllowance, PfTreatment, EsiTreatment, IsProRated, PayType)
   - Build settings page for creating/managing components
   - Without this: "Add Earning" dropdown in wizard is empty

### Phase 2 — Employee Core

4. Domain model cleanup: fix Employee entity, fix enums, add missing entities
5. `EmployeeStatutorySettings` entity
6. `EmployeeBankDetails` entity (encrypted fields)
7. `EmployeeSalaryComponent` junction entity
8. Add Employee wizard — API (4 steps) + UI
9. Employee list page (with 7 system filter views)
10. Employee profile page (5 tabs: Overview, Salary Details, Investments, Payslips & Forms, Loans)

### Phase 3 — Employee Operations

11. Bulk import — Employee Basic Details (25 columns; ImportJob + ImportRow pipeline)
12. Salary revision (dated + direct edit)
13. Prior Employer YTD entry
14. Employee exit + F&F (can defer until after first payroll run works)

### Phase 4 — Portal

15. Employee portal invitation flow (WorkEmail-based auth)
16. Portal settings (Settings → Employee Portal)
17. Employee-facing portal: payslips, TDS, IT Declaration
