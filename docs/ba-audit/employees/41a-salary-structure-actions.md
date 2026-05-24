# Employees > Salary Structure Actions (Addendum to 41)

## Overview
This document covers the salary structure manipulation actions available on the employee Salary Details tab — both the "Add Earning" inline action (in the edit form) and the "Add Scheduled Earning / Deduction / Benefit / Donation" actions from the profile header.

## Action 1: Add Earning (In Edit Salary Details Form)

### Location
`#/people/employees/{id}/edit-salary-details` — last row of the salary structure table

### Interaction
- Button label: "Add Earning" (with + icon)
- Behavior: Inline dropdown (no modal) — opens a listbox within the table row
- Dropdown lists: all **active** salary components not already assigned to this employee's structure
- For EMP001 (Basic, HRA, Fixed Allowance already assigned): only "Conveyance Allowance" appeared

### What Happens After Selection
- New row added to the salary structure table above Fixed Allowance
- Admin can set the amount or percentage for the new component
- Fixed Allowance automatically decreases to absorb the new component (residual formula)
- Admin must still click "Save" to persist the change

### Business Rule
- A component already in the salary structure cannot be added again (it won't appear in the dropdown)
- Only components configured in Settings > Salary Components as "Active" appear
- The component type (Fixed vs Variable) determines whether it affects CTC calculation

### For Our Build
- "Add Earning" = assigning an additional salary component to an employee's salary structure
- Entity: `EmployeeSalaryComponent` (junction: employee_id, salary_component_id, amount/percentage, effective_from)
- UI: inline row addition in the salary structure table; not a modal

---

## Action 2: Add Scheduled Earning (From Profile Header)

### Location
Employee profile header → "Add" split button → "Scheduled Earning"
URL: `#/people/employees/{id}/salary-details?add_scheduled_earning=true`

### Modal: "Add Scheduled Earning"
| Field | Type | Required | Notes |
|---|---|---|---|
| Scheduled Earning | Combobox | Yes | Select from pre-configured scheduled earnings; search available |

### Scheduled Earnings vs Salary Components
- **Salary Components** (Basic, HRA, etc.) = recurring, percentage/formula-based, part of CTC structure
- **Scheduled Earnings** = additional recurring amounts outside the CTC structure (e.g., monthly bonus, project allowance, overtime)
- Scheduled Earnings must be pre-configured in a "Scheduled Earnings" master (separate from Settings > Salary Components)
- In the audit org, no scheduled earnings were pre-configured → dropdown showed "Sorry! No results found" + "New Scheduled Earning" button

### "New Scheduled Earning" Button
- Appears in the dropdown when no results found (or as a create option)
- Likely opens a creation modal to define a new scheduled earning type
- Not navigated in this audit session

### Buttons
| Button | State | Behavior |
|---|---|---|
| Save | Disabled until Scheduled Earning selected | Saves assignment; adds scheduled earning to employee |
| Cancel | Always enabled | Closes modal; returns to Salary Details tab |

### For Our Build
- Scheduled Earnings = a separate entity from salary components
- Entity: `ScheduledEarning` (org-level master: name, amount, earning_type, taxable/non-taxable, effective_from)
- Entity: `EmployeeScheduledEarning` (junction: employee_id, scheduled_earning_id, start_date, end_date)
- Payroll engine adds scheduled earning amounts to the gross for the applicable pay periods

---

## Action 3: Add Deduction (From Profile Header)

### Location
Employee profile header → "Add" split button → "Deduction"
URL: `#/people/employees/{id}/salary-details?add_deduction=true&deduction_type=post-tax`

### Notes
- `deduction_type=post-tax` → post-tax deduction (reduces net pay, not taxable income)
- Modal not investigated in this session (same pattern as Scheduled Earning)
- Examples: salary advance recovery, canteen deduction, professional association fee

---

## Action 4: Add Benefit (From Profile Header)

### Location
Employee profile header → "Add" split button → "Benefit"
URL: `#/people/employees/{id}/salary-details?add_deduction=true&deduction_type=pre-tax`

### Critical Finding
Zoho models **Benefits as pre-tax deductions** (`deduction_type=pre-tax`). This means:
- Benefits (e.g., Voluntary PF, NPS employee contribution) are subtracted before tax computation
- They reduce taxable income (for old regime) or just reduce net pay (for new regime where they don't affect tax)
- Same API endpoint/model as deductions, just with a `pre-tax` flag

### For Our Build
- Benefits and Deductions share the same entity structure; distinguished by `pre_tax` boolean
- Entity: `EmployeeDeduction` (employee_id, deduction_id, amount, is_pre_tax, start_date, end_date)
- In new regime: pre-tax deductions don't reduce tax (except employer NPS contribution 80CCD(2))

---

## Action 5: Add Donation Contribution (From Profile Header)

### Location
Employee profile header → "Add" split button → "Donation Contribution"
URL: `#/people/employees/{id}/salary-details?add_donation=true`

### Notes
- Donation contributions = amounts deducted from salary and donated to eligible charities
- Under Section 80G, donations to approved funds are tax-deductible (old regime only)
- In new regime: 80G deduction is NOT available
- Modal not investigated; pattern same as Scheduled Earning

---

## Header "Add" Button vs Salary Details Card "Edit"

| Action | Via | Purpose |
|---|---|---|
| Add Earning | Profile header "Add" → Scheduled Earning | Add recurring extra income outside CTC |
| Add component to CTC | Salary Details tab "Edit" → Add Earning button | Add a salary component to the CTC structure |
| Add Deduction | Profile header "Add" → Deduction | Add recurring post-tax deduction |
| Add Benefit | Profile header "Add" → Benefit | Add recurring pre-tax benefit (reduces net pay) |
| Add Donation | Profile header "Add" → Donation Contribution | Add recurring charitable donation deduction |
| Revise CTC | Salary Details tab "Edit" | Change Annual CTC and component percentages |

## Summary of Data Model Requirements

```
SalaryComponent (org-level master — from Settings > Salary Components)
  ↓ assigned to employee via
EmployeeSalaryComponent (junction — percentage or flat amount override)
  → Defines the base CTC structure

ScheduledEarning (org-level master — Scheduled Earnings settings)
  ↓ assigned to employee via
EmployeeScheduledEarning (junction — amount, start/end date)
  → Adds to payroll each month within the date range

Deduction (org-level master — pre-tax or post-tax)
  ↓ assigned to employee via
EmployeeDeduction (junction — amount, is_pre_tax, start/end date)
  → Deducted from gross (pre-tax) or net (post-tax) each month
```
