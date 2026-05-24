# Pay Runs > Reprocess & Revision — Delete Recorded Payment, Revise Salary

## URL / Navigation Path

Reprocess/revision actions accessed from:
`https://payroll.zoho.in/#/payruns/{id}/summary` (Paid or Approved state)

- **Delete Recorded Payment**: Page kebab > "Delete Recorded Payment" (Paid state only)
- **Revise Salary**: Row kebab > "Revise Salary" (Draft state only — not present in Paid state)

## Purpose

Documents mechanisms available for correcting or revising payroll after it has progressed beyond Draft state. Two distinct concepts:

1. **Delete Recorded Payment** — reverses the "Paid" status back to "Approved", enabling re-entry of payment details
2. **Revise Salary** — (Draft state only) initiates a salary revision for the employee that takes effect in this or a future pay run

## Delete Recorded Payment

### Availability

Page kebab in **Paid state** only. Not available in Approved, Draft, or other states.

### Dialog

| Field | Type | Content |
|-------|------|---------|
| Message | Paragraph | "You're about to delete the recorded payment for this pay run. Are you sure you want to proceed?" |
| Yes button | Button | Confirms deletion; state → Approved |
| No button | Button | Dismisses dialog; no change |

### Post-Action Behaviour

- Pay run status reverts: Paid → Approved
- Payment date, mode, reference number all cleared
- "Record Payment" action becomes available again
- Variable inputs remain locked (still Approved, not Draft)
- Admin must re-Record Payment with correct details

### State Transition

```
PAID
   └─ [Delete Recorded Payment → Yes] → APPROVED
                                              └─ [Record Payment] → PAID (again)
```

To go back further (to Draft for changing variable inputs), admin must:
1. Delete Recorded Payment (Paid → Approved)
2. Reject Approval (Approved → Draft)
3. Make changes
4. Re-Approve (Draft → Approved)
5. Re-Record Payment (Approved → Paid)

**Observation:** Zoho does not provide a single "Reprocess" button that does all 5 steps. This multi-step reversal is operationally cumbersome.

### Use Cases

- Incorrect payment date entered
- Wrong payment mode selected
- Payment actually made via different method than recorded
- Payment cancelled and needs to be re-initiated

## Revise Salary (Draft State)

### Availability

Per-row kebab in **Draft state** > "Revise Salary"

**Not available in Approved or Paid states.** In Paid state, the per-row kebab shows only: Download Payslip | Send Payslip. No "Revise Salary" option.

### Expected Behaviour (not fully tested)

"Revise Salary" likely navigates to or opens a salary revision form where admin can change the employee's salary components with an effective date. The revision would then reflect in the current or next pay run depending on effective date.

**Not confirmed through direct observation** — clicking was not performed during this audit session (the test run moved quickly through approval). Document as "inferred from label + domain knowledge."

### Salary Revision Patterns (observed from domain knowledge)

Two common patterns in Indian payroll systems:

1. **Effective current run**: Revision applies immediately to this pay run. If salary was already computed, recalculate. System may generate an arrear amount.
2. **Effective next run**: Revision is saved to employee master with a future effective date. Current run uses old salary; next run uses new salary. Any arrear for the current run is computed and paid in next run.

Zoho may support both — "Revise Salary" from within a pay run suggests at least Pattern 1.

## Withold Salary (Draft State)

Observed in Draft state per-row kebab (6 options listed in documentation):
"Withold Salary" — prevents payment for this employee in the current run without skipping them entirely.

**Difference from Skip:**
- Skip: employee is excluded from the run entirely (no payslip, no record)
- Withold: employee is included in the run (payslip generated, salary calculated) but payment is NOT made. Payment is deferred.

Not tested in detail. Relevant for scenarios: salary advance recovery, legal hold, disciplinary action.

## Per-Row Kebab Options by State (Complete Matrix)

| Action | Draft | Approved | Paid |
|--------|-------|----------|------|
| View Payslip | Yes | Yes | Yes (via "View" button) |
| View TDS Sheet | Yes | Yes | Yes (via "View" button) |
| Skip Employee | Yes | No | No |
| Undo Skip | Yes (if skipped) | No | No |
| Withold Salary | Yes | No | No |
| Revise Salary | Yes | No | No |
| Download Payslip | No (via column) | No | Yes |
| Send Payslip | No | No | Yes |

## Key Observations for Our Build

1. **Delete Recorded Payment is the only post-Paid reversal** — our build must implement `DELETE /api/payroll-runs/{id}/payment` → status = Approved. This is a legitimate operations need (wrong payment date, wrong mode).
2. **No single "Reprocess" command** — implement a convenience `POST /api/payroll-runs/{id}/reprocess` command that in one atomic operation: deletes payment → rejects approval → returns to Draft. Zoho lacks this; it's a differentiating feature.
3. **Salary revision complexity** — "Revise Salary" from within a pay run is complex because it affects the immutability of that run. Design: salary revision with effective date creates a new `SalaryRevision` record. If effective date is within current run period, compute arrear and add to current run. If effective date is next period, apply from next run.
4. **Salary withold** — implement as a `PayrollRunEmployee.Status` enum: `Active | Skipped | Withheld`. Withheld employees show on payslip but `PaymentStatus = Withheld` (not Paid). Release on a future run as `Salary Released` (this is tracked in the Overall Insights "Salary Released Employees" counter).
5. **Audit trail for reversals** — every state transition must be logged in `PayrollRunAuditLog`: who, when, from-state, to-state, reason (if applicable). GDPR and Indian regulatory compliance requires this.
6. **Immutability after final payment** — define "final" carefully. In Zoho, Paid + Delete Recorded Payment = not final. True finality should be: payslips distributed + Form 16 generated for that FY. Once Form 16 is issued, no further revision should be allowed without a manual override by a super-admin with documented reason.

## Screenshots

- `screenshots/65-paid-kebab-menu.png` — Page kebab in Paid state (4 options including Delete Recorded Payment)
- `screenshots/65-delete-recorded-payment-dialog.png` — Delete Recorded Payment confirmation dialog
- `screenshots/65-paid-row-kebab-options.png` — Per-row kebab in Paid state (Download Payslip | Send Payslip only)

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
