# Zoho Payroll — FnF Settlement Payroll (Step 2 of Exit)

**Audited:** 2026-05-24
**Tenant:** lerno
**Employee:** Arjun Mehta (EMP001), DOJ 01/04/2025, LWD 30/06/2026, Final Settlement 15/07/2026, Reason "Resigned By Employee"

## 1. Transition into this screen

After Step 1's "Proceed", Zoho shows a confirmation modal:

> ⚠ "You're about to initiate the exit process for **{Employee Name}**. Are you sure you want to proceed?"
> [Yes] [No]

On Yes → POST request creates a `termination` record. Browser navigates to:

```
/payruns/termination/{terminationId}/edit
```

Page title: **Final Settlement Payroll**. Sidebar selection switches from Employees → **Pay Runs** (FnF is a pay-run module under the hood, not an employee sub-resource).

## 2. Page header

Headline (red, large):
> **Final Settlement Payroll for {EmployeeName}({EmployeeCode})** **(POI Based)**

**POI Based** = Proof of Investments. Tag indicates TDS will be computed using the IT proofs the employee submitted (vs declared-only). Likely turns into **Declaration Based** when no proofs were submitted — to verify.

Header metadata strip (read-only labels, value below each):

| Field | Sample | Editable |
| --- | --- | --- |
| Pay Period | June 2026 | No (auto from LWD) |
| Pay Date | 15/07/2026 | No (= Final Settlement Date from Step 1) |
| Last Working Day | 30/06/2026 with pencil icon | Yes — pencil click navigates back to Step 1 form to re-edit |
| Organisation's Base Days | 30 Days | No |

Top-right toolbar icons (in this order):
- Chat/comments bubble (`comments`) — tenant-wide chat or context notes (not investigated)
- Trash (red, `data-test-selector="delete-termination-payrun"`) — abort/cancel FnF
- X (close) — exit screen without saving, back to overview

## 3. Body — section by section

### 3.1 Attendance / LOP block

| Row | Value | Notes |
| --- | --- | --- |
| Base Days for {Period} | 30 Days | Static |
| Payable Days for {Period} | 30 Days | = Base − LOP, live |
| LOP Days | spinbox + "Days" unit suffix | Only visible after clicking **+ Add LOP** |
| Actual Payable Days | recomputed live | Only visible after LOP added |

Actions:
- **+ Add LOP** (`data-test-selector="add-lop"`) — inline reveal of LOP Days input
- **+ Adjust Past LOP** (`data-test-selector="add-lop-reversal"`) — inline reveal of "Past Month LOP Adjustments" grid (see §3.2)

### 3.2 Past Month LOP Adjustments (revealed by + Adjust Past LOP)

Inline editable grid, each row:

| Column | Type | Options/Behavior |
| --- | --- | --- |
| Past Month | Searchable dropdown | Lists months from DOJ up to month-before-current (e.g. "April 2026", "May 2026") |
| Action | Dropdown | **Reversal** (refund LOP wrongly deducted earlier) · **Correction** (deduct LOP that should have been earlier) |
| Amount | Spinbox | Number of days (or ₹? — to verify; label was unitless in test) |
| Remove | Red minus icon | Removes row |

Below the row: another **+ Adjust Past LOP** link to add more month entries.

### 3.3 Earnings / Deductions section header

Two-column header strip:
- **EARNINGS / DEDUCTIONS** (label)
- **AMOUNT** (label)

### 3.4 Additional Earnings card

Sub-header: "Add any additional amount to pay, other than the regular salary."

Four pre-defined editable amount inputs:

| Label | Notes |
| --- | --- |
| Bonus | Free-form ₹ input, default 0 |
| Commission | Free-form ₹ input, default 0 |
| Leave Encashment | Free-form ₹ input, default 0 |
| Gratuity | Free-form ₹ input, default 0, with **"How it is calculated?"** link (`data-test-selector="gratuity-help-btn"`) |

**Gratuity popover content:**
> Gratuity is paid only when the employee completes 5 years of service with your company.

(Implementation: tenure = LWD − DOJ. Eligible when tenure ≥ 5 years per Payment of Gratuity Act. Arjun joined 01/04/2025, LWD 30/06/2026 = ~14 months → ineligible → default 0. Admin can still type any amount to override.)

