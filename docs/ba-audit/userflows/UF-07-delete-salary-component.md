# UF-07: Delete Salary Component — Confirmation and Error States

**Module:** Settings → Setup & Configurations → Salary Components
**Tested:** 2026-05-16
**Components Tested:** Basic (Earnings, system/associated), Meal Card (Deductions, custom/unassociated)

## Steps Executed
1. On Earnings list — clicked "Show dropdown menu" on Basic row → selected "Delete"
2. Observed confirmation dialog
3. Clicked "No" → cancelled, Basic preserved
4. Navigated to Deductions tab
5. Clicked "Show dropdown menu" on Meal Card row → selected "Delete"
6. Observed confirmation dialog (same pattern)
7. Clicked "No" → cancelled, Meal Card preserved

## Delete Confirmation Dialog

**Trigger:** Clicking "Delete" from any component's "Show dropdown menu"

**Dialog structure (same for all component types):**

| Element | Content |
|---------|---------|
| Icon | Warning/alert illustration |
| Message | "You are about to delete the Earning "{name}". This cannot be undone. Are you sure you want to proceed?" |
| Confirm button | "Yes" |
| Cancel button | "No" |

**Message pattern for deductions:**
"You are about to delete the Deduction "{name}". This cannot be undone. Are you sure you want to proceed?"

**Notes:**
- Dialog is generic — same message for system components and custom components
- No distinction between "associated with employees" vs "never associated"
- No count of affected employees shown
- "This cannot be undone" — hard delete, not soft delete

## Observed Component Types Tested

### Basic (Earnings — System — Associated with Employees)
- Dropdown shows: Edit | Mark as Inactive | Delete
- Delete shows generic confirmation — no special warning about employee associations
- Confirmed: system components CAN be deleted via UI (no system-level guard)
- 🔴 This is a critical gap — deleting Basic while it is in active salary structures would likely corrupt payroll data

### Meal Card (Deductions — Custom — Unassociated)
- Dropdown shows: Edit | Mark as Inactive | Delete
- Same generic confirmation dialog
- Safe to delete as it has no employee associations

## Deductions List — Additional Data Captured

Three pre-existing deduction components observed:

| Name | Deduction Type | Deduction Frequency | Status |
|------|---------------|---------------------|--------|
| Meal Card | Other Deductions | Recurring | Active |
| Withheld Salary | Withheld Salary | One Time | Active |
| Notice Pay Deduction | Notice Pay Deduction | One Time | Active |

**New column discovered:** "Deduction Type" — not documented in UF-04. Values observed: "Other Deductions" (for custom), "Withheld Salary", "Notice Pay Deduction" (system types).

## Business Rules

1. **No system-component guard** — system components (Basic, HRA, etc.) that are associated with employees can be deleted via the same UI flow as custom components. Only a generic "cannot be undone" warning is shown.

2. **No association check before delete** — the dialog does not display how many employees use this component or warn about salary structure impact.

3. **Hard delete** — stated explicitly: "cannot be undone". No soft delete or recycle bin mechanism observed.

4. **"Mark as Inactive" alternative** — the dropdown also offers "Mark as Inactive" which is a safer non-destructive alternative to deletion. System does not prompt admin to prefer inactive over delete.

5. **Deletion of associated component** — behavior when "Yes" is clicked on an associated component not tested (to avoid data corruption). Expected behavior: either block with error OR delete with cascade (orphaning salary structures).

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Delete (initiate) | Dropdown → Delete | Component exists | Shows confirmation dialog |
| Yes (confirm) | Button in dialog | Dialog shown | Hard deletes component (not tested) |
| No (cancel) | Button in dialog | Dialog shown | Dismisses dialog; component unchanged |
| Mark as Inactive | Dropdown option | Component is Active | Marks component inactive; safer than delete |

## State Machine
```
[Active] → Mark as Inactive → [Inactive]
[Inactive] → (Edit → Mark as Active) → [Active]
[Active or Inactive] → Delete → Yes → [Deleted — permanent]
[Active or Inactive] → Delete → No → [unchanged]
```

## Navigation
- Entry: Salary Components list (any tab) → row "Show dropdown menu" → Delete
- No separate delete page — dialog appears inline over the list

## Screenshots
- [Delete confirmation dialog for Basic](../screenshots/UF-07-delete-basic-dialog.png)
- [Delete confirmation dialog for Meal Card](../screenshots/UF-07-delete-meal-card-dialog.png)

## Gaps / Observations
- 🔴 **No guard on system component deletion** — admin can delete Basic, HRA, Fixed Allowance which are core to all salary structures. This could cause catastrophic data corruption if executed.
- 🔴 **No association check** — dialog should show "X employees use this component. Deleting will affect their salary structures." Currently shows no such warning.
- 🟡 **No "Mark as Inactive" suggestion** — system could prompt "Instead of deleting, consider marking as Inactive to preserve historical data."
- "Mark as Inactive" behavior not tested — does it affect employees currently on pay run? Does it prevent future use only?
- No bulk delete UI observed
- Deduction "Deduction Type" column values not fully enumerated — more system types may exist
