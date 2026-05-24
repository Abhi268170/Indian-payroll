# Settings > Salary Components

## URL
`#/settings/salary-components/earnings` (default tab)

Sub-routes (tabs):
- `#/settings/salary-components/earnings` — Earnings tab
- `#/settings/salary-components/deductions` — Deductions tab
- `#/settings/salary-components/benefits` — Benefits tab
- `#/settings/salary-components/reimbursements` — Reimbursements tab

New component routes:
- `#/settings/salary-components/earnings/new` — New Earning form

## Purpose
Defines the library of salary components (pay heads) used across all employee salary structures. Covers earnings, deductions, benefits (like VPF), and reimbursements. Each component specifies its EPF/ESI treatement, tax treatment, calculation method, and payslip display.

## Page Layout
Four-tab page. Each tab shows a table of components in that category. "Add Component" button in header opens a dropdown with 5 component types: Earning, Correction, Benefit, Deduction, Reimbursement.

---

## Tab 1: Earnings

### Earnings Table Columns
| Column | Description |
|--------|-------------|
| Name | Component name (e.g., "Basic", "House Rent Allowance") |
| Earning Type | The statutory/system type (e.g., Basic, HRA, Custom Allowance) |
| Calculation Type | How the amount is determined (Fixed/Variable; Flat/% of CTC/% of Basic) |
| Consider for EPF | Yes / No / Yes (If PF Wage < 15k) |
| Consider for ESI | Yes / No |
| Status | Active / Inactive |
| More Actions | Three-dot dropdown (Edit, Delete, Activate/Deactivate) |

### Pre-configured Earnings (14 system components)

| # | Name | Earning Type | Calculation Type | EPF | ESI | Status |
|---|------|-------------|-----------------|-----|-----|--------|
| 1 | Basic | Basic | Fixed; 50% of CTC | Yes | Yes | Active |
| 2 | House Rent Allowance | House Rent Allowance | Fixed; 50% of Basic | No | Yes | Active |
| 3 | Conveyance Allowance | Conveyance Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | No | Active |
| 4 | Children Education Allowance | Children Education Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | Yes | Inactive |
| 5 | Transport Allowance | Transport Allowance | Fixed; Flat amount of 1600 | Yes (If PF Wage < 15k) | Yes | Inactive |
| 6 | Travelling Allowance | Travelling Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | No | Inactive |
| 7 | Fixed Allowance | Fixed Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | Yes | Active |
| 8 | Overtime Allowance | Overtime Allowance | Variable; Flat Amount | No | Yes | Inactive |
| 9 | Gratuity | Gratuity | Variable; Flat Amount | No | No | Active |
| 10 | Bonus | Bonus | Variable; Flat Amount | No | No | Active |
| 11 | Commission | Commission | Variable; Flat Amount | No | Yes | Active |
| 12 | Leave Encashment | Leave Encashment | Variable; Flat Amount | No | No | Active |
| 13 | Notice Pay | Notice Pay | Variable; Flat Amount | No | No | Active |
| 14 | Hold Salary | Hold Salary (Non Taxable) | Variable; Flat Amount | No | No | Active |

### New Earning Form Fields (for Custom Allowance — most complete form)

| Field | Type | Required | Default | Options / Notes |
|-------|------|----------|---------|-----------------|
| Earning Type | Dropdown | Yes | (Select) | 33 options — see full list below |
| Earning Name | Text | Yes | (blank) | Display name in UI |
| Name in Payslip | Text | Yes | (blank) | Label shown on payslip PDF |
| Pay Type | Radio | Yes | (blank) | Fixed Pay (monthly recurring) / Variable Pay (per-payroll manual input) |
| Calculation Type | Radio | Yes | (blank) | Flat Amount / Percentage of Basic |
| Enter Amount | Number (₹) or % | Conditional | (blank) | Shown when Calculation Type is selected; ₹ for flat, % for percentage |
| Mark this as Active | Toggle/Checkbox | No | Active | Activates/deactivates the component |
| Make this earning part of salary structure | Checkbox | No | Unchecked | If checked, component appears in salary template and is assigned per employee |
| This is a taxable earning | Checkbox | No | Unchecked | If checked, amount is included in taxable income |
| Calculate on pro-rata basis | Checkbox | No | Unchecked | If checked, amount is pro-rated based on actual working days (for LOP) |
| Consider for EPF Contribution | Radio (conditional) | No | (blank) | Always / Only when PF Wage < ₹15,000 |
| Consider for ESI Contribution | Checkbox | No | Unchecked | If checked, included in ESI wage |
| Show this component in payslip | Checkbox | No | Unchecked | If checked, component line shown on payslip |

