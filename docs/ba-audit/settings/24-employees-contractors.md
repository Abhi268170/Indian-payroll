# Settings > Module Settings > Employees & Contractors

## URL
`#/settings/employee/contractor` (default sub-tab)

## Sub-tabs (within Employees & Contractors)

| Tab | URL |
|-----|-----|
| Contractor | `#/settings/employee/contractor` |
| Custom Field | `#/settings/employee/custom-field/list` |
| Custom Button | `#/settings/employee/custom-button/list` |
| Validation Rules | `#/settings/employee/field-validations` |
| Record Locking | `#/settings/employee/record-locking` |
| Related List | `#/settings/employee/related-list` |

## Purpose
Configuration for the Employees & Contractors module: enabling the contractor sub-module, adding custom fields to the employee record, setting validation rules, configuring record locking, and related list views.

---

## Tab 1: Contractor (`#/settings/employee/contractor`)

### Current State
Contractor module is NOT enabled.

### Enable Prompt
**Heading:** "Get your contractors onboard"
**Description:** "Capture all necessary details about your contractors and manage their compensation in this module."
**Button:** "Enable Contractors Module"

### Contractor Module Impacts (when enabled):
| Area | Impact |
|------|--------|
| Roles & Permissions | Employee permissions apply to contractors (only relevant permissions) |
| Workflows | Existing Workflow Rules apply to contractors |
| Documents | Org-folder documents for all employees shown to contractors |

---

## Tab 2: Custom Field (`#/settings/employee/custom-field/list`)

### Custom Fields Usage Indicator
`Custom Fields Usage: 0/59` — up to 59 custom fields can be created per employee entity.

### Empty State
"You haven't created any custom fields yet."

### Actions
| Button | Action |
|--------|--------|
| Create New | Opens New Custom Field form at `#/settings/employee/custom-field/new?entity=employee` |
| Create Custom Field | Same (in empty state body) |

---

### New Custom Field Form (`/custom-field/new?entity=employee`)

#### Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Field Name | Text | Yes* | Display name of the custom field |
| Data Type | Dropdown | Yes* | Determines field type and storage |

#### Data Type Options (18 types)
| Data Type | Description |
|-----------|-------------|
| Text Box (Single Line) | Short text input |
| Email | Email address input with format validation |
| URL | URL input with format validation |
| Phone | Phone number input |
| Number | Integer number |
| Decimal | Decimal number |
| Amount | Monetary amount (₹) |
| Percent | Percentage value (%) |
| Date | Date picker |
| Date and Time | Date + time picker |
| Check Box | Boolean checkbox |
| Auto-Generate Number | System auto-increments a number (e.g., employee sequence IDs) |
| Dropdown | Single-select from admin-defined options |
| Multi-select | Multi-select from admin-defined options |
| Lookup | Reference to another record (e.g., employee, department) |
| Text Box (Multi-line) | Large text area |
| Attachment | File upload |
| Formula | Calculated field using a formula |

#### Buttons
| Button | State | Action |
|--------|-------|--------|
| Save | Disabled until Name + Data Type filled | Saves the custom field |
| Cancel | Always enabled | Returns to list |

---

## Tab 3: Custom Button (not deeply explored)
URL: `#/settings/employee/custom-button/list`
Purpose: Create custom action buttons on the employee record that trigger Deluge functions or external actions.

---

## Tab 4: Validation Rules (not deeply explored)
URL: `#/settings/employee/field-validations`
Purpose: Define field-level validation rules (e.g., "Employee ID must start with EMP") applied when creating/editing employees.

---

## Tab 5: Record Locking (not deeply explored)
URL: `#/settings/employee/record-locking`
Purpose: Configure conditions under which employee records are locked from editing (e.g., after payroll is finalised for the month).

---

## Tab 6: Related List (not deeply explored)
URL: `#/settings/employee/related-list`
Purpose: Configure which related entity lists appear on the employee detail page (e.g., Loans, Claims, Pay Runs).

---

## Business Rules

1. **Custom fields limit: 59 per entity** — Hard cap on custom employee fields.
2. **Contractor module is disabled by default** — requires explicit opt-in. Cannot be individually disabled per employee type after enabling (applies org-wide).
3. **Contractor permissions inherit from employees** — no separate permission matrix for contractors; same role structure applies.
4. **Formula fields** — can compute derived values from other fields (e.g., years_of_service = today - date_of_joining).
5. **Auto-Generate Number** — sequential numbering for custom identifiers.

## Cross-Module Impact
| Feature | Impacts |
|---------|---------|
| Custom Fields | Appear on Employee profile form, can be used in Workflow Rules conditions |
| Contractor Module enabled | Contractor entity appears in Employees list; separate management screens |
| Validation Rules | Applied on employee create/edit before save |
| Record Locking | Prevents edits to locked employee records (compliance/audit control) |

## Observations & Notes
1. **18 custom field types** — very flexible. Especially notable: Formula (computed fields) and Lookup (cross-entity references).
2. **Amount type** — ₹ prefixed custom amount fields for tracking custom compensation elements.
3. **Custom Button** — allows attaching Deluge-based actions to the employee record (e.g., "Generate Offer Letter" button).
4. **Contractor as separate entity** — Zoho treats contractors differently from regular employees; different compensation management but shared infrastructure.
5. For our build: Employee entity custom fields as JSONB column (flexible schema) or as a typed CustomField entity table. Max 59 fields is arbitrary — we may set 50. Contractor type is a useful distinction: EmployeeType enum: Regular/Contractor.

## Screenshots
`docs/ba-audit/settings/screenshots/24-employees-contractors.png`
