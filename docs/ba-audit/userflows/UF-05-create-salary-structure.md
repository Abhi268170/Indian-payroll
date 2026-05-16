# UF-05: Create Salary Structure (Standard-Exec)

**Module:** Settings → Customisations → Salary Templates
**Tested:** 2026-05-16
**Mock Data Used:** Annual CTC ₹12,00,000, components: Basic + HRA + Special Allowance + Fixed Allowance

## Steps Executed
1. Navigated to `#/settings/salary-templates`
2. Observed empty state with "Create Salary Template" button and feature marketing panel
3. Clicked "Create Salary Template" → navigated to `#/settings/salary-templates/new`
4. Observed two-panel layout: left = component picker, right = template editor
5. Filled Template Name: "Standard-Exec"
6. Clicked "Earnings" accordion in left panel → expanded to show addable components
7. Clicked "House Rent Allowance" → added to template table
8. Clicked "Special Allowance" → added to template table
9. Entered Annual CTC: ₹12,00,000 → live calculation updated all rows
10. Saved → redirected to salary templates list showing "Standard-Exec" as Active

## Fields & Validations

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Template Name | Text | Yes | — | Identifies the template |
| Description | Textarea | No | — | Max 500 Characters |
| Annual CTC | Number (₹/year) | No | 0 | Live calculation triggers on change |

## Template Table Columns
| Column | Type | Editable |
|--------|------|----------|
| Salary Components | Text | No |
| Calculation Type | Text or formula display | Partially |
| Monthly Amount | Number or auto-calculated | Depends on component |
| Annual Amount | Auto-calculated | No |

## Component Picker (Left Panel)

**Accordion sections:**
1. Earnings — lists all active earnings not already in template
2. Reimbursements
3. FBP Components
4. Benefits
5. One Time Earnings

Each component in the list has a "+" icon to add it to the template. Once added, the component disappears from the picker list.

## Template Structure Created (Standard-Exec)

With Annual CTC = ₹12,00,000:

| Component | Calculation Type | Monthly | Annual |
|-----------|-----------------|---------|--------|
| Basic | 50.00% of CTC | ₹50,000 | ₹6,00,000 |
| Fixed Allowance | Monthly CTC − Sum of all other components | ₹25,000 | ₹3,00,000 |
| House Rent Allowance | 50.00% of Basic | ₹25,000 | ₹3,00,000 |
| Special Allowance | Fixed amount ₹0 | ₹0 | ₹0 |
| **Cost to Company** | | **₹1,00,000** | **₹12,00,000** |

## Fixed Allowance Mechanism (Key Business Rule)

**Fixed Allowance** is a special system component with calculation type: "Monthly CTC − Sum of all other components"

This means:
- It acts as a **residual absorber** — whatever is left after all other components are summed is assigned to Fixed Allowance
- It guarantees the salary structure always sums to exactly 100% of CTC
- Fixed Allowance cannot be removed from the template (it is always present)
- The description text under Fixed Allowance: "Monthly CTC - Sum of all other components"

When Special Allowance has ₹0 fixed amount, Fixed Allowance absorbs the full residual. To use Special Allowance as a residual, the admin would set a fixed amount and the Fixed Allowance would reduce accordingly.

**Calculation verification for CTC ₹12,00,000:**
- Basic = 50% × 1,00,000 = ₹50,000/month
- HRA = 50% × 50,000 = ₹25,000/month
- Special Allowance = ₹0 (fixed)
- Fixed Allowance = 1,00,000 − 50,000 − 25,000 − 0 = ₹25,000/month
- Total = ₹1,00,000/month = ₹12,00,000/year ✓

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Create Salary Template | Button | On list page | Navigates to new template form |
| Save | Button | Template name filled | Saves template; redirects to list |
| Cancel | Button | Any time | Returns to list; prompts "unsaved changes" dialog if data entered |
| Edit | Via list "Show dropdown menu" | Template exists | Not explored |
| Duplicate | Via list "Show dropdown menu" | Template exists | Not explored |

## Template List View

| Column | Notes |
|--------|-------|
| Template Name | Linked to detail page |
| Description | Shows "−" if empty |
| Status | Active / Inactive |
| More Actions | Dropdown (Edit, Duplicate, Delete suspected) |

After creating Standard-Exec:
- Template Name: Standard-Exec
- Description: −
- Status: Active

## Navigation
- URL: `#/settings/salary-templates`
- Create: `#/settings/salary-templates/new`
- View/Edit: `#/settings/salary-templates/3848927000000034363`

## Empty State
- Image + text: "You haven't created any salary templates yet."
- Sub-text: "Create salary templates for commonly used salary structures and assign them to employees."
- Feature marketing: Design (multiple structures per designation), Duplicate (clone template), Save Time (quick assignment)

## Unsaved Changes Guard
When navigating away from the new template form with unsaved data:
- Dialog: "You might have some unsaved changes. Are you sure you want to leave this page?"
- Buttons: "Stay on this page" (primary/active), "Leave this page"

## Cross-Module Effects
- Salary templates are assigned to employees during the "Salary Details" step of Add Employee wizard
- When assigned, the template's component structure is copied to the employee's salary record
- Subsequent edits to the template do NOT auto-update existing employee assignments (changes are non-retroactive)

## Gaps / Observations
- "Salary Template" is the Zoho term — equivalent to "Salary Structure" in other systems
- No validation observed for components summing > 100% of CTC (Fixed Allowance prevents this by being residual)
- Special Allowance added at ₹0 — to set a meaningful amount, admin must set it at employee assignment time
- HRA calculation type in template shows "50.00% of Basic" — percentage is editable in the template (spinbutton e2503)
- Basic percentage is also editable (spinbutton e2442, default 50.00% of CTC)

## Screenshots
- [Salary templates empty state](../screenshots/UF-05-salary-templates-empty.png)
- [New template form with components](../screenshots/UF-05-salary-template-new.png)
- [Template preview with CTC ₹12,00,000](../screenshots/UF-05-salary-template-preview.png)
