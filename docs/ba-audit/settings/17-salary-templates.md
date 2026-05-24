# Settings > Salary Templates

## URL
`#/settings/salary-templates`

New template form: `#/settings/salary-templates/new`

## Purpose
Define reusable salary structure templates that can be assigned to employees. Each template specifies the CTC breakdown — which salary components are included, their calculation method (% of CTC, flat amount, etc.), and the resulting monthly/annual amounts. Templates reduce data entry when onboarding employees with similar salary structures.

## Page Layout

### List Page (empty state)
- **Empty state message:** "You haven't created any salary templates yet. Create salary templates for commonly used salary structures and assign them to employees."
- **Primary CTA:** "Create Salary Template" button
- **Feature highlights (3 cards):**
  - Design — "Design multiple salary structures for each designation"
  - Duplicate — "Clone a template and modify it to create a new template"
  - Save Time — "Associate predefined salary templates to complete employee profiles quickly"

---

## Create Salary Template Form (`/new`)

### Layout
Split-panel:
- **Left panel (sidebar):** Component picker — 5 collapsible sections of available components to add
- **Right panel (main):** Template form — name, description, CTC input, salary component table

---

### Right Panel: Template Form Fields

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Template Name | Text | Yes* | (blank) | Display name for the template |
| Description | Textarea | No | (blank) | Max 500 characters |
| Annual CTC | Number (₹) | No | 0 | CTC in rupees per year; drives % of CTC calculations |

*Required indicated by asterisk

### Salary Components Table

**Columns:** Salary Components | Calculation Type | Monthly Amount | Annual Amount | (Delete)

**Default components (always present in new template):**

| Component | Calculation Type | Monthly Amount | Annual Amount | Editable? |
|-----------|-----------------|----------------|---------------|-----------|
| Basic | % of CTC (default 50.00%) | Auto-calculated | Auto-calculated | Yes — % field |
| Fixed Allowance | Monthly CTC - Sum of all other components (residual) | Auto-calculated | Auto-calculated | No (residual) |

**Footer row:**
| Label | Monthly | Annual |
|-------|---------|--------|
| Cost to Company | ₹0 (until CTC entered) | ₹0 |

**Fixed Allowance tooltip/note:** "Monthly CTC - Sum of all other components" — this is the balancing/residual component; it absorbs any CTC not allocated to specific components.

---

### Left Panel: Component Picker (5 Sections)

Components listed here can be clicked/dragged to add to the template.

| Section | Available Components (active, not yet in template) |
|---------|--------------------------------------------------|
| EARNINGS | House Rent Allowance, Conveyance Allowance (Basic and Fixed Allowance already included by default) |
| REIMBURSEMENTS | (empty — no active reimbursements configured yet) |
| FBP COMPONENTS | (empty — no active FBP components configured yet) |
| BENEFITS | (empty — VPF is inactive) |
| ONE TIME EARNINGS | (empty — no one-time variable earnings configured as template components) |

**Note:** The left panel only shows active components not yet added to the template. As components are added to the right table, they disappear from the left panel.

---

### Buttons

| Button | Location | Action |
|--------|----------|--------|
| Create Salary Template | List page | Opens new template form |
| Save | Form footer | Saves the template |
| Cancel | Form footer | Cancels and returns to list |
| Close (×) | Form header | Returns to list without saving |

---

## Business Rules

1. **Basic and Fixed Allowance are mandatory** — they appear in every template by default and cannot be removed.
2. **Fixed Allowance is residual** — it is automatically set to (Monthly CTC - sum of all other components). It cannot have a manually entered amount.
3. **CTC drives calculations** — entering Annual CTC auto-calculates Monthly CTC; all % of CTC components update automatically.
4. **Active components only** — only active salary components from Settings > Salary Components appear in the left picker.
5. **Template immutability after assignment** — once a template is assigned to an employee, changes to the template do NOT retroactively affect that employee's salary (similar to the salary component immutability rule).
6. **Duplicate feature** — templates can be cloned (available on existing templates via More Actions) to create variations.

## State Machine (after templates exist)
| State | Description |
|-------|-------------|
| No templates | Empty state |
| Template created | Listed in table with Edit/Duplicate/Delete actions |
| Template assigned to employee | Cannot be deleted; can be edited with restricted fields |

## Cross-Module Impact

| Template Setting | Impacts |
|-----------------|---------|
| Template CTC | Starting point for employee salary negotiation |
| Components included | Determines which components appear on employee's payslip |
| EPF/ESI treatment | Inherited from each component's settings in Salary Components |
| Tax treatment | Inherited from each component's taxable/non-taxable flag |

## Observations & Notes

1. **Two-panel builder UX** — visual drag-and-add pattern for building salary structures; reminiscent of Keka and Darwinbox salary builder.
2. **Fixed Allowance = balancing component** — critical design decision; ensures the sum always equals CTC. Any unallocated CTC goes to Fixed Allowance.
3. **Basic defaults to 50% of CTC** — this is the industry standard; also aligns with PF wage calculation (PF on Basic if Basic = 50% of CTC).
4. **No gratuity, bonus in template** — those are variable earnings and don't appear in the fixed-pay template structure.
5. For our build: SalaryTemplate entity needs: template_name, description, annual_ctc, and a list of SalaryTemplateComponents (component_id, calculation_type, percentage_or_amount). Fixed Allowance component is always present as the residual; Basic defaults to 50%.

## Screenshots
- `docs/ba-audit/settings/screenshots/17-salary-templates-new.png`
