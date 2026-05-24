# Pay Runs Module — Audit File 52
**Date:** 2026-05-15
**Reference App:** Zoho Payroll India (payroll.zoho.in, org: lerno)
**Status:** COMPLETE — full cycle from creation to payment documented
**Employees Used:** EMP001 (Arjun Mehta, ₹70,000/mo), EMP002 (Priya Sharma, ₹22,000/mo)
**Pay Run Executed:** April 2026 (01/04/2026 – 30/04/2026)

---

## Page 1: Pay Runs List — "Run Payroll" Tab

**URL:** `#/payruns`
**Title:** "Pay Runs | Pay Runs | Zoho Payroll"
**Module:** Pay Runs

### Layout

Top sub-nav with 2 tabs: "Run Payroll" (current) | "Payroll History"
Toolbar right: "New" dropdown button | "Instant Helper" button

### "New" Button Dropdown

Opens a dropdown with 2 options (NOT for creating a regular pay run — regular runs are auto-created by the system):
1. **One Time Payout** — ad-hoc non-salary payment outside regular cycle
2. **Off Cycle Payrun** — mid-cycle pay run

### Regular Pay Run Card (per period)

One card per pending pay run period. System auto-generates the next period's card after current one is paid.

Fields on the card:
| Field | Value | Notes |
|---|---|---|
| Period Heading | "Process Pay Run for [Month] [Year]" | Auto-generated from pay schedule |
| Status Badge | "Ready" | Shown when pay run is pending |
| Employees' Net Pay | "Yet To Process" | Until pay run created |
| Payment Date | dd/MM/yyyy | From pay schedule configuration |
| No. of Employees | integer | Total active employees |
| Create Pay Run button | primary CTA | Triggers pay run creation |
| Overdue warning | "You haven't processed this pay run and it's past the pay day." | Shows when pay day has passed |
| Urgency alert | "Please process and approve this pay run before {date}" | Shows upcoming deadline |

**Business Rules:**
- Only one "current" pay run card shown at a time in "Run Payroll" tab
- Card auto-advances to next period once current period is paid
- Regular pay runs auto-created — no period selection by user
- Payout banner at top: "Pay salaries from any bank account without switching to your bank portal using Payouts by Zoho Payments" — integration with Zoho Payments for bank transfer automation

---

## Page 2: Pay Run — "Add Employees" Gate

**URL:** `#/payruns/{run_id}/add-employees?employee_type=employee&filter_by=Employee.MISSING&page=1&per_page=100`
**Triggered by:** Clicking "Create Pay Run" on the list page, then "View Employees" on pending tasks warning
**Also triggered by:** Clicking "View Employees" link on the "WITHOUT_PAN" filter variant

### Variants

Two filter_by values create two distinct page states:

#### Variant A: `filter_by=Employee.MISSING`
- Page heading: "Regular Payroll (01/04/2026 to 30/04/2026)"
- Message: "The following employees are yet to be added in this payrun. Complete/Add employees to process payroll for this period."
- Table columns: [checkbox], Employee Name (link), Onboarding Status, Work Email, Designation, [actions]
- Per-row actions: "Complete Now" link → `#/people/employees/{id}?from=payrun&from_route_id={run_id}` | "Skip" button
- Multi-select: bulk action bar appears with "Add to payroll" + "Skip" buttons + "N Selected" count + "Esc" button
- Kebab dropdown: "Delete Pay Run" only

#### Variant B: `filter_by=Employee.WITHOUT_PAN`
- Page heading: "Employees without PAN number"
- No checkbox — no bulk action for PAN missing employees
- Per-row action: "Update Now" link → `#/people/employees/{id}/edit/personal-details?from=employees-with-incomplete-details`
- Cannot skip PAN-missing employees from this page — must update their PAN

### Skip Employee — Single Dialog

Triggered by: "Skip" per-row button

| Field | Type | Notes |
|---|---|---|
| Employee (display) | read-only | Shows employee name |
| Payroll Period (display) | read-only | Shows period |
| Please enter a reason* | textbox | MANDATORY. Validated by API |

