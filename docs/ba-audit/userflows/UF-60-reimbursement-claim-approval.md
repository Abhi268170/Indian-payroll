# UF-60: Reimbursement Claim Approval

**Module:** Approvals > Reimbursements
**Tested:** 2026-05-16
**Mock Data Used:** No active reimbursement claims in system
**App State Before:** Approvals > Reimbursements page — empty state

## Steps Executed
1. Navigate to `#/approvals/reimbursements`
2. Observe empty state — no claims to approve
3. Document expected flow from UI patterns and Zoho Payroll domain knowledge

---

## Flow Not Executable — Reason
No reimbursement claims exist in the demo org. The approval page shows empty state for all filter combinations.

---

## Expected Approve Flow (From UI Patterns)

### Pre-conditions
- Employee has submitted a reimbursement claim (via employee portal or admin-created)
- Claim is in "Pending" status
- Admin navigates to `#/approvals/reimbursements`

### Approve Steps
1. Filter by Claim Month or Payout Month to locate the claim
2. Review claim row: Employee Name, Claim Date, Reimbursement Type, Amount
3. Click "Approve" action button on the claim row
4. Modal or inline confirmation appears
5. Select Payout Month (if not pre-filtered) — which pay run to include it in
6. Confirm — claim status changes to "Approved"
7. Claim appears in next pay run's Earnings section as a reimbursement line

### Post-Approval Behavior
- Claim moves from Pending to Approved state
- Payout Month pay run includes the reimbursement amount in the employee's earnings
- Amount is non-taxable (reimbursement of actual expense — not income)
- Employee receives notification (if configured)
- Payslip shows reimbursement as separate line item

---

## Reimbursement Types (Expected — From Zoho Payroll Standard)
| Type | Taxability | Notes |
|------|------------|-------|
| Medical Reimbursement | Non-taxable up to ₹15,000/year | Section 17(2) exemption |
| Fuel / Conveyance | Non-taxable with supporting bills | Actual expense reimbursement |
| Internet / Phone | Non-taxable (business use) | Requires bills |
| Food Allowance (bills) | Non-taxable with bills | FBP food component |
| LTA | Non-taxable up to actual travel cost | Section 10(5); twice in 4-year block |

---

## Business Rules
1. Reimbursements flow into pay run as non-taxable earnings — they do not increase TDS base
2. Payout Month determines which pay run the reimbursement appears in
3. Only "Approved" claims are included in pay run
4. Rejected claims are archived — employee can be notified to resubmit with correct documents
5. No double-payment risk: once included in a pay run, the claim is "Paid" — cannot be re-approved

## Gaps / Observations
- Full approve/reject flow not tested due to empty state
- 🟡 Mark for future session: create a test reimbursement claim and test the approval workflow
- No claim ID format observed (likely system-generated REIMB-XXXXX)
- No attachment preview of supporting documents observed from empty state

## Open Questions
- [ ] Are supporting documents (receipts, bills) attached by employee at submission time?
- [ ] Can admin approve a partial amount (e.g., approve ₹500 of a ₹1,000 claim)?
- [ ] What happens if the Payout Month pay run is already finalized when approval happens?
- [ ] Is there an expiry for pending claims (e.g., cannot approve claims older than 3 months)?
