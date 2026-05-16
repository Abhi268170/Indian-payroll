# UF-20: Bulk Import Employees — CSV Import Flow

**Module:** Employees → Import Data
**Tested:** 2026-05-16

## Steps Executed
1. Navigated to `#/people/employees`
2. Clicked "Show dropdown menu" (caret next to Add button) → selected "Import Data"
3. Dialog appeared: "Import Data" with type selector dropdown
4. Clicked dropdown → enumerated all import types
5. Selected "Employee Basic Details" → clicked "Proceed"
6. Import page loaded at `#/people/employees/import?entity_type=employee_basic_personal_details`
7. Downloaded sample CSV from the link provided on the page

## Import Type Selection Dialog

**Trigger:** Employees list → dropdown caret next to "Add" → "Import Data"

**Dialog content:**
- Heading: "Import Data"
- Label: "Select the type of employee details to import"
- Dropdown: Ember Power Select with grouped options
- Buttons: "Proceed" (disabled until type selected) | "Cancel"

### Import Types — Full List (Grouped)

**Group: Employee Details**
| Import Type | Description |
|-------------|-------------|
| Employee Basic Details | Core employee fields including name, dates, work location |
| Employee Statutory Details | EPF/ESI/PT/LWF toggles per employee |
| Employees Payment Information | Bank account details |

**Group: Salary Details**
| Import Type | Description |
|-------------|-------------|
| Employee Salary Details | Salary structure assignment and component amounts |
| Salary Revision | Batch salary revisions with new CTC and effective dates |
| Employee Scheduled Earnings | Recurring variable pay entries |
| Employee Benefit Details | Benefit component values |
| Employee Deduction Details | Deduction component values per employee |
| Perquisites | Perquisite values (car, accommodation, ESOP, etc.) |
| Employee Vehicle Details | Vehicle perquisite data |

**Group: Complete Employee Basic Details**
| Import Type | Description |
|-------------|-------------|
| Employee Details | Full employee record (combines multiple groups) |

**Group: Employee Exit Details**
| Import Type | Description |
|-------------|-------------|
| Employee Exit Details | Last working day, exit reason |

**Group: Investments**
| Import Type | Description |
|-------------|-------------|
| Chapter VI-A Details | 80C/new-regime investment declarations |
| HRA Details | HRA declaration (rent amounts, landlord details) |
| Previous Employment Details | Prior employer YTD figures for TDS |
| Allowance Declaration Details | Allowance-specific declarations |
| Other Income Details | Income from other sources |
| Let Out Property Details | Rental income from let-out property |
| Tax Regime Details | Employee tax regime assignment (old/new) |

## Import Page — Employee Basic Details

**URL:** `#/people/employees/import?entity_type=employee_basic_personal_details`
**Page title:** "Employee Basic Details - Select File"

### Form Fields

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| File | File upload | Yes | — | Drag-and-drop or "Choose File" button |
| How should duplicate entries be handled? | Radio | Yes | Overwrite | Options: Skip / Overwrite |
| Character Encoding | Dropdown | Yes | UTF-8 (Unicode) | Other encodings available |

**File constraints:**
- Max size: 5 MB
- Formats: CSV or TSV or XLS

**Sample file links:**
- Download `.csv format` — `/common/import/in/sample_employees_basic_and_personal_details.csv`
- Download `.xls format` — `/common/import/in/sample_employees_basic_and_personal_details.xls`

**Action buttons:** "Next" (disabled until file uploaded) | "Cancel"

### Side Panel — "Things to Note"
- Create all work locations under Settings > Work Locations before importing
- Check column mapping is correct during field mapping step
- Download sample XLS for field explanations
- Ensure date format matches what is specified during field mapping

## Sample CSV — Employee Basic Details

**Filename:** `sample_employees_basic_and_personal_details.csv`

**Columns (25 fields):**