**Buttons:** "Proceed" | "Cancel"
**Warning:** "Once you skip an employee(s) from the pay run, you will not be able to pay them later for this pay cycle"

### Skip Employee — Bulk Dialog

Triggered by: Select all → "Skip" bulk action

Identical to single dialog except:
- Shows "Number of Employee(s): N" instead of employee name
- Same mandatory reason field

**API:** `PUT /api/v1/payrollruns/{run_id}/employees/skip?notes={reason}&pay_joinee_arrear_later=false`
**Note:** `pay_joinee_arrear_later` boolean param — if true, skipped new joinee's prorated salary can be paid as arrear in next run
**API Validation:** Rejects notes with certain characters (hyphen `-` observed to cause 400). Short alphanumeric reason text required.

### Business Rules
- Skipping is PERMANENT for that pay cycle — cannot be undone via UI
- Skipped employees still appear on the Add Employees page with "Skipped in this Payrun" status + disabled checkbox
- "Continue to Payroll" bypasses all pending employees and returns to preview
- PAN-missing employees: warning shown but NOT a hard block to creating the pay run
- Employees missing PAN have TDS computed at flat 20% (statutory rule — Income Tax Act §206AA)

### Post-Skip State

Skipped employees shown with:
- "Skipped" badge in name cell
- "Reason: {entered reason}" in dedicated cell
- "Complete Now" link (for future pay runs)
- Checkbox disabled (cannot re-add within this pay run)

### Navigation
- Back link → `#/payruns`
- Continue to Payroll → `#/payruns/{run_id}/preview`

---

## Page 3: Pay Run Preview

**URL:** `#/payruns/{run_id}/preview`
**Also with query params:** `?selectedTab=employee|deductions|insights`
**Status:** Draft

### Header

| Element | Notes |
|---|---|
| Back link | → `#/payruns` |
| "Regular Payroll" text | Pay run type label |
| "Draft" badge | Current state |
| "Comments" button | Add comments to pay run |
| "Submit and Approve" button | Primary CTA — blocked by pending tasks |
| Kebab dropdown | "Show Downloads" + "Delete Pay Run" |

### Warning Banners (conditional)

1. **Overdue banner:** "This payment is overdue by N day(s)." — appears when pay day has passed
2. **Pending Tasks banner:** "Pending Tasks" with expandable tasks list
   - Task 1: "N employees are not included in this payroll." → "View Employees" button
   - Task 2: "1 Employee(s) PAN details are not found." → "View Employees" button
   - "+N more task(s) to be completed before you approve this payroll." — expands to show all tasks

### Summary Card (left panel)

| Field | Example | Notes |
|---|---|---|
| Period | "01/04/2026 - 30/04/2026" | Full period dates |
| Base Days | "30 Base Days" | Calendar days in period |
| Month | "April 2026" | Human-readable |
| Payroll Cost | ₹92,000.00 | = Gross pay of included employees |
| Total Net Pay | ₹92,000.00 | = Net after deductions (₹0 deductions in this case) |
| Pay Day | "30 Apr, 2026" | From pay schedule |
| Employee Count | "2 Employees" | Only included (non-skipped) |

### Taxes & Deductions Summary (mini table in card)

| Row | Value |
|---|---|
| Taxes | ₹0.00 |
| Benefits | ₹0.00 |
| Donations | ₹0.00 |
| Total Deductions | ₹0.00 |

### Table Tabs

3 tabs: "Employee Summary" | "Taxes & Deductions" | "Overall Insights"

#### Tab 1: Employee Summary

Toolbar: "All Employees" button | "Select an Employee" search combobox | "Filter" button | "Import / Export" button

Table columns:
| Column | Type | Notes |
|---|---|---|
| [checkbox] | multi-select | Select for bulk actions |
| Employee Name | button (clickable) | Opens per-employee split panel |
| Paid Days | integer | 30 for full month |
| Gross Pay | currency | Sum of all earning components |
| Deductions | currency | Post-tax deductions |
| Taxes | currency | Income Tax + PT |
| Benefits | currency | Pre-tax deductions (e.g., VPF) |
| Reimbursements | currency | Approved reimbursement claims |
| Net Pay | currency | Gross - Deductions - Taxes + Benefits |
| [kebab] | dropdown | Per-employee actions |

