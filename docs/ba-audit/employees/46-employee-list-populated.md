# Employees > Employee List (Populated State)

## URL / Navigation Path
- Route: `#/people/employees`
- Full URL: `https://payroll.zoho.in/#/people/employees`
- Entry: Left sidebar "Employees" link
- Page title: "Employees | Zoho Payroll"

## Current State (Post 5-Employee Creation)
5 employees: EMP001 Arjun Mehta, EMP002 Priya Sharma, EMP003 Vikram Nair, EMP004 Aisha Khan, EMP005 Rahul Desai

## Layout
- Page header with filter/view selector, action buttons, notification banner
- Full-width data table with sortable columns, row checkboxes, row-level navigation

## Page Header Components

### View/Filter Selector
- Button "Active Employees" (current active view) with dropdown caret
- Clicking reveals a listbox with pre-built views + custom view link

**Available Views:**
| View Name | Description |
|---|---|
| All Employees | All employees regardless of status |
| Active Employees | Employees with Active status (default) |
| Exited Employees | Employees who have gone through exit process |
| Incomplete Employees | Employees with incomplete profiles |
| Portal Enabled Employees | Employees with self-service portal access enabled |
| Portal Disabled Employees | Employees with portal access disabled |
| Yet to Accept Portal Invite Employees | Portal invite sent but not accepted |

- Each view has a **"Mark as Favorite"** star button to pin it
- **New Custom View** link → `#/customviews/new?entity_type=employee` — creates a filtered/sorted custom view with custom criteria

### Incomplete Employees Banner
- Persistent alert when incomplete employee profiles exist
- Message: "You have 3 incomplete employees." + "View" button
- "View" switches to "Incomplete Employees" view
- Incomplete = employee profile wizard not fully completed (e.g., payment details skipped, or wizard abandoned partway)

### Action Buttons
| Button | Action | Notes |
|---|---|---|
| Add | Opens Add Employee wizard (`#/people/employees/new`) | Primary action |
| Show dropdown menu (caret) | Dropdown: Import Data / Export Data | Bulk operations |
| [unnamed icon button] | Likely Advanced Filter or Sort — icon not inspected | To be investigated |
| Instant Helper | Opens in-app help overlay | Context-sensitive help |

### Add Dropdown (Caret)
- **Import Data** → bulk employee import flow (item 51)
- **Export Data** → export employee list to file (CSV/XLSX expected)

## Employee Table

### Default Columns (4 of 21)
| Column | Sortable | Notes |
|---|---|---|
| Checkbox | N/A | Select all / individual row selection |
| Employee Name | Yes (Employee Name sort button with icon) | Shows Avatar Initial + "Name - EMP_ID" + Designation below |
| Work Email | No | For incomplete employees: shows warning + "Complete now" link |
| Department | No | — |
| Employee Status | No | Badge: Active / On Notice Period / Exited / Inactive |
| (action col) | N/A | Column header dropdown (Customize Column / Clip text) |

### Customize Columns
Available columns (21 total). Currently 4 selected. Full list:

| Column Name | Notes |
|---|---|
| Employee Name | Locked — always visible |
| Work Email | Default visible |
| Department | Default visible |
| Employee Status | Locked — always visible |
| Cost to Company | Salary field — CTC |
| Date of Birth | — |
| Date of Joining | — |
| Designation | — |
| ESI Number | ESI registration number per employee |
| Employee ID | EMP001, EMP002, etc. |
| Father Name | — |
| Gender | — |
| Last Working Day | For exited/notice employees |
| Mobile Number | — |
| Onboarding Status | Tracks wizard completion state |
| PAN | Tax ID — likely masked |
| PF A/C Number | PF account number |
| Portal Status | Enabled/Disabled/Not Invited |
| Prior Payroll Status | Whether prior employer YTD has been entered |
| UAN | Universal Account Number (PF) |
| Work Location | — |

Each column can be dragged to reorder (drag handles present). Columns also have inline remove (X) buttons.

### Row Interaction
- Clicking row → navigates to employee profile (`#/people/employees/{id}`)
- Clicking employee name link → same
- Checkbox: row-level multi-select for bulk actions (bulk delete, bulk export expected)

