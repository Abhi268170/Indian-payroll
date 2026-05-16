# Settings > Reporting Tags

## URL
`#/settings/advanced-reportingtags`

New tag wizard: `#/settings/advanced-reportingtags/configure-tag`

## Purpose
Reporting Tags allow organisations to create custom classification dimensions for employees and payroll data. Tags enable payroll segregation and analysis by custom groups (e.g., business unit, project, cost centre) beyond the built-in Department and Designation fields. Cross-applies to all Zoho Finance apps when org is shared.

## Page Layout

### List Page (empty state)
- **Empty state message:** "Configure tags. Use tags to segregate, visualize, and analyze organizational data like an information system to enable improved, data-backed decision making in business payroll management."
- **Primary CTA:** "New Tag" link/button → navigates to wizard

---

## New Tag Wizard (`/configure-tag`)

### 3-Step Wizard Navigation
| Step | Label |
|------|-------|
| 1 | Create Reporting Tag |
| 2 | Configure Options |
| 3 | Configure Visibility Conditions |

---

### Step 1: Create Reporting Tag

#### Fields

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Name | Text | Yes* | (blank) | Display name for the reporting tag |
| Description | Text | No | (blank) | Optional description of what this tag represents |
| Associate This Reporting Tag To | Checkbox (disabled) | N/A | Employees (checked, disabled) | In Zoho Payroll context, tags are always associated to Employees; the checkbox is locked |
| Make this Reporting Tag mandatory | Checkbox | No | Unchecked | If checked, this tag field becomes mandatory when creating/editing an employee |

#### Association Notice
> "If the same organization exists across multiple Zoho Finance apps, this preference will be applied universally. However, you can manually update it later within any specific app if needed."

#### Mandatory Configuration Note
> "Requires users to provide input for the reporting tag field. However, it will be skipped for auto-created transactions and in certain apps where this field is not exposed to users."

#### Buttons
| Button | Action |
|--------|--------|
| Save & Proceed | Saves Step 1 and advances to Step 2 (Configure Options) |
| Cancel | Returns to Reporting Tags list |

*Mandatory fields marked with asterisk.

---

### Step 2: Configure Options (not yet visited — wizard flow)

Expected: Add the tag values/options (e.g., for a "Business Unit" tag: Engineering, Marketing, Sales).

---

### Step 3: Configure Visibility Conditions (not yet visited)

Expected: Define when/where the tag field is shown (e.g., only show for certain employee types or modules).

---

## Business Rules

1. **Tags are always associated to Employees** in Zoho Payroll — the association checkbox is disabled/locked.
2. **Cross-app application** — if the org is connected to Zoho Books/Zoho Expense etc., this reporting tag appears in those apps too.
3. **Mandatory flag** — makes the tag a required field during employee creation/update workflows.
4. **3-step wizard** — tag creation is structured: first define the tag, then its options, then visibility conditions.
5. **Custom dimensions** — tags extend the standard Dept/Designation classification with arbitrary user-defined axes.

## Cross-Module Impact

| Tag Config | Impacts |
|-----------|---------|
| Reporting Tag created | Appears as a field in Employee profile |
| Mandatory tag | Blocks employee creation if not filled |
| Tag values | Used as filter dimensions in Reports |
| Cross-app sharing | Same tag appears in Zoho Books, Zoho Expense etc. |

## Observations & Notes

1. **Custom cost centre support** — Reporting Tags are the mechanism for allocating payroll costs to cost centres/projects. Critical for multi-department organisations.
2. **Currently no tags configured** — empty state for "lerno" org.
3. **3-step wizard with conditional visibility** — step 3 suggests tags can be conditionally shown (e.g., only when employee is in a certain department).
4. **Shared with Zoho Finance apps** — this is a Zoho ecosystem integration point; tags defined here appear in Books for accounting entries.
5. For our build: Custom employee classification fields (beyond Dept/Designation) as a tenant-configurable entity. Each tag has: name, options list, mandatory flag, visibility rules. Could be modelled as a custom field entity with type=dropdown.

## Screenshots
`docs/ba-audit/settings/screenshots/19-reporting-tags.png`