| Column | Example Value | Notes |
|--------|--------------|-------|
| Employee Number | 2406 | Employee ID |
| First Name | Manish | |
| Middle Name | (blank) | Optional |
| Last Name | Patel | |
| Gender | Male | |
| Employee Status | Active | |
| Date of Joining | 02-05-2018 | Format: dd-MM-yyyy |
| Gratuity Calculation Date | 02-05-2015 | Can differ from DOJ |
| Last Working Day | (blank) | For exits |
| Designation | Technical Staff | Must match existing designation master |
| Work Email | manish.patel@zylker.com | |
| Department | Finance | Must match existing department master |
| Worklocation Name | Head Office | Must match existing work location |
| Enable Portal | Yes | Yes/No |
| Personal Email | manish.patel@zillium.com | |
| Father Name | patel | |
| Mobile Number | (blank) | Optional |
| Date of Birth | 31-10-1994 | Format: dd-MM-yyyy |
| Personal AddressLine1 | No. 71 | |
| Personal AddressLine2 | Marutham Nagar | |
| Personal City | Kancheepuram | |
| Personal StateCode | TN | 2-letter state code |
| Personal Country | India | |
| Personal PostalCode | 641041 | |
| PAN Number | CPFPK2320L | 10-char PAN format |

**Notable observations:**
- Gratuity Calculation Date can differ from Date of Joining (e.g., prior service credit)
- State uses 2-letter code (TN, KA, KL, MH, etc.) not full name
- PAN follows AAAAA9999A format (5 letters, 4 digits, 1 letter)
- No Aadhaar field in basic details import — Aadhaar capture not in this template
- No Employee Type (Full-time/Contract/Intern) field visible

## Import Wizard Steps (Inferred from "Next" button)

Step 1: Select File (observed)
Step 2: Field Mapping (inferred — map CSV columns to system fields)
Step 3: Preview / Validate
Step 4: Import

## Prior Payroll Page

**URL:** `#/prior-payroll`
**Status:** Disabled for this org (prior payroll was not enabled during setup)

**Empty state message:**
"You have not checked the option to include prior payroll during setup. In case you need to add prior payroll data for your employees, you can import the necessary details and continue processing payrolls."

**CTA:** "Enable Prior Payroll" button

**Error on attempt to enable:**
Toast notification: "Organisation prior payroll cannot be updated since payroll had been run already."

This means prior payroll can only be enabled BEFORE the first pay run is processed. Once any pay run exists, this setting is permanently locked.

## Business Rules

1. **Import types are granular** — 14 distinct import entity types covering all employee data domains. Admins can import specific data without re-importing all fields.

2. **Duplicate handling** — "Overwrite" (default) replaces existing records; "Skip" ignores rows where employee already exists. Employee Number is the matching key.

3. **Work Location / Designation / Department must pre-exist** — import will fail (or create errors) if referenced masters don't exist.

4. **Prior payroll lock** — once any payroll run is processed, prior payroll data cannot be enabled. Must be configured at org setup.

5. **PAN is in the basic import** — required for TDS compliance. Format constraint (10-char AAAAA9999A) is enforced.

6. **Tax Regime Details import** — separate import type allows bulk assignment of tax regime (old/new) across employees.

## Cross-Module Effects
- Employee Basic Details import → creates employee records, populates employee master
- Previous Employment Details import → feeds IT Declaration prior employer YTD for TDS computation
- Salary Revision import → creates pending revisions for bulk salary changes
- Tax Regime Details import → overwrites per-employee tax regime preference

## Navigation
- Entry: Employees list → dropdown caret → Import Data → dialog → type selection → Proceed
- Import page URL: `#/people/employees/import?entity_type={type}`
- Sample CSV: `/common/import/in/sample_employees_basic_and_personal_details.csv`
- Export: same dropdown → Export Data (not explored)

## Screenshots
- [Import type dropdown — all options](../screenshots/UF-20-import-type-dropdown.png)
- [Import page — Employee Basic Details](../screenshots/UF-20-import-employee-basic.png)
- [Prior payroll disabled state](../screenshots/UF-25-prior-payroll-disabled.png)
- [Prior payroll enable error toast](../screenshots/UF-25-prior-payroll-error.png)

## Gaps / Observations
- No Aadhaar field in Employee Basic Details CSV — Aadhaar capture mechanism not identified (may be employee self-entry via portal)
- No Employee Type field (Full-time/Contract/Intern) in import template — contract employees may need special handling
- Import field mapping step not documented (not reached — requires actual file upload)
- Export Data functionality not explored
- "Previous Employment Details" sample CSV not accessible (URL guessing failed) — columns not documented
- Prior payroll completely locked once any pay run exists — no workaround visible
