# UF-06: Edit Salary Component — Immutability Constraints

**Module:** Settings → Setup & Configurations → Salary Components → Earnings
**Tested:** 2026-05-16
**Components Examined:** Special Allowance (custom, not yet associated), Basic (system, associated with employees)

## Steps Executed
1. Navigated to `#/settings/salary-components/earnings`
2. Clicked "Show dropdown menu" on Special Allowance row → selected "Edit"
3. Observed edit form for non-associated custom component
4. Navigated to Basic component edit page directly
5. Compared field states between associated vs non-associated component

## Dropdown Menu Options (per row)

All earnings components (both system and custom) show the same three options:
| Option | Type | Notes |
|--------|------|-------|
| Edit | Link | Navigates to edit form |
| Mark as Inactive | Button | Toggles active status |
| Delete | Button | Shows confirmation dialog |

No distinction in available actions between system components and custom components at the list level.

## Edit Form — Non-Associated Component (Special Allowance)

**URL:** `#/settings/salary-components/earnings/{id}`
**Page title:** "Edit Earning"

### Field States

| Field | Editable | Value | Notes |
|-------|----------|-------|-------|
| Earning Type | **Disabled** | Custom Allowance | Locked from creation — cannot change earning type |
| Earning Name | Editable | Special Allowance | Can be renamed freely |
| Name in Payslip | Editable | Special Allowance | Can be renamed freely |
| Pay Type (Fixed/Variable) | Editable | Fixed Pay | Radio buttons active |
| Calculation Type (Flat/Percentage) | Editable | Flat Amount | Radio buttons active |
| Enter Amount | Editable | 0 | Spinbutton; amount can be changed |
| Mark this as Active | Editable | Checked | Checkbox active |
| Calculate on pro-rata basis | Editable | Checked | Checkbox active |
| Consider for EPF Contribution | Editable | Unchecked | Checkbox active |
| Consider for ESI Contribution | Editable | Checked | Checkbox active |
| Make this earning a part of salary structure | **Disabled** | Checked | Always locked |
| This is a taxable earning | **Disabled** | Checked | Locked per earning type |
| Show this component in payslip | **Disabled** | Checked | Always locked |

**Immutability note (non-associated):**
> "Note: Once you associate this component with an employee, you will only be able to edit the Name and Amount/Percentage. The changes you make to Amount/Percentage will apply only to new employees."

## Edit Form — Associated Component (Basic)

**URL:** `#/settings/salary-components/earnings/3848927000000032463`
**Page title:** "Edit Earning"

### Field States

| Field | Editable | Value | Notes |
|-------|----------|-------|-------|
| Earning Type | **Disabled** | Basic | Locked — type tooltip shown: "Fixed amount paid at the end of every month." |
| Earning Name | Editable | Basic | Can be renamed |
| Name in Payslip | Editable | Basic | Can be renamed |
| Pay Type | **NOT SHOWN** | — | Pay Type radio buttons absent for associated components |
| Calculation Type | **Disabled** | Percentage of CTC (selected) | Radio buttons shown but disabled |
| Enter Percentage | Editable | 50.00 | Spinbutton still editable |
| Mark this as Active | Editable | Checked | Checkbox active |
| Calculate on pro-rata basis | **Disabled** | Checked | Locked once associated |
| Consider for EPF Contribution | **Disabled** | Checked (Always) | Locked once associated |
| EPF sub-radio (Always / PF Wage < ₹15k) | **Disabled** | Always | Both options disabled |
| Consider for ESI Contribution | **Disabled** | Checked | Locked once associated |
| Make this earning a part of salary structure | **Disabled** | Checked | Always locked |
| This is a taxable earning | **Disabled** | Checked | Locked per type |
| Show this component in payslip | **Disabled** | Checked | Always locked |

**Immutability note (associated):**
> "Note: As you've already associated this component with one or more employees, you can only edit the Name and Amount/Percentage. The changes made to Amount/Percentage will apply only to new employees."

**Key difference from non-associated version:** "Once you associate" vs "As you've already associated"

## Immutability Comparison Summary

| Field | Non-Associated | Associated |
|-------|---------------|-----------|
| Earning Type | Disabled (always) | Disabled (always) |
| Earning Name | **Editable** | **Editable** |
| Name in Payslip | **Editable** | **Editable** |
| Pay Type | Editable | Not shown |
| Calculation Type | Editable | **Disabled** |
| Amount / Percentage | Editable | **Editable** (but applies only to new employees) |
| Mark as Active | Editable | Editable |
| Pro-rata basis | Editable | **Disabled** |
| Consider for EPF | Editable | **Disabled** |
| Consider for ESI | Editable | **Disabled** |

## Business Rules

1. **Earning Type is always immutable** — cannot be changed after creation. This is enforced even before employee association.

2. **Three-field editable** for associated components — only Name (display), Name in Payslip, and Amount/Percentage remain editable once an employee is associated.

3. **Amount/Percentage changes apply only to new employees** — existing employees with this component are NOT retroactively updated. Each employee's salary structure holds a snapshot of the component's values at time of assignment.

4. **Calculate on pro-rata** locks once associated — cannot change whether a component is pro-rated after it's been in use.

5. **EPF/ESI flags lock once associated** — ensures statutory compliance consistency — changing EPF flag mid-association could cause incorrect PF wage calculations.

6. **System tooltip on Earning Type** — for Basic specifically, a tooltip/badge shows: "Fixed amount paid at the end of every month." This is the Zoho system description, not user-entered.

## Navigation
- Entry: Earnings list → row "Show dropdown menu" → Edit
- URL: `#/settings/salary-components/earnings/{component-id}`
- Post-save: returns to earnings list
- Cancel link: `#/settings/salary-components/earnings`

## Screenshots
- [Edit Special Allowance — non-associated](../screenshots/UF-06-edit-special-allowance.png)
- [Edit Basic — associated with employees](../screenshots/UF-06-edit-basic-associated.png)

## Gaps / Observations
- 🔴 Amount/Percentage changes for associated components only apply to NEW employees — existing employees retain old values. System does not show a warning about this in the edit form itself (only in the immutability note). Admins may not realize existing employees are unaffected.
- No "Preview impact" or "Affected employees count" shown before saving an amount change
- No bulk update mechanism to apply new percentage to existing employees
- "Mark as Inactive" from dropdown not tested — behavior when deactivating a component in use by employees not documented
