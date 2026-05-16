# Employees > Bulk Import

## URL / Navigation Path
- Route: `#/people/employees` (Employee List page)
- Entry: "Show dropdown menu" (caret) next to "Add" button → "Import Data"
- Trigger: Dialog/modal overlay — no separate page URL

## Import Data Modal

### Header
"Import Data"

### Primary Field
**Select the type of employee details to import** — combobox (required, must select before "Proceed" is enabled)

### Import Categories (Full List)

The import type dropdown is grouped into 5 categories:

#### Group: Employee Details
| Import Type | Purpose |
|---|---|
| Employee Basic Details | Name, email, DOJ, designation, department, work location, gender, mobile |
| Employee Statutory Details | PAN, PF account number, UAN, ESI number, PT enable/disable |
| Employees Payment Information | Bank account, IFSC, payment mode |

#### Group: Salary Details
| Import Type | Purpose |
|---|---|
| Employee Salary Details | CTC and salary component percentages |
| Salary Revision | New CTC + effective date (dated salary hike import) |
| Employee Scheduled Earnings | Recurring additional earnings beyond base salary |
| Employee Benefit Details | Pre-tax benefits (VPF, NPS employee) |
| Employee Deduction Details | Post-tax deductions |
| Perquisites | Non-cash benefits (company car, rent-free accommodation) |
| Employee Vehicle Details | Vehicle registration for perquisite valuation |

#### Group: Complete Employee Basic Details
| Import Type | Purpose |
|---|---|
| Employee Details | Full employee onboarding — all basic + statutory + payment in one import |

#### Group: Employee Exit Details
| Import Type | Purpose |
|---|---|
| Employee Exit Details | Last working day, exit reason, F&F timing for bulk exits |

#### Group: Investments
| Import Type | Purpose |
|---|---|
| Chapter VI-A Details | 80C, 80D, 80G, etc. investment declarations (old regime) |
| HRA Details | HRA exemption claim details |
| Previous Employment Details | Prior employer YTD: gross salary, TDS deducted, period |
| Allowance Declaration Details | LTA, children education allowance claims |
| Other Income Details | Interest income, capital gains declared by employee |
| Let Out Property Details | Rental income declared by employee |
| Tax Regime Details | Which tax regime employee has opted for (old/new) |

### Buttons
| Button | State | Behavior |
|---|---|---|
| Proceed | Disabled until import type selected | Opens step 2: file upload |
| Cancel | Always enabled | Closes modal; returns to employee list |

## Import Flow (Inferred — Step 2 Not Navigated)

Standard Zoho import flow (consistent across all Zoho products):
1. **Step 1: Select Type** — choose what to import (this modal is step 1)
2. **Step 2: Download Template** — download XLSX/CSV template with required columns
3. **Step 3: Upload File** — drag-and-drop or file picker for the populated template
4. **Step 4: Field Mapping** — confirm or remap column headers to system fields
5. **Step 5: Validation Preview** — shows rows with errors in red; valid rows in green
6. **Step 6: Import** — final import; shows success/error counts

## Employee Basic Details Import — Expected Columns
| Column | Mandatory | Format | Notes |
|---|---|---|---|
| First Name | Yes | Text | — |
| Last Name | Yes | Text | — |
| Work Email | Yes | Email format | Must be unique |
| Employee ID | Yes | Text | Must be unique |
| Date of Joining | Yes | dd/MM/yyyy | — |
| Designation | No | Text | Must match existing or will create new |
| Department | No | Text | Must match existing or will create new |
| Work Location | No | Text | Must match existing; cannot create via import |
| Gender | No | Male/Female | — |
| Mobile | No | 10-digit | — |

## Salary Revision Import — Expected Columns
| Column | Mandatory | Format | Notes |
|---|---|---|---|
| Employee ID or Work Email | Yes | Text | Identifier |
| New Annual CTC | Yes | Number | In rupees |
| Effective From Date | Yes | dd/MM/yyyy | Revision effective date |
| Reason | No | Text | Hike reason |

## Previous Employment Details Import — Expected Columns
| Column | Mandatory | Format | Notes |
|---|---|---|---|
| Employee ID or Work Email | Yes | Text | — |
| Employer Name | Yes | Text | Prior employer name |
| Period From | Yes | dd/MM/yyyy | — |
| Period To | Yes | dd/MM/yyyy | — |
| Gross Salary | Yes | Number | Total gross for the period |
| Standard Deduction Claimed | No | Number | 0 or 75000 |
| Professional Tax Paid | No | Number | — |
| TDS Deducted | Yes | Number | — |
| Other Income | No | Number | — |

## Business Rules

1. **Template download is mandatory** — the system provides the correct column structure; users should not create their own templates.
2. **Duplicate detection** — on "Employee Basic Details" import, if work email or employee ID already exists, row is flagged as duplicate (update vs. insert mode may be available).
3. **Partial import success** — valid rows are imported even if some rows have errors. Error rows are shown in the validation preview.
4. **Work location must exist** — unlike designation/department, work location cannot be created via import. Admin must configure it in Settings first.
5. **Investments imports are FY-scoped** — previous employment details and Chapter VI-A declarations apply to a specific financial year. The import may prompt for FY selection.
6. **Tax Regime import** — separate from investment declaration; allows bulk assignment of tax regime (old/new) per employee.
7. **Import history** — Zoho typically keeps an import log. Not verified in this audit.

## Key Observations for Our Build

1. **19 import types** — our import framework must support all major entity types. Implement as a generic CSV/XLSX import pipeline with per-type column definitions.

2. **Import entity architecture:**
   ```
   ImportJob {
     id: UUID
     tenant_id: UUID
     import_type: Enum (EmployeeBasic, SalaryRevision, PreviousEmployment, ...)
     file_url: String (S3/MinIO path)
     status: Enum (Pending, Validating, Validated, Importing, Completed, Failed)
     total_rows: Int
     valid_rows: Int
     error_rows: Int
     started_at: Timestamp
     completed_at: Timestamp
     initiated_by: FK(AdminUser)
   }
   ImportRow { job_id, row_number, status, error_message, raw_data }
   ```

3. **Template generation** — our API must generate downloadable CSV templates per import type with correct headers and example data.

4. **Validation rules per type** — each import type has its own validation: date formats, required fields, referential integrity (employee must exist for salary revision import), value ranges.

5. **Previous Employment Details** — this is a V1 priority for mid-year joiners. Our import must process prior employer YTD into the TDS engine computation.

6. **Salary Revision import** — critical for bulk hike cycles. Import must trigger TDS recomputation for all affected employees.

7. **Tax Regime import** — for our new-regime-only V1, this can be simplified: all employees default to new regime. Import would be used if old regime support is added later.

8. **Real-time validation feedback** — show per-row validation results before committing import. Allow download of error report (CSV with error column appended).

## Cross-Module Impact
- Employee Basic Details import → creates Employee records; triggers onboarding workflow
- Salary Revision import → creates SalaryRevision records; triggers TDS recomputation
- Previous Employment Details import → creates PriorEmployerYtd records; triggers TDS recomputation
- Employee Exit Details import → creates EmployeeExit records; triggers F&F pay run creation

## Open Questions
- [ ] Does Zoho support "update" mode for Employee Basic Details import (update existing employee) or only "insert" (create new)?
- [ ] What is the maximum file size / row count limit for imports?
- [ ] Is there an import history log viewable by admin?
- [ ] For "Employee Details" (complete basic details) import — does it also create salary structure in one step?
