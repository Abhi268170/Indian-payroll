# Employees > Add Employee — Step 1: Basic Details

## URL / Navigation Path
- Route: `#/people/employees/new`
- Full URL: `https://payroll.zoho.in/#/people/employees/new`
- Entry point: "Add Employee" button on empty state OR "Add" button in toolbar on Employee List
- Page title: "Employees | New | Zoho Payroll"

## Purpose
Step 1 of 4 in the Add Employee wizard. Captures the core identifying and organisational information for a new employee. This is the minimum data needed to create an employee record.

## Wizard Structure (4 Steps)
The Add Employee flow is a linear 4-step wizard shown as a numbered progress list at the top:
1. **Basic Details** (this step) — identity, contact, org placement
2. **Salary Details** — compensation structure
3. **Personal Details** — PAN, Aadhaar, DOB, address, emergency contact
4. **Payment Information** — bank account details

Steps are displayed as a horizontal numbered list. Navigation between steps is sequential (must save each step to advance). No visible "jump to step" links — steps 2–4 appear non-clickable until step 1 is saved.

## Layout
- **Page heading**: "Add Employee" (h3)
- **Step progress bar**: Numbered list with step labels
- **Form body**: Single-column form with field pairs laid out in responsive grid
- **Form footer**: "Save and Continue" button + "Cancel" link + mandatory field note

## Fields

| Field | Type | Required | Options / Format | Help Text / Notes |
|---|---|---|---|---|
| Employee Name — First Name | Text (autocomplete/type-ahead) | Yes | Free text; placeholder "First Name" | Three sub-fields for name: First, Middle, Last |
| Employee Name — Middle Name | Text | No | Free text; placeholder "Middle Name" | Optional |
| Employee Name — Last Name | Text | No | Free text; placeholder "Last Name" | No salutation/title prefix field exists |
| Employee ID | Text | Yes | Free text; no auto-generation observed; no placeholder; no format hint | Must be manually entered; likely must be unique |
| Date of Joining | Text (date input with calendar picker) | Yes | Format: `dd/MM/yyyy`; opens Bootstrap-style calendar on click | Calendar shows month/year navigation; correctly parses typed date |
| Work Email | Text (styled autocomplete) | Yes | Placeholder `abc@xyz.com`; free text entry | Warning shown on entry: "You cannot change this Email address later on, as this will be used to send payslips and also for employees to sign in to their portal." — irreversible field |
| Mobile Number | Tel input | No | Numeric; no format hint; no country code prefix | `type="tel"` — no visible +91 prefix or 10-digit validation hint |
| Employee is a Director / person with substantial interest in the company | Checkbox | No | Unchecked by default | Has tooltip icon. Statutory relevance: Directors have different TDS rules (no 87A rebate, surcharge applies differently) |
| Gender | Custom dropdown (ac-box) | Yes | Male / Female / Others | 3 options confirmed |
| Work Location | Custom dropdown (ac-box) | Yes | Populated from configured Work Locations; default pre-filled with "Head Office" if only one exists | Work Location determines PT slab (key statutory relevance) |
| Designation | Custom dropdown (ac-box) with inline creation | Yes | Shows "Sorry! No results found" if none configured; "New Designation" inline link creates via modal | Modal fields: Designation Name* only |
| Department | Custom dropdown (ac-box) with inline creation | Yes | Shows "Sorry! No results found" if none configured; "New Department" inline link creates via modal | Modal fields: Department Name* + Department Code (optional) |
| Enable Portal Access | Checkbox | No | Unchecked by default | Help text: "The employee will be able to view payslips, submit their IT declaration and create reimbursement claims through the employee portal." When Work Email is filled, a "Preview mail" button appears beside this checkbox |

## Inline Creation Modals Observed

### New Designation Modal
- **Trigger**: "New Designation" link in Designation dropdown when no results found
- **Fields**: Designation Name* (text, required only)
- **Actions**: Save, Cancel
- **Behaviour**: On Save, designation is immediately created and selected in the dropdown; no page reload

