# UF-59: Approvals Module — List View

**Module:** Approvals
**Tested:** 2026-05-16
**Mock Data Used:** No active approvals; May 2026 pay run = PAID
**App State Before:** No pending reimbursement claims or salary revisions

## Steps Executed
1. Navigate to `#/approvals/reimbursements`
2. Observe page layout and filter controls
3. Navigate to `#/approvals/salary-revision`
4. Observe page layout and filter controls

---

## Approvals Module — Navigation Structure

Left sidebar sub-items under "Approvals":
| Menu Item | URL | Purpose |
|-----------|-----|---------|
| Reimbursements | `#/approvals/reimbursements` | Approve/reject employee expense claims |
| Salary Revision | `#/approvals/salary-revision` | Approve/reject salary change requests |
| Proof of Investments | `#/approvals/proof-of-investment` | Approve/reject POI documents submitted by employees |

---

## Reimbursements Page (`#/approvals/reimbursements`)

### Layout
- Page title: "Reimbursements"
- Filter bar at top
- Empty state table below

### Filters
| Filter | Type | Options/Notes |
|--------|------|---------------|
| Claim Month | Month picker | Select the month for which claims are to be reviewed |
| Payout Month | Month picker | Month in which approved claims will be paid out |
| Employee | Combobox (searchable) | Filter by specific employee |

### Table Columns (Empty State Observed)
| Column | Notes |
|--------|-------|
| Employee Name | Claimant name |
| Claim Date | Date claim was submitted |
| Reimbursement Type | Category (medical, travel, etc.) |
| Amount | Claimed amount in ₹ |
| Status | Pending / Approved / Rejected |
| Action | Approve / Reject buttons |

### Empty State
Message: No reimbursement claims visible for current filters.

### Business Rules
1. Reimbursements are submitted by employees (via portal or admin) and queued here for admin approval
2. Approved claims are paid out in the selected Payout Month's pay run
3. Claims are filter-visible by Claim Month (when submitted) and Payout Month (when paid)
4. Employee filter allows narrowing to specific individuals

---

## Salary Revision Page (`#/approvals/salary-revision`)

### Layout
- Page title: "Salary Revision"
- Filter bar at top (identical pattern to Reimbursements)
- Empty state table below

### Filters
| Filter | Type | Options/Notes |
|--------|------|---------------|
| Claim Month | Month picker | Month revision was requested |
| Payout Month | Month picker | Month revision becomes effective |
| Employee | Combobox (searchable) | Filter by specific employee |

### Table Columns (Empty State Observed)
| Column | Notes |
|--------|-------|
| Employee Name | Employee whose salary is being revised |
| Requested By | Initiator (manager / HR / admin) |
| Current CTC | Existing salary |
| Revised CTC | Proposed new salary |
| Effective Date | When revision takes effect |
| Status | Pending / Approved / Rejected |
| Action | Approve / Reject |

### Empty State
No pending salary revisions visible.

### Business Rules
1. Salary revisions can be initiated by admin (confirmed from Arjun's pending revision observed in UF-21)
2. If approval workflow is enabled, revisions queue here before taking effect
3. Approved revisions become effective in the next applicable pay run
4. Arjun's pending revision (from ₹70,000 to ₹9,45,000) was visible in his profile but not visible here — suggesting it may have been auto-approved or the approval workflow was not triggered

---

## Cross-Module Effects
- Approved reimbursements flow into the Earnings section of the next pay run as non-taxable reimbursement lines
- Approved salary revisions update the Salary Structure Assignment and affect future pay run calculations
- Rejected items generate a notification to the employee (if notifications configured)

## Gaps / Observations
- Both pages show empty state — no active claims to test approve/reject flow
- 🟡 Arjun's salary revision visible in his profile but absent from `#/approvals/salary-revision` — unclear if the approval step was bypassed
- No "Approve All" / "Reject All" bulk action visible in empty state (may appear when items exist)
- Payout Month filter implies reimbursements are pay-run-linked, not ad-hoc payments
- No "Reason" field visible for rejection in empty state (may be a modal)
- Third approvals item (Proof of Investments) not yet investigated — see UF-31

## Open Questions
- [ ] Does Arjun's salary revision require approval, or did admin bypass the workflow?
- [ ] What triggers appearance of items in Salary Revision approvals — is there a "Request Revision" flow vs admin-direct edit?
- [ ] Can reimbursements be submitted directly by admin on behalf of employee?
- [ ] Is there a bulk approve action when multiple claims exist?
