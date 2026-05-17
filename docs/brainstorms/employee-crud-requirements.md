# Employee CRUD + Salary Structure Assignment — Requirements Spec

> Zoho Payroll parity. New tax regime only (v1). Stack: .NET 8 + React 18 + TypeScript + PostgreSQL.
> Research: all audit docs in `docs/ba-audit/employees/` + live Zoho exploration (May 2026).

---

## 1. Domain Model Changes

### 1.1 Employee Entity — Required Additions

Current entity (`src/Payroll.Domain/Entities/Employee.cs`) is missing:

| Field | Type | Notes |
|---|---|---|
| `MiddleName` | `string?` | Optional |
| `FathersName` | `string` | Required |
| `WorkEmail` | `string` | Required, **immutable after creation** (portal login) |
| `PersonalEmail` | `string?` | Optional; required at exit if portal disabled |
| `MobileNumber` | `string?` | Optional at creation |
| `IsDirector` | `bool` | Affects TDS calculation |
| `WorkLocationId` | `Guid` | Replaces `WorkState` — state derived from WorkLocation |
| `BusinessUnitId` | `Guid?` | Optional org structure tag |
| `EnablePortalAccess` | `bool` | Toggle portal login |
| `PaymentMode` | `PaymentMode` enum | ManualBankTransfer / DirectDeposit / Cheque / Cash |
| `AccountHolderName` | `string?` | Bank detail |
| `BankName` | `string?` | Auto-populated via IFSC lookup |
| `AccountType` | `AccountType` enum | Savings / Current |
| `EpfEnabled` | `bool` | Per-employee toggle (only if org EPF enabled) |
| `EsiEnabled` | `bool` | Per-employee toggle (only if org ESI enabled) |
| `PtEnabled` | `bool` | Per-employee toggle |
| `LwfEnabled` | `bool` | Per-employee toggle |
| `ProfileComplete` | `bool` | False until all 4 wizard steps saved |

**Remove**: `WorkState` (derive from `WorkLocation.State`). Replace `BranchId` with `WorkLocationId`.

**Keep**: `EncryptedPAN`, `EncryptedAadhaar`, `EncryptedBankAccount`, `EncryptedIFSC` (AES-256), `UAN`, `ESICIPNumber`, `IsPWD`, `EmploymentType`.

**Remove**: `PFOptOut` — replaced by `EpfEnabled` (inverted logic; migration: `EpfEnabled = !PFOptOut`).

**`EmployeeStatus` enum** — change to:
```
Active | Exited | Inactive
```
Remove `OnNotice`, `Terminated`, `Resigned` (exit reason is stored on `EmployeeExit`, not the status enum).

### 1.2 EmployeeSalaryStructure Entity — Required Additions

Current entity missing:

| Field | Type | Notes |
|---|---|---|
| `SalaryStructureTemplateId` | `Guid` | Which org-level template was used |
| Component overrides | separate table | See §1.5 |

### 1.3 New Entity: SalaryRevision

```
SalaryRevision
  EmployeeId          Guid (FK)
  PreviousAnnualCTC   decimal
  NewAnnualCTC        decimal
  EffectiveFromMonth  int   (1–12)
  EffectiveFromYear   int
  PayoutMonth         int   (1–12)
  PayoutYear          int
  SalaryStructureTemplateId  Guid?   (optional override)
  Notes               string?
  Status              enum  (Pending / Applied)
  CreatedBy / timestamps
```

### 1.4 New Entity: EmployeeExit

```
EmployeeExit
  EmployeeId          Guid (FK, unique — one active exit record)
  LastWorkingDay      DateOnly
  Reason              ExitReason enum
  SettlementMode      enum (RegularSchedule / CustomDate)
  SettlementDate      DateOnly?   (only if CustomDate)
  PersonalEmail       string?
  Notes               string?
  CreatedBy / timestamps
```

**ExitReason enum**: `TerminatedByEmployer | TerminationByDeath | TerminationByDisability | ResignedByEmployee`

### 1.5 New Entity: EmployeeSalaryComponentOverride

Stores per-employee overrides when their component percentages differ from the template:

```
EmployeeSalaryComponentOverride
  EmployeeSalaryStructureId  Guid (FK)
  SalaryComponentId          Guid (FK)
  FormulaType                ComponentFormulaType enum
  Percentage                 decimal?
  FixedAmount                decimal?
```

