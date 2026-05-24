# Settings > Statutory Components

## URL
`#/settings/statutory-details/list`

Sub-routes (tabs):
- `#/settings/statutory-details/list` — EPF tab (default)
- `#/settings/statutory-details/list/esi` — ESI tab
- `#/settings/statutory-details/list/pt` — Professional Tax tab
- `#/settings/statutory-details/list/lwf` — Labour Welfare Fund tab
- `#/settings/statutory-details/list/statutory-bonus` — Statutory Bonus tab

Edit/Enable routes:
- `#/settings/statutory-details/edit-epf-details` — EPF configuration form
- `#/settings/statutory-details/edit-esi-details` — ESI configuration form
- `#/settings/statutory-details/edit-statutorybonus-details` — Statutory Bonus configuration form

## Purpose
Configures all mandatory Indian statutory deductions and contributions: EPF (retirement), ESI (health insurance), Professional Tax (state income levy), Labour Welfare Fund (state welfare), and Statutory Bonus (annual/monthly bonus mandated by law). These settings feed directly into every payroll run's statutory deduction calculations.

## Page Layout
Tabbed page with 5 tabs: EPF | ESI | Professional Tax | Labour Welfare Fund | Statutory Bonus.
"Instant Helper" button in page header.

---

## Tab 1: EPF (Employees' Provident Fund)

### Empty State
- Heading: "Are you registered for EPF?"
- Text: "Any organisation with 20 or more employees must register for the Employee Provident Fund (EPF) scheme, a retirement benefit plan for all salaried employees."
- CTA: "Enable EPF" link → `#/settings/statutory-details/edit-epf-details`

### EPF Configuration Form (`#/settings/statutory-details/edit-epf-details`)

| Field | Type | Required | Default / Value | Format | Tooltip / Help | Notes |
|-------|------|----------|-----------------|--------|----------------|-------|
| EPF Number | Text | No | (empty) | `AA/AAA/0000000/XXX` (e.g., `MH/MUM/1234567/001`) | None | Employer's PF registration number; used in ECR and PF challans |
| Deduction Cycle | Text (disabled) | N/A | Monthly | — | Tooltip: EPF deducted monthly and deposited by 15th of following month | Hardcoded — EPF is always monthly |
| Employee Contribution Rate | Dropdown (disabled) | N/A | 12% of Actual PF Wage | Options: fixed at 12% | None | Statutory rate; cannot be changed |
| Employer Contribution Rate | Dropdown (enabled) | Yes | 12% of Actual PF Wage | 1. "12% of Actual PF Wage", 2. "Restrict Contribution to ₹15,000 of PF Wage" | "View Splitup" button shows breakdown | Option 2 caps employer PF wage at ₹15,000 regardless of actual salary |
| Include employer's contribution in salary structure | Checkbox | No | Checked | — | — | If checked, employer PF is shown as a CTC component on payslips |
| Include employer's EDLI contribution in salary structure | Checkbox | No | Unchecked | — | Tooltip: "EDLI contribution is 0.50% of PF Wage. Maximum Employer Contribution for EDLI is ₹75" | EDLI = Employees' Deposit Linked Insurance scheme |
| Include admin charges in salary structure | Checkbox | No | Unchecked | — | Tooltip: "EPF Admin Charges is 0.50% of PF Wage" | EPF administrative charges paid by employer to EPFO |
| Override PF contribution rate at employee level | Checkbox | No | Unchecked | — | — | If checked, individual employee PF rates can differ from org-level default |

#### PF Configuration when LOP Applied
| Option | Type | Default | Description |
|--------|------|---------|-------------|
| Pro-rate Restricted PF Wage | Checkbox | Unchecked | PF contribution pro-rated on number of days worked (for LOP months) |
| Consider all applicable salary components if PF wage < ₹15,000 after LOP | Checkbox | Checked | If LOP brings actual PF wage below ₹15,000, uses actual earned salary for PF computation instead of salary structure amount |

#### Employer Contribution Splitup (as shown in Sample EPF Calculation panel)
For PF Wage = ₹20,000:
- **Employee's Contribution**: EPF = 12% of 20,000 = ₹2,400
- **Employer's Contribution**:
  - EPS = 8.33% of 20,000 (capped at ₹15,000 max wage) = ₹1,250
  - EPF = 12% of 20,000 − EPS = ₹2,400 − ₹1,250 = ₹1,150
  - Total employer = ₹2,400
- "Preview EPF Calculation" button — opens multi-scenario preview modal

#### Buttons
| Button | Action |
|--------|--------|
| View Splitup | Opens employer contribution breakdown panel |
| Preview EPF Calculation | Opens multi-case EPF calculation preview |
| Enable | Activates EPF for the organisation |
| Cancel | Returns to EPF empty state |

---

## Tab 2: ESI (Employees' State Insurance)

