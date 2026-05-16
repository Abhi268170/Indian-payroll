# Employees > Add Employee — Step 2: Salary Details

## URL / Navigation Path
- Route: `#/people/employees/{id}/edit/salary-details`
- Full URL: `https://payroll.zoho.in/#/people/employees/3848927000000032948/edit/salary-details`
- Entry: Reached after saving Basic Details (Step 1) in the wizard
- Page title: "Employees | Salary Details | Employees | Edit | Zoho Payroll"

## Purpose
Step 2 of 4 in Add Employee wizard. Captures annual CTC, salary structure (component breakdown), statutory component enablement (PF, ESI, PT), and other benefits.

## Prerequisite
A Pay Schedule must be configured in Settings > Pay Schedules before this step can be saved. Without a pay schedule, clicking "Save and Continue" shows:
> "You cannot update salary details without configuring a pay schedule."

Pay Schedule configured for lerno org: Monthly, Mon–Fri work week, last day of month pay date, starting April 2026, first pay date 30/04/2026.

## Sections

### Section 1: Statutory Components
Checkboxes to enable/disable statutory deductions for this employee.

| Component | Present? | Notes |
|---|---|---|
| Provident Fund (PF) | No | Not shown — PF not configured in Settings for this org |
| ESI | No | Not shown — ESI not configured in Settings for this org |
| Professional Tax (PT) | Yes | Checkbox, unchecked by default; enabling deducts PT per work location slab |

Only components enabled in org Settings appear here. Absence of PF/ESI checkboxes is NOT an error — it reflects org-level configuration.

### Section 2: Salary Structure
CTC-anchored, percentage-based. Components calculated from annual CTC.

#### Annual CTC Field
| Field | Type | Required | Validation | Notes |
|---|---|---|---|---|
| Annual CTC | Number (currency, INR) | Yes | Must be > 0 | Basis for all percentage-based component calculations |

#### Salary Components Table
Columns: Salary Components | Calculation Type | Monthly Amount | Annual Amount

| Component | Calculation Type | Editable | Notes |
|---|---|---|---|
| Basic | % of CTC | Percentage editable; Monthly/Annual read-only | Core component; drives HRA calculation |
| Fixed Allowance | Fixed amount | Not editable | Residual balancer = CTC − sum of all other components; auto-calculated |
| HRA | % of Basic | Percentage editable; Monthly/Annual read-only | Appears if HRA component pre-configured in Settings |

- "Add Earning" dropdown: only shows pre-configured salary components from Settings > Salary Components. Default lerno org had HRA and Conveyance Allowance.
- Conveyance Allowance: Tax-exempt up to ₹1,600/month (pre-GST legacy limit; currently fully taxable under new regime — no exemption under new regime).
- Fixed Allowance is NOT directly editable — it auto-adjusts to make components sum to CTC.
- Users cannot directly enter ₹ amounts for percentage-based components — must enter percentage.

#### EMP001 Salary Values Saved
- Annual CTC: ₹8,40,000
- Basic: 57.14% of CTC → ₹39,998/month (≈₹4,79,976/year)
- HRA: 40% of Basic → ₹15,999/month (≈₹1,91,988/year)
- Fixed Allowance: ₹14,003/month (≈₹1,68,036/year) [residual]
- Total Monthly: ₹70,000 | Total Annual: ₹8,40,000 ✓

### Section 3: Other Benefits
"Add Benefit" button opens modal: "Select a benefit plan*" dropdown. No benefit plans configured in lerno org by default. "Proceed" button available to skip Other Benefits without configuring any.

## Buttons & Actions

| Button | Behaviour |
|---|---|
| Save and Continue | Validates Annual CTC > 0, pay schedule exists; saves and navigates to Step 3 (Personal Details) |
| Skip | Skips salary details entirely; navigates to next step |

## Business Rules
1. Annual CTC is the anchor — all percentage components derive from it. Cannot be zero.
2. Fixed Allowance is always present and non-editable — it absorbs the remainder.
3. Pay Schedule is a hard prerequisite — save fails with actionable error message directing user to Settings.
4. Only pre-configured salary components appear in "Add Earning" — org admin must configure components in Settings first.
5. PT checkbox only appears if PT is enabled at org level; PF/ESI likewise.
6. Under new regime: HRA exemption is not applicable; all salary components are fully taxable. Zoho captures HRA for payslip structure but no tax exemption is computed under new regime.

## Cross-Module Impact
- Salary components defined here feed into Payroll Run (gross pay computation).
- Basic salary drives PF wage calculation (when PF is enabled): PF = 12% of Basic, capped at 12% of ₹15,000 = ₹1,800/month employer + employee each.
- HRA component will appear on payslip but no exemption computed (new regime).
- PT enablement here triggers PT deduction per work location slab in payroll run.

## Key Observations for Our Build
1. **CTC-anchored percentage model** — our salary structure entity must support `% of CTC` and `% of Basic` calculation types, not just fixed amounts.
2. **Fixed Allowance as residual** — engine must compute Fixed Allowance = Annual CTC − sum of all other components. Not user-input.
3. **Component configurability** — salary components must be configured at org level before they appear in employee salary setup. Our Settings module needs a Salary Components page.
4. **Pay Schedule gating** — salary cannot be saved without a pay schedule. Our API should return a clear error (not a generic 500) if pay schedule is missing.
5. **No direct ₹ input for percentage components** — UX decision: force percentage entry to maintain CTC integrity. Consider if we want to allow ₹ input with back-calculation.
6. **Statutory component visibility is config-driven** — PF/ESI checkboxes only appear if enabled at org. Our salary detail form must query org statutory config.

## Screenshots
- `screenshots/37-salary-details.png` — Initial salary details form with statutory components section
- `screenshots/37-salary-structure-active.png` — Salary structure table with CTC input active
- `screenshots/37-salary-structure-filled.png` — Completed salary structure for EMP001
- `screenshots/37-add-earning-dropdown.png` — "Add Earning" dropdown showing available components
- `screenshots/37-other-benefits-modal.png` — Other Benefits modal (no plans configured)
- `screenshots/37-salary-no-payschedule-error.png` — Error when saving without pay schedule
