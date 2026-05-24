# Item 89: Loan Approval Flow

**URL / Navigation Path:** N/A (no dedicated approval URL observed)
**Module:** Loans
**Status:** NOT FOUND — no approval workflow exists in this version

---

## Purpose

Document the loan approval workflow (if any) between loan creation and disbursement.

---

## Finding

**No loan approval workflow was observed in Zoho Payroll India.**

Upon creating a loan (Item 88), the form is submitted and the system immediately creates the loan with status **"Open"**. There is no intermediate "Pending Approval" or "Draft" state.

The loan status transitions observed:
- **Open** — active loan, EMI deductions will be scheduled
- **Paused** — instalment deductions paused (via "Pause Instalment Deduction" action)
- **Closed** — loan fully repaid or foreclosed (not directly observed but listed in filter)

There is no:
- Approval request step
- Approver assignment
- Approval notification
- Approval/Reject action on a separate approval page
- Status "Pending Approval" or "Under Review"

The "Approvals" section in Zoho Payroll's main navigation covers: **Salary Revisions**, **Leave Requests**, **Expense Claims**, **Reimbursements**, **Salary Advances** — but NOT loans.

---

## Implication for Product Design

If the client's business process requires a loan approval step (e.g., Finance Head must approve before loan is active), this feature is absent. Implementation would require:
- A "Draft" status on loan creation
- An approval workflow (approver role, approve/reject action, notification)
- Loan moves from Draft → Open only after approval

---

## Cross-Reference

- Zoho Payroll does have an "Approvals" module but it does not include loans.
- Salary Advances (a related feature) may have approval workflow — not audited in this session.

---

## Open Questions

- [ ] Does the Salary Advance feature in Zoho Payroll have an approval workflow?
- [ ] Is there a configuration option to enable loan approval in Settings?
- [ ] Is loan approval available in higher Zoho Payroll subscription tiers?