### Empty State
- Heading: "Are you registered for ESI?"
- Text: "Organisations having 10 or more employees must register for Employee State Insurance (ESI). This scheme provides cash allowances and medical benefits for employees whose monthly salary is less than ₹21,000."
- CTA: "Enable ESI" → `#/settings/statutory-details/edit-esi-details`

### ESI Configuration Form (`#/settings/statutory-details/edit-esi-details`)

| Field | Type | Required | Default / Value | Format | Help / Tooltip |
|-------|------|----------|-----------------|--------|----------------|
| ESI Number | Text | No | (empty) | `00-00-000000-000-0000` | Employer's ESI registration number |
| Deduction Cycle | Text (disabled) | N/A | Monthly | — | Tooltip: "ESI contribution for each month should be deposited to ESIC within the 21st of the following month." |
| Employees' Contribution | Text (disabled) | N/A | 0.75% | `% of Gross Pay` | Statutory rate — fixed at 0.75% |
| Employer's Contribution | Text (disabled) | N/A | 3.25% | `% of Gross Pay` | Statutory rate — fixed at 3.25% |
| Include employer's contribution in salary structure | Checkbox | No | Unchecked | — | If checked, employer ESI contribution appears as CTC component |

#### Statutory Note (displayed on form)
> "ESI deductions will be made only if the employee's monthly salary is less than or equal to ₹21,000. If the employee gets a salary revision which increases their monthly salary above ₹21,000, they would have to continue making ESI contributions till the end of the contribution period in which the salary was revised (April–September or October–March)."

This implements ESI Rule 50 — contribution must continue till period end even after crossing the threshold.

#### Deposit Deadline (tooltip on Deduction Cycle)
> "ESI contribution for each month should be deposited to the Employee State Insurance Corporation (ESIC) within the 21st of the following month."

#### Buttons
| Button | Action |
|--------|--------|
| Enable | Activates ESI for the organisation |
| Cancel | Returns to ESI tab list view |

---

## Tab 3: Professional Tax

### Configured State (Kerala, from Head Office work location)
The PT tab shows cards per work location (auto-configured from the state of each work location).

| Display Field | Value |
|---------------|-------|
| Location name | Head Office |
| State | Kerala |
| Deduction Cycle | Half Yearly |
| PT Number | (not set — "Update PT Number" button) |
| PT Slabs | "View Tax Slabs" button |

#### Kerala PT Slabs (Half Yearly)
| Half Yearly Gross Salary (₹) | Half Yearly Tax Amount (₹) |
|------------------------------|---------------------------|
| 1 – 11,999 | 0 |
| 12,000 – 17,999 | 320 |
| 18,000 – 29,999 | 450 |
| 30,000 – 44,999 | 600 |
| 45,000 – 99,999 | 750 |
| 1,00,000 – 1,24,999 | 1,000 |
| 1,25,000 – 99,99,99,999 | 1,250 |

Effective from: 15/05/2026

#### Buttons
| Button | Action |
|--------|--------|
| Update PT Number | Opens modal/form to enter the PT registration number for this location |
| View Tax Slabs | Opens modal showing the state PT slab table (read-only) |
| Edit icon | Opens PT configuration for this location |

**Note**: PT is configured per work location, not per organisation. Each location with a PT-applicable state gets its own card. PT slabs are system-provided and state-specific — not user-configurable.

---

## Tab 4: Labour Welfare Fund

### Configured State (Kerala, auto-detected from work location)
| Display Field | Value |
|---------------|-------|
| State | Kerala |
| Employees' Contribution | ₹50.00 |
| Employer's Contribution | ₹50.00 |
| Deduction Cycle | Monthly |
| Status | Disabled (with "Enable" link) |

Kerala LWF: Employee ₹50 + Employer ₹50 = ₹100 total monthly. Currently disabled — user must explicitly enable it.

**Enable link**: Clicking "Enable" activates LWF deductions for all employees at this work location.

**Note**: Like PT, LWF is configured per work location state. The amounts are system-defined and state-specific (not user-editable).

---

## Tab 5: Statutory Bonus

### Empty State
- Text: "According to the Payment of Bonus Act, 1965, an eligible employee can receive a statutory bonus of 8.33% (min) to 20% (max) of their salary earned during a financial year."
- CTA: "Enable Statutory Bonus" → `#/settings/statutory-details/edit-statutorybonus-details`

### Statutory Bonus Configuration Form

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Payment Frequency | Radio group | Yes | Monthly | Options: Monthly / Yearly |
| Monthly Percentage of Bonus | Number (%) | Yes | (blank) | Must be between 8.33% and 20% (statutory range per Payment of Bonus Act 1965) |
| Minimum Wage (per state) | Section | Conditional | Not set for Kerala | "Add Minimum Wage" link per state — required because bonus = higher of (% of min wage) or (% of Basic + DA) |

