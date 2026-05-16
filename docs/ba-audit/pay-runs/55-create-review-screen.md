# Pay Runs > Review Screen — Draft State Summary Page

## URL / Navigation Path

`https://payroll.zoho.in/#/payruns/{id}/summary`
State: Draft

Navigation: Pay Runs > [period card] > (auto-created on click) — lands here in Draft state

## Purpose

Central review and management screen for an in-progress pay run. Three-tab layout: Employee Summary (default), Taxes & Deductions, Overall Insights. Admin reviews all employees' pay, enters variable inputs, manages pending tasks, and approves from here.

## Page Layout

- **Top header bar**: Back link | "Regular Payroll" title | State badge ("Draft") | action buttons (page-level kebab)
- **Info card strip**: Period, Base Days, Month, Payroll Cost, Total Net Pay, Pay Day, Employee count, Skipped count, Taxes & Deductions summary table
- **Tab navigation**: Employee Summary | Taxes & Deductions | Overall Insights
- **Tab content area**: Employee list table (default tab) with filter/search/export controls

## Fields — Info Card Strip

| Field | Type | Required | Options | Behaviour |
|-------|------|----------|---------|-----------|
| Period | Read-only | N/A | "01/05/2026 - 31/05/2026" | Locked at run creation |
| Base Days | Read-only | N/A | e.g., "31 Base Days" | From pay schedule |
| Month | Read-only | N/A | e.g., "May 2026" | Derived from period |
| Payroll Cost | Read-only ₹ | N/A | Gross total including employer contributions | Updates live as variable inputs saved |
| Total Net Pay | Read-only ₹ | N/A | Sum of all employee net pay | Updates live |
| Pay Day | Read-only | N/A | e.g., "29 May, 2026" | From pay schedule |
| Employees count | Read-only | N/A | e.g., "5 Employees" | Total active employees in run |
| Skipped count | Read-only button | N/A | e.g., "( 3 Skipped )" | Clickable — opens skipped employees panel |
| Taxes | Read-only ₹ | N/A | e.g., "₹0.00" | TDS + PT sum |
| Benefits | Read-only ₹ | N/A | e.g., "₹0.00" | Benefits deductions |
| Donations | Read-only ₹ | N/A | e.g., "₹0.00" | Charitable deductions |
| Total Deductions | Read-only ₹ | N/A | e.g., "₹0.00" | Sum of all deductions |

### Employee Summary Tab — Table Columns

| Column | Type | Notes |
|--------|------|-------|
| Checkbox (select all / per row) | Checkbox | Bulk selection for actions |
| Employee Name (EMP ID) | Button link | Click opens split panel |
| Paid Days | Integer | Base Days − LOP Days |
| Net Pay | Decimal ₹ | Per-employee net |
| Payslip | "View" button | Opens payslip preview (read-only in Draft) |
| TDS Sheet | "View" button | Opens TDS sheet PDF in modal |
| Payment Mode | Read-only | From employee bank/payment settings |
| Payment Status | Read-only | In Draft: blank or "Pending" |
| Row kebab | Dropdown button | Per-employee actions (see below) |

### Per-Row Kebab Menu — Draft State (6 options)

1. **View Payslip** — opens payslip preview split panel
2. **View TDS Sheet** — opens TDS Sheet PDF in iframe modal
3. **Skip Employee** — shows "Skip Employee?" dialog with mandatory Reason field
4. **Undo Skip** — (only if employee was skipped) reverses skip
5. **Withold Salary** — (observed in docs, not tested) prevents payment for this employee
6. **Revise Salary** — navigates to salary revision flow for this employee

### Taxes & Deductions Tab

Aggregate table showing:
- TDS total for the run
- Professional Tax total
- ESI employee deduction total
- PF employee deduction total
- Any other deductions

(In this org: all ₹0.00 due to no PF/ESI/PT configuration)

### Overall Insights Tab

See `67-statutory-summary.md` for detailed breakdown.

## Buttons & Actions (Page Level)

