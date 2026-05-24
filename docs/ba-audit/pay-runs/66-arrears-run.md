# Pay Runs > Off-Cycle Runs — One Time Payout & Off Cycle Pay Run

## URL / Navigation Path

`https://payroll.zoho.in/#/payruns` (Run Payroll tab)

Trigger: "New" dropdown button (always visible regardless of outstanding pay run status)

Sub-flows:
- New > One Time Payout → "Create One Time Payrun" dialog
- New > Off Cycle Pay Run → "Initiate Off Cycle Pay Run" dialog

## Purpose

Enables ad-hoc and non-standard payment runs outside the regular monthly cycle. Two distinct types:

1. **One Time Payout** — pays a single salary component (e.g., Bonus, Commission) to all or selected employees on a specific date, outside the regular run
2. **Off Cycle Pay Run** — a mini pay run for a specific payment date, outside the regular pay period (e.g., F&F settlement, advance salary)

## One Time Payout

### Dialog: "Create One Time Payrun"

| Field | Type | Required | Options | Behaviour |
|-------|------|----------|---------|-----------|
| Select One Time Component | Combobox | Yes | List of configured one-time components | Dropdown shows components tagged as one-time in salary structure config |
| When would you like to pay? | Date input | Yes | dd/MM/yyyy | Payment date for this payout |

**Buttons:** Save and Continue | Cancel

### Post-Save Behaviour (inferred)

After clicking "Save and Continue":
- System creates a One Time Payout run record
- Navigates to a selection screen where admin can choose which employees receive this payout
- Admin enters the amount per employee (or bulk amount)
- Approval + Record Payment flow same as regular run

### Use Cases

- Company-wide bonus payout (not included in regular run)
- Quarterly commission payment
- Festival bonus (Diwali, etc.) as a separate run

### Key Constraints

- Only one-time components configured in the salary structure can be selected. Cannot create ad-hoc component names here.
- One component per One Time Payout run (based on dialog having single component field)

## Off Cycle Pay Run

### Dialog: "Initiate Off Cycle Pay Run"

| Field | Type | Required | Options | Behaviour |
|-------|------|----------|---------|-----------|
| Select Date | Date input | Yes | dd/MM/yyyy | The payment date for this off-cycle run |

**Buttons:** (Save / Initiate / Continue) | Cancel

Note: Exact button label not confirmed — captured dialog had date field only.

### Post-Save Behaviour (inferred)

After initiating:
- Creates an off-cycle run for the selected date
- May default to covering the remaining days of the current month from the selected date, OR may be for a specific partial period
- Admin selects employees to include
- Proceeds through variable inputs → approval → payment flow (same as regular)

### Use Cases

- Full & Final Settlement for exiting employee
- Advance salary payment (e.g., employee needs salary before month-end)
- Payroll correction run for a specific employee who was miscalculated in the regular run
- Rejoiner mid-month who was not included in the regular run

### Off Cycle vs One Time Payout — Differences

| Dimension | Off Cycle Pay Run | One Time Payout |
|-----------|------------------|-----------------|
| Components | All salary components | Single one-time component |
| Period | Specific date / partial period | Specific date |
| Employees | Selected subset | All or selected employees |
| Use case | F&F, partial month, correction | Bonus, commission, one-time earnings |
| Payslip | Full payslip generated | Payout statement only (inferred) |
| Payroll type label | "Off Cycle Payrun" | "One Time Payout" |
| Visible in history | Yes | Yes (with type filter) |

## Resettlement Payroll

Observed in the Payroll History "Payroll Type" filter dropdown but not triggered/tested. Likely a third run type for arrear settlements (e.g., retrospective salary revision arrears). Not to be confused with Off Cycle.

## New Dropdown Options (Complete)

From the "New" button dropdown on `#/payruns`:

1. **One Time Payout** — single component mass payout
2. **Off Cycle Pay Run** — mini full-salary run on a specific date

(Only 2 options observed — Resettlement Payroll not in the New dropdown; only in history filter.)

## Key Observations for Our Build

1. **Off Cycle is the F&F mechanism** — based on Zoho's design, F&F settlement appears to use the Off Cycle Pay Run flow (select the exit date, pick the employee, enter prorated salary + leave encashment). Our build should make this explicit: Off Cycle run should have a "Purpose" field (F&F, Advance, Correction, Other).
2. **One Time Payout needs component pre-configuration** — admin cannot type a free-form payout name. Components must be configured in Settings > Salary Components as "one-time" type first. Our build: support "One Time Component" flag in salary component config.
3. **Resettlement Payroll = arrears run** — implement as a distinct PayrollRunType enum value: `Regular | OffCycle | OneTimePayout | Resettlement`. Resettlement would be triggered automatically when a salary revision with a past effective date is approved, computing the delta for prior months.
4. **Off Cycle does not block regular run** — admin can initiate an Off Cycle run even when a regular run is in progress (or no regular run exists). These are parallel, independent runs.
5. **Single date for Off Cycle** — Zoho only asks for a payment date, not a "from date / to date" period. Our build should support both: payment date (required) + optional coverage period for payslip header.
6. **One Time Payout single component** — enforce one component per run at DB level. If admin needs to pay Bonus + Commission in one shot, they create two separate One Time Payout runs (or add both in the regular run via Add Earning).
7. **History filter for off-cycle runs** — the Payroll History filter allows filtering by payroll type. Ensure our payroll_run table has a `type` column (enum) indexed for this query.

## Screenshots

- `screenshots/66-new-dropdown-options.png` — "New" dropdown: One Time Payout | Off Cycle Payrun
- `screenshots/66-off-cycle-payrun-dialog.png` — "Initiate Off Cycle Pay Run" dialog (single date field)
- `screenshots/66-one-time-payout-dialog.png` — "Create One Time Payrun" dialog (component + date fields)

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
