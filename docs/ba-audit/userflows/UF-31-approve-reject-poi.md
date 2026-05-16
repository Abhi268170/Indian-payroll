# UF-31: Approve / Reject Proof of Investment

**Module:** Approvals > Proof of Investments
**Tested:** 2026-05-16
**Mock Data Used:** No POI submissions in system
**App State Before:** IT Declaration locked; Approvals > POI page not navigated

## Steps Executed
1. IT Declaration LOCKED — no POI submissions
2. Approvals > POI page URL identified: `#/approvals/proof-of-investment`
3. Flow documented from domain knowledge

---

## POI Approvals Page (`#/approvals/proof-of-investment`)

### Expected Layout
Similar pattern to Reimbursements approvals page:
- Filter bar: Financial Year, Employee, Investment Type, Status
- Table of submissions

### Expected Columns
| Column | Notes |
|--------|-------|
| Employee | Claimant |
| Investment Category | 80C / 80D / HRA / etc. |
| Sub-type | LIC / PPF / ELSS / etc. |
| Declared Amount | What employee declared in IT Declaration |
| Document | Link to view uploaded PDF |
| Submission Date | When POI was uploaded |
| Status | Pending / Approved / Rejected |
| Action | Approve / Reject |

---

## Approve Flow

1. Admin filters by current FY and finds pending POI submissions
2. Clicks on employee row or "View Document" to open the uploaded proof
3. Verifies document authenticity and amount
4. Clicks "Approve"
5. (Optionally) Adjusts the approved amount if different from declared (e.g., employee declared ₹1,50,000 ELSS but proof shows only ₹1,20,000)
6. Confirm approval
7. **TDS impact:** TDS for remaining months in FY recalculates using verified exemption amounts

---

## Reject Flow

1. Admin reviews document — finds discrepancy (wrong FY, illegible, amount mismatch)
2. Clicks "Reject"
3. Enters rejection reason (likely required text field)
4. Employee receives notification to resubmit

---

## Business Rules
1. Approved POI amount may differ from declared amount — admin can adjust downward
2. If admin approves less than declared, TDS recomputes using the lower approved figure
3. Rejected items do not affect TDS — employee's declared amount may still be used if within a grace period
4. Once financial year ends, POI verification window closes; any unverified declarations become permanent
5. POI for new regime employees: Only NPS and donations (if any) need POI — most exemptions irrelevant

## Gaps / Observations
- Page not navigated — empty state unknown
- Approve/Reject flow not tested
- 🟡 Mark for future session after releasing IT Declaration

## Open Questions
- [ ] Can admin view the document inline (PDF viewer) or only download?
- [ ] Is there a bulk approve option for all items from one employee?
- [ ] What happens to TDS when admin rejects POI in the final month of FY (March)?
- [ ] Is there an audit log of who approved which POI document?