No standard "Leave Encashment" auto-calc visible — admin enters manually. (Zoho likely doesn't track leave balance natively in this trial; full Zoho People or third-party HRMS would push the number.)

### 3.5 Deductions card

Sub-header: "One-time deductions that must be deducted from this employee's pay are listed below"

Initial empty state:
> (i) You haven't added any deductions yet
> + **Add Deduction** (`data-test-selector="add-deduction"`)

Click → dropdown showing **Create New Deduction** (since no pre-configured one-time deductions exist). Selecting it inserts an inline row:

| Column | Type |
| --- | --- |
| Deduction Name | Text input (free-form name) |
| Amount | ₹ spinbox |
| Remove | X icon |

A second **+ Add Deduction** link appears below to add more rows.

### 3.6 Notice Pay section

Single checkbox: **Does this Employee hold Notice Pay?** (`data-test-selector="notice-pay-check-box"`)
Help text under: "Check this option if, the employee is not serving the mandated notice period (you can recover an amount) or, the company terminates a employee without prior notice (you need to pay an amount)."

On check, reveals:

| Field | Type | Default | Notes |
| --- | --- | --- | --- |
| Notice pay type | Radio (Payable / Receivable) | **Receivable** | Receivable = employee owes company (didn't serve full notice). Payable = company owes employee (terminated without notice). |
| Receivable Amount (label changes with radio) | ₹ spinbox | 0 | Label flips to "Payable Amount" when Payable radio selected. |

### 3.7 Notes section

| Field | Type | Notes |
| --- | --- | --- |
| Notes | Textarea | Help text: "This will be shown in full and final settlement payslip" — i.e. text appears on the printed/emailed FnF payslip itself, not just as internal log. |

### 3.8 Bottom Note banner (always visible above footer)

Amber info banner — content depends on whether employee submitted IT proofs:

> ⚠ "Looks like this employee hasn't submitted the investment proofs. Make sure you collect them before processing the Final Settlement payroll."

Implication: TDS on FnF is computed using POI when available; absence flagged so admin doesn't compute TDS on declarations alone (which legally cannot be honored at year-end for FnF).

### 3.9 Footer actions

| Button | Selector | Behavior |
| --- | --- | --- |
| Reset | `reset-data` | Clears all amounts/inputs on the form, keeps the termination record itself |
| Save and Continue | `save-and-continue` | Primary blue — saves and likely moves to next step (preview / approve) |
| Save Draft | `save-draft` | Persists state but stays on this page |

(No "Cancel" button — close-X in header is the cancel path. Trash icon deletes the entire FnF termination.)

## 4. Side-effects of initiation (what changes elsewhere)

After Step 1 confirmation:
- Employee's status in Employees list flips from **Active** → **Active (Will be resigned on dd/MM/yyyy)** (badge on row)
- A `termination` payroll-run record is created (separate from regular pay-run; visible likely under Pay Runs as a special row)
- Sidebar **Pay Runs** is now the active section when navigating to the FnF screen

(Yet to verify if regular monthly pay-run continues to include this employee or whether they're auto-skipped from the next regular run that contains their LWD.)

## 5. Field-by-field implementation notes for our schema

| Zoho field | Suggested storage |
| --- | --- |
| Last Working Day | `employee_exits.last_working_day date NOT NULL` |
| Reason for Exit | `employee_exits.exit_reason text NOT NULL` (enum: Terminated/Death/Disability/Resigned) |
| Final Settlement Date | `employee_exits.settlement_date date NULL` (NULL = follow next regular schedule) |
| Personal Email | `employee_exits.personal_email text NULL` |
| Notes (Step 1) | `employee_exits.notes text NULL` |
| FnF Bonus, Commission, Leave Encashment, Gratuity | `payrun_termination.{bonus,commission,leave_encash,gratuity} numeric(18,2)` |
| Adhoc deductions (inline) | new entity `termination_deductions(termination_id, name, amount)` — name is free-form per row |
| Past Month LOP adjustments | new entity `past_lop_adjustments(termination_id, month, action enum(Reversal,Correction), days int)` |
| Notice Pay holding | `payrun_termination.has_notice_pay bool`, `notice_pay_direction enum(Payable,Receivable)`, `notice_pay_amount numeric(18,2)` |
| Notes (FnF screen) | `payrun_termination.payslip_notes text` (printed on FnF payslip) |

`employee_exits` table already exists in our schema (per Phase 6 docker DB inspection: `tenant_kerala_test_corp.employee_exits`). Phase 13 audit step will confirm which columns are already there.

## 6. Open questions / next steps

- Click **Save and Continue** — what does Step 3 look like? (preview of computed FnF? approval flow?)
- POI vs Declaration tag — confirm by initiating exit on an employee with submitted proofs
- Delete (trash) vs Reset semantics — what survives each?
- Last Working Day pencil → Step 1 — does it carry forward all already-entered FnF figures, or reset them?
- Past Month LOP "Amount" column — confirm whether the unit is days (most likely) or rupees
- Default Leave Encashment / Gratuity calc — does Zoho auto-pre-fill when leave balance/tenure data is available?
- "POI Based" — captured as banner-style metadata or as a flag on the termination record?

## 7. Screenshots captured (under docs/ba-audit/exit/)

- `zoho-fnf-step1.png` — landing on FnF screen
- `zoho-fnf-mid.png` — middle scroll (Deductions/Notice Pay/Notes)
- `zoho-fnf-bottom.png` — bottom (banner + Reset/Save buttons)
- `zoho-fnf-gratuity-help.png` — gratuity 5-year popover
- `zoho-fnf-add-deduction.png` — Add Deduction → Create New Deduction tooltip
- `zoho-fnf-new-deduction.png` — inline deduction row added
- `zoho-fnf-notice-pay.png` — Notice Pay expanded with Payable/Receivable radio
- `zoho-fnf-add-lop.png` — LOP Days input revealed
- `zoho-fnf-past-lop.png` — Past Month LOP grid revealed with month dropdown
- `zoho-fnf-past-lop-action.png` — Reversal/Correction options
- `zoho-fnf-toolbar.png` — full screen baseline