### Incomplete Employee Row State
- Work Email cell shows warning icon + "This employee's profile is incomplete. Complete now"
- "Complete now" is a link (`href="#"` — JS-handled). Navigates back into wizard at incomplete step.
- No Department or Employee Status shown for incomplete employees (cells are empty/replaced by warning)

## Employee Status Values (Observed + Inferred)
| Status | Badge Color | Visible in |
|---|---|---|
| Active | Green | Active Employees view |
| On Notice Period | Amber (inferred) | All Employees view |
| Exited | Grey/Red (inferred) | Exited Employees view |
| Inactive | Grey (inferred) | All Employees view |

EMP001 and EMP002 show "Active". EMP003, EMP004, EMP005 show no status badge (incomplete profile — status not yet assigned).

## Sort Behavior
- Only "Employee Name" column has a sort button visible (with up/down arrow icon)
- Other columns are not sortable from the column header
- Default sort: by Employee Name (alphabetical ascending? or by creation order — not confirmed)

## Business Rules Observed
1. **Incomplete employee profiles are visible** in the list — they are not hidden or in draft state. They appear alongside complete employees.
2. **"Incomplete Employees" is a pre-built view** — Zoho tracks completion state per employee and exposes it as a first-class filter.
3. **Work email is the identifier** shown in the Work Email column for complete employees; replaced by incomplete warning for incomplete employees.
4. **21 available columns** reveal the full employee entity attribute set: PAN, Aadhaar (not listed — not in Zoho Payroll), PF A/C, UAN, ESI Number, Prior Payroll Status, Onboarding Status, Portal Status.
5. **Aadhaar is absent** from column list — Zoho Payroll does not collect Aadhaar in the employee entity. Notable gap vs. some Indian HR systems.
6. **Prior Payroll Status** column name confirms Zoho tracks whether prior employer YTD has been entered per employee.
7. **Custom Views** allow admins to create and save filtered/sorted views with custom criteria — supports org-specific payroll workflows.
8. **Export Data** is available — important for audit and reconciliation purposes.

## Key Observations for Our Build
1. **Employee list columns** — our default view: Name, Work Email, Department, Status, DOJ. All 21 Zoho columns should be available as optional columns.
2. **Incomplete profile tracking** — `profile_complete` boolean on Employee entity. Show banner count + per-row indicator. "Complete now" routes back to wizard at the first incomplete step.
3. **Pre-built views** — implement as named filters: Active, Exited, Incomplete, Portal Enabled/Disabled. Store as FilterPreset entity with `is_system` flag to prevent deletion.
4. **Custom Views** — `FilterPreset` entity with user-defined criteria. Shareable within org. Favoriteable per admin user.
5. **No Aadhaar in Zoho** — we may need to add Aadhaar field (masked) if required for compliance; it is not part of Zoho's employee model.
6. **UAN tracking** — UAN is PF-related. Our employee entity must have `uan` field even if PF is not yet enabled at org level.
7. **Import/Export** — bulk import is documented in item 51. Export = download employee list (CSV/XLSX) for reconciliation.

## Column Details Cross-Reference
| Column | Entity Attribute | Data Type | Sensitivity |
|---|---|---|---|
| PAN | `pan` | String (AAAA0000A format) | High — mask in list, reveal on detail |
| PF A/C Number | `pf_account_number` | String | Medium |
| UAN | `uan` | String (12 digits) | Medium |
| ESI Number | `esi_number` | String | Medium |
| Prior Payroll Status | `prior_payroll_entered` | Boolean or Enum | Low |
| Onboarding Status | `onboarding_status` | Enum | Low |
| Portal Status | `portal_status` | Enum | Low |

## Navigation From This Page
- → Employee Profile: click row or name link
- → Add Employee Wizard: "Add" button
- → Import Flow: "Import Data" from dropdown
- → Custom View Builder: "New Custom View" from filter dropdown
- ← From: Left sidebar "Employees" link; breadcrumb

## Screenshots
- No screenshots captured for this item (see item 34 for empty state screenshot reference)