**For Variable/Bonus-type earnings, additional field:**

| Field | Type | Options |
|-------|------|---------|
| Tax Deduction Preference | Radio | "Deduct tax in subsequent payrolls of the financial year" (spread evenly) / "Deduct tax in same payroll" (full TDS in payment month) |

### All 33 Earning Type Options
Basic, House Rent Allowance, Dearness Allowance, Retaining Allowance, Conveyance Allowance, Bonus, Commission, Children Education Allowance, Hostel Expenditure Allowance, Transport Allowance, Helper Allowance, Travelling Allowance, Uniform Allowance, Daily Allowance, City Compensatory Allowance, Overtime Allowance, Telephone Allowance, Fixed Medical Allowance, Project Allowance, Food Allowance, Holiday Allowance, Entertainment Allowance, Custom Allowance, Food Coupon, Gift Coupon, Research Allowance, Books and Periodicals Allowance, Shift Allowance, Fuel Allowance, Driver Allowance, Leave Travel Allowance, Vehicle Maintenance Allowance, Telephone And Internet Allowance

**Key note on Scheduled Earnings:** Some types (e.g., Bonus) show a "This is a scheduled earning" checkbox, allowing the earning to be scheduled for a future pay period rather than the current one.

**Immutability note (shown on form):**
> "Once you associate this component with an employee, you will only be able to edit the Name and Amount/Percentage. The changes you make to Amount/Percentage will apply only to new employees."

---

## Tab 2: Deductions

### Deductions Table Columns
| Column | Description |
|--------|-------------|
| Name | Component name |
| Deduction Type | System type |
| Deduction Frequency | One Time / Recurring |
| Status | Active / Inactive |
| More Actions | Edit, Delete, etc. |

### Pre-configured Deductions (2 system components)

| # | Name | Deduction Type | Frequency | Status |
|---|------|---------------|-----------|--------|
| 1 | Withheld Salary | Withheld Salary | One Time | Active |
| 2 | Notice Pay Deduction | Notice Pay Deduction | One Time | Active |

---

## Tab 3: Benefits

### Benefits Table Columns
| Column | Description |
|--------|-------------|
| Name | Component name |
| Benefit Type | System type |
| Benefit Frequency | Recurring / One Time |
| Status | Active / Inactive |
| More Actions | Edit, Delete, etc. |

### Pre-configured Benefits (1 system component)

| # | Name | Benefit Type | Frequency | Status |
|---|------|-------------|-----------|--------|
| 1 | Voluntary Provident Fund | Voluntary Provident Fund | Recurring | Inactive |

---

## Tab 4: Reimbursements

### Note shown above table:
> "With these reimbursement components, employees can claim reimbursements for the components which are part of the payroll and not for other expenses reimbursements."

### Reimbursements Table Columns
| Column | Description |
|--------|-------------|
| Name | Component name |
| Reimbursement Type | System type |
| Maximum Reimbursable Amount | ₹ cap per period |
| Status | Active / Inactive |
| More Actions | Edit, Delete, etc. |

### Pre-configured Reimbursements (5 system components)

| # | Name | Reimbursement Type | Max Amount | Status |
|---|------|-------------------|------------|--------|
| 1 | Fuel Reimbursement | Fuel Reimbursement | 0 | Inactive |
| 2 | Driver Reimbursement | Driver Reimbursement | 0 | Inactive |
| 3 | Vehicle Maintenance Reimbursement | Vehicle Maintenance Reimbursement | 0 | Inactive |
| 4 | Telephone Reimbursement | Telephone Reimbursement | 0 | Inactive |
| 5 | Leave Travel Allowance | Leave Travel Allowance | 0 | Inactive |