Skipped employee rows: different structure — no Paid Days / Gross / Net Pay columns; shows "Skipped" + "Reason: ..." + "Complete Now" link

#### Per-Employee Split Panel (dialog overlay)

Triggered by: clicking employee name button in table.

Header:
- Employee name link → employee profile
- "Net Pay" label
- Emp. ID: {code}
- Net Pay amount

Body sections:

**1. Paid Days table**
- Row: "Payable Days: {N}" — calendar days in period

**2. LOP (Loss of Pay) section**
- "Add LOP" button → expands LOP Days form
  - "LOP Days" spinbutton (default 0) + "Days" label + remove X button
  - "Actual Payable Days" auto-calculates: Payable Days − LOP Days
  - Dynamically updates earnings (proration applied in real-time)
- LOP kebab dropdown: "Adjust Past LOP" — for retroactive LOP entry from prior pay periods

**3. Earnings table (+)**
- Columns: (+) Earnings | [empty] | Amount | [actions]
- Rows: one per component
- In this pay run: Basic ₹39,998 | HRA ₹15,999 | Fixed Allowance ₹14,003 (total ₹70,000 for EMP001)
- "Add Earning" button → adds one-time earning to this pay run for this employee

**4. Deductions table (-)**
- "Taxes" section header
- Income Tax row: amount + **"Edit" button** → inline TDS override form
  - Fields: Amount (spinbutton) | "Calculated Value: {N}" helper | Reason* (mandatory textbox)
  - Close X button to cancel edit
- KL Professional Tax row: read-only amount (Kerala — based on employee work state)

**5. Net Pay** — auto-computed total

**Footer buttons:** "Save" | "Cancel" | "Click Save to update Net Pay" notice (appears after any change)

**Business Rules:**
- Changes to earnings/LOP in Draft state require "Save" to apply — not live-updated to summary table
- Income Tax "Edit" allows admin to override engine-computed TDS with mandatory reason (audit trail)
- "Add Earning" allows variable/one-time earnings per employee per run (e.g., bonus, arrear)

#### Tab 2: Taxes & Deductions

3 sections:

**Tax Details table:**
| Column | Notes |
|---|---|
| Tax Name | Income Tax, KL Professional Tax (Head Office) |
| Paid By Employer | always ₹0 for these taxes |
| Paid By Employee | TDS + PT amounts |

Note: PT label includes work location in brackets: "KL Professional Tax (Head Office)" — confirms PT is work-location specific.

**Benefits table:**
- Columns: Benefit Name | Employer's Contribution | Employees' Contribution
- Empty state: "There are no deductions present in this payrun."

**Donations table:**
- Columns: Deduction Name | Employees' Contribution
- Empty state: "There are no donations present in this payrun."

#### Tab 3: Overall Insights

**Employee Breakdown metrics:**
| Metric | Value | Notes |
|---|---|---|
| Active Employees | 2 | Count with icon |
| Paid Employees | 0 | Always 0 in Draft |
| New Joinee's Skipped | 0 | New employees whose first pay run was skipped |
| Skipped Employees | 0 | Explicitly skipped |
| Salary Withheld Employees | 0 | Hold salary applied |
| New Joinee's Arrear Released | 0 | Arrear from prior skip |
| Salary Released Employees | 0 | Withheld salary released |
| LOP Reversed Employees | 0 | LOP reversal applied |

**Statutory Summary:** "No data to display" — when PF/ESI/PT all ₹0

**Payment Mode Summary table:**
| Mode | Count |
|---|---|
| Direct Deposit | 0 |
| Bank Transfer | 2 |
| Cheque | 0 |
| Cash | 0 |

**Component Wise Breakdown** (collapsible accordion):
- Section: "Base Earning ₹{total}" — expandable via chevron
- Table: Components | Employees Involved | Total Amount
- Per component: clickable link → `#/payruns/insights/{run_id}/earnings/{component_id}?override_type=` (drill-down insights)