### New Department Modal
- **Trigger**: "New Department" link in Department dropdown when no results found
- **Fields**:
  - Department Name* (text, required)
  - Department Code (text, optional)
- **Actions**: Save, Cancel
- **Behaviour**: On Save, department is immediately created and selected; no page reload

## Buttons & Actions

| Button / Link | Type | Behaviour |
|---|---|---|
| Save and Continue | Primary button | Validates form; on success advances to Step 2 (Salary Details) |
| Cancel | Link (href `#/people/employees`) | Returns to Employee List without saving |
| Preview mail | Button (appears after Work Email is filled, next to Enable Portal Access) | Presumably shows a preview of the portal invite email sent to the employee |
| New Designation | Inline link in dropdown | Opens New Designation modal |
| New Department | Inline link in dropdown | Opens New Department modal |

## Conditional Logic

1. **Work Email warning banner**: Appears immediately when Work Email field gains a value. Warning is inline below the field: "You cannot change this Email address later on..." — signals immutability post-save.
2. **Preview mail button**: Appears next to "Enable Portal Access" checkbox only after Work Email has been entered.
3. **Work Location pre-selection**: If only one Work Location exists in the org, it is pre-selected. Has a "Clear Selection" X button to deselect.
4. **Designation / Department inline creation**: Both dropdowns offer inline entity creation when no results exist, preventing the user from having to abandon the form and go to Settings.

## Key Observations for Our Build

1. **No salutation field**: Zoho does not capture Mr/Mrs/Ms/Dr prefix. First Name is plain text. Our build also omits salutation — confirmed correct.
2. **Employee ID is manual**: No auto-generation pattern is visible (e.g., no "EMP-001" auto-increment). Users must type their own ID. We should consider auto-generation with a configurable prefix as a UX improvement.
3. **Work Email is immutable post-save**: Strong warning shown before save. This is a critical business rule — the email is the login credential for the employee portal. Our build must enforce this same immutability.
4. **Mobile is `type="tel"` with no visible 10-digit validation**: No +91 prefix, no digit-count hint. We should enforce 10-digit Indian mobile validation.
5. **Director checkbox has statutory implications**: Directors are treated differently under TDS (Section 194A/194J surcharge, no 87A rebate). The checkbox on this form is the trigger for that logic. Key for our TDS engine.
6. **Work Location drives PT**: The Work Location selected here determines which Professional Tax slab applies. This linkage must be maintained in our data model.
7. **Inline entity creation is a strong UX pattern**: Being able to create Designation and Department without leaving the form reduces friction significantly. We should replicate this for Designation, Department, and potentially Work Location.
8. **Department modal has Department Code field**: A useful additional field for payroll grouping/reporting. Our Department entity should include a `code` attribute.
9. **No Employment Type on this step**: Employment Type (permanent/contract) is not on this step. Likely on Step 3 (Personal Details) or post-creation.
10. **No Reporting Manager field on this step**: Org hierarchy (reporting manager) is absent from the basic details step entirely. May appear on Step 3 or be absent in this product.

## EMP001 Data Filled
- First Name: Arjun | Middle: (blank) | Last: Mehta
- Employee ID: EMP001
- Date of Joining: 01/04/2025
- Work Email: arjun.mehta@lerno.com
- Mobile: 9876543210
- Gender: Male
- Work Location: Head Office (pre-selected)
- Designation: Senior Software Engineer (created inline)
- Department: Engineering (created inline)
- Enable Portal Access: unchecked (default)
- Director checkbox: unchecked

## Screenshots
- `screenshots/35-add-employee-personal.png` — Initial page load of Step 1
- `screenshots/35-new-designation-modal.png` — New Designation inline modal
- `screenshots/35-new-department-modal.png` — New Department inline modal (showing 2 fields)
- `screenshots/35-add-employee-basic-filled.png` — All fields filled for EMP001
