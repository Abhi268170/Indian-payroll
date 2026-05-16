# Settings > Module Settings > Salary Revisions

## URL
`#/settings/salary-revision/custom-approval/list`

## Sub-tabs

| Tab | URL |
|-----|-----|
| Approvals | `#/settings/salary-revision/custom-approval/list` |

## Purpose
Configure the approval workflow required before a salary revision can be finalised and applied to an employee's salary structure.

---

## Approval Workflow Selection

### Field: Select Approval Workflow
**Type:** Radio group — identical structure to Pay Run Approvals

| Option | Description |
|--------|-------------|
| Simple Approval | Any approver with "Salary Revision approval" permission can approve. |
| Multi-Level Approval | All approvers must approve in sequence before salary revision proceeds. |
| Custom Approval | Approval flow based on one or more criteria (e.g., revision % threshold, department). |

**Save button:** Saves selected workflow.

---

## Business Rules
1. **Same 3-option approval model as Pay Runs** — identical UI and logic, but applied to Salary Revision entity.
2. **Salary Revision approval is separate from Pay Run approval** — different permissions and workflows.
3. **Salary revision state machine:** Draft → Submitted → [Approval step(s)] → Approved → Applied (to employee salary structure).

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| Approval workflow | Salary revision effective date and pay structure update are gated on approval |
| Simple approval | Any user with Salary Revision APPROVE permission can approve |

## Observations & Notes
1. **No Custom Button or Record Locking sub-tabs** unlike Pay Runs — Salary Revision settings are simpler (approval only).
2. For our build: SalaryRevision entity has same approval state machine as PayrollRun. Separate approval type config per entity.

## Screenshots
`docs/ba-audit/settings/screenshots/26-salary-revision-approvals.png`