### 1.6 New Entity: EmployeeVehicleDetail

For IT perquisite valuation of employer-provided vehicles:

```
EmployeeVehicleDetail
  EmployeeId         Guid (FK, unique — one record per employee)
  Owner              enum (Employer)      // Employee-owned not currently supported
  MaintainedBy       enum (Employer / Employee)
  CubicCapacity      enum (UpTo1600cc / Above1600cc)
  DriverProvided     bool
  CreatedBy / timestamps
```

Perquisite amounts (from IT rules, stored in config table — not hardcoded):
- ≤1600cc: ₹1,800/month; >1600cc: ₹2,400/month
- Employer maintenance adds: ₹900/month (≤1600cc) or ₹1,200/month (>1600cc)
- Driver adds: ₹900/month

### 1.7 New Entity: PriorEmployerYtd

For mid-year joiners:

```
PriorEmployerYtd
  EmployeeId             Guid (FK, unique per FY)
  FinancialYear          int  (e.g. 2026 for FY 2025-26)
  EmployerName           string
  PeriodFrom             DateOnly
  PeriodTo               DateOnly
  GrossSalary            decimal
  StandardDeductionClaimed  decimal  (0 or ₹75,000)
  ProfessionalTaxPaid    decimal
  TdsDeducted            decimal
  OtherIncome            decimal
  CreatedBy / timestamps
```

---

## 2. Add Employee Wizard — 4 Steps

Route: `POST /employees` (creates with `ProfileComplete = false` until step 4 saved).

### Step 1: Basic Details

| Field | Required | Validation | Notes |
|---|---|---|---|
| First Name | Yes | 2–100 chars | |
| Middle Name | No | | |
| Last Name | Yes | 2–100 chars | |
| Employee ID | Yes | Unique within tenant | Default: auto-generated (EMP001, EMP002…) or manual |
| Date of Joining | Yes | Must be ≥ first pay period start | |
| Work Email | Yes | Valid email, unique within tenant | **Immutable after creation.** Used as portal login. |
| Mobile Number | No | 10 digits | |
| Gender | Yes | Male / Female / Other | |
| Work Location | Yes | FK to WorkLocation | Determines PT/LWF state |
| Designation | Yes | FK to Designation | Inline create option |
| Department | Yes | FK to Department | Inline create option |
| Director | No | Checkbox | IsDirector flag |
| Enable Portal Access | No | Checkbox | Sends invite email if checked |

**UI notes**: Designation and Department have "Create new" inline without leaving the form.

### Step 2: Salary Details

Prerequisite: Pay Schedule must be configured. If not, show warning and block.

| Field | Required | Notes |
|---|---|---|
| Annual CTC | Yes | Decimal, min ₹1 |
| Salary Structure Template | Yes | Dropdown of org templates; selects which template to assign |
| Component percentages | — | Table pre-filled from template; each editable |

**Salary structure table** (editable):

| Salary Components | Calculation Type | Monthly Amount | Annual Amount |
|---|---|---|---|
| Basic | `% of CTC` (editable %) | auto | auto |
| HRA | `% of Basic` (editable %) | auto | auto |
| … other components from template | | | |
| Fixed Allowance | Residual (auto) | auto | auto |

**Statutory checkboxes** (conditional):
- PT: always shown (state known from Work Location)
- EPF: shown only if org has EPF enabled; checkbox to opt-in/out
- ESI: shown only if org has ESI enabled; checkbox to opt-in/out
- LWF: shown based on Work Location state

**Fixed Allowance** = Monthly CTC − sum of all other components. Read-only. Cannot be manually set.

**Validation**: Sum of all named component amounts must not exceed Annual CTC.

### Step 3: Personal Details

| Field | Required | Validation |
|---|---|---|
| Date of Birth | Yes | Must be ≥ 18 years ago |
| Age | — | Auto-calculated from DOB, read-only |
| Father's Name | Yes | |
| PAN | No at creation | Format: `AAAAA9999A` (regex) |
| Differently Abled Type | Yes | None / Visual / Hearing / Speech / Mobility / Other |
| Personal Email | No | Valid email |
| Address Line 1 | No | |
| Address Line 2 | No | |
| City | No | |
| State | No | Dropdown (IndianState enum) |
| PIN Code | No | 6 digits |

