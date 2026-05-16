# Form 16 > List View

## URL / Navigation Path
- Taxes & Forms > Form 16: `#/taxes-and-forms/form16`
- With FY filter: `#/taxes-and-forms/form16?fiscal_year=2026`

## Purpose
Central hub for managing Form 16 generation, status tracking, and distribution across all employees for a selected financial year.

## Current State in Test Org
The Form 16 list page is in a **pre-generation instructional state** because:
1. Tax Deductor is not configured (hard gate)
2. Form 24Q has not been filed (soft gate — Part A not available on TRACES)

The page shows the instructional flow rather than an employee list table.

## Page Layout

### Header
| Element | Notes |
|---------|-------|
| Page title: "Form 16" | Heading level 3 |
| Financial Year dropdown | Combobox labelled "Select Financial Year"; defaults to current FY (e.g., "Select" when none chosen, then defaults once FY is determined) |
| Instant Helper button | Context-sensitive help |

### Main Content (Current: Pre-generation state)

**Section 1: "It's time to generate Form 16 for the financial year"**
- Sub-heading: "Verify your tax deductor"
- Gate indicator: "Tax Deductor is not found" with "Add Tax Deductor" → `#/settings/taxes`
- Note: "Remember that once you generate Form 16, you cannot change the deductor details."

**Section 2: "How to generate Form 16 for your employees?"**
- Sub-heading: "Steps to generate Form 16"
- Help Guide link: `https://www.zoho.com/in/payroll/help/employer/taxes-forms/form-16.html`
- 4-step visual flow:
  1. Upload Form 16 Part A
  2. Generate Form 16
  3. Sign Form 16
  4. Publish/Email
- "Learn how to generate Form 16" button (likely opens video or walkthrough)

## Expected List View (when configured and Form 16s generated)

Based on Zoho's standard patterns across other list views, the Form 16 list would display:

| Column | Type | Notes |
|--------|------|-------|
| Employee Name | Text | Link to employee profile |
| Employee ID | Text | (e.g., EMP001) |
| PAN | Text (masked?) | Employee PAN for Part A matching |
| Status | Badge | Not Generated / Part A Uploaded / Generated / Signed / Published / Emailed |
| Download | Action | Download individual Form 16 PDF |
| Email | Action | Email individual Form 16 to employee |

## Filters (Expected)
| Filter | Notes |
|--------|-------|
| Financial Year | Primary filter; set in heading dropdown |
| Employee | Search/select specific employee |
| Status | Not Generated / Generated / Signed / Published / Emailed |

## Empty States

| Scenario | Empty State Message |
|----------|-------------------|
| Tax Deductor not found | "Tax Deductor is not found — Add Tax Deductor" |
| No Form 16s generated for FY | Instructional 4-step flow shown |
| All employees generated | Table with status badges |

## Navigation From This Page
- "Add Tax Deductor" → `#/settings/taxes`
- "Help Guide" → External Zoho help URL
- Individual employee Form 16 → download or email action
- Upload Part A → in-page file upload component (appears after Tax Deductor is configured)

## Navigation To This Page
- Left sidebar: Taxes & Forms > Form 16
- Employee Profile > Payslips & Forms tab (employee's own Form 16 — read-only for employee)

## Key Observations for Our Build
1. **Financial Year is the primary scoping dimension** — our Form 16 list should be scoped to a selected FY.
2. **Status column is the key tracking mechanism** — build a proper state machine with these states.
3. **Deductor lock after generation** — display a clear warning before generation, then lock the deductor record.
4. **Employee PAN must be present** — before generating Form 16 for an employee, validate PAN is filled; block if missing.
5. **Bulk actions** (bulk generate, bulk email, bulk download ZIP) should be accessible from this list view.
6. **Per-employee individual actions** should be available via row-level kebab menu.

## Screenshots
- `screenshots/form16-landing.png` — Form 16 landing/list page (pre-generation state)
