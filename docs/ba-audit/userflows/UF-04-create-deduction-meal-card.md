# UF-04: Create Custom Deduction (Meal Card)

**Module:** Settings → Setup & Configurations → Salary Components → Deductions
**Tested:** 2026-05-16
**Mock Data Used:** Meal Card deduction component

## Steps Executed
1. Navigated to `#/settings/salary-components/deductions/new`
2. Observed the New Deduction form (simpler than Earnings form)
3. Filled Name in Payslip: "Meal Card"
4. Selected deduction frequency: Recurring
5. Checked "Mark this as Active"
6. Clicked "Save" → redirected to `#/settings/salary-components/deductions`

## Fields & Validations

| Field | Type | Required | Default | Options/Rules |
|-------|------|----------|---------|---------------|
| Name in Payslip | Text | Yes | — | Free text; this is the ONLY name field (no separate "Component Name") |
| Select the deduction frequency | Radio | Yes | One-time deduction | Two options (see below) |
| Mark this as Active | Checkbox | No | Unchecked | Activates for use in pay runs |

**Deduction Frequency Options:**
- "One-time deduction" — deducted in a single payroll run
- "Recurring deduction for subsequent Payrolls" — repeats each month

## Critical Observation: Deduction Form is Minimal

The deduction form has dramatically fewer fields than the Earnings form:
- **No** pre-tax / post-tax distinction (all deductions appear to be post-tax)
- **No** EPF/ESI impact settings
- **No** calculation type (Flat Amount / Percentage)
- **No** taxability setting
- Only name, frequency, and active status

The amount is set at the employee level when the deduction is assigned, not at the component definition level.

**Immutability note displayed:** "Once you associate this benefits with an employee, you will only be able to edit the Name in Payslip. The change will be reflected in both new and existing employees."
(Note: The UI says "benefits" instead of "deduction" — likely a copy/paste error in Zoho's UI)

## Deductions List (observed after creation)
After save, the deductions tab shows the Meal Card component. Pre-existing deductions were not enumerated in this session.

## Navigation
- URL pattern for list: `#/settings/salary-components/deductions`
- URL pattern for new: `#/settings/salary-components/deductions/new`

## UI Patterns
- "Cancel" link returns to `#/settings/salary-components/deductions`
- "* indicates mandatory fields" shown below action buttons
- The form has very minimal layout (3 fields only)

## Gaps / Observations
- No pre-tax deduction configuration — this is a significant gap for Indian payroll where items like NPS, professional tax, VPF can be pre-tax
- The label says "benefits" in immutability note — UI copy error
- No way to set a default amount at component level — must be set per employee per pay run
- No "Consider for EPF/ESI" flag — deductions presumably do not affect PF/ESI wage computation (correct per statute for most deductions)

## Screenshots
- [New deduction form](../screenshots/UF-04-deduction-form.png)
