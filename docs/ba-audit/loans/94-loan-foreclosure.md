# Item 94: Loan Foreclosure and Lifecycle Actions

**URL / Navigation Path:** `https://payroll.zoho.in/#/loans/{record_id}`
**Module:** Loans
**Entry Point:** Loan Detail page → "More" button (kebab/three-dot) → dropdown menu

---

## Purpose

Document the loan lifecycle management actions available after a loan is created — including edit, pause, delete, and any foreclosure mechanisms.

---

## Screenshots

- `screenshots/94-loan-more-actions-dropdown.png` — More actions menu (Edit Loan | Pause Instalment Deduction | Delete Loan)
- `screenshots/94-pause-instalment-modal.png` — Pause Loan Instalment modal

---

## Actions Available on Loan Detail

### Primary Action
**"Record Repayment"** button (visible on loan detail, always accessible):
- Opens Record Repayment modal (see Item 90)
- For out-of-band/manual repayments

### More Actions Dropdown (kebab menu)

Triggered by "More" button (class: `btn-more`). Three options:

---

### 1. Edit Loan

**URL:** `#/loans/{id}/edit`
**Title:** "Edit Loan details for {Employee Name}"

All loan fields are editable after creation (same form as Create Loan):
- Loan Name
- Employee Name
- Loan Amount
- Disbursement Date
- Reason
- Exempt checkbox
- EMI Deduction Start Date
- Instalment Amount

**Implication:** Changing instalment amount or loan amount will recalculate the number of instalments and closing date. No constraint observed preventing edits on active/partially repaid loans — this is a design risk (edit after partial repayment could create inconsistency).

---

### 2. Pause Instalment Deduction

**Modal Title:** "Pause Loan Instalment"

#### Fields

| Field | Type | Required | Options / Notes |
|-------|------|----------|-----------------|
| Pause Instalment From | Radio button group | Yes | "Immediately" / "On Scheduled Month" |
| Resume On | Date picker | No | Optional date to auto-resume deductions |
| Reason | Textarea | Yes | Max 100 characters |

**Actions:** Save | Cancel

**Effect:** Changes loan status to **Paused**. EMI deduction is suspended for pay runs during the pause period. The loan appears in "Paused Loans" filter.

**"On Scheduled Month" option** — likely means pause begins from the next scheduled EMI date rather than the current pay cycle.

**Resume On** — if filled, deductions auto-restart on specified date. If blank, manual resume required (likely via same dropdown showing "Resume" after pause).

---

### 3. Delete Loan

**Confirmation dialog:** "Are you sure you want to delete {Loan Type} for {Employee Name}?"
**Actions:** Yes | No

**Effect:** Permanently deletes the loan record. No soft-delete/archive observed — this appears to be a hard delete.

**Business Risk:** No warning about existing repayment records was observed. If repayments have been processed, deleting the loan may cause data integrity issues.

---

## Foreclosure (Closing a Loan Early)

**No explicit "Foreclose Loan" or "Close Loan" action was found** in the UI for loans in "Open" status with no repayments made.

The "Closed" status does appear in the filter options, indicating loans can reach Closed state. Likely mechanisms for closure:

1. **Automatic closure** — when final instalment is processed by pay run (all instalments completed)
2. **Full prepayment via Record Repayment** — recording a repayment equal to the remaining amount may trigger closure. (Not tested — repayment date constraints prevented testing in this session.)

If a "Foreclose" option becomes available after partial repayments, it would likely appear in the "More" dropdown alongside Edit/Pause/Delete.

**Gap identified:** Zoho Payroll does not provide a dedicated "Foreclose Loan" button for immediate closure with partial repayment settlement. This is a limitation for clients who need to handle loan foreclosures (e.g., employee exits, voluntary prepayment).

---

## Loan Status State Machine

```
[Form Submitted] → Open
      |
      ├── Pause Instalment Deduction → Paused
      |         |
      |         └── Resume → Open
      |
      ├── Delete Loan → [Deleted]
      |
      ├── Final EMI processed → Closed (automatic)
      |
      └── Full prepayment recorded → Closed (likely)
```

**Observed statuses:** Open, Paused, Closed (from filter options)
**Not observed:** Draft, Pending Approval, Foreclosed (no distinct "Foreclosed" status)

---

## Business Rules

1. **Edit Loan** is available on Open loans regardless of repayment history — potential data integrity risk.
2. **Pause Instalment** requires a reason (max 100 chars) and optionally a resume date.
3. **Delete Loan** is permanent with a simple Yes/No confirmation — no undo mechanism.
4. **Closed status** appears achievable only via full repayment (automatic or manual).
5. **No foreclosure workflow** — no dedicated UX for early settlement with balance waiver or penalty calculation.

---

## Cross-Module Impact

- Pausing a loan affects EMI deductions in pay runs during the pause period.
- Deleting a loan after EMI deductions may leave orphaned payslip line items (risk).
- Loan closure affects the "Closed Loans" filter and removes from "Open Loans" count.

---

## Open Questions

- [ ] Does "Record Repayment" for the full remaining amount automatically close the loan?
- [ ] Is there a "Resume Instalment Deduction" option visible when loan is Paused?
- [ ] Does deletion of a loan reverse or void previously processed EMI deductions in past pay runs?
- [ ] Is there a "Foreclose" option with penalty/waiver calculation for enterprise use cases?
- [ ] What happens to a loan when the employee is terminated? Is it auto-closed or remains Open?
- [ ] Is there a distinction between "Closed" (fully repaid) and "Foreclosed" (early settlement) in Zoho's data model?
- [ ] Can a Closed loan be reopened?