Example (April 2026 — 2 employees):
| Component | Employees | Total |
|---|---|---|
| Basic | 2 | ₹50,998.00 |
| House Rent Allowance | 1 | ₹15,999.00 |
| Fixed Allowance | 2 | ₹25,003.00 |

### Submit and Approve — Pre-conditions and Blocking Rules

**Hard block (returns error toast):** Pending Tasks NOT completed:
- "3 employees are not included in this payroll" — blocks approval
- Error toast: "Please complete your pending tasks to approve this payroll."

**Soft warning (allows approval):** PAN missing for one employee:
- "1 Employee(s) PAN details are not found" — shows in pending tasks
- Does NOT block "Submit and Approve" — dialog still opens

### Approve Payroll Confirmation Dialog

Triggered by: "Submit and Approve" with all hard-block tasks resolved

Content:
- Title: "Approve Payroll"
- Warning: "On approving this payroll, your employees will not be able to,"
  - "Raise any Reimbursement claims for this month"
  - "Declare or update the IT or POI declaration for this month"
- Buttons: "Submit and Approve" | "Cancel"

**Business Rules:**
- Reimbursement claims locked after approval
- IT Declaration and POI locked after approval
- No approval password or second-factor required in test environment

### State Transition

**Draft → "Payment Due"** on approval
- URL stays at `#/payruns/{run_id}/preview` momentarily → redirects to `#/payruns/{run_id}/summary`
- Status badge changes from "Draft" to "Payment Due"
- Notification count increments (+1)

---

## Page 4: Pay Run Summary

**URL:** `#/payruns/{run_id}/summary`
**Status sequence:** Payment Due → Paid

### Header (Payment Due state)

| Element | Notes |
|---|---|
| Back link | → `#/payruns` (while Payment Due) |
| "Regular Payroll" + "Payment Due" badge | State indicator |
| Icon button (unknown — Comments) | Same as in preview |
| "Record Payment" button | Primary CTA |
| Kebab dropdown | "Show Downloads" + "Delete Pay Run" |

### Summary Card Changes (vs Preview/Draft)

- NEW: **"Download Bank Advice"** button appears — generates bank transfer advice file
- Employee count shows: "5 Employees | (3 Skipped)" — total org count now visible

### Employee Table — Major Column Change

**Payment Due / Paid state columns:**
| Column | Notes |
|---|---|
| [checkbox] | Multi-select |
| Employee Name | clickable — opens view-only split panel |
| Paid Days | integer |
| Net Pay | currency |
| **Payslip** | "View" button — opens view-only salary breakdown panel |
| **TDS Sheet** | "View" button — opens TDS computation |
| **Payment Mode** | "Manual Bank Transfer" / "Direct Deposit" / "Cheque" / "Cash" |
| **Payment Status** | "Yet To Pay" → "Paid on {date}" |
| [kebab] | Per-row actions |

**Key change from Draft:** Gross Pay, Deductions, Taxes, Benefits, Reimbursements columns REMOVED. Net Pay remains. Payslip, TDS Sheet, Payment Mode, Payment Status columns ADDED.

**Toolbar change:** "Import / Export" → "Export Data" only (no import after approval)

### View-Only Split Panel (post-approval)

Same layout as Draft per-employee panel but:
- No "Add LOP" button
- No "Add Earning" button
- No "Edit" on Income Tax (read-only)
- No "Save" / "Cancel" buttons

### Record Payment Dialog

Fields:
| Field | Type | Default | Notes |
|---|---|---|---|
| Payment Date | date input (dd/MM/yyyy) | Scheduled pay day | Admin can override |
| Payment Mode Summary | read-only table | Bank Transfer: 2 | Summary by mode |
| Send payslip notification | checkbox | CHECKED | Triggers email to employees |

Checkbox behavior:
- When checked: sends email with link to portal to view payslip
- Employees without portal: payslip downloadable directly from email
- Unchecked: payslip still appears in portal after payment is recorded

