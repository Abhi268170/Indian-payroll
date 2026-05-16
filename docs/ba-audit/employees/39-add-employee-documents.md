# Employees > Documents Module

## URL / Navigation Path
- Route: `#/documents/folder`
- Full URL: `https://payroll.zoho.in/#/documents/folder`
- Entry: Left sidebar nav "Documents" link; NOT a per-employee wizard step
- Page title: "All Documents | Documents | Zoho Payroll"

## Purpose
Org-wide document management — upload, organize, and distribute documents to employees. Separate from the Add Employee wizard; there is NO per-employee Documents tab within the employee profile. Documents are organized in folders (Org Folder or Employee Folder) and are accessible company-wide.

## Note on Wizard Step Mapping
The audit plan listed item 39 as "Add Employee — Documents tab" — however, Zoho Payroll does NOT have a Documents tab in the Add Employee wizard or employee profile. Documents is a standalone module in the left nav. This file documents the Documents module as observed.

## Layout
Split-pane layout:
- Left pane: Documents sidebar — folder tree
- Right pane: Document listing / empty state for selected folder

## Left Sidebar Components

### Navigation Items
| Item | URL | Notes |
|---|---|---|
| All Documents | `#/documents/folder` | Root view — shows all docs across all folders |
| Org Folder section | — | Section header with "New Org Folder" button |
| Employee Folder section | — | Section header with "New Employee Folder" button |
| Storage Limit indicator | — | Shows "1GB / 100 employees"; static display |
| Trash | `#/documents/trash` | Soft-deleted documents |

### Folder Creation Actions
| Button | Behaviour |
|---|---|
| New Org Folder | Creates a folder accessible to all employees (org-wide visibility) |
| New Employee Folder | Creates a folder scoped to specific employee(s) |
| New folder (inside section) | Duplicate action button inside each empty-state section |

## Right Pane — All Documents View

### Filter Bar
| Filter | Type | Options | Notes |
|---|---|---|---|
| Select Employee Status | Combobox dropdown | (values not yet inspected) | Filters documents by employee status |
| Select an Employee | Combobox dropdown | Employee search/select | Filters to specific employee's documents |
| Close Filter | Button | — | Hides filter bar |
| Filter | Button (icon) | — | Opens/shows the filter bar |

### Empty State
- Illustration + message: "You have not created any folders yet to upload documents"
- No "Upload" button at root level — must create folder first, then upload within it

## Business Rules
1. Document upload requires a folder to exist first — cannot upload to root.
2. Two folder types: Org Folder (all employees) vs Employee Folder (specific employee scoped).
3. Storage quota: 1GB per 100 employees (appears to scale with employee count — exact formula unclear).
4. Trash provides soft-delete recovery.
5. No per-employee Documents tab in employee profile — all documents are accessed through this global module.
6. Filter by employee allows viewing documents belonging to a specific employee.

## Cross-Module Impact
- Documents uploaded here (e.g., offer letter, appointment letter) can be associated with employees.
- Payslips and Form 16 are NOT stored here — they have their own tab in the employee profile (`#/people/employees/{id}/payslips-and-forms`).
- This module is separate from Proof of Investments (which is in the Investments tab of employee profile).

## Key Observations for Our Build
1. **No per-employee Documents wizard step** — documents management is org-level, not part of onboarding flow. Our build should mirror this: a global Documents module, not embedded in employee profile.
2. **Folder-first upload flow** — our document storage must enforce folder creation before upload.
3. **Two folder access scopes** — Org vs Employee folder is a key access control dimension. Employee folder must enforce tenant + employee-level access.
4. **Storage quota** — our MinIO-backed document storage needs a per-org quota mechanism.
5. **Filter by employee** — document listing must support employee-scoped filtering.
6. **Payslips/Form 16 live elsewhere** — do not conflate document management with payslip distribution.

## Screenshots
- `screenshots/39-documents-module.png` — Documents module: All Documents view with empty state
