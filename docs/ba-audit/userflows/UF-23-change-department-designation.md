# UF-23: Change Department / Designation

**Module:** Employees > Overview > Basic Information Edit
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta EMP001 (ID: 3848927000000032948)
**App State Before:** Designation = "Senior Software Engineer", Department = "Engineering"

## Steps Executed
1. Navigate to Arjun Mehta's Overview: `#/people/employees/3848927000000032948`
2. Click Edit button on Basic Information section
3. Navigated to: `#/people/employees/3848927000000032948/edit-basic-details?add_department=false&add_designation=false&add_work_location=false`
4. Observed full edit form

## Page Identity
- **URL:** `#/people/employees/{id}/edit-basic-details?add_department=false&add_designation=false&add_work_location=false`
- **Page title:** "Employees | Basic Information | Zoho Payroll"
- **Heading:** "Arjun's basic information"

## All Editable Fields

| Field | Type | Required | Default / Current | Notes |
|-------|------|----------|-------------------|-------|
| Employee Name (First) | Textbox | Yes | "Arjun" | Three separate inputs: First, Middle, Last |
| Employee Name (Middle) | Textbox | No | "" | Optional |
| Employee Name (Last) | Textbox | Yes | "Mehta" | |
| Employee ID | Textbox | Yes | "EMP001" | Can be changed post-creation |
| Date of Joining | Textbox (date) | Yes | "01/04/2025" | dd/MM/yyyy format |
| Work Email | Textbox | Yes | "arjun.mehta@lerno.com" | abc@xyz.com placeholder |
| Mobile Number | Textbox | No | "9876543210" | |
| Employee is a Director/person with substantial interest | Checkbox | No | Unchecked | Statutory: affects TDS computation (Section 17 perquisite valuation) |
| Gender | Dropdown (combobox) | Yes | "Male" | |
| Work Location | Dropdown (combobox) | Yes | "Head Office (lerno, kazhakoottam, thiruvananthapuram, Kerala-695010)" | Has "Clear Selection" button |
| Designation | Dropdown (combobox) | Yes | "Senior Software Engineer" | Has "Clear Selection" button |
| Department | Dropdown (combobox) | Yes | "Engineering" | Has "Clear Selection" button |
| Enable Portal Access | Checkbox | No | Unchecked | |

## URL Query Parameters
The edit URL contains: `?add_department=false&add_designation=false&add_work_location=false`

This reveals that the form has inline "Add" capability for Department, Designation, and Work Location — allowing admins to create new values on-the-fly from within the employee edit form (when `add_department=true` etc.)

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Save | Button | Required fields filled | Saves changes, returns to Overview |
| Cancel | Link | Always | Returns to Overview without saving |
| Preview mail | Button (next to Enable Portal Access) | Portal Access checkbox | Shows email preview sent to employee |

## Business Rules
- **No approval required** for designation or department change — change is effective immediately on Save
- **No effective date field** — the change is immediately applied without any "effective from" date concept
- Work Location change determines PT applicability (PT is state-specific — changing location can change which PT slab applies)
- Designation is a freeform dropdown from Settings > Designations list — can be cleared and a new one created inline
- Department is from Settings > Departments list — same inline creation
- "Employee is a Director" checkbox: statutory implication — directors have different perquisite valuation rules under Section 17 of IT Act (car perquisites, accommodation etc. valued differently)
- Portal Access + "Preview mail" button suggests an invitation email is sent to the employee when portal is enabled
- Employee ID (EMP001) is editable — no lock after creation (risk: changing EMP ID after pay runs have occurred could break audit trail references)

## No Effective Date for Designation Change
Unlike salary revisions, there is no "effective date" for designation or department changes. The change takes effect immediately. This means:
- Historical payslips will NOT show the old designation (payslip shows designation at time of pay run, not at time of slip viewing)
- No history/changelog visible on this page for previous designations
- This is a gap compared to HRMS systems that track designation history for career progression reporting

## Cross-Module Effects
- Work Location change → affects Professional Tax slab (PT is work-location-based)
- Designation change → visible on next payslip
- Portal Access enable → triggers invitation email to employee

## Gaps / Observations
- No effective date for designation/department changes — no timeline audit
- Employee ID is mutable post-creation — high risk for data integrity if changed after pay runs
- No designation history stored (each edit overwrites previous)
- "Director" checkbox has major tax implications but no tooltip or help text explaining the consequences beyond the label itself