**Buttons:** "Confirm" | "Cancel"

**Post-record state:**
- Status: "Payment Due" → "Paid"
- Payment Status per row: "Yet To Pay" → "Paid on {date}"
- Back link changes: → `#/payruns/payroll-history` (not `#/payruns`)
- "Record Payment" button → "Send Payslip" button
- Notification count +1
- "Download Bank Advice" still available

### Paid State — Actions Available

**Page-level kebab:**
1. "Download all Payslips" — bulk ZIP/PDF of all employee payslips
2. "Download all TDS Worksheets" — bulk TDS computation sheets
3. "Show Downloads" — download history panel (url: `?canShowDownloadHistory=true`)
4. "Delete Recorded Payment" — REVERSES payment status back to "Payment Due"

**Per-employee row kebab (paid state):**
1. "Download Payslip" — individual payslip PDF
2. "Send Payslip" — resend payslip email to this employee

**Page-level button:** "Send Payslip" — bulk resend to all employees

---

## Page 5: Payroll History

**URL:** `#/payruns/payroll-history`
**Title:** "Payroll History | Pay Runs | Zoho Payroll"

### Toolbar

Filter: "Payroll Type:" combobox
- Default: "All"

### Payroll Type Filter Options (8 types)

| Option | Description |
|---|---|
| All | Default — all types |
| Regular Payroll | Monthly regular pay run |
| Past Payroll | Catch-up run for prior periods |
| Final Settlement Payroll | F&F run for one employee exit |
| One Time Payout | Ad-hoc payment outside regular cycle |
| Off Cycle Payroll | Mid-cycle pay run |
| Bulk Final Settlement Payroll | F&F run for multiple exiting employees |
| Resettlement Payroll | Arrear / pay revision adjustment run |

### History Table

| Column | Notes |
|---|---|
| Payment Date | Date payment was recorded |
| Payroll Type | From the 8-type enum above |
| Details | Pay period range (clickable → summary page) |
| Payroll Status | "Paid" badge |

---

## Pay Run State Machine

```
[Auto-created for period]
        ↓
    READY (card on Run Payroll tab)
        ↓ Create Pay Run
    DRAFT (preview page)
        ↓ Submit and Approve (with all tasks resolved)
  PAYMENT DUE (summary page)
        ↓ Record Payment
    PAID (summary page — moved to Payroll History)
        ↓ Delete Recorded Payment
  PAYMENT DUE (can be re-recorded)
```

Additional transitions:
- Draft → DELETED (via "Delete Pay Run" in kebab)
- PAID → PAYMENT DUE (via "Delete Recorded Payment") — reversible

**Status badge text mapping:**
| Internal state | Badge shown |
|---|---|
| Ready | "Ready" (on list card) |
| Draft | "Draft" |
| Approved | "Payment Due" |
| Paid | "Paid" |

---

## Business Rules — Pay Run Module

### Creation Rules
- Regular pay runs auto-created by system per pay schedule — admin cannot create a new regular period manually
- "New" button only creates One Time Payout or Off Cycle Payrun
- Pay schedule defines: payment day, pay period start/end, base days calculation method

### Employee Inclusion Rules
- Active employees with complete profiles auto-included
- Employees with incomplete onboarding (status = Pending) excluded — shown in "MISSING" filter
- Employees can be skipped (PERMANENT for that pay cycle) with mandatory reason
- `pay_joinee_arrear_later=false/true` — if true, skipped new joinees can get arrear in next run
- PAN-missing employees: included in pay run but warning shown; TDS computed at 20% flat (§206AA)

### LOP Rules
- LOP (Loss of Pay) entry per employee per run in the split panel
- Actual Payable Days = Payable Days − LOP Days
- Earnings auto-prorate based on actual payable days vs base days
- "Adjust Past LOP" allows retroactive LOP correction for prior pay periods

### TDS Override Rule
- Admin can override engine-computed Income Tax per employee per run
- "Calculated Value" shown for reference
- Mandatory reason required for audit trail
- Override applies to this run only; not carried to future runs

