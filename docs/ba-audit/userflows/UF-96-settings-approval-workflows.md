# UF-96: Settings — Approval Workflows (Pay Runs and Salary Revisions)

**Module:** Settings > Module Settings > General > Pay Runs / Salary Revisions
**Tested:** 2026-05-16
**URLs:**
- Pay Run Approvals: `#/settings/payrun/custom-approval/list`
- Salary Revision Approvals: `#/settings/salary-revision/custom-approval/list`

---

## Pay Runs Settings — Approvals Tab

### Page Layout
- Heading: "Pay Runs"
- Sub-tabs: Approvals, Custom Button, Record Locking, Related List

### Approval Workflow Options (Radio Group: "Select Approval Workflow")

**Option 1: Simple Approval**
"In this approval flow, any approver with Pay Run approval permission can approve."
- Single-level approval
- Any authorized user with "Pay Run approval" permission can finalize
- Suitable for small orgs (1–10 employees) with single payroll admin

**Option 2: Multi-Level Approval**
"Set many levels of approval. The Pay Run will be approved only when all the approvers approve."
- Sequential multi-level approval chain
- Example: Payroll Preparer → Finance Manager → CFO
- Pay run only finalizes after ALL approvers have approved
- Suitable for organizations with governance requirements

**Option 3: Custom Approval**
"You can set up an approval flow based on one or more criteria. These criteria can be chosen by you."
- Criteria-based routing: e.g., "If total payroll > ₹50,00,000 → route to CFO for approval"
- Multiple criteria possible
- Most flexible; for large enterprises

### Save Button
"Save" button confirms the selected approval workflow type.

### Additional Tabs
| Tab | URL | Description |
|-----|-----|-------------|
| Approvals | `/custom-approval/list` | Workflow selection (current page) |
| Custom Button | `/custom-button/list` | Add custom action buttons to pay run UI |
| Record Locking | `/record-locking` | Configure when pay run records lock |
| Related List | `/related-list` | Related entities in pay run detail |

---

## Salary Revisions Settings

### URL
`#/settings/salary-revision/custom-approval/list`

**Expected same structure as Pay Runs approval:**
- Simple Approval
- Multi-Level Approval
- Custom Approval

**Additional Salary Revision specific settings (expected):**
- Effective date rules (can revisions be backdated?)
- Whether revision requires pay run re-processing
- Auto-arrears calculation on approval

---

## Leave & Attendance Settings

### URL
`#/settings/holiday-leave/enable-module`

**This module is separate and appears to integrate with Zoho People.**

Expected settings:
- Enable/disable Leave module
- Leave types (annual, sick, casual, maternity, etc.)
- Holiday list (state-wise public holidays)
- LOP calculation method (calendar days vs working days)

**Demo org:** Leave & Attendance likely disabled (no Zoho People integration configured).

---

## Employees & Contractors Settings

### URL
`#/settings/employee/contractor`

Expected settings:
- Contractor payment configuration (separate from employee payroll)
- Required fields for employee profile
- Onboarding checklist configuration
- Employee ID format (EMP001, EMP002, etc.)

---

## Pay Run Approval Workflow — Business Impact

### Without Configured Approval (Simple Approval Default)
- Any Admin/Manager can finalize a pay run
- No additional approval gate
- Suitable for demo org (single admin user)

### With Multi-Level Approval
- Pay run cannot be finalized until ALL approvers in the chain sign off
- Prevents unauthorized salary processing
- Creates audit trail of approvals

### With Custom Approval
- Conditional routing based on pay run characteristics
- Example rules:
  - "Pay run with total disbursement > ₹10,00,000 requires CFO approval"
  - "Pay run with bonus component requires HR Manager approval"
  - "Pay run for a specific department requires that department head's approval"

---

## Business Rules
1. Default approval workflow is Simple Approval (any authorized user can approve)
2. Multi-Level Approval requires ALL approvers — one rejection blocks the flow
3. Custom Approval routes based on criteria — highly configurable
4. Pay run approval workflow applies to ALL pay runs (regular, off-cycle, bonus, FnF)
5. Changing approval workflow takes effect for future pay runs (not in-progress ones)
6. Users with "Reimbursements and POI Reviewer" role cannot approve pay runs

## Gaps / Observations
- Current approval workflow selection not confirmed (which radio is selected) — snapshot shows radio group but selected state not visible
- Salary Revision approval page not directly navigated
- Leave & Attendance module not explored (separate integration)
- Employees & Contractors page not navigated

## Open Questions
- [ ] What approval workflow is currently configured for the demo org?
- [ ] Can a user be both a preparer AND an approver in the same pay run (conflict of interest)?
- [ ] If an approver rejects a multi-level pay run, does it go back to start or to the previous level?
- [ ] Is there a deadline/SLA for pay run approval? Does it auto-escalate if not approved?