### Step 4: Payment Information

| Field | Required | Condition |
|---|---|---|
| Payment Mode | Yes | ManualBankTransfer / DirectDeposit / Cheque / Cash |
| IFSC Code | Yes (if bank modes) | 11 chars; live lookup populates Bank Name + Branch |
| Account Number | Yes (if bank modes) | Stored encrypted; masked as XXXX{last4} |
| Account Holder Name | Yes (if bank modes) | |
| Account Type | Yes (if bank modes) | Savings / Current |

**IFSC lookup**: On IFSC entry, call bank lookup API → auto-populate Bank Name and Branch Name. User can override.

**Displayed as**: XXXX6789 with "Show A/C No" button (requires authorised role + audit log).

---

## 3. Employee List Page

Route: `GET /employees`

### Views (pre-built, not paginated with a separate API — filtered server-side):

| View | Filter |
|---|---|
| All Employees | No filter |
| Active Employees | Status = Active |
| Exited Employees | Status = Exited |
| Incomplete Employees | ProfileComplete = false |
| Portal Enabled Employees | EnablePortalAccess = true |
| Portal Disabled Employees | EnablePortalAccess = false |
| Yet to Accept Portal Invite Employees | Portal invited but not yet accepted |

Views can be marked as Favorite. Custom views also supported (saved filter combos).

### Default Columns:
Employee Name (always) | Work Email | Department | Employee Status

### Selectable Columns (via column picker):
ID | Work Email | Mobile Number | Status | Portal Status | Date of Joining | Last Working Date | Payment Mode | Department | Designation | Work Location | Investment Declaration | Proof Of Investments | Flexible Benefit Plan | Reimbursement | Reason for Exit

### Incomplete Employee Indicator:
Row shows: "This employee's profile is incomplete. **Complete now**" link in the Work Email column (since email not saved yet).

### Banner:
"You have N incomplete employees. [View]" — shown when count > 0.

### Actions (list-level toolbar):
- **Add** button → opens 4-step wizard
- **More Actions** dropdown → Import Data | Export Data
- Column picker button
- Instant Helper button

### Import Types (by category):

**Employee Details**: Employee Basic Details | Employee Statutory Details | Employees Payment Information

**Salary Details**: Employee Salary Details | Salary Revision | Employee Scheduled Earnings | Employee Benefit Details | Employee Deduction Details | Perquisites | Employee Vehicle Details

**Complete Employee Basic Details**: Employee Details

**Employee Exit Details**: Employee Exit Details

**Investments**: Chapter VI-A Details | HRA Details | Previous Employment Details | Allowance Declaration Details | Other Income Details | Let Out Property Details | Tax Regime Details

---

## 4. Employee Overview Page

Route: `/employees/{id}`

**Header**: Avatar (initials) | Employee Code - Name | Status badge | Designation

**Sub-navigation tabs**: Overview | Salary Details | Investments | Payslips & Forms | Loans

**Header actions**:
- **Add** button (opens per-section add flows)
- **Dropdown** → Add / Update Vehicle Details | **Initiate Exit Process**
- **Close** button (back to list)

### 4.1 Basic Information Card

Display: Name | Email Address | Mobile Number | Date of Joining | Gender | Work Location | Designation | Departments | Portal Access (Disabled/Enabled with inline toggle button)

Edit route: opens inline edit modal for basic details.

### 4.2 Statutory Information Card

Display: EPF (Disabled/Enabled) | ESI (Disabled/Enabled) | Professional Tax (Disabled/Enabled) | Labour Welfare Fund (Disabled/Enabled)

Each shows current state with an inline "(Enable)" or "(Disable)" button — **one-click toggle with confirmation**.

Edit route: `#/employees/{id}/edit-statutory-details`

### 4.3 Personal Information Card

Display: Date of Birth | Father's Name | PAN | Email Address | Residential Address | Differently Abled Type

Edit route: `#/employees/{id}/edit-personal-details`

### 4.4 Payment Information Card

Display: Payment Mode | Account Number (XXXX{last4} + "Show A/C No" button) | Account Holder Name | Bank Name | IFSC | Account Type

Edit: inline modal.

---

## 5. Salary Details Tab

Route: `#/employees/{id}/salary-details`

### 5.1 Salary Details Card

Display: Annual CTC | Monthly CTC