### Approval Gates (hard blocks)
- Any employee in "MISSING" status blocks approval
- Pending tasks list must be cleared (or employees skipped)
- PAN missing = soft warning only (not a block)

### Post-Approval Locks
- Reimbursement claims: locked (cannot raise or modify)
- IT Declaration: locked (cannot update exemptions/investments)
- POI: locked (cannot update proof of investments)
- Salary breakdown: read-only (no LOP, no Add Earning, no TDS edit)

### Payment Recording Rules
- Admin sets payment date (default = pay schedule pay day; overridable)
- "Send payslip notification" default ON — emails employees with payslip link/download
- Payslip visible in employee portal regardless of notification setting, once payment recorded
- Payment can be "deleted" (reversed) — resets to "Payment Due" state

---

## Data Relationships

- PayRun → PaySchedule (N:1) — schedule defines pay day, period
- PayRun → Employee (M:N via PayRunEmployee junction)
  - PayRunEmployee has: paid_days, lop_days, gross, net, taxes, payment_status
- PayRun → SalaryComponent (M:N via PayRunComponentBreakdown)
- PayRun → Payslip (1:N — one per included employee)
- PayRun → TDSWorksheet (1:N — one per included employee)
- PayRun → BankAdvice (1:1 — aggregate bank transfer file)

---

## EMP001 Salary Breakdown — April 2026

**CTC:** ₹70,000/month (∼₹8,40,000 annual)

| Component | Amount | % of Monthly Gross |
|---|---|---|
| Basic | ₹39,998.00 | ~57% |
| House Rent Allowance | ₹15,999.00 | ~23% |
| Fixed Allowance | ₹14,003.00 | ~20% (residual) |
| **Gross Pay** | **₹70,000.00** | 100% |
| Income Tax | ₹0.00 | — |
| KL Professional Tax | ₹0.00 | — (below PT threshold) |
| **Net Pay** | **₹70,000.00** | — |

Note: Basic = ~57% (not 50%) suggests EMP001's salary structure was configured with Basic at a higher percentage than 50%. Or the annual CTC entered was different. The Fixed Allowance = residual (₹70,000 − ₹39,998 − ₹15,999 = ₹14,003) confirms the residual invariant.

---

## API Contracts Observed

| Operation | Method + Endpoint |
|---|---|
| Skip employee(s) from run | PUT `/api/v1/payrollruns/{run_id}/employees/skip?notes={reason}&pay_joinee_arrear_later={bool}` |
| Create pay run | (triggered by "Create Pay Run" — exact endpoint not captured) |
| Approve pay run | (triggered by "Submit and Approve" — exact endpoint not captured) |
| Record payment | (triggered by "Confirm" in Record Payment dialog) |
| Component insight drill-down | GET `#/payruns/insights/{run_id}/earnings/{component_id}?override_type=` |
| Add employees page (missing filter) | `#/payruns/{run_id}/add-employees?filter_by=Employee.MISSING` |
| Add employees page (PAN filter) | `#/payruns/{run_id}/add-employees?filter_by=Employee.WITHOUT_PAN` |

---

## Key Findings

### Zoho Pay Run Model vs Our Data Model

| Zoho Behavior | Our Current Model | Gap |
|---|---|---|
| Status: Ready, Draft, Payment Due, Paid | PayrollRunStatus: Pending, Processing, Draft, Finalised, Failed | "Payment Due" = Approved (between our Draft and Finalised). "Paid" = our Finalised. |
| LOP per employee per run | Not yet modeled | Need `PayRunEmployee.lop_days` field |
| TDS override per employee per run | Not yet modeled | Need `PayRunEmployee.tds_override_amount` + `tds_override_reason` |
| Pay run types: 8 types | PayrollRunType: Regular, FullAndFinal | Missing: Past, OneTimePayout, OffCycle, BulkF&F, Resettlement |
| Skip with reason | Not modeled | Need skip mechanism + reason storage |
| Payslip per employee | Not modeled | Payslip entity needed |
| Bank Advice file | Not modeled | Aggregate file generation |
| `pay_joinee_arrear_later` | Not modeled | Arrear mechanism for skipped joinee |

