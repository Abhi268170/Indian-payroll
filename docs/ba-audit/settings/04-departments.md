# Settings > Departments

## URL
`#/settings/departments`

Sub-routes:
- `#/settings/departments?add_department=true` — New Department modal (query param triggers modal on same page)
- `#/settings/departments/import` — Import departments from file

## Purpose
Defines the organisational department taxonomy used to categorise employees. Departments appear on employee profiles, payslips, and payroll reports, enabling cost-centre-level reporting and analytics.

## Page Layout
**Empty state** (no departments exist): 
- Illustration graphic + headline: "Enhance organisation structure with new departments"
- Subtext: "Create department based on the once present in the organization and associate with employees"
- Two action buttons: "New Department" + "Import"
- Feature callouts: "Generate reports by department" | "Break down payroll data by department"

**Populated state** (when departments exist):
- Table/list view with department records
- Header: "Departments" heading + "New Department" button + sort/filter icon button

## Fields

### New Department Modal
| Field | Type | Required | Default | Options / Format | Notes |
|-------|------|----------|---------|------------------|-------|
| Department Name | Text | Yes | (blank) | Free text | Primary identifier; appears on employee profiles and reports |
| Department Code | Text | No | (blank) | Free text | Alphanumeric shortcode (e.g., "ENG", "HR") for report filtering |
| Description | Textarea | No | (blank) | Max 250 characters (placeholder text) | Internal description, not displayed on payslips |

### Import Page (`#/settings/departments/import`)
| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| File upload | Drag-and-drop / click-to-upload | Yes | — | Max 5 MB; accepts CSV, TSV, or XLS |
| Character Encoding | Dropdown | Yes | UTF-8 (Unicode) | Encoding for the imported file |

Import page also provides: "Download a sample file" link to get the template.

## Buttons & Actions

### Empty State Page
| Button | Label | State | Action |
|--------|-------|-------|--------|
| New Department (header) | "New Department" | Always enabled | Opens "New Department" modal (appends `?add_department=true` to URL) |
| Sort/Filter | (icon only) | Always enabled | Sorts or filters the list (no effect in empty state) |
| New Department (empty state body) | "New Department" | Always enabled | Same as above — opens modal |
| Import | "Import" | Always enabled | Navigates to `#/settings/departments/import` |

### New Department Modal
| Button | Label | State | Action |
|--------|-------|-------|--------|
| Close (X icon) | (icon) | Always enabled | Closes modal without saving |
| Save | "Save" | Enabled when Name is filled | Creates the department record |
| Cancel | "Cancel" | Always enabled | Closes modal without saving |

### Import Page
| Button | Label | State | Action |
|--------|-------|-------|--------|
| Download sample file | Link | Always enabled | Downloads CSV/XLS template |
| Next | "Next" | Enabled after file upload | Proceeds to column mapping step |
| Cancel | "Cancel" | Always enabled | Returns to Departments list |

## Tabs (if any)
None.

## Conditional Logic
1. **Empty state vs populated state** — When zero departments exist, a full empty-state UI (illustration + CTAs) is shown instead of the table.
2. **Save button** — enabled only when Department Name (required field) is filled.
3. **Import wizard** — multi-step: file upload → column mapping → review → confirm. Only first step observed.

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| Department | Assigned to each employee; appears on payslip PDF |
| Department | Used as a filter/grouping dimension in payroll reports (department-wise payroll summary, cost breakdown) |
| Department | Can be used as a Reporting Tag / dimension for analytics |
| Department | Referenced in workflow rules (e.g., route approvals to department head) |

## Observations & Notes
1. **Department Code field** is optional but important for report filtering in large organisations. Our implementation should support this.
2. **Import via CSV/TSV/XLS** with encoding selection — UTF-8 default is correct; organisations using older Excel files may need Windows-1252 encoding.
3. **No parent-department field** — Zoho Payroll does not support hierarchical departments (parent → child). This limits very large orgs but simplifies the data model.
4. **No budget or cost-centre linkage** — departments are purely organisational labels in this module; no financial dimension mapping (unlike Zoho Books GL integration).
5. **Typo in Zoho UI**: "Create department based on the **once** present in the organization" — should be "ones".

## Screenshots
- `docs/ba-audit/settings/screenshots/04-departments-empty.png` — empty state
- `docs/ba-audit/settings/screenshots/04-departments-modal.png` — New Department modal