#### Business Rule Note (displayed on form)
> "Statutory Bonus is a percentage of either the minimum wage or Basic + DA (whichever is higher)."

> "Statutory Bonus rate should be in-between 8.33% and 20%, based on the Statutory Bonus Act."

> "NOTE: The payment frequency of this statutory bonus is monthly and taxable."

> "Once you've associated the statutory bonus with an employee, you can change the bonus percentage only at the beginning of the next fiscal year."

#### Buttons
| Button | Action |
|--------|--------|
| Add Minimum Wage | Opens input for state-specific minimum wage (used as bonus base) |
| Save | Enables statutory bonus with configured settings |
| Cancel | Discards and returns to bonus empty state |

---

## Cross-Module Impact

| Setting | Impacts |
|---------|---------|
| EPF enabled + rates | Every pay run deducts 12% employee PF and adds employer PF. Feeds ECR (Electronic Challan-cum-Return) file generation |
| EPF Number | Printed on PF challan, ECR file header, and employee PF passbook records |
| ESI enabled + rates | Pay run deducts 0.75% employee + 3.25% employer ESI for eligible employees (salary ≤ ₹21,000) |
| ESI Number | Printed on ESI challan and ESI returns |
| Professional Tax | Pay run deducts PT per applicable slab for each employee's work location state |
| LWF enabled | Pay run deducts fixed LWF amounts (₹50/₹50 for Kerala) per configured cycle |
| Statutory Bonus enabled | Monthly or annual bonus added as a salary component in pay runs |
| Include employer contributions in salary structure | Controls whether employer EPF/ESI appears on payslip as CTC breakdown |

## Statutory References
- **EPF**: Employees' Provident Funds and Miscellaneous Provisions Act, 1952; EPF Scheme 1952; EPS Scheme 1995; EDLI Scheme 1976
- **ESI**: Employees' State Insurance Act, 1948; ESI (Central) Rules 1950
- **PT**: Individual state legislation (Kerala Municipal Act for Kerala; varies by state)
- **LWF**: Individual state LWF Acts (Kerala Labour Welfare Fund Act, 1975 for Kerala)
- **Statutory Bonus**: Payment of Bonus Act, 1965; bonus range 8.33%–20% of salary

## Observations & Notes

1. **EPF registration threshold is 20 employees**, ESI is 10 employees — system shows correct thresholds in empty-state messaging.
2. **Employee Contribution Rate (EPF) is locked at 12%** — correctly enforces statutory rate. Employer rate has two options to accommodate the ₹15,000 PF wage cap scenario.
3. **EPS split within employer EPF** — employer's 12% is split into EPS (8.33% of wages, max ₹15,000) and EPF (balance). This is correct per EPF scheme rules.
4. **EDLI and Admin Charges are separate from EPF** — EDLI = 0.50% (capped at ₹75), Admin = 0.50% of PF wage. Both are employer costs only.
5. **ESI rates are hardcoded** — 0.75% employee, 3.25% employer. These are the rates post the 2019 ESIC revision. The form correctly shows these as read-only.
6. **ESI continuation rule** is explicitly documented in the UI — contributes till period end after crossing ₹21,000. This is a rare edge case handled correctly.
7. **PT is auto-configured per work location state** — when a work location is created with a PT-applicable state, PT slabs are auto-loaded. User only needs to provide the PT number and enable.
8. **Kerala PT is Half Yearly** (April–September, October–March cycles) — deduction happens twice a year. Other states may be monthly or annual.
9. **LWF is state-specific fixed amounts** — ₹50 each for Kerala. Maharashtra, Karnataka have different amounts/cycles. All are system-defined.
10. **Statutory Bonus minimum wage per state** — the Payment of Bonus Act requires bonus to be computed on the higher of minimum wage or Basic+DA. Zoho requires state minimum wages to be entered manually (they are not pre-loaded for Kerala).
11. **Statutory Bonus immutability** — once associated with an employee, the bonus % can only be changed at the start of the next fiscal year. This is an important business rule for our implementation.
12. For our build: All statutory rates (EPF %, ESI %, LWF amounts, PT slabs) must come from a DB config table keyed by state + effective date, not hardcoded. The UI just needs to show/hide based on whether the component is applicable per the work location's state.

## Screenshots
- `docs/ba-audit/settings/screenshots/11-statutory-epf-empty.png`
- `docs/ba-audit/settings/screenshots/11-statutory-epf-form.png`
- `docs/ba-audit/settings/screenshots/11-statutory-esi-form.png`
- `docs/ba-audit/settings/screenshots/11-statutory-pt.png`
- `docs/ba-audit/settings/screenshots/11-statutory-lwf.png`
- `docs/ba-audit/settings/screenshots/11-statutory-bonus.png`