### 🔴 Critical Gaps

1. **Payroll Run Status mismatch:** Our `PayrollRunStatus` enum is missing "Approved" (Payment Due) as a distinct state. "Finalised" conflates approved-but-not-paid with paid.
2. **LOP not modeled:** No `lop_days` field in pay run employee junction. LOP is core to Indian payroll (salary = (days worked / calendar days) × monthly).
3. **TDS override not modeled:** No mechanism for admin to override engine-computed TDS per employee per run.
4. **Payslip entity missing:** No Payslip entity or generation pipeline.
5. **8 pay run types:** Only 2 modeled (Regular, FullAndFinal). Missing OneTimePayout, OffCycle, PastPayroll, BulkF&F, Resettlement.

### 🟡 Ambiguities

1. **EMP001 Basic = ₹39,998 (57%) not ₹35,000 (50%):** Needs investigation — was the salary structure configured with Basic > 50% CTC? Or is the ₹70,000 CTC monthly (not per component calculation basis)?
2. **Residual Fixed Allowance rounding:** ₹14,003 (not ₹14,002 or ₹14,000). Component-level rounding rules need to be explicit — which component absorbs rounding?
3. **PT threshold check:** "KL Professional Tax ₹0.00" — EMP001's salary ₹70,000 >> Kerala PT slabs. PT should be non-zero for ₹70,000. Possible reason: PT not configured in org settings for this test org.

### 🟢 Well-Implemented

1. **Complete audit trail for skip:** Mandatory reason, permanent status, visible in summary table
2. **Pending Tasks system:** Structured pre-approval gate with actionable links to resolve each task
3. **LOP with real-time recalculation** in split panel
4. **Payment mode tracking:** Per-employee payment mode stored and visible in summary
5. **Download Bank Advice:** Post-approval artifact for bank operations team
6. **Payslip notification control:** Per-run opt-out with clear explanation of fallback behavior

---

## Navigation Flows

**Entry points to Pay Runs module:**
- Sidebar: "Pay Runs" link → `#/payruns`
- Getting Started step 5 ("Run Payroll") — not audited

**Pay Run lifecycle navigation:**
```
#/payruns (list — Run Payroll tab)
  → Create Pay Run
    → #/payruns/{id}/add-employees (if employees missing)
      → Complete Now → employee profile with payrun context
      → Skip → (dialog) → back to add-employees
      → Continue to Payroll → preview
    → #/payruns/{id}/preview (draft state)
      → View Employees (pending tasks) → add-employees
      → Submit and Approve → confirmation dialog → summary
    → #/payruns/{id}/summary (payment due state)
      → Record Payment → dialog → summary (paid state)
      → Download Bank Advice → file download
#/payruns/payroll-history (all paid runs)
```

---

## Open Questions

- [ ] What is EMP001's Basic salary structure — was it configured as 50% of what base? Why is Basic ₹39,998 (57%) and not ₹35,000 (50%)?
- [ ] Why is KL Professional Tax ₹0.00 for EMP001 who earns ₹70,000/month — is PT unconfigured in this org?
- [ ] Does "Delete Recorded Payment" require a reason (like Skip does)?
- [ ] What does the "Comments" button (icon-only) do — is it a comment thread on the pay run?
- [ ] What are the available options when clicking "Send Payslip" (bulk) — all employees? Specific employees?
- [ ] Does "Adjust Past LOP" open a date-period picker to select which past month to adjust?
- [ ] What happens if admin enters negative LOP days or LOP days > Payable Days?
- [ ] What's the "One Time Payout" creation flow — does it require selecting employees and amounts?
- [ ] What's the "Off Cycle Payrun" creation flow — does it require a different pay period selection?
- [ ] The "Import / Export" button in Draft state — what import types are available for a pay run?
- [ ] Does the "Payroll Cost" equal "Total Net Pay" when there are no employer contributions (PF, ESI)? Should Payroll Cost = Gross + Employer PF + ESI?