Actions:
- **Edit** → full page at `#/employees/{id}/edit-salary-details`
- **Revise** button → opens salary revision form
- **Dropdown next to Revise** → Print Salary Structure | Send Salary Certificate | Print Salary Certificate

**Pending revision banner**: "The revised salary amount will be reflected in salary details upon the completion of {Month, Year} pay run. [View Details]"

### 5.2 Salary Structure Card

Read-only table: Salary Components | Monthly Amount | Annual Amount

Rows: each component with formula description in parens: e.g. "Basic (57.14% of CTC)" | ₹39,998.00 | ₹4,79,976.00

Footer row (bold): Cost to Company | Monthly CTC | Annual CTC

### 5.3 Perquisites Card

Display: Additional Benefits total | View Details link → `#/employees/{id}/perquisites`

### 5.4 Edit Salary Details (Full Page)

Route: `#/employees/{id}/edit-salary-details` — **full page, not a modal**.

Layout:
- **Salary Templates** dropdown (top) — select a saved org template to auto-populate the component table
- **Annual CTC** field — decimal input (₹)
- **Component table** (editable): Salary Components | Calculation Type (formula spinner) | Monthly Amount (read-only, auto-calculated) | Annual Amount (read-only)
  - Each component row has a formula-type spinner: `% of CTC` / `% of Basic` / `% of Gross` / `Fixed Amount` etc.
  - Fixed Allowance row is read-only (residual; always last row)
- **Add Earning** dropdown: shows only salary components that are **not already assigned** to the employee's current structure. Only earnings-category components appear.
- **Benefits** section (below table): shows employer-side benefits. Displays "EPF - Employer Contribution" entry only if that component is not already in the structure.
- **Save** / **Cancel** buttons

**Business rule on save**: creates a new effective salary structure record (or updates the current one if no payroll run has used it yet — TBD based on whether any payrun has been finalised for this employee).

### 5.5 Edit Statutory Details (Full Page)

Route: `#/employees/{id}/edit-statutory-details` — **full page, not a modal**.

Layout: simple form with 4 independent checkboxes:
- **Enable EPF** (shown only if org-level EPF enabled)
- **Enable ESI** (shown only if org-level ESI enabled)
- **Enable Professional Tax**
- **Enable Labour Welfare Fund**

**Save** / **Cancel** buttons.

Same as the inline toggles on the Overview tab but in page form. No additional fields.

---

## 6. Salary Revision Flow

Route: `#/employees/{id}/salary-revision/new` (new) or `.../salary-revision/{revId}/edit` (edit pending)

**Header info bar** (read-only): Previous CTC | Previous Monthly Salary | Last Revision month

### Fields

| Field | Required | Notes |
|---|---|---|
| Salary Templates | No | Dropdown to apply a saved template |
| Revision type | Yes | Radio: "Revise CTC by percentage" OR "Enter the new CTC amount below" |
| Percentage (if % mode) | Yes if % | Spinbutton, disabled if flat mode |
| Revised Annual CTC (if flat mode) | Yes if flat | ₹ input, disabled if % mode |
| Component table | — | Editable percentages; monthly auto-calculated (disabled) |
| Revised Salary effective from | Yes | Month/year picker (`M yyyy`) |
| Payout Month | Yes | Month/year picker (`M yyyy`) |

**Auto-arrear calculation**: System automatically calculates arrears if effective month ≠ payout month. No manual arrear component needed.

**Buttons**: Submit | Cancel

**Business rule**: Percentage mode → enter %; system calculates new CTC. Flat mode → enter new CTC; system recalculates component amounts.

**Previous revision pending note**: Cannot create a second revision while one is pending (status: Pending).

---

## 7. Initiate Exit Process

Route: `#/employees/{id}/terminate` (POST to `/employees/{id}/exit` — Zoho uses `/dismiss`)

**Navigation**: Cannot navigate directly to this URL — causes perpetual "Loading…" state in Ember. Must reach via employee profile dropdown → "Initiate Exit Process".

**Validation (pre-form)**: Cannot exit an employee who is the Tax Deductor for the org. Error: "You cannot initiate the exit process for {Name} as the employee is the Tax Deductor for your organisation. To change the tax deductor details, go to Settings → Taxes."

### Exit Form Fields

