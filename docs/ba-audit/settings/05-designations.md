# Settings > Designations

## URL
`#/settings/designations`

Sub-routes:
- `#/settings/designations?add_designation=true` — New Designation modal
- `#/settings/designations/import` — Import designations from file (inferred from Departments pattern)

## Purpose
Defines employee job title/designation taxonomy used on employee profiles, payslips, and payroll reports. Designations enable designation-wise payroll reporting and streamline HR processes like salary revisions and exit management.

## Page Layout
**Empty state** (no designations exist):
- Illustration graphic + headline: "Track employee job titles with designations"
- Subtext: "Create designation based on the once present in the organization and associate with employees"
- Two action buttons: "New Designation" + "Import"
- Feature callouts: "Generate reports that break down payroll data by designation" | "Carry out tasks such as salary revisions and exit processes easily"

**Populated state**: Table/list of designation records (not observed — no data exists).

## Fields

### New Designation Modal
| Field | Type | Required | Default | Options / Format | Notes |
|-------|------|----------|---------|------------------|-------|
| Designation Name | Text | Yes | (blank) | Free text | Job title; e.g., "Software Engineer", "HR Manager" |

**Notable contrast with Departments**: Designation has only ONE field (Name). No code or description fields. Simpler entity.

### Import Page (inferred — same pattern as Departments)
| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| File upload | Drag-and-drop / click-to-upload | Yes | — | CSV, TSV, or XLS; max 5 MB |
| Character Encoding | Dropdown | Yes | UTF-8 (Unicode) | File character encoding |

## Buttons & Actions

### Empty State Page
| Button | Label | State | Action |
|--------|-------|-------|--------|
| New Designation (header) | "New Designation" | Always enabled | Opens "New Designation" modal |
| Sort/Filter | (icon only) | Always enabled | Sort/filter list |
| New Designation (body) | "New Designation" | Always enabled | Same — opens modal |
| Import | "Import" | Always enabled | Navigates to import page |

### New Designation Modal
| Button | Label | State | Action |
|--------|-------|-------|--------|
| Save | "Save" | Enabled when Name filled | Creates designation record |
| Cancel | "Cancel" | Always enabled | Closes modal without saving |

## Tabs (if any)
None.

## Conditional Logic
1. **Empty state vs populated state** — same pattern as Departments.
2. **Save button** — requires Designation Name to be filled.

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| Designation | Assigned to each employee record; printed on payslips |
| Designation | Filter/grouping dimension in payroll reports |
| Designation | Referenced in salary revision workflows (designation-based salary bands in some configurations) |
| Designation | Used in exit process flows to track position vacated |

## Observations & Notes
1. **Designation is simpler than Department** — only Name, no Code or Description. This may limit reporting granularity but keeps the entity clean.
2. **No salary band or grade linkage** — designations are purely labels here; no pay-grade matrix attached. This is in contrast to products like GreytHR or Keka which link designations to salary bands.
3. **Import available** — bulk import of designations via CSV/XLS, consistent with Departments pattern.
4. **Feature callout**: "Carry out tasks such as salary revisions and exit processes easily" — confirms Designation is used as a trigger/filter in salary revision and exit workflows.
5. Same **typo** as Departments: "based on the **once** present in the organization."
6. For our implementation: Designation should be a simple name-only entity assigned to employees. If salary bands are needed, that's a separate Salary Grade entity.

## Screenshots
- `docs/ba-audit/settings/screenshots/05-designations-empty.png` — empty state
