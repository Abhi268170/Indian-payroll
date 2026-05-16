# UF-61: Reject an Approval Item

**Module:** Approvals > Reimbursements / Salary Revision / Proof of Investments
**Tested:** 2026-05-16
**Mock Data Used:** No active approval items in system
**App State Before:** All approvals pages — empty state

## Steps Executed
1. Navigate to `#/approvals/reimbursements` — empty, no items to reject
2. Observed no reject-capable items across any approvals sub-module

---

## Flow Not Executable — Reason
No pending approval items exist in the demo org. Rejection flow cannot be tested without an active claim.

---

## Expected Reject Flow (From UI Patterns)

### Pre-conditions
- An item exists in Pending status in any approvals queue
- Admin has reviewed and determined the item should not be approved

### Reject Steps — Reimbursement
1. Navigate to `#/approvals/reimbursements`
2. Locate pending claim row
3. Click "Reject" action button
4. Modal appears with:
   - Claim summary (read-only)
   - Rejection Reason (text field — likely required)
5. Click Confirm / Reject
6. Claim status changes to "Rejected"
7. Employee notified (if notifications configured)

### Reject Steps — Salary Revision
1. Navigate to `#/approvals/salary-revision`
2. Locate pending revision row
3. Click "Reject"
4. Provide reason (likely required)
5. Revision is cancelled — salary remains at current level

### Reject Steps — Proof of Investments
1. Navigate to `#/approvals/proof-of-investment`
2. Review uploaded document
3. Click "Reject" on specific POI line
4. Enter reason (e.g., "Document not legible", "Wrong financial year", "Amount mismatch")
5. Investment declaration reduces back to the declared amount with no proof — system may flag it as unverified and disallow the exemption

---

## Business Rules
1. Rejected reimbursements are NOT included in any pay run
2. Rejected salary revisions leave the current salary structure unchanged
3. Rejected POI: the investment remains declared but unproved — TDS computation reverts to treating the exemption as unverified (may increase TDS)
4. Rejection reasons should be preserved as audit trail
5. Employee can typically resubmit after addressing the rejection reason

## Gaps / Observations
- Full reject flow not tested due to empty state
- 🟡 Mark for future session: create test data and test rejection with reason field
- No bulk reject behavior observable from empty state
- Unclear if rejection triggers an email automatically or requires manual notification

## Open Questions
- [ ] Is Rejection Reason a free-text field or a dropdown of predefined reasons?
- [ ] Is a reason mandatory for rejection?
- [ ] Can a rejected item be re-opened to Pending by the approver (undo rejection)?
- [ ] Does POI rejection automatically increase TDS for the current month?
