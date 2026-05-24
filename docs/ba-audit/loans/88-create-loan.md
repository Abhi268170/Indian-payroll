# Item 88: Create Loan Form

**URL / Navigation Path:** `https://payroll.zoho.in/#/loans/new`
**Module:** Loans
**Entry Points:** "Add" button on Loans list; direct navigation to `#/loans/new`

---

## Purpose

Form to create a new employee loan. Associates a loan type with an employee, specifies financial terms (amount, instalment, schedule), and captures perquisite exemption intent.

---

## Screenshots

- `screenshots/88-create-loan-form.png` — Initial form state (fields visible)
- `screenshots/88-loan-type-dropdown.png` — Loan Name dropdown open
- `screenshots/88-loan-amount-tooltip.png` — Tooltip on Loan Amount

---

## Layout

Single-column form within a card/container. Two logical sections:

1. **Primary Info** (top) — Loan Name selection (large, full-width)
2. **Form Content** (below) — All other fields in a 2-column grid at lg breakpoint

**Form title:** "Create Loan"

---

## Data Fields

### Section 1: Primary Info

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Loan Name | Combobox (Ember ac-box) | Yes | Must select from admin-defined loan types | Shows "Perquisite Rate: X%" inline below after selection. Only admin-defined types appear. |

**Loan Name combobox behaviour:**
- Dropdown shows all admin-defined loan types + "Manage Loans" option
- "Manage Loans" opens the loan type management panel (see Item 87)
- After selection, shows: `Perquisite Rate: X%` immediately below the combobox

### Section 2: Form Content

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Employee Name | Combobox (employee search) | No (aria-required="false") | Must be active employee | Search-capable dropdown. Shows all 5 employees in test org. Employee code shown in brackets. |
| Loan Amount | Number (₹) | Yes | Positive number | Prefixed with ₹ symbol. Tooltip: "The loan amount will not be paid as a part of the pay run. You need to pay the amount to your employee separately." |
| Disbursement Date | Date picker (dd/MM/yyyy) | Yes | Must be in a non-completed pay period. Must be a future pay period start. | Bootstrap datepicker popup. Selecting a date in a completed pay period gives error: "The loan disbursement date that you've selected falls on a completed pay period. Select a date in the future." |
| Reason | Textarea (2 rows) | Yes | Non-empty | Free text. No character limit observed. |
| Exempt from perquisite | Checkbox | No | — | Appears with label "Exempt this loan from perquisite calculation". Help text references Rule 15(5) IT Rules 2026. Appears for all loans (including 0% rate). |

### Section 3: Repayments (CONDITIONAL — appears only after Disbursement Date is selected)

**Trigger:** Setting the Disbursement Date via the bootstrap-datepicker's `changeDate` event renders the Repayments section.

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| EMI Deduction Start Date | Date picker (dd/MM/yyyy) | Yes | Must be after disbursement date; cannot be in the future (for pre-payment recording). Note: the form itself does not enforce future/past — only the Record Repayment modal does. | Second date input, appears after disbursement date selection |
| Instalment Amount | Number (₹) | Yes | Positive number | EMI per deduction cycle |

**Calculated display (after amount and instalment filled):**
- "This loan will be fully paid off in N instalments. The first deduction for this loan will be on DD/MM/YYYY."
- N = ceiling(Loan Amount / Instalment Amount) — integer division
- First deduction = EMI Deduction Start Date

### Section 4: Perquisite Exemption (below Reason)

| Element | Type | Notes |
|---------|------|-------|
| Exempt checkbox | Checkbox | Pre-checked when perquisite rate = 0% (inference — observed with Personal Loan 0%) |
| Help text | Static text | "According to Rule 15(5) of the Income Tax Rules, 2026, employees availing medical loans (for treatment of diseases specified in Rule 18) or any other loans below ₹2,00,000 in aggregate can be exempted from perquisite calculation." |

---

## Actions

| Action | Trigger | Pre-condition | Post-behaviour |
|--------|---------|---------------|----------------|
| Save | Button (type=submit) | All required fields valid | Creates loan, redirects to Loan Detail page `#/loans/{id}` |
| Cancel | Button (type=button) | — | Navigates back to loans list |

---

## Validation Errors Observed

| Scenario | Error Message |
|----------|---------------|
| Missing disbursement date | "Please select a loan disbursement date" |
| Missing EMI start date | "Please select a date from which the repayment starts" |
| Missing instalment amount | "Please enter the instalment amount" |
| Disbursement date in completed pay period | "The loan disbursement date that you've selected falls on a completed pay period. Select a date in the future." |
| Disbursement date before first pay period start | "The loan start date should fall after the first pay period start date." |

---

## Conditional Logic

1. **Repayments section hidden by default** — only appears after disbursement date is selected (datepicker `changeDate` event fires).
2. **Perquisite rate display** — shown below Loan Name combobox after loan type is selected.
3. **"This loan will be fully paid off in N instalments"** — auto-calculated and displayed in real time when amount and instalment amount are both filled.
4. **Exempt checkbox** — present for all loan types; pre-checked when rate = 0%.

---

## Business Rules

- Disbursement date must be in a future (un-finalized) pay period.
- Loan amount is paid out of band — NOT through the payroll run. (Explicitly stated in tooltip.)
- EMI deduction starts from EMI Deduction Start Date via the payroll run mechanism.
- Number of instalments = Loan Amount / Instalment Amount (integer). System auto-calculates and displays.
- Perquisite applies to non-zero-rate loans per the Income Tax Act / Rule 15(5).
- Exemption under Rule 15(5): medical loans (Rule 18 diseases) OR aggregate loans below ₹2,00,000.

---

## Cross-Module Impact

- Loan type config feeds from Manage Loans (admin-defined, no system presets).
- Employee dropdown pulls from Employee Master (active employees only — inferred).
- Loan creation ties to Pay Runs: EMI deductions begin from the specified EMI start month in subsequent pay runs.
- Perquisite rate affects TDS calculation (loan perquisite = taxable perquisite for TDS).

---

## Form HTML Key Attributes

```html
<!-- Loan Name combobox -->
<div role="combobox" aria-required="true" class="ac-box">

<!-- Employee Name combobox -->
<div class="employee-search-box ember-view">

<!-- Loan Amount -->
<input type="number" step="0" class="form-control text-end">
<!-- Tooltip: "The loan amount will not be paid as a part of the pay run..." -->

<!-- Disbursement Date -->
<input type="text" placeholder="dd/MM/yyyy" class="form-control zf-date-picker date-picker">

<!-- Reason -->
<textarea rows="2" class="form-control"></textarea>

<!-- EMI Date (conditional) -->
<input type="text" placeholder="dd/MM/yyyy"> <!-- second date input, same class -->

<!-- Instalment Amount (conditional) -->
<input type="number" step="0"> <!-- second number input -->

<!-- Exempt checkbox -->
<input type="checkbox" class="form-check-input">

<!-- Save -->
<button type="submit" class="btn btn-primary">Save</button>

<!-- Cancel -->
<button type="button" class="btn btn-default action-cancel">Cancel</button>
```

---

## Open Questions

- [ ] Is "Employee Name" truly optional (aria-required=false)? Can a loan be created without an employee?
- [ ] What is the perquisite tax calculation formula for non-zero rate loans (Rule 3 IT Rules)?
- [ ] Does the EMI Deduction Start Date have to align to a specific pay period start date?
- [ ] Is there a maximum loan amount limit configured anywhere?
- [ ] Are attachments/documents supported on the loan creation form?
