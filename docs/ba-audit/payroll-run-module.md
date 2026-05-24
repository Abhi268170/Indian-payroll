# Payroll Run Module — Exhaustive BA Audit & Build Guide

**Source:** Zoho Payroll India (payroll.zoho.in), organisation "lerno"
**Audit Dates:** 2026-05-15 and 2026-05-16
**Pay Runs Observed:** April 2026 (Regular, PAID), May 2026 (Regular, PAID)
**Pay Run IDs:** April: (not captured), May: `3848927000000034159`
**Auditors:** BA Agent (Playwright MCP) — 17-file deep-dive in `docs/ba-audit/pay-runs/`
**Reference Files:** `pay-runs/52` through `pay-runs/67`, userflows `UF-36` through `UF-58`, `UF-A1`, `UF-A2`, `UF-A12`, `UF-A13`

---

## Table of Contents

1. [Module Overview & Navigation](#1-module-overview--navigation)
2. [Pay Run Initiation — Period Selection](#2-pay-run-initiation--period-selection)
3. [Pre-Payroll Checklist (Pending Tasks)](#3-pre-payroll-checklist-pending-tasks)
4. [Draft State — Variable Inputs](#4-draft-state--variable-inputs)
5. [Run Payroll — Approval Flow](#5-run-payroll--approval-flow)
6. [Payroll Summary / Results Screen](#6-payroll-summary--results-screen)
7. [Employee Line-Item Table](#7-employee-line-item-table)
8. [Individual Payslip View](#8-individual-payslip-view)
9. [Payslip Download & Email](#9-payslip-download--email)
10. [Record Payment (Mark as Paid)](#10-record-payment-mark-as-paid)
11. [Bank Advice / Salary Payment](#11-bank-advice--salary-payment)
12. [Overall Insights Tab](#12-overall-insights-tab)
13. [Taxes & Deductions Tab](#13-taxes--deductions-tab)
14. [Delete Recorded Payment (Reversal)](#14-delete-recorded-payment-reversal)
15. [Off-Cycle Pay Runs](#15-off-cycle-pay-runs)
16. [Payroll History](#16-payroll-history)
17. [State Machine — Complete](#17-state-machine--complete)
18. [Business Rules Reference](#18-business-rules-reference)
19. [Data Relationships](#19-data-relationships)
20. [Statutory Filings Cross-Reference](#20-statutory-filings-cross-reference)
21. [Build Guide](#21-build-guide)

---

## 1. Module Overview & Navigation

### Sidebar Position

"Pay Runs" is a top-level left sidebar item. Clicking it lands on `#/payruns` (Run Payroll tab).

### URL Structure

| Route | Description |
|-------|-------------|
| `#/payruns` | Run Payroll tab — outstanding pay runs |
| `#/payruns/payroll-history` | Payroll History — all completed runs |
| `#/payruns/{id}/summary` | Pay run detail page (Draft / Approved / Paid states) |
| `#/payruns/{id}/summary?selectedTab=taxes` | Taxes & Deductions tab |
| `#/payruns/{id}/summary?selectedTab=insights` | Overall Insights tab |
| `#/payruns/{id}/add-employees?employee_type=employee&filter_by=Employee.MISSING` | Add missing employees gate |
| `#/payruns/{id}/add-employees?filter_by=Employee.WITHOUT_PAN` | PAN-missing employees view |
| `#/payruns/insights/{runId}/earnings/{componentId}?override_type=` | Component drill-down |

### Module-Level Action Buttons

| Button | Location | State | Purpose |
|--------|----------|-------|---------|
| New | Top-right toolbar | Always | Opens dropdown: One Time Payout / Off Cycle Pay Run |
| Instant Helper | Top-right | Always | Contextual help overlay |
| Run Payroll tab | Sub-nav | Default | Outstanding pay runs view |
| Payroll History tab | Sub-nav | Always | Historical runs |

---

## 2. Pay Run Initiation — Period Selection

### URL: `#/payruns` (Run Payroll tab)

### How Regular Pay Runs Are Created

Regular monthly pay runs are **auto-created by the system** — there is no "Create Regular Pay Run" button or period picker. The system:

1. Determines the next payable period from the last completed run + 1 month
2. Displays a "period card" on the Run Payroll tab
3. Admin clicks the card to initiate the run

**There is no manual period selection.** Admin cannot pick a past or future month for a regular run.

### Period Card Fields

| Field | Type | Notes |
|-------|------|-------|
| Heading | Read-only | "Process Pay Run for [Month] [Year]" |
| Pay period range | Read-only | e.g., "01/05/2026 - 31/05/2026" |
| Payment Date | Read-only | Configured pay day from Settings > Pay Schedule |
| Employees' Net Pay | Read-only | "Yet To Process" until run created |
| No. of Employees | Read-only | Total active employees eligible |

### Conditional Banners on Card

| Condition | Banner text |
|-----------|-------------|
| Pay day has passed and run not created | "You haven't processed this pay run and it's past the pay day." (warning style) |
| Pay day is approaching | "Please process and approve this pay run before {date}" (urgency style) |
| No outstanding runs exist | Empty state heading "You deserve a break today!" + "You have no outstanding pay runs." |

### Create Pay Run Action

Clicking the period card:
1. Creates a PayrollRun record immediately (assigns an ID)
2. Checks employee eligibility
3. If employees are missing/incomplete: navigates to `#/payruns/{id}/add-employees?filter_by=Employee.MISSING`
4. Otherwise: navigates directly to `#/payruns/{id}/summary` in Draft state

**There is no intermediate confirmation dialog for regular run creation.** Click = create.

### New Dropdown — Non-Regular Run Types

| Option | Dialog | Purpose |
|--------|--------|---------|
| One Time Payout | "Create One Time Payrun" — Component selector + Pay Date | Single salary component mass payout |
| Off Cycle Pay Run | "Initiate Off Cycle Pay Run" — Pay Date only | Mid-cycle full pay run |

---

## 3. Pre-Payroll Checklist (Pending Tasks)

### URL: `#/payruns/{id}/summary` (Draft state) — warning banner section

Zoho does not use the term "pre-payroll checklist." Instead it shows a **"Pending Tasks"** collapsible banner at the top of the Draft summary page.

### Pending Tasks Banner

**Location:** Top of the Draft summary page, below the header.
**Expand/collapse:** Clicking reveals all pending task items.
**Counter:** "+N more task(s) to be completed before you approve this payroll." — expands to show all.

### Task Types Observed

| Task | Type | Blocking? | Resolution Action |
|------|------|-----------|-------------------|
| "N employees are not included in this payroll." | Hard block | YES | "View Employees" button → add-employees page |
| "1 Employee(s) PAN details are not found." | Soft warning | NO | "View Employees" button → PAN update page |

### Hard Block Tasks

These tasks **prevent approval** — clicking "Submit and Approve" triggers an error toast:
> "Please complete your pending tasks to approve this payroll."

**Hard block tasks confirmed:** Employees with onboarding status "Missing" (incomplete profiles).

### Soft Warning Tasks

These tasks show in the pending tasks list but **do NOT block approval.** The approval dialog still opens.

**Soft warning tasks confirmed:** PAN missing for one or more employees.

**Statutory implication of missing PAN:** Section 206AA of the Income Tax Act requires TDS at 20% flat if PAN is not available. Zoho computes TDS at 20% for these employees and flags them as a warning. This is legally compliant behavior (admin is warned, not blocked).

### Add Employees Gate (Hard Block Resolution)

**URL:** `#/payruns/{id}/add-employees?employee_type=employee&filter_by=Employee.MISSING`

**Purpose:** Shows employees who are active but cannot be included due to incomplete onboarding.

**Table columns:**

| Column | Notes |
|--------|-------|
| Checkbox | Bulk-select for actions |
| Employee Name | Link to employee profile |
| Onboarding Status | e.g., "Pending" |
| Work Email | From employee profile |
| Designation | From employee profile |
| Actions | "Complete Now" + "Skip" per row |

**Per-row actions:**

| Action | Destination | Effect |
|--------|-------------|--------|
| Complete Now | `#/people/employees/{id}?from=payrun&from_route_id={runId}` | Opens employee profile with payrun context |
| Skip | Skip dialog (see below) | Permanently skips employee for this pay cycle |

**Bulk actions:** Select multiple employees → "Add to payroll" or "Skip" bulk action bar appears.

**Skip Employee Dialog — Single:**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Employee (display) | Read-only | — | Shows employee name |
| Payroll Period (display) | Read-only | — | Shows period |
| Please enter a reason | Textarea | YES — mandatory | Reason stored permanently |

Warning shown: "Once you skip an employee(s) from the pay run, you will not be able to pay them later for this pay cycle."

**Buttons:** "Proceed" | "Cancel"

**API:** `PUT /api/v1/payrollruns/{run_id}/employees/skip?notes={reason}&pay_joinee_arrear_later={bool}`

`pay_joinee_arrear_later=true` flag allows skipped new joiners to receive their prorated salary as an arrear in the next pay run.

**API validation:** Notes field rejects certain characters (hyphen `-` causes 400). Short alphanumeric text required.

**Navigation at bottom:** "Continue to Payroll" — bypasses unresolved employees and returns to preview. **This allows approval even with unresolved employees** (those employees must have been skipped first).

**PAN-missing variant:** `filter_by=Employee.WITHOUT_PAN`
- No checkbox — no bulk action available
- Per-row: "Update Now" → `#/people/employees/{id}/edit/personal-details?from=employees-with-incomplete-details`
- Cannot skip from this page — PAN must be updated
- Blocking only as a warning, not a hard block for approval

### Onboarding Completeness Gate

An employee is **included** in the pay run only if ALL of the following are complete:

- Date of Birth
- Father's Name / Husband's Name
- Personal Email
- Permanent Address
- Bank Account Details
- Salary Structure Assignment

**PAN is NOT required for inclusion** — employees without PAN are included with a soft warning and TDS computed at 20%.

**Important:** A missing subset of fields causes the "Onboarding incomplete" status — it is a composite check, not a single-field gate.

---

## 4. Draft State — Variable Inputs

### URL: `#/payruns/{id}/summary` (Status: Draft)

### Page Header (Draft State)

| Element | Notes |
|---------|-------|
| Back link | → `#/payruns` |
| Title | "Regular Payroll" |
| Status badge | "Draft" |
| Comments button | Icon-only — adds comments to pay run |
| Submit and Approve button | Primary CTA — blocked by pending tasks |
| Page kebab (three-dot) | "Show Downloads" + "Delete Pay Run" |

### Summary Card / Info Strip

| Field | Notes |
|-------|-------|
| Period | "01/05/2026 - 31/05/2026" |
| Base Days | Calendar days in month (e.g., 31 for May) |
| Month | "May 2026" |
| Payroll Cost | Sum of gross pay for included employees |
| Total Net Pay | Sum of net pay after deductions |
| Pay Day | From pay schedule configuration |
| Employee Count | Included (non-skipped) employees only |

**Note:** In Draft, "Download Bank Advice" button is NOT shown. It only appears after approval.

### Mini Taxes Summary (in card)

| Row | Value |
|-----|-------|
| Taxes | ₹ sum of TDS + PT |
| Benefits | ₹ pre-tax benefit deductions |
| Donations | ₹ charitable deductions |
| Total Deductions | ₹ total |

### Employee Summary Tab — Draft Columns

| Column | Notes |
|--------|-------|
| Checkbox | Multi-select |
| Employee Name | Clickable — opens variable inputs split panel |
| Paid Days | Calendar days in period (before LOP) |
| Gross Pay | Sum of all earning components |
| Deductions | Post-tax deductions |
| Taxes | Income Tax + PT |
| Benefits | Pre-tax deductions |
| Reimbursements | Approved claims |
| Net Pay | Gross - Taxes - Deductions + Benefits |
| Kebab (per row) | 6 actions (see Row Kebab Matrix below) |

**Toolbar:** "All Employees" filter | "Select an Employee" search combobox | "Filter" | "Import / Export" dropdown

### Skipped Employee Rows (Draft State)

Different column structure for skipped employees:
- No Paid Days, Gross, Taxes, Net Pay
- Shows: "Skipped" badge + "Reason: {entered reason}" + "Complete Now" link
- Checkbox disabled — cannot re-add within this pay run

### Per-Employee Variable Inputs — Split Panel

Triggered by clicking any employee name in the Draft table. Panel slides in from the right. Does NOT navigate to a new URL.

#### Panel Header

| Field | Notes |
|-------|-------|
| Employee name | Link to employee profile |
| Salary Structure name | e.g., "Senior Engineer" |
| "Net Pay" label | |
| Emp. ID | e.g., EMP001 |
| Net Pay amount | Auto-updates as inputs change |

#### Section 1: Paid Days Table

| Field | Notes |
|-------|-------|
| Payable Days | Calendar days in pay period |

#### Section 2: LOP (Loss of Pay) Entry

Activated by clicking "Add LOP" inline action within the panel.

| Field | Type | Default | Validation | Notes |
|-------|------|---------|------------|-------|
| LOP Days | Spinbutton (integer) | 0 | 0 to (Base Days − 1) | Prorates all fixed salary components |
| Actual Payable Days | Read-only | Base Days | Auto-calc: Base − LOP | Updates live |

**LOP save behavior:** After clicking Save, all fixed salary components recalculate via:
```
Prorated Amount = (Base Days − LOP Days) / Base Days × Full Component Amount
```

**Observed proration (EMP001, May 2026, 2 LOP, 31 base days):**
- Basic ₹40,000 × 29/31 = ₹37,417
- HRA ₹16,000 × 29/31 = ₹14,967
- Fixed Allowance ₹14,000 × 29/31 = ₹13,100

**LOP kebab dropdown within panel:** "Adjust Past LOP" — allows retroactive LOP correction for prior pay periods.

**Component proration rules:**

| Component Type | Prorated on LOP | Notes |
|----------------|----------------|-------|
| Basic | Yes | Fixed component × payable days / calendar days |
| HRA | Yes | Fixed component × payable days / calendar days |
| Conveyance Allowance | Yes | Fixed component |
| Special Allowance | Yes | Fixed component |
| Fixed Allowance | Configuration-dependent | May be marked non-prorated |
| Variable (Bonus, Commission) | No | Flat amounts entered by admin |

#### Section 3: Earnings Table (one-time additions)

Each configured component shows with its amount. Plus:

**"Add Earning" button** → opens a listbox with earning types:
- Bonus
- Commission
- Leave Encashment

(Only earning types pre-configured in Settings > Salary Components are shown.)

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Earning Type | Listbox | Yes | Bonus / Commission / Leave Encashment |
| Amount | Number input | Yes | Decimal ₹, positive |

One-time earnings do NOT affect LOP proration — they are additive to gross.

#### Section 4: Deductions Table (TDS Override)

**Income Tax row:** Shows computed TDS amount with an **"Edit" button** (inline).

Clicking Edit reveals:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| TDS Amount | Spinbutton (decimal ₹) | No | Override for this month only |
| Calculated Value | Read-only | — | "Calculated Value: {engine amount}" |
| Reason | Textarea | YES — mandatory | Cannot save override without reason |

**Business rule:** TDS override is month-scoped only. Does not affect future runs. Reason stored as audit trail.

**Professional Tax row:** Read-only — system-computed from state slab. No override mechanism observed.

#### Section 5: Net Pay

Auto-computed: Gross Earnings − Total Deductions. Updates in real-time as inputs change.

#### Panel Footer Buttons

| Button | Notes |
|--------|-------|
| Save | Applies all changes; panel refreshes; summary table row updates |
| Cancel | Shows confirmation dialog if unsaved changes exist |

**Cancel Confirmation Dialog:**
> "You are about to cancel the changes done to this employee. All the values you have entered will be removed. Are you sure you want to proceed?"
Buttons: "Yes" (discards) | "No" (returns to panel)

### Import / Export (Draft State)

**Location:** "Import / Export" dropdown button in toolbar of Employee Summary tab (Draft state only).

#### Import Options (5 types)

| Import Type | Purpose |
|-------------|---------|
| Import LOP | Bulk LOP days entry (CSV/XLSX) |
| Import One Time Earnings | Bulk bonus / commission / leave encashment |
| Import Reimbursements | Bulk approved reimbursement amounts |
| Import Adhoc Deductions | Bulk ad-hoc deduction entries |
| Import Variable Pay | Bulk variable salary component entry |

**Import page features:**
- File upload (CSV/XLSX)
- Encoding selector (UTF-8, ISO-8859-1, etc.) — for Indian names with special characters
- Template download (matching format expected for upload)

#### Export Options (2 types)

| Export Type | Purpose |
|-------------|---------|
| Export Payroll Data | Current month's payroll data as CSV/XLSX |
| Export Comparison Report | Current vs prior month comparison |

**Note:** "Import / Export" changes to "Export Data" only after approval — import is disabled post-approval.

### Per-Row Kebab — Draft State (6 Actions)

| Action | Notes |
|--------|-------|
| View Payslip | Opens read-only payslip panel (computed payslip, not final) |
| View TDS Sheet | Opens TDS computation PDF iframe |
| Skip Employee | Opens Skip dialog; permanent for this cycle |
| Undo Skip | Removes skip (only if employee is skipped in this run) |
| Withold Salary | Includes employee in run but flags payment as withheld (not paid) |
| Revise Salary | Opens salary revision form — applies to current or next run per effective date |

---

## 5. Run Payroll — Approval Flow

### URL: `#/payruns/{id}/summary` (Draft state) — page kebab action

### Approval Trigger

**Page kebab (three-dot menu)** → "Approve Payroll"

In Zoho's observed implementation, the "Submit and Approve" is the primary CTA button at top-right of the Draft header. The approval action is also called "Approve Payroll" from kebab.

### Pre-conditions — Hard Block Check

Before the approval dialog opens, the system checks for pending tasks:

**If hard-block tasks exist:**
- Error toast shown: "Please complete your pending tasks to approve this payroll."
- Approval dialog does NOT open.

**If no hard-block tasks (or only soft warnings):**
- Approval confirmation dialog opens.

### Approve Payroll Confirmation Dialog

| Field | Type | Content |
|-------|------|---------|
| Dialog title | Heading | "Approve Payroll" |
| Warning bullets | Paragraph | "On approving this payroll, your employees will not be able to: Raise any Reimbursement claims for this month / Declare or update the IT or POI declaration for this month" |
| Summary | Read-only | Period, employee count, total net pay |
| Confirm button | Button | "Submit and Approve" |
| Cancel button | Button | Returns to Draft |

### Locks Applied on Approval

| Item Locked | Cannot Change After Approval |
|-------------|------------------------------|
| Variable inputs (LOP, earnings, TDS override) | Read-only |
| Reimbursement claims | Cannot raise or modify |
| IT Declaration | Cannot update exemptions/investments |
| POI (Proof of Investment) | Cannot upload or update |

### Post-Approval State Transition

- Status badge: "Draft" → "Approved" (or "Payment Due" if pay day has passed)
- URL: stays at `#/payruns/{id}/summary`
- Page kebab changes: "Approve Payroll" disappears; "Reject Approval" appears
- Split panel becomes read-only: no LOP, no Add Earning, no TDS Edit, no Save/Cancel buttons
- Notification count increments

### Approved State Badge Variants

| Badge | Condition |
|-------|-----------|
| "Approved" | Pay day is in the future |
| "Payment Due" | Pay day has passed but payment not yet recorded |

Both badges represent the same logical state (between Draft and Paid).

### Reject Approval (Approved → Draft)

Available from page kebab in Approved state:

**Reject Approval Dialog:**

| Field | Type | Required |
|-------|------|----------|
| Reason | Textarea | Optional (not mandatory) |
| Confirm ("Reject") | Button | Triggers Draft reversion |
| Cancel | Button | Stays Approved |

After rejection: status → Draft; all variable inputs re-editable.

---

## 6. Payroll Summary / Results Screen

### URL: `#/payruns/{id}/summary` (Approved or Paid state)

### Header (Approved/Paid State)

| Element | Approved State | Paid State |
|---------|---------------|------------|
| Back link | `#/payruns` | `#/payruns/payroll-history` |
| Title | "Regular Payroll" | "Regular Payroll" |
| Status badge | "Approved" / "Payment Due" | "Paid" |
| Primary CTA | "Record Payment" button | "Send Payslip" button |
| Secondary | Page kebab | Page kebab (4 options) |

### Info Strip / Summary Card

| Field | Notes |
|-------|-------|
| Period | "01/05/2026 - 31/05/2026" |
| Base Days | "31 Base Days" |
| Month | "May 2026" |
| Payroll Cost | Total gross + employer contributions (= net pay when no PF/ESI) |
| Total Net Pay | Sum of all employees' net pay |
| Pay Day | Configured pay date |
| Employees | "5 Employees" (total including skipped) |
| Skipped | "( 3 Skipped )" button — filter for skipped employees |
| Download Bank Advice | Button — appears ONLY after approval; not in Draft |
| Taxes | ₹ |
| Benefits | ₹ |
| Donations | ₹ |
| Total Deductions | ₹ |

**Payroll Cost vs Total Net Pay:**
- When employer PF + ESI = ₹0: Payroll Cost = Total Net Pay
- When employer PF/ESI configured: Payroll Cost = Total Net Pay + Employer PF + Employer ESI + EDLI

### Three Tabs

| Tab | URL Param | Content |
|-----|-----------|---------|
| Employee Summary | (default, no param) | Per-employee table |
| Taxes & Deductions | `?selectedTab=taxes` | Statutory breakdown by type |
| Overall Insights | `?selectedTab=insights` | Aggregate analytics |

---

## 7. Employee Line-Item Table

### Tab: Employee Summary (Approved / Paid State)

**Column structure changes between Draft and Approved/Paid:**

| Column | Draft | Approved / Paid |
|--------|-------|-----------------|
| Checkbox | Yes | Yes |
| Employee Name | Yes | Yes |
| Paid Days | Yes | Yes |
| Gross Pay | Yes | NO |
| Deductions | Yes | NO |
| Taxes | Yes | NO |
| Benefits | Yes | NO |
| Reimbursements | Yes | NO |
| Net Pay | Yes | Yes |
| Payslip (View) | NO | YES |
| TDS Sheet (View) | NO | YES |
| Payment Mode | NO | YES |
| Payment Status | NO | YES |
| Row kebab | Yes (6 options) | Yes (2 options in Paid) |

### Toolbar (Approved / Paid)

| Control | Notes |
|---------|-------|
| "All Employees" filter dropdown | Filter by: All / Paid / Skipped |
| "Search Employee" combobox | Text search within current run |
| "Filter" button | Additional filter panel |
| "Export Data" dropdown | Export current data (no import post-approval) |

### Paid Employee Row Data

| Field | Example |
|-------|---------|
| Employee Name (EMP ID) | "Arjun Mehta (EMP001)" |
| Paid Days | 29 (calendar days minus LOP) |
| Net Pay | ₹65,484.00 |
| Payslip | "View" button |
| TDS Sheet | "View" button |
| Payment Mode | "Manual Bank Transfer" |
| Payment Status | "Paid on 29/05/2026" |
| Row kebab | Download Payslip / Send Payslip |

### Skipped Employee Row (All States)

| Field | Value |
|-------|-------|
| Employee Name | "Vikram Nair (EMP003)" |
| Status badge | "Skipped" |
| Skip reason | "Reason: Onboarding incomplete" |
| Paid Days | — (empty) |
| Net Pay | — (empty) |
| Payslip | — (no button) |
| TDS Sheet | — (no button) |
| Payment Mode | — (empty) |
| Payment Status | — (empty) |
| Row kebab | NONE in Paid state |

### Per-Row Kebab Matrix (All States)

| Action | Draft | Approved | Paid |
|--------|-------|----------|------|
| View Payslip | Yes | Yes | Via "View" button in column |
| View TDS Sheet | Yes | Yes | Via "View" button in column |
| Skip Employee | Yes | No | No |
| Undo Skip | Yes (if skipped) | No | No |
| Withold Salary | Yes | No | No |
| Revise Salary | Yes | No | No |
| Download Payslip | No | No | Yes |
| Send Payslip | No | No | Yes |

---

## 8. Individual Payslip View

### Trigger

"View" button in Payslip column of Employee Summary table → slide-in panel from right (not a new page/URL).

OR: Row kebab > "View Payslip" (Draft / Approved states).

### Payslip Split Panel Layout

The panel is a read-only drawer that overlays the summary page. It has no dedicated URL.

#### Panel Header

| Field | Source | Example |
|-------|--------|---------|
| Company Name | Settings > Org Profile | "lerno" |
| Payslip label | Static | "Payslip" |
| Pay Period | Pay run period | "May 2026" |
| Employee Name | Employee master | "Arjun Mehta" (link to profile) |
| "Net Pay" label | Static | — |
| Employee ID | Employee master | "EMP001" |
| Net Pay Amount | Computed | ₹65,484.00 |
| Payment info banner | Run data | "Paid on 29/05/2026 through Manual Bank Transfer" (with tick icon) |

#### Attendance Section

| Field | Example |
|-------|---------|
| Payable Days | 31 (calendar days in May) |
| LOP Days | 2 |
| Actual Payable Days | 29 |

#### Leave Summary (conditional)

Shown if leave data is configured. Empty if no leave module integrated.

#### Earnings Table

Two-column: Component Name | Amount

| Component | Example |
|-----------|---------|
| Basic | ₹37,417.00 |
| House Rent Allowance | ₹14,967.00 |
| Fixed Allowance | ₹13,100.00 |
| Bonus (if added) | ₹X |
| Commission (if added) | ₹X |
| Leave Encashment (if added) | ₹X |
| Reimbursements (if any) | ₹X |
| **Gross Earnings** | **₹65,484.00** |

#### Deductions Table

Grouped by category:

| Category | Component | Example |
|----------|-----------|---------|
| Taxes | Income Tax | ₹0.00 |
| Taxes | KL Professional Tax (Head Office) | ₹0.00 |
| Benefits | PF Employee Contribution | ₹X (if configured) |
| Benefits | ESI Employee Contribution | ₹X (if configured) |
| Other | Ad-hoc Deductions | ₹X (if any) |
| **Total Deductions** | | **₹0.00** |

PT label format: "{State Code} Professional Tax ({Work Location Name})" — e.g., "KL Professional Tax (Head Office)"

#### Net Pay Footer

| Field | Example |
|-------|---------|
| Net Pay | ₹65,484.00 |

### Panel Action Buttons

| Button | Notes |
|--------|-------|
| Download Payslip | Triggers password protection modal → generates PDF |
| Send Payslip | Emails payslip PDF to employee's registered email |

---

## 9. Payslip Download & Email

### Individual Download

**Trigger:** "Download Payslip" button in payslip split panel or "Download Payslip" in per-row kebab (Paid state).

**Password Protection Modal:**

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| "Protect this file with a password" | Checkbox | CHECKED | RC4 encryption applied |

Buttons: "Download" | "Cancel"

**Behavior with checkbox checked:**
- PDF encrypted with RC4
- User password is empty (opens without password prompt in any viewer)
- Owner password set by Zoho (prevents modification)
- The password is NOT the employee's PAN

**PDF File Properties:**

| Property | Value |
|----------|-------|
| Filename | `Payslip_{EmpID}_{MonthName}_{Year}.pdf` |
| Example | `Payslip_EMP001_May_2026.pdf` |
| Producer | OpenPDF 1.3.26 |
| Page size | 612 × 792 pts (US Letter / 8.5 × 11 inches) |
| Pages | 1 |
| Font | Ubuntu |
| Encrypted | Yes (RC4, when protection enabled) |

### Payslip PDF Content Layout

```
HEADER ROW:
  Company Name | "Payslip For the Month" | Month Year
  Company Address (from Settings)

EMPLOYEE SUMMARY (left column):
  Employee Name: {name}
  Designation: {designation}
  Employee ID: {id}
  Date of Joining: dd/MM/yyyy
  Paid Days: {N}
  Pay Period: Month Year
  LOP Days: {N}
  Pay Date: dd/MM/yyyy
  Bank Account No: {full account number — NOT masked in PDF}

NET PAY BANNER (right column):
  ₹{amount}
  "Total Net Pay"

EARNINGS TABLE (3 columns):
  | Component       | Amount      | YTD          |
  | Basic           | ₹37,417.00  | ₹77,415.00   |
  | HRA             | ₹14,967.00  | ₹30,966.00   |
  | Fixed Allowance | ₹13,100.00  | ₹27,103.00   |
  | Gross Earnings  | ₹65,484.00  |              |

DEDUCTIONS TABLE (3 columns):
  | Component          | Amount  | YTD     |
  | [deductions here]  |         |         |
  | Total Deductions   | ₹0.00   | ₹0.00   |

NET PAYABLE ROW:
  "Gross Earnings - Total Deductions"
  ₹65,484.00

AMOUNT IN WORDS:
  "Indian Rupee Sixty-Five Thousand Four Hundred Eighty-Four Only"

FOOTER:
  "-- This is a system-generated document. --"
```

**Key observations on PDF content:**
- YTD column shows year-to-date cumulative values for each component
- Bank account number shown in FULL (not masked) — security concern
- No company logo (text only for company name)
- No signature block or digital signature
- Amount in words uses Indian denomination (lakh/crore system implicit)

### Bulk Download — "Download all Payslips"

**Trigger:** Page kebab → "Download all Payslips"

**Bulk Download Modal:**

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Portal Status | Dropdown | "Both Enabled and Disabled" | Filter which employees to include |
| Work Location | Multi-select listbox | "All Locations" | Filter by work location |
| Designation | Multi-select listbox | "All Designations" | Filter by designation |

Buttons: "Download" | "Cancel"

**Behavior:** Async background job. Toast shown:
> "Downloading process has been initiated! Kindly wait, Within 15 minutes the link to download your documents will be ready."

The download link is delivered via the Notifications (bell icon) after the job completes.

No password protection option in bulk download modal.

### "Download all TDS Worksheets"

Page kebab → "Download all TDS Worksheets" — also async, similar behavior to bulk payslips.

### Send Payslip — Individual

"Send Payslip" button in payslip panel or per-row kebab (Paid state).

Sends PDF to employee's registered email. No observable confirmation dialog on content — no email address preview before sending.

### Send Payslip — Bulk

"Send Payslip" button in Paid state page header. Sends payslip emails to ALL paid employees in the run.

### Send Payslip on Record Payment

The Record Payment dialog has a "Send payslip notification" checkbox (default: CHECKED). When checked, Zoho sends an email notification to employees with a link to view their payslip once payment is recorded. This is NOT the same as the payslip PDF email — it's a notification email with a portal link.

---

## 10. Record Payment (Mark as Paid)

### URL: `#/payruns/{id}/summary` (Approved state) — "Record Payment" button

### Trigger

Primary "Record Payment" button visible when status is Approved ("Payment Due").

### Record Payment Dialog

| Field | Type | Required | Default | Options |
|-------|------|----------|---------|---------|
| Payment Date | Date (dd/MM/yyyy) | Yes | Configured pay day from schedule | Admin can override |
| Payment Mode (per employee summary) | Read-only table | — | — | Shows: Bank Transfer: 2 / Direct Deposit: 0 / Cheque: 0 / Cash: 0 |
| Reference Number | Text | No | — | Optional bank transaction reference |
| Notes | Textarea | No | — | Internal notes |
| Send payslip notification | Checkbox | — | CHECKED | If checked: notifies employees via email with portal link |

Buttons: "Confirm" | "Cancel"

### Post-Record State Transition

- Status: "Approved" / "Payment Due" → "Paid"
- Payment date, mode, reference stored on the run record
- "Payment Status" per employee row changes: "Yet To Pay" → "Paid on {date}"
- Back link changes to `#/payruns/payroll-history`
- "Record Payment" button → replaced by "Send Payslip" button
- "Download Bank Advice" remains available
- Notification count +1
- Payslips visible in employee portal (regardless of notification setting)

### Payment Mode Options

| Mode | Notes |
|------|-------|
| Manual Bank Transfer | Admin initiates NEFT/RTGS separately via bank portal using Bank Advice file |
| Direct Deposit | Zoho Payments integration for automated salary disbursement |
| Cheque | Manual cheque payment |
| Cash | Cash payment |

### Important Note

Zoho Payroll does NOT directly initiate bank transfers. "Record Payment" is an administrative recording action only. The actual bank transfer happens externally (via bank's bulk payment portal using the Bank Advice file, or via Zoho Payments integration).

---

## 11. Bank Advice / Salary Payment

### Bank Advice Button

**Location:** Info strip of summary card (appears only after Approval, remains in Paid state).
**Selector:** `button[data-test-selector="download-bank-advice"]`

### Download Bank Advice Modal

| Field | Type | Default | Editable |
|-------|------|---------|----------|
| Generate Bank Advice for | Dropdown (disabled) | "Bank Transfer Employees (Paid & Unpaid)" | No — locked |
| Filters | Expandable | — | Yes |
| Bank Statement Format | Dropdown (searchable) | "Standard Format" | Yes |
| Download as | Dropdown | "XLS (Microsoft Excel 1997-2004)" | Yes |
| Protect this file with a password | Checkbox | Unchecked | Yes |

Buttons: "Download" | "Cancel"

**Upsell banner:** "Skip the bank advice next time. Use Payouts by Zoho Payments to disburse salaries directly from Zoho Payroll." [Setup Payout] button.

### Bank Statement Format Options (Complete List)

| Format | Notes |
|--------|-------|
| Standard Format | Generic — default |
| Axis Bank | Bank-specific columns |
| Axis Bank Standard Format | Variation |
| Citi Bank | — |
| Citi Bank Standard Format | — |
| DMIT Bank | — |
| HDFC Bank (Updated) | — |
| ICICI Bank | — |
| ICICI Bank (Biz360) | — |
| IDFC Bank | — |
| Kotak Mahindra Bank | — |
| Standard Chartered Bank | — |

**Note:** SBI (State Bank of India) has NO bank-specific format. SBI employees use Standard Format.

### Standard Format XLS Structure

**Filename:** `Payroll_Bank_Statement.xls`
**Sheet:** "BankStatement"
**Columns:**

| Column | Header | Example |
|--------|--------|---------|
| A | Employee No | EMP001 |
| B | Employee Name | Arjun Mehta |
| C | Amount | 65484.0 (float, no currency symbol) |
| D | Bank Name | HDFC Bank |
| E | Bank Account No | 50100123456789 (FULL, unmasked) |
| F | IFSC Code | HDFC0001234 |
| G | Beneficiary Name | Arjun Mehta |

- No totals row
- Only employees with payment mode = "Bank Transfer" included
- Synchronous download (immediate, not async unlike bulk payslips)
- Amount stored as float (65484.0) — no formatting

### Direct Deposit Integration

If "Payouts by Zoho Payments" is configured:
- Salary is sent electronically via Zoho's banking partner
- No manual file upload to bank portal required
- Bank Advice still downloadable as confirmation

---

## 12. Overall Insights Tab

### URL: `#/payruns/{id}/summary?selectedTab=insights`

### Page Heading

"Insights for {Month} {Year} Payrun"

### Section A: Employee Breakdown

| Metric | Type | Notes |
|--------|------|-------|
| Active Employees | Count | Total active in this run |
| Paid Employees | Count | Successfully paid |
| New Joinee's Skipped | Count | New employees excluded |
| Skipped Employees | Count | Explicitly skipped |
| Salary Withheld Employees | Count | Included but payment held |
| New Joinee's Arrear Released | Count | First-month arrear paid in this run |
| Salary Released Employees | Count | Previously withheld salary released |
| LOP Reversed Employees | Count | LOP from prior run reversed |

### Section B: Statutory Summary

Shows aggregate statutory liabilities when PF/ESI/PT are configured:

| Row (Expected) | Description |
|----------------|-------------|
| EPF Employee Contribution | 12% of PF wage |
| EPF Employer Contribution | 12% of PF wage (EPS + EPF split) |
| EPS Employer Contribution | 8.33% of PF wage (capped ₹1,250/month) |
| EDLI Employer Contribution | 0.5% of PF wage (capped ₹75/month) |
| ESI Employee Contribution | 0.75% of ESI wage |
| ESI Employer Contribution | 3.25% of ESI wage |
| Professional Tax | Per state slab |
| Labour Welfare Fund | Per state |
| TDS (Income Tax) | Aggregate across all employees |

**Empty state (when no statutory configured):** "No data to display" — with no explanation.

### Section C: Payment Mode Summary

| Mode | Count |
|------|-------|
| Direct Deposit | 0 / N |
| Bank Transfer | 0 / N |
| Cheque | 0 / N |
| Cash | 0 / N |

### Section D: Component Wise Breakdown

Two-level collapsible accordion:
- Category level: "Base Earning ₹{total}" (expandable)
- Component level: per-component rows

**Table columns:** Component | Employees Involved | Total Amount

**Component links:** Each component name is a clickable link:
```
#/payruns/insights/{runId}/earnings/{componentId}?override_type=
```
Navigates to per-employee breakdown for that component.

---

## 13. Taxes & Deductions Tab

### URL: `#/payruns/{id}/summary?selectedTab=taxes`

### Section 1: Tax Details Table

| Column | Notes |
|--------|-------|
| Tax Name | e.g., "Income Tax", "KL Professional Tax (Head Office)" |
| Paid By Employer | Always ₹0 for Income Tax and PT |
| Paid By Employee | TDS + PT amounts |

PT label format: "{State Code} Professional Tax ({Work Location Name})"

### Section 2: Benefits Table

Columns: Benefit Name | Employer's Contribution | Employees' Contribution

**Empty state:** "There are no deductions present in this payrun."

**When configured:** Shows PF, ESI, LWF per benefit type.

### Section 3: Donations Table

Columns: Deduction Name | Employees' Contribution

**Empty state:** "There are no donations present in this payrun."

---

## 14. Delete Recorded Payment (Reversal)

### Available From

Page kebab in **Paid state only** → "Delete Recorded Payment"

### Dialog

| Field | Content |
|-------|---------|
| Message | "You're about to delete the recorded payment for this pay run. Are you sure you want to proceed?" |
| Yes | Confirms; state → Approved |
| No | Dismisses; stays Paid |

### Post-Action Behavior

- Status: Paid → Approved ("Payment Due" or "Approved")
- Payment date, mode, reference cleared
- "Record Payment" action becomes available again
- **Variable inputs remain locked** — still Approved, not Draft
- To go back to Draft: must additionally "Reject Approval"

### Full Reversal to Draft (5 Steps)

To re-enter Draft state for changing variable inputs from Paid:

1. Page kebab → "Delete Recorded Payment" → Yes (Paid → Approved)
2. Page kebab → "Reject Approval" → Reject (Approved → Draft)
3. Make changes in split panel
4. Page kebab → "Approve Payroll" → Confirm (Draft → Approved)
5. "Record Payment" → Confirm (Approved → Paid)

**No single "Reprocess" button exists in Zoho for this 5-step flow.**

### What Reversal Does NOT Do

- Does NOT delete the pay run itself (data preserved)
- Does NOT reverse actual bank transfers (external to Zoho)
- Does NOT revert EPF ECR once submitted to EPFO
- Does NOT revert ESI challan once paid
- Does NOT revert TDS challan once deposited

---

## 15. Off-Cycle Pay Runs

### Types Available (via "New" Dropdown)

#### 1. One Time Payout

**Dialog: "Create One Time Payrun"**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Select One Time Component | Combobox | Yes | Only one-time components pre-configured in Settings |
| When would you like to pay? | Date (dd/MM/yyyy) | Yes | Payment date |

Buttons: "Save and Continue" | "Cancel"

**Post-save:** Creates run record → navigates to employee selection + amount entry screen.

**Constraints:**
- Only one component per One Time Payout run
- Component must be pre-configured in Settings > Salary Components as one-time type
- Visible in Payroll History under type "One Time Payout"

**Use cases:** Company-wide bonus, quarterly commission, festival bonus as standalone run.

#### 2. Off Cycle Pay Run

**Dialog: "Initiate Off Cycle Pay Run"**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Select Date (When would you like to pay?) | Date (dd/MM/yyyy) | Yes | Payment date only |

Buttons: Confirm | Cancel

**Post-save:** Creates run → admin selects employees and components → full approval + payment flow.

**Use cases:** F&F settlement for exiting employee, advance salary, payroll correction for specific employee, rejoiner mid-month.

**Differences from Regular Pay Run:**

| Dimension | Regular | Off Cycle |
|-----------|---------|-----------|
| Creation | Auto by system | Admin-initiated |
| Period | Full calendar month | Custom payment date |
| Employees | All eligible | Admin-selected subset |
| Components | All salary components | All salary components |
| Payslip | Full monthly payslip | Full payslip |
| History type | "Regular Payroll" | "Off Cycle Payroll" |

#### 3. Resettlement Payroll (Arrears)

Visible in Payroll History filter but NOT in the "New" dropdown. Triggered when a salary revision with a past effective date is approved — Zoho computes the delta for prior months.

**History type label:** "Resettlement Payroll"

**Use case:** Salary revision approved in June 2026 effective from April 2026 → Zoho creates a Resettlement run for the 2-month arrear difference.

### Payroll History Type Labels (Complete — 8 Types)

| Type | Description |
|------|-------------|
| Regular Payroll | Monthly pay run |
| Past Payroll | Catch-up run for prior periods |
| Final Settlement Payroll | F&F run for single employee exit |
| One Time Payout | Ad-hoc single component payment |
| Off Cycle Payroll | Mid-cycle full run |
| Bulk Final Settlement Payroll | F&F run for multiple exits simultaneously |
| Resettlement Payroll | Arrear / pay revision adjustment |
| [Custom / Legacy types] | — |

---

## 16. Payroll History

### URL: `#/payruns/payroll-history`

### Toolbar

**Filter:** "Payroll Type:" combobox — default "All" — shows all 8 type options.

### History Table Columns

| Column | Notes |
|--------|-------|
| Payment Date | Date payment was recorded |
| Payroll Type | From 8-type enum |
| Details | Pay period range (clickable → summary page) |
| Payroll Status | "Paid" badge |

### Accessing a Historical Run

Clicking a row → `#/payruns/{id}/summary` (Paid state, read-only)

All three tabs available in read-only mode.

### What Is Immutable in Past Runs

| Item | Mutable? |
|------|----------|
| Employee salaries | No — use revision/resettlement run |
| LOP days | No |
| Variable inputs | No |
| Payment date | No (can "Delete Recorded Payment" to correct) |
| Payslip content | No — frozen at finalization |
| TDS amounts | No — frozen; feeds Form 24Q |

---

## 17. State Machine — Complete

```
[SYSTEM AUTO-CREATES PERIOD CARD]
         |
         | (Admin clicks period card)
         v
      READY
         |
         | (Admin clicks "Create Pay Run")
         v
      DRAFT  <────────────────────────────────────────────────────────┐
         |                                                             |
         |  ┌─ Enter variable inputs:                                  |
         |  │   - LOP days per employee                                |
         |  │   - Add Earning (bonus/commission/leave encash)          |
         |  │   - TDS override (mandatory reason)                      |
         |  │   - Import LOP/earnings/reimbursements                   |
         |  │                                                           |
         |  ├─ Manage pending tasks:                                   |
         |  │   - Skip employees (mandatory reason)                    |
         |  │   - Undo skip                                            |
         |  │   - Withold salary                                       |
         |  │   - Revise salary                                        |
         |  │                                                           |
         |  └─ (all hard-block tasks resolved)                         |
         |                                                             |
         | (Admin: "Approve Payroll" → confirmation dialog)            |
         v                                                             |
     APPROVED ─────────────────────────────────────────────────────────┤
         |                            ↑                               |
         |                            | ("Reject Approval" optional   |
         |                            |   reason → Draft)             |
         |  ┌─ Download Bank Advice                                    |
         |  │                                                           |
         |  └─ (Admin: "Record Payment" dialog:                        |
         |        Payment Date / Mode / Reference / Notify employees)   |
         v                                                             |
       PAID ─────────────────────────────────────────────────────────>─┘
         |                            ↑
         |                            | ("Delete Recorded Payment" → Approved)
         |
         └─ TERMINAL (for normal operations)
             [Payroll History — immutable unless Revision Run created]
```

**Additional transitions:**

| From | Action | To |
|------|--------|----|
| DRAFT | Delete Pay Run (kebab) | DELETED |
| PAID | Create Revision Run | New RESETTLEMENT run |

### Status Badge Display Labels

| Internal State | Badge Shown | URL State |
|----------------|-------------|-----------|
| Pending | "Ready" (on list card) | list |
| Draft | "Draft" | `/summary` |
| Approved (pay day future) | "Approved" | `/summary` |
| Approved (pay day passed) | "Payment Due" | `/summary` |
| Paid | "Paid" | `/summary` |

---

## 18. Business Rules Reference

### Creation Rules

| Rule | Detail |
|------|--------|
| Sequential monthly runs only | System computes next period; admin cannot select period manually |
| One period card at a time | Cannot pre-initiate future month's regular run |
| Auto-advance | Card advances to next period once current period is Paid |
| No period picker | Hard architectural constraint in Zoho's design |
| Off-cycle is always available | "New" dropdown always shows regardless of outstanding regular run |

### Employee Inclusion Rules

| Rule | Detail |
|------|--------|
| Onboarding completeness gate | All required fields must be complete (composite check) |
| PAN-missing = soft warning | Included with 20% flat TDS per §206AA |
| Skip is permanent | For this pay cycle; cannot be undone within the same run |
| Skip requires mandatory reason | API-validated; reason stored and displayed permanently |
| `pay_joinee_arrear_later` | If true: skipped new joiners can receive arrear in next run |
| Withold salary | Employee included in run, payslip generated, but payment held |

### LOP Rules

| Rule | Detail |
|------|--------|
| Calendar days proration | Formula: `Component × (Base Days − LOP Days) / Base Days` |
| Per-employee per-run entry | Manual entry; no auto-detection from attendance |
| Component-level proration | Fixed components prorated; variable components not prorated |
| Fixed Allowance may be exempt | Configurable — some orgs mark it as non-prorated |
| Adjust Past LOP | Can correct LOP for prior pay periods retroactively |
| NCP Days in ECR | LOP Days = NCP Days reported to EPFO in ECR file |

### Proration Calculation

```
Prorated Component = Full Component Amount × (Payable Days / Base Days)
Payable Days = Base Days − LOP Days
Base Days = Calendar days in the pay period month
```

**Confirmed with EMP001 (May 2026, 2 LOP days, 31 base days):**
- Payable Days = 29
- Basic: ₹40,000 × 29/31 = ₹37,419 (shown ₹37,417 — per-component rounding)
- Net: ₹70,000 × 29/31 = ₹65,484 ✓

**Mid-month joiners:** NOT auto-prorated in Zoho. Admin must manually enter LOP days for days before the joining date.

### TDS Rules

| Rule | Detail |
|------|--------|
| New regime only (V1) | No old regime TDS in this org's configuration |
| Standard Deduction FY2026 | ₹75,000 (new regime) |
| 87A Rebate | ₹25,000 if taxable income ≤ ₹7,00,000 |
| Spreading method | Annual TDS computed; divided over remaining months in FY |
| Override allowed | Per employee per run; mandatory reason; audit trail |
| PAN missing | TDS at 20% flat (§206AA) |
| YTD factored | Prior months' TDS deducted already is subtracted from annual liability |

### New Regime Tax Slabs (FY2026)

| Slab | Rate |
|------|------|
| ₹0 – ₹4,00,000 | 0% |
| ₹4,00,001 – ₹8,00,000 | 5% |
| ₹8,00,001 – ₹12,00,000 | 10% |
| ₹12,00,001 – ₹16,00,000 | 15% |
| ₹16,00,001 – ₹20,00,000 | 20% |
| ₹20,00,001 – ₹24,00,000 | 25% |
| ₹24,00,001 and above | 30% |

### Approval Rules

| Rule | Detail |
|------|--------|
| Hard block on pending tasks | Cannot approve if employees in "Missing" status |
| PAN warning is soft | Does not block approval |
| Reimbursements lock on approval | Cannot raise or modify claims after approval |
| IT Declaration locks on approval | Cannot update exemptions or investments |
| POI locks on approval | Cannot upload proof of investment |

### Post-Approval Immutability

| Item | Locked after Approval |
|------|-----------------------|
| LOP days | Yes |
| Variable earnings | Yes |
| TDS override | Yes |
| Salary structure | Yes (for this run) |

### Payment Rules

| Rule | Detail |
|------|--------|
| Payment date defaults to pay schedule | Admin can override |
| Payslip notification default ON | Checkbox in Record Payment dialog |
| Bank transfer is external | Zoho records payment; actual transfer via bank portal |
| Bank Advice is immediate download | Synchronous; not logged in Downloads history |
| "Paid" is reversible | "Delete Recorded Payment" reverts to Approved |
| Payslips visible on portal after payment recorded | Regardless of notification setting |

### Finalization and Immutability

| Rule | Detail |
|------|--------|
| Paid state is not truly immutable | Delete Recorded Payment → Approved → Reject Approval → Draft (5-step reversion) |
| Payslip PDF frozen at finalization | Generated from run data at that point; not recalculated |
| TDS liability locked at Paid | Feeds Form 24Q quarterly return |
| ECR not auto-submitted | Admin must download and submit to EPFO |

---

## 19. Data Relationships

```
PayrollRun
  ├── PaySchedule (N:1) — schedule defines pay day, period, base days method
  ├── PayRunEmployee (1:N) — junction: one row per employee in this run
  │     ├── Employee (N:1)
  │     ├── SalaryStructure (N:1) — structure at time of run
  │     ├── paid_days
  │     ├── lop_days
  │     ├── actual_payable_days
  │     ├── gross_pay
  │     ├── net_pay
  │     ├── taxes_amount
  │     ├── benefits_amount
  │     ├── reimbursements_amount
  │     ├── payment_status (Active | Paid | Skipped | Withheld)
  │     ├── payment_mode (BankTransfer | DirectDeposit | Cheque | Cash)
  │     ├── payment_date
  │     ├── skip_reason (nullable)
  │     ├── tds_override_amount (nullable)
  │     ├── tds_override_reason (nullable)
  │     └── is_new_joinee_arrear (bool)
  ├── PayRunComponentBreakdown (1:N) — per employee per component
  │     ├── SalaryComponent (N:1)
  │     ├── full_amount
  │     ├── prorated_amount
  │     └── override_type
  ├── Payslip (1:N) — one per included employee
  │     ├── Employee (N:1)
  │     ├── pdf_storage_key (MinIO)
  │     └── ytd_amounts (JSON or per-component table)
  ├── TDSWorksheet (1:N) — one per included employee
  │     ├── annual_projected_income
  │     ├── standard_deduction
  │     ├── taxable_income
  │     ├── tax_before_rebate
  │     ├── rebate_87a
  │     ├── surcharge
  │     ├── cess
  │     ├── annual_tax_liability
  │     ├── ytd_tds_deducted
  │     ├── remaining_months
  │     └── tds_this_month
  └── BankAdvice (1:1 per run) — bank transfer file (MinIO)
```

---

## 20. Statutory Filings Cross-Reference

| Statutory | Pay Run Impact | Filing Generated |
|-----------|---------------|-----------------|
| TDS (Income Tax) | Deducted from employee per TDS worksheet | Form 24Q (quarterly) / Form 16 (annual) |
| PF (EPF) | Employee 12% + Employer 12% of PF wage | EPF ECR file (monthly) |
| ESI | Employee 0.75% + Employer 3.25% of ESI wage | ESI Return / Challan (monthly) |
| Professional Tax | Per state slab on gross | PT Challan (varies by state: monthly/half-yearly) |
| LWF | Per state annual/half-annual amount | LWF Challan |

**Statutory data appears in:** Taxes & Deductions tab (per run) + Overall Insights > Statutory Summary + Compliance Calendar module

---

## 21. Build Guide

This section translates Zoho Payroll observations into a concrete build specification for the Indian Payroll SaaS.

---

### A. Domain Model Gaps (vs Current Codebase)

The current `PayrollRun` entity is minimal. The following additions are required:

#### PayrollRun Entity (extend existing)

```csharp
public sealed class PayrollRun : AuditableEntity
{
    // EXISTING (keep)
    public Guid TenantId { get; private set; }
    public PayPeriod PayPeriod { get; private set; }
    public PayrollRunStatus Status { get; private set; }
    public string? VariableInputsFileKey { get; private set; }

    // REQUIRED ADDITIONS
    public PayrollRunType Type { get; private set; }      // enum
    public DateOnly PayDay { get; private set; }           // from pay schedule
    public decimal PayrollCost { get; private set; }       // gross + employer contributions
    public decimal TotalNetPay { get; private set; }
    public decimal TotalEmployerPf { get; private set; }
    public decimal TotalEmployerEsi { get; private set; }
    public decimal TotalEdli { get; private set; }
    public decimal TotalTds { get; private set; }
    public decimal TotalPt { get; private set; }
    public string? ApprovalRejectionReason { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateOnly? PaymentDate { get; private set; }
    public PaymentMode? PaymentMode { get; private set; }
    public string? PaymentReference { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? BankAdviceFileKey { get; private set; }  // MinIO key
    public string? DeletePaymentReason { get; private set; }
}
```

#### PayrollRunStatus Enum (replace current)

Current (insufficient):
```csharp
// Pending, Processing, Draft, Finalised, Failed
```

Required:
```csharp
public enum PayrollRunStatus
{
    // Zoho "Ready" — system-generated card visible
    Ready,

    // Zoho "Draft" — pay run created, variable inputs editable
    Draft,

    // Zoho "Approved" / "Payment Due" — inputs locked, awaiting payment
    Approved,

    // Zoho "Paid" — payment recorded, payslips published
    Paid,

    // Terminal failure
    Failed,

    // Soft-deleted
    Deleted
}
```

#### PayrollRunType Enum (add)

```csharp
public enum PayrollRunType
{
    Regular,
    OffCycle,
    OneTimePayout,
    PastPayroll,
    FinalSettlement,
    BulkFinalSettlement,
    Resettlement        // arrears / pay revision
}
```

#### PayrunEmployee Entity (new)

```csharp
public sealed class PayrunEmployee : AuditableEntity
{
    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public PayrunEmployeeStatus Status { get; private set; }
    public int BaseDays { get; private set; }
    public int LopDays { get; private set; }
    public int ActualPayableDays { get; private set; }  // BaseDays - LopDays
    public decimal GrossPay { get; private set; }
    public decimal TaxesAmount { get; private set; }
    public decimal BenefitsAmount { get; private set; }
    public decimal ReimbursementsAmount { get; private set; }
    public decimal OtherDeductions { get; private set; }
    public decimal NetPay { get; private set; }
    public decimal EmployeePf { get; private set; }
    public decimal EmployerPf { get; private set; }
    public decimal EmployeeEsi { get; private set; }
    public decimal EmployerEsi { get; private set; }
    public decimal PtAmount { get; private set; }
    public decimal TdsAmount { get; private set; }
    public decimal? TdsOverrideAmount { get; private set; }
    public string? TdsOverrideReason { get; private set; }
    public PaymentMode PaymentMode { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public DateOnly? PaymentDate { get; private set; }
    public string? SkipReason { get; private set; }
    public bool IsNewJoineeArrear { get; private set; }
    public bool IsLopReversed { get; private set; }
    public bool IsSalaryReleased { get; private set; }
}

public enum PayrunEmployeeStatus
{
    Active,     // included and paid
    Skipped,    // explicitly skipped with reason
    Withheld    // included in run, payment held
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Withheld
}
```

#### PayrunComponentBreakdown Entity (new)

```csharp
public sealed class PayrunComponentBreakdown : Entity
{
    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid SalaryComponentId { get; private set; }
    public decimal FullAmount { get; private set; }
    public decimal ProratedAmount { get; private set; }
    public bool IsManualOverride { get; private set; }
    public string? OverrideReason { get; private set; }
}
```

#### Payslip Entity (new)

```csharp
public sealed class Payslip : AuditableEntity
{
    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string PdfStorageKey { get; private set; }      // MinIO key
    public DateTimeOffset GeneratedAt { get; private set; }
    public bool IsPublished { get; private set; }           // visible in portal
    public bool IsPasswordProtected { get; private set; }
    public decimal NetPay { get; private set; }
    public string NetPayInWords { get; private set; }        // "Indian Rupee..."
    public PayslipYtdData YtdData { get; private set; }    // per component YTD
}
```

#### TdsWorksheet Entity (new)

```csharp
public sealed class TdsWorksheet : Entity
{
    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public int FiscalYear { get; private set; }            // e.g., 2026 = FY2025-26
    public string TaxRegime { get; private set; }          // "New" in V1
    public decimal AnnualProjectedIncome { get; private set; }
    public decimal StandardDeduction { get; private set; }  // from StatutoryConfig
    public decimal TaxableIncome { get; private set; }
    public decimal TaxBeforeRebate { get; private set; }
    public decimal Rebate87A { get; private set; }
    public decimal Surcharge { get; private set; }
    public decimal Cess { get; private set; }              // 4% of (tax + surcharge)
    public decimal AnnualTaxLiability { get; private set; }
    public decimal YtdTdsDeducted { get; private set; }
    public int RemainingMonthsInFy { get; private set; }
    public decimal TdsThisMonth { get; private set; }
    public bool HasPanOverride { get; private set; }       // 20% if no PAN
    public string? PdfStorageKey { get; private set; }
}
```

#### PayrollRunAuditLog Entity (new)

```csharp
public sealed class PayrollRunAuditLog : Entity
{
    public Guid PayrollRunId { get; private set; }
    public PayrollRunStatus FromStatus { get; private set; }
    public PayrollRunStatus ToStatus { get; private set; }
    public Guid ActorUserId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string? Reason { get; private set; }
}
```

---

### B. API Endpoints Needed

#### Pay Run CRUD

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/payroll-runs` | List outstanding runs (Run Payroll tab) |
| GET | `/api/payroll-runs/history` | Payroll History tab (completed runs) with type filter |
| POST | `/api/payroll-runs` | Create Off Cycle or One Time Payout run |
| GET | `/api/payroll-runs/{id}` | Single run detail (header + summary card data) |
| DELETE | `/api/payroll-runs/{id}` | Delete a Draft run |

#### Run Payroll Tab — Active Period Card

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/payroll-runs/current-period` | Returns next payable period + employee count + pay day |
| POST | `/api/payroll-runs/initiate` | Creates the regular pay run for current period |

#### Employee Gate

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/payroll-runs/{id}/employees?filter=missing` | Employees not yet included |
| GET | `/api/payroll-runs/{id}/employees?filter=no-pan` | Employees without PAN |
| POST | `/api/payroll-runs/{id}/employees/{employeeId}/skip` | Skip employee (body: reason, pay_joinee_arrear_later) |
| DELETE | `/api/payroll-runs/{id}/employees/{employeeId}/skip` | Undo skip |

#### Variable Inputs (Draft State)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/payroll-runs/{id}/employees/{employeeId}/inputs` | Get current variable inputs for employee |
| PUT | `/api/payroll-runs/{id}/employees/{employeeId}/lop` | Set LOP days → triggers proration recalc |
| POST | `/api/payroll-runs/{id}/employees/{employeeId}/earnings` | Add one-time earning |
| PUT | `/api/payroll-runs/{id}/employees/{employeeId}/tds-override` | Override TDS (body: amount, reason) |
| POST | `/api/payroll-runs/{id}/employees/{employeeId}/withhold` | Withold salary |
| DELETE | `/api/payroll-runs/{id}/employees/{employeeId}/withhold` | Release withheld salary |
| POST | `/api/payroll-runs/{id}/import/lop` | Bulk import LOP (CSV/XLSX) |
| POST | `/api/payroll-runs/{id}/import/earnings` | Bulk import one-time earnings |
| POST | `/api/payroll-runs/{id}/import/reimbursements` | Bulk import reimbursements |
| POST | `/api/payroll-runs/{id}/import/deductions` | Bulk import ad-hoc deductions |
| GET | `/api/payroll-runs/{id}/export` | Export payroll data |
| GET | `/api/payroll-runs/{id}/export/comparison` | Export comparison vs prior month |

#### State Transitions

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/payroll-runs/{id}/approve` | Draft → Approved |
| POST | `/api/payroll-runs/{id}/reject-approval` | Approved → Draft (body: optional reason) |
| POST | `/api/payroll-runs/{id}/record-payment` | Approved → Paid (body: payment_date, mode, reference, notify) |
| DELETE | `/api/payroll-runs/{id}/payment` | Paid → Approved (Delete Recorded Payment) |
| POST | `/api/payroll-runs/{id}/reprocess` | One-command: Paid → Approved → Draft (differentiator) |

#### Payroll Summary & Insights

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/payroll-runs/{id}/summary` | Header data + taxes mini-summary |
| GET | `/api/payroll-runs/{id}/employees` | Employee line-item table (all states) |
| GET | `/api/payroll-runs/{id}/taxes` | Taxes & Deductions tab data |
| GET | `/api/payroll-runs/{id}/insights` | Overall Insights tab data |
| GET | `/api/payroll-runs/{id}/insights/components` | Component wise breakdown |
| GET | `/api/payroll-runs/{id}/insights/components/{componentId}` | Per-employee component drill-down |
| GET | `/api/payroll-runs/{id}/pending-tasks` | Pending tasks list for Draft gate |

#### Payslip

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/payroll-runs/{id}/employees/{employeeId}/payslip` | Payslip data (JSON for panel rendering) |
| GET | `/api/payroll-runs/{id}/employees/{employeeId}/payslip/pdf` | Download payslip PDF |
| POST | `/api/payroll-runs/{id}/employees/{employeeId}/payslip/send` | Email payslip to employee |
| POST | `/api/payroll-runs/{id}/payslips/bulk-send` | Bulk email payslips |
| POST | `/api/payroll-runs/{id}/payslips/bulk-download` | Async bulk ZIP of all PDFs |

#### TDS Worksheet

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/payroll-runs/{id}/employees/{employeeId}/tds-worksheet` | TDS computation as JSON |
| GET | `/api/payroll-runs/{id}/employees/{employeeId}/tds-worksheet/pdf` | TDS computation as PDF |
| GET | `/api/payroll-runs/{id}/tds-worksheets/bulk-download` | Async bulk ZIP |

#### Bank Advice

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/payroll-runs/{id}/bank-advice?format=Standard&output=xls` | Download bank advice file |

---

### C. Frontend Pages & Components

#### Pages

| Page | Route | Notes |
|------|-------|-------|
| Run Payroll | `/pay-runs` | Period card + empty state + New dropdown |
| Payroll History | `/pay-runs/history` | Table with type filter |
| Pay Run Detail | `/pay-runs/:id` | Tabs: Employee Summary / Taxes & Deductions / Overall Insights |
| Add Employees Gate | `/pay-runs/:id/employees` | Missing employee management |

#### Page Components

| Component | Purpose |
|-----------|---------|
| `PeriodCard` | Run Payroll tab card — period, pay day, employee count, CTA |
| `PayRunHeader` | Status badge, action buttons, kebab, info strip |
| `PendingTasksBanner` | Collapsible warning with task list and resolution links |
| `EmployeeSummaryTable` | Main employee table (all states) — column set changes by status |
| `EmployeeVariableInputsPanel` | Slide-in drawer — LOP, earnings, TDS override, save/cancel |
| `LopEntry` | Spinbutton + actual payable days auto-calc |
| `AddEarningForm` | Listbox + amount input inline form |
| `TdsOverrideForm` | Amount spinbutton + mandatory reason textarea |
| `SkipEmployeeDialog` | Confirmation modal with mandatory reason |
| `WitdholdSalaryDialog` | Hold/release salary dialog |
| `ApprovePayrollDialog` | Approval confirmation with lock warnings |
| `RejectApprovalDialog` | Rejection with optional reason |
| `RecordPaymentDialog` | Payment date / mode / reference / notify checkbox |
| `DeletePaymentDialog` | Simple yes/no confirmation |
| `PayslipPanel` | Slide-in read-only payslip view with download + send |
| `PayslipDownloadDialog` | Password protection checkbox modal |
| `TdsSheetModal` | PDF iframe modal |
| `BankAdviceModal` | Format selector + download options |
| `BulkDownloadModal` | Filters (location, designation, portal status) |
| `SendPayslipDialog` | Confirmation with employee email preview |
| `OverallInsightsTab` | Four sections: Employee Breakdown / Statutory Summary / Payment Mode / Component Breakdown |
| `StatutoryBreakdownSection` | PF/ESI/PT summary table |
| `ComponentBreakdownAccordion` | Collapsible earning/deduction breakdown with drill-down links |
| `TaxesDeductionsTab` | Three tables: Tax Details / Benefits / Donations |
| `ComponentDrillDown` | Per-employee breakdown for a single component |
| `ImportModal` | File upload + encoding selector for bulk imports |
| `PayrollHistoryTable` | Historical runs with type filter |
| `OneTimePayoutDialog` | Component selector + date |
| `OffCyclePayRunDialog` | Date only |

---

### D. State Machine (Text Diagram for Implementation)

```
READY
  |-- trigger: POST /api/payroll-runs/initiate
  v
DRAFT
  |-- actions allowed: lop, add-earning, tds-override, skip, withold, import
  |-- guard: pending tasks must be resolved
  |-- trigger: POST /api/payroll-runs/{id}/approve (all tasks clear)
  v
APPROVED
  |-- actions allowed: reject-approval, record-payment, download-bank-advice
  |-- trigger: POST /api/payroll-runs/{id}/record-payment
  v
PAID
  |-- actions allowed: delete-payment, download-payslips, send-payslips
  |-- trigger: DELETE /api/payroll-runs/{id}/payment (→ APPROVED)

DRAFT --[DELETE /api/payroll-runs/{id}]--> DELETED
APPROVED --[POST reject-approval]--> DRAFT
```

---

### E. Key Business Logic Rules to Implement

#### 1. LOP Proration Engine

```
ProratedAmount(component, lopDays, baseDays) =
    component.FullMonthAmount × (baseDays - lopDays) / baseDays

Applied to all Fixed salary components.
Variable components (one-time earnings): NOT prorated — flat amount.
```

Must be implemented in `Payroll.Engine` as a pure function with `decimal` arithmetic. Never `float`.

#### 2. TDS Computation (New Regime, FY2026)

```
1. ProjectedAnnualIncome = (CurrentMonthGross × RemainingMonths) + YtdGross + PriorEmployerIncome
2. TaxableIncome = ProjectedAnnualIncome - StandardDeduction(₹75,000)
3. TaxBeforeRebate = ApplySlabs(TaxableIncome, slabs from StatutoryConfig)
4. Rebate87A = min(TaxBeforeRebate, ₹25,000) if TaxableIncome ≤ ₹7,00,000 else 0
5. TaxAfterRebate = TaxBeforeRebate - Rebate87A
6. Surcharge = ApplySurcharge(TaxAfterRebate, TaxableIncome)
7. Cess = (TaxAfterRebate + Surcharge) × 0.04
8. AnnualTax = TaxAfterRebate + Surcharge + Cess
9. TdsThisMonth = (AnnualTax - YtdTdsDeducted) / RemainingMonths

Special case: If PAN missing → TdsThisMonth = max(computed, MonthlyGross × 0.20)
```

All values from `StatutoryConfig` (DB table). No hardcoded slabs in engine.

#### 3. PF Computation

```
PfWage = min(PfEligibleComponents after LOP proration, ₹15,000)
EmployeePf = PfWage × 12%
EmployerEpf = PfWage × 3.67%   (EPF split)
EmployerEps = PfWage × 8.33%   (EPS split, capped: EPS Wage max ₹15,000)
EdliContribution = min(PfWage × 0.5%, ₹75)
```

#### 4. ESI Computation

```
EsiWage = ESI-eligible components after LOP proration
If EsiWage ≤ ₹21,000:
    EmployeeEsi = EsiWage × 0.75%
    EmployerEsi = EsiWage × 3.25%
Else:
    ESI = 0 (employee above wage ceiling)
```

Once an employee's ESI wage exceeds ₹21,000 mid-year, they continue contributing until year-end (contribution period). Implement per ESIC contribution period rules.

#### 5. Professional Tax

```
PtWage = gross pay for the month (after LOP)
PtAmount = LookupPtSlab(State, PtWage, FiscalYear) from StatutoryConfig
```

PT is a flat amount from state-specific slabs. Not a percentage. Varies by state. Some states: monthly; others: half-yearly (September + March).

#### 6. Mid-Month Joiner

```
If employee.DateOfJoining is within current pay period:
    DaysBeforeJoining = JoinDate - PeriodStart - 1
    Auto-set LOP = DaysBeforeJoining
    (Zoho does NOT do this automatically — we should as a differentiating feature)
```

#### 7. Pending Tasks Gate

Before `ApprovePayrollRunCommand` executes:

```csharp
var pendingHardBlocks = await CheckPendingTasks(runId);
if (pendingHardBlocks.Any())
    throw new PayrollRunHasBlockingTasksException(pendingHardBlocks);
```

Blocking tasks:
- Employees with status = "Pending" (onboarding incomplete) — not skipped
- Employees with missing salary structure assignment

Non-blocking (soft warnings):
- Employees without PAN

#### 8. Approval Locks

On `ApprovePayrollRunCommand` success:
- Lock reimbursement claims for this month (no new claims for the approved period)
- Lock IT declarations for this month (employees cannot update)
- Lock POI submissions for this month

This should be enforced at the API level, not just UI.

#### 9. Payslip Generation

Triggered on `RecordPaymentCommand` success (or as async background job):
1. For each `PayrunEmployee` where `Status = Active`:
   - Fetch component breakdown
   - Fetch YTD data from prior runs
   - Generate PDF via `PayslipPdfGenerator`
   - Store in MinIO under key: `payslips/{tenantId}/{runId}/{employeeId}.pdf`
   - Create `Payslip` entity with `IsPublished = true`
2. Employee portal can now show the payslip

#### 10. Payroll Cost vs Total Net Pay

```
PayrollCost = TotalNetPay + TotalEmployerPf + TotalEmployerEsi + TotalEdli
```

When PF/ESI = 0: PayrollCost = TotalNetPay (as observed in test org).

---

### F. Suggested Build Order

Phase 1 — Core Pay Run Lifecycle (MVP)

1. `PayrollRunStatus` enum update (add Approved, Paid, Deleted)
2. `PayrollRunType` enum (Regular, OffCycle, OneTimePayout, Resettlement)
3. `PayrunEmployee` entity + EF configuration
4. `PayrunComponentBreakdown` entity + EF configuration
5. `PayrollRunAuditLog` entity + EF configuration
6. `GET /api/payroll-runs/current-period` — period card data
7. `POST /api/payroll-runs/initiate` — create regular run
8. `GET /api/payroll-runs/{id}` — run detail
9. `GET /api/payroll-runs/{id}/employees` — employee table
10. Engine: LOP proration function (pure, `decimal`)
11. `PUT /api/payroll-runs/{id}/employees/{employeeId}/lop` — LOP entry
12. Engine: TDS computation (new regime, no old regime)
13. `POST /api/payroll-runs/{id}/employees/{employeeId}/earnings` — one-time earning
14. `PUT /api/payroll-runs/{id}/employees/{employeeId}/tds-override`
15. `GET /api/payroll-runs/{id}/pending-tasks`
16. `POST /api/payroll-runs/{id}/approve` — with task gate
17. `POST /api/payroll-runs/{id}/reject-approval`
18. `POST /api/payroll-runs/{id}/record-payment`
19. `DELETE /api/payroll-runs/{id}/payment`

Phase 2 — Payslip & Bank Advice

20. `Payslip` entity + PDF generator
21. `TdsWorksheet` entity + computation storage
22. `GET /api/payroll-runs/{id}/employees/{employeeId}/payslip` (JSON)
23. `GET /api/payroll-runs/{id}/employees/{employeeId}/payslip/pdf` (PDF)
24. `POST .../payslip/send` — email payslip
25. `GET /api/payroll-runs/{id}/bank-advice` — XLS download
26. Bulk payslip download (async Hangfire job)

Phase 3 — Insights, Import, Off-Cycle

27. `GET /api/payroll-runs/{id}/insights` — Overall Insights tab
28. `GET /api/payroll-runs/{id}/taxes` — Taxes & Deductions tab
29. Import: LOP, earnings, reimbursements (CSV upload)
30. Off-cycle pay run creation flow
31. One Time Payout creation flow
32. Resettlement / Arrear run (triggered by salary revision)
33. `POST /api/payroll-runs/{id}/reprocess` (convenience command — differentiator)

Phase 4 — Statutory Integration

34. PF ECR generation from run data
35. ESI return from run data
36. PT challan from run data
37. Form 24Q data from TDS worksheets
38. Compliance Calendar fed by statutory totals per run

---

### G. Differentiating Features (Better than Zoho)

| Gap in Zoho | Our Build Approach |
|-------------|-------------------|
| No auto-proration for mid-month joiners | Auto-compute LOP from Date of Joining if DOJ is within pay period |
| 5-step reversal to get to Draft from Paid | Single `POST /api/payroll-runs/{id}/reprocess` command |
| Bank Advice not stored — no re-download | Store Bank Advice in MinIO; `PayrollRunDownload` history table |
| TDS Sheet is PDF-only — cannot extract data | Expose TDS computation as JSON API; generate PDF from that |
| "No data to display" statutory empty state | Show explanatory empty state: "Configure PF/ESI in Settings to see statutory summary" |
| Date range filter missing in Payroll History | Add from/to date filter + type filter |
| Bank account shown in full on payslip PDF | Add tenant-level config: "Mask account number on payslip" (last 4 digits shown) |
| Async bulk download delivered via notification | Show progress indicator; deliver to Downloads history page |

---

*Document compiled from 17 pay-run audit files, 22 userflow files (UF-36 through UF-58, UF-A1, UF-A2, UF-A12, UF-A13), and direct Zoho Payroll India browser observation.*
*Date compiled: 2026-05-17*
