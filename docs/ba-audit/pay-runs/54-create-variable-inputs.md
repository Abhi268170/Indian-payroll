# Pay Runs > Variable Inputs — LOP, Earnings, TDS Override, Import/Export

## URL / Navigation Path

`https://payroll.zoho.in/#/payruns/{id}/summary` (Draft state)

Employee split-panel opens by clicking any employee row. The variable inputs panel slides in from the right on the same page — it does NOT navigate to a new URL.

Navigation: Pay Runs > [period card] > Draft Summary > click employee row

## Purpose

Per-employee variable data entry for the current pay run. Handles non-salary items that vary month to month: LOP days, one-time earnings (bonus, commission, leave encashment), TDS amount override, reimbursements. Also provides bulk import/export for large teams.

## Fields

### Employee Split Panel (right panel, per employee)

| Field | Type | Required | Options | Behaviour |
|-------|------|----------|---------|-----------|
| Employee name + ID | Read-only display | N/A | — | Header of split panel |
| Salary Structure name | Read-only display | N/A | — | e.g., "Senior Engineer" |
| Basic | Read-only display | N/A | Decimal ₹ | Recomputed after LOP save |
| HRA | Read-only display | N/A | Decimal ₹ | Recomputed after LOP save |
| Fixed Allowance | Read-only display | N/A | Decimal ₹ | Recomputed after LOP save |
| Total Earnings | Read-only display | N/A | Decimal ₹ | Sum of all earning components |
| Taxes & Deductions | Read-only display | N/A | Decimal ₹ | TDS + PT + any deductions |
| Net Pay | Read-only display | N/A | Decimal ₹ | Total Earnings − Total Deductions |

### LOP (Loss of Pay) Section

Activated by clicking "Add LOP" inline action within the split panel.

| Field | Type | Required | Options | Behaviour |
|-------|------|----------|---------|-----------|
| LOP Days | Spinbutton (integer) | Yes (if section open) | 0 to Base Days − 1 | Triggers proration on save |
| Actual Payable Days | Read-only display | N/A | Computed: Base Days − LOP Days | Updates live as LOP Days changes |
| Base Days | Read-only display | N/A | e.g., "31" (May 2026) | From pay schedule |

**LOP save behaviour:** After saving, all salary components are recalculated using:
`Prorated Amount = (Base Days − LOP Days) / Base Days × Full Component Amount`

Example (EMP001, 2 LOP days, 31 base days):
- Basic: ₹40,000 × 29/31 = ₹37,417
- HRA: ₹16,000 × 29/31 = ₹14,967
- Fixed: ₹14,000 × 29/31 = ₹13,100

### Add Earning Section

| Field | Type | Required | Options | Behaviour |
|-------|------|----------|---------|-----------|
| Earning Type | Listbox / dropdown | Yes | Bonus, Commission, Leave Encashment | Listbox appears on "Add Earning" click |
| Amount | Number input | Yes | Decimal ₹, positive | Entered after selecting type |
| Description / Reason | Text input | Varies | Free text | May be optional per type |

One-time earnings do NOT affect LOP proration — they are additive.

### TDS Override Section

| Field | Type | Required | Options | Behaviour |
|-------|------|----------|---------|-----------|
| TDS Amount | Spinbutton (decimal ₹) | No | Any positive decimal | Overrides system-calculated TDS for this month |
| Reason | Text input | Yes (if overriding) | Free text | Mandatory justification field |
| Calculated Value | Read-only display | N/A | e.g., "Calculated Value: 0" | Shows system's computed TDS before override |

**TDS override scope:** Month-level only. Does not affect future months. Audit trail via Reason field.

## Buttons & Actions

