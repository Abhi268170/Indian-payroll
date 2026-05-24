# Settings > Module Settings > Pay Runs

## URL
`#/settings/payrun/custom-approval/list` (default sub-tab)

## Sub-tabs

| Tab | URL |
|-----|-----|
| Approvals | `#/settings/payrun/custom-approval/list` |
| Custom Button | `#/settings/payrun/custom-button/list` |
| Record Locking | `#/settings/payrun/record-locking` |
| Related List | `#/settings/payrun/related-list` |

---

## Tab 1: Approvals (Pay Run Approval Workflow)

### Purpose
Configure the approval workflow required before a pay run can be finalised and paid. Three mutually exclusive options.

### Field: Select Approval Workflow
**Type:** Radio group

| Option | Description |
|--------|-------------|
| Simple Approval | Any approver with "Pay Run approval" permission can approve. No sequence required. |
| Multi-Level Approval | Multiple levels of approval; all approvers must approve in sequence before pay run proceeds. |
| Custom Approval | Approval flow defined by custom criteria (e.g., based on department, payroll amount threshold). Multiple criteria combinations possible. |

**Save button:** Saves the selected workflow type.

### Business Rules
1. **Default workflow** — Simple Approval (any authorised approver can approve).
2. **Multi-Level Approval** — requires configuring the sequence of approvers (e.g., Manager → HR → Finance).
3. **Custom Approval** — allows criteria-based routing (e.g., if total payroll > ₹10 lakhs, require Finance Head approval).
4. **Permission dependency** — the "APPROVE" permission on Payroll Run (configured in Roles settings) determines who counts as an "approver".

### State Machine Implication
Payroll Run states: Draft → Submitted for Approval → [Approval step(s)] → Approved → Finalized → Paid.

---

## Tab 2: Custom Button (not explored)
URL: `#/settings/payrun/custom-button/list`
Purpose: Add custom action buttons to Pay Run screens (Deluge-based).

## Tab 3: Record Locking (not explored)
URL: `#/settings/payrun/record-locking`
Purpose: Define when pay runs are locked from editing.

## Tab 4: Related List (not explored)
URL: `#/settings/payrun/related-list`
Purpose: Configure related data lists shown on Pay Run detail page.

---

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| Simple Approval | Any user with APPROVE permission on Pay Runs can approve |
| Multi-Level Approval | Pay run waits for all levels to approve sequentially |
| Custom Approval | Dynamic routing based on configured criteria |

## Observations & Notes
1. **3-tier approval system** matches enterprise payroll needs: simple orgs use Simple, complex orgs use Multi-Level or Custom.
2. **Custom Approval criteria** are not explored — likely allows conditions on: pay run amount, department, employee count, etc.
3. For our build: PayrollRun approval workflow = ApprovalType enum (Simple/MultiLevel/Custom). Simple approval: any role with ApprovePayroll permission. Multi-level: ordered ApprovalStep records. Custom: ApprovalRule with criteria.

## Screenshots
`docs/ba-audit/settings/screenshots/25-payrun-approvals.png`