| Field | Required | Notes |
|---|---|---|
| Last Working Day | Yes | Date picker (dd/MM/yyyy); **must fall within current month** |
| Reason for Exit | Yes | Dropdown: 4 options |
| Final pay settlement | Yes | Radio: Pay as per regular pay schedule (default) OR Pay on a given date |
| Settlement date | Conditional | Date picker; shown only if "Pay on a given date" selected |
| Personal Email Address | No | With info icon; shown as note if portal disabled |
| Notes | No | Textarea |

**Reason for Exit options**: Terminated By Employer | Termination By Death | Termination by Disability | Resigned By Employee

**Right panel** (read-only employee card): Avatar | Name | Employee ID | Designation | Department | Date of Joining

**Portal-disabled note**: "Portal is not enabled for this employee. Kindly collect the proof of investments before processing the payroll."

**POI warning** (always shown at bottom): Advisory that Proof of Investment must be submitted before the final settlement pay run can be approved.

**Buttons**: Proceed | Cancel

### After Submission

1. `EmployeeExit` record created; `DateOfLeaving` = Last Working Day.
2. **Employee status remains `Active`** until the Last Working Day passes — it does NOT flip to `Exited` immediately.
3. System auto-creates a draft pay run: **"Bulk Final Settlement Payroll (POI Based)"** for the settlement month. This pay run calculates pro-rated pay for paid days worked. It cannot be approved until POI is submitted.
4. Confirmation dialog shown to admin (does NOT auto-send anything to employee unless portal enabled).

### Post-Exit Profile State (while LWD is in the future)

Header actions change:
- **"Edit Exit Details"** button appears prominently (replaces "Initiate Exit Process")
- Dropdown now shows: **Add / Update Vehicle Details** | **Exit History** | **Revert Exit Process**
- "Initiate Exit Process" is no longer shown

### Edit Exit Details

Route: same exit form pre-populated with existing values. Same field set and validations as the initiation form. Allows updating LWD, reason, settlement mode, personal email, notes. Auto-updates the final settlement pay run if settlement date changes.

### Exit History Modal

Accessible via dropdown → "Exit History". Shows a table:

| DATE OF JOINING | LAST WORKING DATE | FINAL SETTLEMENT DATE |
|---|---|---|
| 16/05/2025 | 17/06/2026 | 30/06/2026 |

Final Settlement Date = last day of the settlement month (either regular schedule month end or custom date).

### Revert Exit Process

Accessible via dropdown → "Revert Exit Process". Cancels the exit:
- Deletes the `EmployeeExit` record
- Voids the auto-created final settlement pay run (if not yet approved)
- Restores employee to normal active profile header actions
- Not available once the final settlement pay run is finalised

---

## 8. Vehicle Details (Perquisite)

Accessible from employee profile header dropdown: **"Add / Update Vehicle Details"**. Opens as a modal (not a full page).

This records employer-provided vehicle details for Income Tax perquisite valuation. Used to compute the taxable benefit of a company car.

### Vehicle Details Modal Fields

| Field | Options | Notes |
|---|---|---|
| Owner | Employer / Employee | Employer is default and pre-selected; Employee option is disabled (employer-owned vehicles only) |
| Maintained by | Employer / Employee | Who pays for running expenses |
| Cubic Capacity | ≤ 1600cc / > 1600cc | Determines perquisite rate |
| Driver provided | Yes / No | Adds driver perquisite value |

**Business logic**: IT rules set the taxable perquisite value as:
- Car ≤1600cc: ₹1,800/month; >1600cc: ₹2,400/month
- If maintenance by employer: add ₹900/month (≤1600cc) or ₹1,200/month (>1600cc)
- If driver provided: add ₹900/month

These amounts are taxable income added to the employee's gross for TDS calculation.

**Impact on payroll**: Perquisite value is included in the employee's gross income for TDS purposes. Appears in the Perquisites card on the Salary Details tab.

---

## 9. Prior Employer YTD

Route: accessible from Investments tab (when employee joined mid-financial year)

Fields: Employer Name | Period From | Period To | Gross Salary | Standard Deduction Claimed (0 or ₹75,000) | Professional Tax Paid | TDS Deducted | Other Income

**Business rule**: Only applicable when Date of Joining is after April 1 of the current financial year.

---

## 10. Investments Tab (New Regime v1 Scope)

Route: `#/employees/{id}/investments-and-proofs`

