# UF-14: Add Employee — Arjun Mehta (Basic Details Step)

**Module:** Employees → Add Employee Wizard → Step 1: Basic Details
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta, Senior Software Engineer, Engineering dept, DOJ 01/04/2025

## Context

The system already had Arjun Mehta as EMP001 (pre-existing data). The Add Employee wizard was observed in blank/new state, and the completed employee profile was documented from the Overview tab. Actual data differs from mock spec (CTC ₹8,40,000 not ₹12,00,000; DOJ April 2025 not June 2025; PAN ABCPM1234A not AAAPZ1234C).

## Steps Executed
1. Navigated to `#/employees` → clicked "Add Employee" button
2. Wizard opened at Step 1: Basic Details
3. Observed all fields in blank state
4. Captured screenshot
5. Navigated to EMP001 (Arjun Mehta) → Overview tab to document completed state

## Add Employee Wizard — Step 1: Basic Details

**URL:** `#/employees/new` (wizard)
**Wizard Progress Bar:** 4 steps — Basic Details → Salary Details → Personal Details → Payment Information

### Fields (Step 1)

| Field | Type | Required | Default | Validation / Notes |
|-------|------|----------|---------|-------------------|
| Employee Name — First Name | Text | Yes | — | Three-part name (First / Middle / Last) |
| Employee Name — Middle Name | Text | No | — | Optional middle name |
| Employee Name — Last Name | Text | No | — | Optional last name |
| Employee ID | Text | Yes | — | Auto-generated or manual; e.g. EMP001 |
| Date of Joining | Date | Yes | — | Format dd/MM/yyyy |
| Work Email | Email | Yes | — | Placeholder: abc@xyz.com |
| Mobile Number | Text | No | — | No format hint shown |
| Employee is a Director/person with substantial interest in the company | Checkbox | No | Unchecked | Tooltip (?) present. Relevant for TDS computation — directors have different perquisite tax rules |
| Gender | Dropdown | Yes | — | Options: Male / Female / Other (not confirmed) |
| Work Location | Dropdown | Yes | Head Office (lerno, kazhakoottam, thiruvanan...) | Pre-fills with org's default work location |
| Designation | Dropdown | Yes | — | References designation master |
| Department | Dropdown | Yes | — | References department master |
| Enable Portal Access | Checkbox | No | Unchecked | Sub-text: "The employee will be able to view payslips, submit their IT declaration and create reimbursement claims through the employee portal." |

### Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Save and Continue | Button (primary) | Mandatory fields filled | Advances to Step 2: Salary Details |
| Cancel | Button | Any time | Returns to Employees list; unsaved changes dialog if data entered |

### Footer Notes
- "* Indicates mandatory fields" displayed at bottom right

## Employee Overview — Arjun Mehta (Completed State)

**URL:** `#/employees/{id}` → Overview tab
**Employee ID:** EMP001
**Status Badge:** Active (green)
**Designation:** Senior Software Engineer (shown below name)

### Basic Information Section (read view, edit via pencil icon)

| Field | Value |
|-------|-------|
| Name | Arjun Mehta |
| Email Address | arjun.mehta@lerno.com |
| Mobile Number | 9876543210 |
| Date of Joining | 01/04/2025 |
| Gender | Male |
| Work Location | Head Office |
| Designation | Senior Software Engineer |
| Departments | Engineering |
| Portal Access | Disabled — (Enable) button |

### Statutory Information Section (read view, edit via pencil icon)

| Statutory Item | Status | Toggle |
|----------------|--------|--------|
| EPF | Disabled | (Enable) |
| ESI | Disabled | (Enable) |
| Professional Tax | Enabled | (Disable) |
| Labour Welfare Fund | Disabled | (Enable) |

**Note:** EPF and ESI are disabled for Arjun Mehta despite him being a regular employee. This deviates from mock data which required PF eligible, ESI eligible. Likely pre-configured without statutory enablement.

### Personal Information Section (read view, edit via pencil icon)

| Field | Value |
|-------|-------|
| Date of Birth | 15/03/1990 |
| Father's Name | Rajesh Mehta |
| PAN | ABCPM1234A |
| Email Address | — (personal email not entered) |
| Residential Address | — |
| Differently Abled Type | None |

### Employee Profile Tabs

| Tab | Purpose |
|-----|---------|
| Overview | Basic, Statutory, Personal Information sections |
| Salary Details | CTC breakdown, Salary Structure, Perquisites, Salary Revision history |
| Investments | IT Declaration, Proof Of Investments sub-tabs with fiscal year filter |
| Payslips & Forms | Payslips list, Form 12BB, Form 16 |
| Loans | Loan history and management |

### Header Actions
- **Add** dropdown button (top right) — likely "Add Pay", "Add Loan", etc.
- **...** (more) button
- **X** (close/back) button

## Business Rules

- Work Location field defaults to the org's primary work location (Head Office, lerno, kazhakoottam, thiruvanan...)
- "Director/substantial interest" checkbox affects TDS perquisite computation — statutory relevance per Income Tax Act Section 17
- Portal Access is opt-in per employee — once enabled, employee can submit IT declarations and view payslips
- Statutory deductions (EPF/ESI/PT/LWF) are toggled per-employee from Overview page — they do NOT auto-enable from wizard

## Data Relationships
- Employee → Work Location (M:1)
- Employee → Designation (M:1)
- Employee → Department (M:1)
- Employee → Statutory Flags: EPF, ESI, PT, LWF (1:1 booleans per employee)

## Navigation
- Entry: `#/employees` list → "Add Employee" button
- Wizard URL: `#/employees/new`
- Post-save: `#/employees/{id}` Overview tab
- Edit from Overview: inline edit via pencil icons per section

## Screenshots
- [Add Employee wizard Step 1](../screenshots/UF-14-add-employee-wizard.png)
- [Arjun Mehta employee list](../screenshots/UF-14-employees-list.png)
- [Arjun Mehta Overview tab](../screenshots/UF-14-arjun-mehta-overview.png)

## Gaps / Observations
- No "Employee Type" field (Full-time / Contract / Intern) visible in Step 1 — may be absent from this product version
- Portal Access defaults to disabled — employees must be explicitly enabled; no bulk-enable option observed at this stage
- EPF/ESI statutory enablement is NOT part of the Add Employee wizard — must be done post-creation from Overview tab (extra step that could be missed)
- Director checkbox has tooltip but tooltip text not captured — needs follow-up
- Employee ID appears to be manually entered (no auto-increment UI visible) — needs clarification