| Action | Trigger | Pre-condition | Post-behaviour |
|--------|---------|---------------|----------------|
| Back | Click Back link | Any state | Navigates to `#/payruns/payroll-history` |
| Approve Payroll | Page kebab in Draft | All Pending Tasks completed | Opens Approve Payroll confirmation dialog |
| Show Pending Tasks | Page kebab / banner | Draft state with tasks | Expands pending tasks section |
| Import (5 types) | Import menu | Draft state | Opens import page per type |
| Export Data | Button | Any state | Downloads payroll data CSV/XLSX |
| Export Comparison Report | Export menu | Any state | Downloads comparison CSV |
| Filter | Button | Any state | Opens filter panel for employee list |
| Search Employee | Combobox | Any state | Filters employee list by name/ID |

### Pending Tasks Section (Draft state)

When a run is created, the system may flag pending tasks that must be resolved before approval:

**Observed tasks:**
1. "Add Employees" — shown when some employees have incomplete onboarding and are in a "Pending" state. Admin must either add them (complete onboarding) or skip them.
2. (Additional tasks may appear based on config: missing bank details, no salary structure assigned, etc.)

Pending tasks section is collapsible. Each task shows: task name + description + action button.

## Conditional Logic

- "Approve Payroll" option is **blocked** (hard stop) if any Pending Tasks remain. Clicking shows error toast: "Please complete your pending tasks."
- "Skip Employee?" dialog requires a Reason (mandatory). Reason is shown in the skipped employee row in the table.
- Skipped employees appear in the table with: name | "Skipped" label | Reason column content | no checkbox | no kebab.
- Employee table row click opens split panel for variable input (only for non-skipped employees in Draft state).
- "Undo Skip" appears in kebab only for employees who are currently skipped in this run.

## Cross-Module Links

- Salary Structure → determines salary component breakdown in split panel
- Employee onboarding status → gates "Add Employees" pending task
- Pay Schedule → base days, pay day shown in info strip
- TDS declarations → affects TDS computed value in split panel
- Bank details → Payment Mode column value

## Key Observations for Our Build

1. **Hard block on incomplete tasks** — Zoho does not let you approve with pending tasks. Our implementation must replicate this gate. Pending task types to support: "Add Employees" (incomplete onboarding), missing bank details, no salary structure.
2. **Pending Tasks section is dynamic** — tasks appear/disappear as admin resolves them. This requires a task-resolution polling or event-driven refresh mechanism.
3. **Three-tab layout on single URL** — tab state is managed client-side (no URL change between Employee Summary and Taxes & Deductions tabs; Overall Insights adds `?selectedTab=insights` to URL).
4. **Payslip and TDS Sheet accessible in Draft** — read-only preview before approval. Important: Payslip in Draft is a preview, not final. Our build must clearly label Draft payslips.
5. **No "Save Draft" button** — changes auto-persist. Every LOP/earning/TDS edit saves immediately via API call. Our build should follow the same auto-save pattern.
6. **Per-employee kebab options change by state** — Draft has 6 options, Approved has fewer, Paid has only Download/Send Payslip.
7. **Payroll Cost vs Total Net Pay** — these were equal (₹87,484) in this test org because PF/ESI employer contribution is zero (not configured). In a real org with PF, Payroll Cost > Total Net Pay by the employer PF/ESI contribution.

## Screenshots

- `screenshots/53-pending-tasks-expanded.png` — Pending Tasks section with 2 tasks
- `screenshots/53-add-employees-missing.png` — Add Employees page showing EMP003/004/005 pending
- `screenshots/53-skip-employee-dialog.png` — "Skip Employee?" dialog with mandatory Reason
- `screenshots/53-all-employees-skipped.png` — All 3 employees showing "Skipped in this Payrun"
- `screenshots/55-taxes-deductions-tab.png` — Taxes & Deductions tab content
- `screenshots/55-overall-insights-tab.png` — Overall Insights tab
- `screenshots/55-employee-row-kebab-draft.png` — Per-row kebab in Draft state (6 options)

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