**Sub-tabs**: IT Declaration | Proof Of Investments

**Period selector**: Financial year dropdown (e.g. "2026 - 27")

**IT Declaration state**: Locked (default admin-side) OR open for submission.

**Lock/unlock**: Admin can lock or allow employee portal submission. When locked, admin can "Submit Declaration" on behalf.

### V1 New Regime — What to Show

Since new regime has no 80C/80D exemptions, the declaration form is minimal:

| Section | New Regime |
|---|---|
| HRA (rented house toggle) | DEFERRED — old regime only |
| Home loan repayment | DEFERRED — old regime only |
| Let-out property income | Include (taxable income) |
| Other Sources of Income | Include |
| Section 80C investments | DEFERRED — old regime only |
| Section 80D exemptions | DEFERRED — old regime only |
| Other Investments (NPS/Education Loan) | Include (NPS 80CCD(2) applies to new regime) |
| Prior Employer YTD | Include |
| Tax Regime selection | Include (but v1 = new regime only, so fixed) |

**Submit and Compare** (Zoho shows regime comparison) → v1: skip comparison, just submit.

---

## 11. Business Rules Summary

### Immutability Rules

| What | When it locks |
|---|---|
| Work Email | After creation — never changeable |
| Employee Code | After creation — never changeable |
| Work Location State | After creation (affects historical PT) |
| Salary Component flags (EPF/ESI/formula type) | After any payroll run uses the component |

### Statutory Toggle Rules

- EPF enable/disable per-employee: only available if org-level EPF is enabled in Settings
- ESI enable/disable per-employee: only if org-level ESI enabled
- PT: can disable per-employee (e.g. international contractors)
- LWF: can disable per-employee
- Toggles take effect from the next pay run — no retroactive effect

### Salary Structure Assignment Rules

- Employee must have exactly one active salary structure at any time
- New revision creates a new `EmployeeSalaryStructure` record with new effective date and closes the old one (`EffectiveTo`)
- Fixed Allowance is always the residual — cannot be given a custom formula
- If template has no Fixed Allowance component, system does not add it

### Profile Completeness

- `ProfileComplete = false` until all 4 wizard steps are saved
- Incomplete employees appear in list with "Complete now" link
- Incomplete employees cannot be included in payroll runs
- Counter banner on list page shows count

### Exit Rules

- Cannot exit the Tax Deductor (check against Settings → Taxes config)
- LWD **must fall within the current calendar month** — cannot be a past or future month
- After initiation: `EmployeeExit` record created; employee status **stays `Active`** until LWD passes
- After LWD passes: system marks employee `Exited` (scheduled job or on-access check)
- Final pay settlement: either folded into next regular run OR on custom date
- Auto-creates draft "Bulk Final Settlement Payroll (POI Based)" pay run immediately on initiation
- Final settlement pay run cannot be approved until POI submitted by employee (or admin)
- Final settlement date = last day of settlement month (regular schedule) or the custom date
- Revert is possible while final settlement pay run is not yet finalised
- Employee record is soft-deleted (never hard-deleted)
- Once `Exited`: "Rehire" button appears; employee can be re-activated as a new employment record

### Employee Code Auto-generation

- Pattern: EMP001, EMP002, EMP003 (incrementing, padded to 3+ digits)
- Admin can override with custom code
- Must be unique within tenant

---

## 12. API Endpoints Needed

```
POST   /employees                                    Create (step 1 only; returns id)
PUT    /employees/{id}/basic-details                 Update basic info
PUT    /employees/{id}/salary-details                Update salary + structure
PUT    /employees/{id}/personal-details              Update personal info
PUT    /employees/{id}/payment-info                  Update bank/payment
PUT    /employees/{id}/statutory-details             Update EPF/ESI/PT/LWF toggles

GET    /employees                                    List (with view + column filters)
GET    /employees/{id}                               Overview
GET    /employees/{id}/salary-details                Salary tab
GET    /employees/{id}/investments                   Investments tab

POST   /employees/{id}/salary-revision               Create revision
GET    /employees/{id}/salary-revisions              Revision history
GET    /employees/{id}/salary-revision/{revId}       Single revision
DELETE /employees/{id}/salary-revision/{revId}       Cancel pending revision

POST   /employees/{id}/exit                          Initiate exit (creates EmployeeExit + final settlement pay run)
PUT    /employees/{id}/exit                          Edit exit details
DELETE /employees/{id}/exit                          Revert exit (voids final settlement pay run; only if not finalised)
GET    /employees/{id}/exit                          Exit record (for Exit History modal)

PUT    /employees/{id}/vehicle-details               Create or update vehicle/perquisite details
DELETE /employees/{id}/vehicle-details               Remove vehicle details

POST   /employees/{id}/portal-access                 Enable portal
DELETE /employees/{id}/portal-access                 Disable portal

POST   /employees/{id}/prior-employer-ytd            Create YTD entry
PUT    /employees/{id}/prior-employer-ytd/{ytdId}    Update
DELETE /employees/{id}/prior-employer-ytd/{ytdId}    Delete

GET    /bank/ifsc/{code}                             IFSC lookup (proxy to external API)

GET    /employees/{id}/account-number                Masked; full reveal = authorised role + audit log
```

