# Employees > Salary Revision

## Overview
Salary revision in Zoho Payroll has two distinct mechanisms:

### Mechanism 1: Direct Edit (Immediate Effect)
- Route: `#/people/employees/{id}/edit-salary-details`
- Access: "Edit" link on Salary Details tab of employee profile
- Page title: "Employees | Salary Details | Zoho Payroll"
- Heading: "{First Name}'s salary details" (e.g., "Arjun's salary details")
- Effect: Changes effective from current pay run (if not yet Approved)
- Note displayed: "Any changes made to the salary components will take effect in the current pay run, provided it is not Approved."
- This is NOT a revision with historical tracking — it overwrites the current salary structure

### Mechanism 2: Salary Revision (Dated, Historical)
- Route: `#/people/employees/{id}/salary-revision`
- Access: Unknown — page renders empty when accessed directly via URL. Route exists but requires trigger from a specific UI state (possibly triggered from within pay run context or through an "Initiate Salary Revision" flow not yet observed)
- No salary revision option in employee profile header kebab dropdown (only: Add/Update Vehicle Details, Delete Employee, Initiate Exit Process)
- No salary revision option in Salary Details card kebab dropdown (only: Print Salary Structure, Send Salary Certificate, Print Salary Certificate)
- Salary Revision is available as an **import type** in the bulk Import Data modal — confirming Zoho supports dated salary revisions but the UI trigger may require a pay run to exist

## Edit Salary Details Page (Mechanism 1)

### URL
`#/people/employees/{id}/edit-salary-details`

### Fields
| Field | Type | Editable | Notes |
|---|---|---|---|
| Annual CTC | Spinbutton (number) | Yes | ₹ prefix, "per year" suffix; changing recalculates all component amounts |
| Basic % | Spinbutton | Yes | Shows current %. Changing % recalculates Basic monthly amount |
| Basic Monthly | Spinbutton | No (disabled) | Auto-calculated from CTC × Basic% |
| HRA % | Spinbutton | Yes | % of Basic Amount |
| HRA Monthly | Spinbutton | No (disabled) | Auto-calculated |
| Fixed Allowance | Display only | No | Formula: "Monthly CTC - Sum of all other components"; tooltip explains this |
| Calculation Type | Display | No | Shows "% of CTC" / "% of Basic" / "Fixed amount" per component |

### Add Earning Button
- Last row in the salary structure table
- Inline dropdown button — opens component selection dropdown
- Dropdown shows: only active, unassigned components not already in this employee's structure
- For EMP001: only "Conveyance Allowance" appeared (Basic, HRA, Fixed Allowance already assigned)
- Selecting a component adds a new row to the structure with editable amount/percentage
- New component reduces Fixed Allowance (residual absorbs the change)

### Dirty State Detection
- Changing any value triggers dirty state
- Navigating away shows confirmation dialog: "You might have some unsaved changes. Are you sure you want to leave this page?"
- Options: "Stay on this page" | "Leave this page"
- This dialog appears for ALL navigation attempts while in edit mode (including browser back, sidebar link clicks, tab changes)

### Buttons
| Button | Behavior |
|---|---|
| Save | Saves changes; effect date = current pay run (if not approved) |
| Cancel | Link → `#/people/employees/{id}/salary-details`; triggers dirty-state dialog if unsaved |

## Salary Revision via Bulk Import (Mechanism 2 — Indirect)

The Import Data modal (accessed from Employee List) has "Salary Revision" as an importable type under the "Salary Details" group. This is the primary way to do dated salary revisions for multiple employees at once. The import presumably accepts:
- Employee ID or Email
- New CTC
- Effective From Date
- Reason (optional)

This allows payroll admins to do hike cycles via CSV upload rather than editing each employee one-by-one.

## Business Rules

1. **Direct edit has no effective date field** — the change applies to the current open pay run. If pay run is Approved, the change takes effect from the next pay run.
2. **Salary revision (dated) likely creates a new salary record** — preserving history. Previous salary amounts remain accessible for historical payslips.
3. **Fixed Allowance always auto-adjusts** — it is the residual component. Any CTC change or component addition/removal automatically recalculates Fixed Allowance.
4. **Dirty state protection** — edit page protects against accidental navigation loss of unsaved changes.
5. **Add Earning dropdown only shows unassigned components** — a component already in the salary structure cannot be added again.
6. **Components once assigned follow org-level type rules** — percentage components (Basic, HRA) remain percentage-based; cannot switch to flat amount per-employee.

## Key Observations for Our Build

1. **Two distinct entities for salary changes:**
   - `SalaryStructure` — the current/active salary structure per employee (components + amounts). One active record per employee.
   - `SalaryRevision` — a dated record capturing: previous CTC, new CTC, effective date, reason, initiated by. Many records per employee. Creates new `SalaryStructure` effective from the revision date.

2. **Effective date handling:**
   - If revision effective date falls mid-month, engine must prorate: old salary for days before effective date, new salary for days from effective date.
   - `salary_effective_from` must be stored per salary structure record.

3. **Fixed Allowance invariant** — always = `Monthly CTC - sum(all other component monthly amounts)`. This constraint must be enforced by the engine, not just the UI.

4. **Add Earning → component assignment** — this adds a salary component to an employee's structure. Our data model: `EmployeeSalaryComponent` as junction table between `Employee` and `SalaryComponent`, with per-employee percentage/amount override.

5. **Dirty state detection** — implement `beforeunload` handler and route guard in React Router. Show confirmation dialog matching Zoho's UX: "You have unsaved changes. Are you sure you want to leave?"

6. **Bulk salary revision via import** — our import pipeline must support CSV with: `employee_id`, `new_annual_ctc`, `effective_from`, `reason`. Engine recomputes all component amounts from new CTC using existing percentage formulas.

## State Machine: Salary Structure
```
Initial (from wizard) → Active
Active → Revised (when new revision takes effect) → becomes Historical
Historical → [read-only; accessible for past payslip generation]
```

## Open Questions
- [ ] What exactly triggers the `salary-revision` page route — is it only accessible from within a pay run, or is there a standalone "Initiate Salary Revision" button somewhere not yet found?
- [ ] Does Zoho show salary revision history on the Salary Details tab (e.g., a revision timeline below the current structure)?
- [ ] For mid-month salary revision, how does Zoho prorate — using calendar days or working days?
- [ ] Can a future-dated salary revision be entered (e.g., hike effective 01/06 entered in May)?