| Action | Trigger | Pre-condition | Post-behaviour |
|--------|---------|---------------|----------------|
| Add LOP | Click in split panel | Draft state | Reveals LOP Days spinbutton + Actual Payable Days |
| Save (LOP) | Click Save within LOP section | LOP Days entered | API call; all components recalculate; split panel refreshes |
| Add Earning | Click "Add Earning" | Draft state | Shows listbox: Bonus / Commission / Leave Encashment |
| Save (Earning) | Click Save within earning section | Earning type + amount entered | Adds earning line to panel; total recalculates |
| Edit TDS | Click TDS amount in panel | Draft state | Inline form: Amount spinbutton + Reason* + Calculated Value |
| Save (TDS) | Click Save within TDS section | Reason filled | Saves override; flagged visually as overridden |
| Cancel (panel) | Click Cancel/X in split panel | Any unsaved changes | Shows confirmation dialog (see below) |
| Import > Import LOP | Click Import menu | Draft state | Opens import page with file upload (CSV/XLSX, encoding selector) |
| Import > Import One Time Earnings | Click Import menu | Draft state | Opens import page for bonus/comm/encashment CSV |
| Import > Import Reimbursements | Click Import menu | Draft state | Opens import page for reimbursement data |
| Import > Import Adhoc Deductions | Click Import menu | Draft state | Import for ad-hoc deductions |
| Import > Import Variable Pay | Click Import menu | Draft state | Import for variable pay components |
| Export > Export Payroll Data | Click Export menu | Draft state | Downloads current payroll data as CSV/XLSX |
| Export > Export Comparison Report | Click Export menu | Draft state | Downloads comparison vs prior month |

### Cancel Confirmation Dialog

When closing the split panel with unsaved changes, dialog appears:

> "You are about to cancel the changes done to this employee. All the values you have entered will be removed. Are you sure you want to proceed?"

Buttons: **Yes** (discards changes, closes panel) | **No** (returns to panel, preserves edits)

## Conditional Logic

- LOP Days field only appears after clicking "Add LOP" — not shown by default.
- "Add Earning" listbox shows only earning types configured in the org's salary components (Bonus, Commission, Leave Encashment observed).
- TDS override Reason field is mandatory — cannot save TDS override without it.
- Import/Export menu is a unified dropdown with 5 import + 2 export options.
- Split panel is read-only once payrun moves out of Draft state (Approved/Paid). Fields become display-only.

## Cross-Module Links

- Salary structure (components, percentages) → determines what fields appear in split panel
- Pay Schedule (base days) → feeds LOP proration denominator
- TDS configuration (regime, declarations) → determines the "Calculated Value" shown before override
- Import templates → must match column format from Export Payroll Data

## Key Observations for Our Build

1. **LOP proration is manual** — admin enters LOP days; system prorates automatically on save. There is no auto-detection of absenteeism from an attendance module (Zoho Payroll does not have built-in attendance; integration with Zoho People required).
2. **No mid-month joiner auto-proration** — confirmed with EMP002 (joined 16 May). System shows full 31 days / full salary. Admin must manually enter equivalent LOP days (15 days = days before join date). Our build should consider auto-computing LOP from joining date if date of joining falls within the pay period.
3. **Three earning types hardcoded in listbox** — Bonus, Commission, Leave Encashment. Cannot add arbitrary one-time earning types from this panel; must be pre-configured as salary components.
4. **TDS override requires reason** — good compliance design. Our build must replicate: mandatory reason field with audit log entry.
5. **Cancel confirmation dialog** — good UX pattern, prevents accidental loss of entered data. Replicate in our split panel component.
6. **Import file encoding selector** — the import page shows an encoding dropdown (UTF-8, ISO-8859-1, etc.) for CSV files. Needed for Indian names with special characters.
7. **Five import categories** — LOP, One Time Earnings, Reimbursements, Adhoc Deductions, Variable Pay are all importable separately. Each has its own template. Our build should support at minimum LOP and One Time Earnings import from day one.

## Screenshots

- `screenshots/54-emp001-split-panel.png` — EMP001 variable inputs panel (initial state)
- `screenshots/54-lop-entry-panel.png` — LOP Days spinbutton revealed
- `screenshots/54-lop-2days-before-save.png` — 2 LOP days entered, Actual Payable Days = 29
- `screenshots/54-emp001-after-lop-save.png` — Post-save: prorated amounts displayed
- `screenshots/54-add-earning-dropdown.png` — Earning type listbox: Bonus/Commission/Leave Encashment
- `screenshots/54-tds-edit-inline-form.png` — TDS override inline: Amount + Reason* + Calculated Value
- `screenshots/54-import-export-menu.png` — Full Import/Export menu (5+2 options)
- `screenshots/54-import-one-time-earnings.png` — Import page: file upload + encoding selector

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