---

## Add Component Dropdown (5 types)
When "Add Component" button is clicked, a dropdown appears with:
1. **Earning** → `#/settings/salary-components/earnings/new`
2. **Correction** → correction component form
3. **Benefit** → benefit component form
4. **Deduction** → deduction component form
5. **Reimbursement** → reimbursement component form

---

## Buttons & Actions

| Button | Label | State | Action |
|--------|-------|-------|--------|
| Add Component | "Add Component" | Always enabled | Opens dropdown with 5 component types |
| More Actions (each row) | Three-dot | Always enabled | Edit, Delete/Deactivate, Mark as Active/Inactive |
| Page Tips | Link | Always enabled | Opens contextual help tips for the new earning form |

## Conditional Logic in New Earning Form

1. **Earning Type selected** — determines which additional fields appear (e.g., Basic has fixed 50% of CTC with no changes allowed; Custom Allowance shows full form)
2. **Pay Type = Fixed** — no Tax Deduction Preference field; tax spread evenly by default
3. **Pay Type = Variable** — shows Tax Deduction Preference radio (spread vs. same payroll)
4. **Calculation Type = Flat Amount** — shows ₹ amount input
5. **Calculation Type = Percentage of Basic** — shows % input
6. **Consider for EPF = Yes** — shows sub-option: Always / Only when PF Wage < ₹15,000
7. **This is a taxable earning = checked** — shows tax deduction preference (for variable types)
8. **"This is a scheduled earning"** checkbox — appears for variable earning types (e.g., Bonus); allows scheduling for a specific future pay period

## Cross-Module Impact

| Setting | Impacts |
|---------|---------|
| Salary Components | Appear in Salary Templates and Employee Salary Structure assignments |
| Active/Inactive status | Only active components appear in salary templates |
| EPF Consider | Determines which components are included in PF wage calculation in pay runs |
| ESI Consider | Determines which components are included in ESI wage calculation |
| Taxable / Non-taxable | Determines which components are included in TDS computation |
| Calculation Type (% of Basic) | Creates a dependency: this component's value changes when Basic salary changes |
| Show in payslip | Controls which line items appear on employee payslip PDF |
| Pro-rata basis | If checked, component is deducted proportionally for LOP days |

## Observations & Notes

1. **Immutability after employee association** is a critical design constraint — once a component is linked to an employee's salary structure, only Name and Amount/Percentage can be edited, and only for new employees. Existing employees are unaffected. This prevents retroactive salary structure changes.
2. **"Yes (If PF Wage < 15k)"** for EPF is the conditional EPF inclusion — standard practice for allowances that should be included in PF wage only when the employee's total PF wage is below the ₹15,000 statutory cap.
3. **Correction** component type (in Add Component dropdown) is not listed in any tab — it's a separate category for adjustment entries (e.g., salary arrears corrections).
4. **Maximum Reimbursable Amount = 0** for all reimbursements means they haven't been configured yet. The max amount likely becomes the per-period cap used when employees submit claims.
5. **VPF (Voluntary Provident Fund)** is a Benefit, not an Earning — it's an additional employee contribution to PF beyond the statutory 12%.
6. **Gratuity** appears as an Earning with Variable/Flat Amount — this is the monthly accrual provision, not the actual payment. It's included in CTC display.
7. **Hold Salary** is marked as "Non Taxable" in the earning type display — salary withheld is held in trust and not taxable until paid.
8. The "Page Tips" button on the New Earning form is a unique in-context help feature.
9. For our build: Salary components must be a typed entity with: component_type (Earning/Deduction/Benefit/Reimbursement), earning_type (enum from the 33 types), pay_type (Fixed/Variable), calculation_type (Flat/Percentage), tax_treatment, epf_treatment, esi_treatment, and active flag. Once assigned to an employee, only amount/name changes allowed for existing employees.

## Screenshots
- `docs/ba-audit/settings/screenshots/12-salary-components-earnings.png`
- `docs/ba-audit/settings/screenshots/12-salary-components-new-earning.png`