---

## 13. Domain Entity Gaps vs Current Code

| Gap | Action |
|---|---|
| `Employee.WorkState` | Replace with `WorkLocationId` FK; derive state from WorkLocation |
| `Employee.BranchId` | Remove; WorkLocationId serves this purpose |
| `Employee.PFOptOut` | Remove; replace with `EpfEnabled` (inverted logic — migration: `EpfEnabled = !PFOptOut`) |
| `EmployeeSalaryStructure` missing template FK | Add `SalaryStructureTemplateId` |
| `EmployeeStatus` enum stale | Replace with `Active / Exited / Inactive` |
| No `SalaryRevision` entity | Create new entity |
| No `EmployeeExit` entity | Create new entity |
| No `PriorEmployerYtd` entity | Create new entity |
| No `EmployeeSalaryComponentOverride` entity | Create new entity |
| `PaymentMode` enum missing | Create enum |
| `AccountType` enum missing | Create enum |
| `ExitReason` enum missing | Create enum |
| `DifferentlyAbledType` enum missing | Create enum |
| No `EmployeeVehicleDetail` entity | Create new entity (Owner, MaintainedBy, CubicCapacity, DriverProvided) |

---

## 14. UI Layout Notes (Zoho Parity)

### List Page Layout
- Top bar: [View selector dropdown] ... [3 incomplete banner] [Add] [More Actions ▾] [Column picker] [Help]
- Table: fixed Employee Name column; scrollable right for other columns
- Row click → navigate to employee overview
- Incomplete rows: Work Email column replaced with warning + "Complete now" link

### Overview Page Layout
- Left: employee avatar + name header, sub-nav tabs
- Content: 4 cards (Basic Info, Statutory Info, Personal Info, Payment Info)
- Each card: 2-column grid of label-value pairs, Edit button top-right
- Statutory toggles: inline status icon + "(Enable)"/"(Disable)" button — NOT a separate page

### Salary Details Page Layout
- CTC summary card (top): Annual CTC | Monthly CTC | Revise button
- Pending revision info bar (if applicable)
- Salary structure card: 3-column table (component | monthly | annual)
- Perquisites card: total + View Details link

### Edit Salary Details Page Layout
- Full page (not modal)
- Top: Salary Templates dropdown + Annual CTC input
- Middle: component table (Salary Components | Calculation Type | Monthly | Annual)
- Below table: Add Earning dropdown (only non-assigned earning components) + Benefits section
- Bottom: Save / Cancel buttons

### Edit Statutory Details Page Layout
- Full page (not modal)
- Simple 4-checkbox form: EPF | ESI | PT | LWF
- Conditional display: EPF/ESI checkboxes hidden if org-level feature is disabled
- Save / Cancel buttons

### Vehicle Details Modal Layout
- Modal overlay (not full page)
- 4 fields: Owner (radio, Employer preselected/Employee disabled) | Maintained By (radio) | Cubic Capacity (radio ≤1600cc/>1600cc) | Driver Provided (radio Yes/No)
- Save / Cancel buttons

### Exit Process Page Layout
- Left 2/3: form fields
- Right 1/3: employee summary card (read-only)
- Note bar at bottom (if portal disabled warning)
- POI warning advisory at bottom

### Add Employee Wizard Layout
- Progress indicator showing 4 steps
- Step title + form fields
- "Save & Continue" / "Back" navigation
- Inline create for Designation and Department (modal or inline row)
