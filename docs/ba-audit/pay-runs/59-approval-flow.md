# Pay Runs > Approval Flow — Draft to Approved State Transition

## URL / Navigation Path

`https://payroll.zoho.in/#/payruns/{id}/summary` (Draft state)

Approval action accessed via: Page-level kebab (three-dot menu) in top-right of the summary header > "Approve Payroll"

## Purpose

Controls the state transition from Draft to Approved. Implements a gate-checked approval flow: all pending tasks must be resolved before approval is allowed. Approval is a deliberate, confirmable action that locks variable inputs and signals payroll is ready for payment.

## Pre-conditions for Approval

| Pre-condition | Check type | What happens if failed |
|---------------|------------|------------------------|
| All Pending Tasks resolved | Hard block | Error toast: "Please complete your pending tasks" — Approve dialog does NOT open |
| At least one non-skipped employee | Implicit | System allows approval; no check observed |
| No open variable input panels | Implicit (auto-saved) | No explicit check — changes save on each action |

## Approval Flow — Step by Step

### Step 1: Admin Opens Page Kebab
- Clicks the "..." (three-dot / Show dropdown menu) in the summary header
- Menu shows: "Approve Payroll" (and possibly other options in Draft state)

### Step 2: Pending Tasks Check (client-side)
- If any Pending Tasks exist: toast shown — "Please complete your pending tasks"
- Admin must resolve each task (complete employee onboarding, skip employees, etc.) before proceeding

### Step 3: Approve Payroll Confirmation Dialog
When all tasks resolved, clicking "Approve Payroll" opens a modal dialog:

| Field | Type | Content |
|-------|------|---------|
| Dialog title | Heading | "Approve Payroll" (inferred — not captured in snapshot text) |
| Summary info | Read-only | Pay period, employee count, total net pay |
| Confirmation message | Paragraph | Confirmation prompt asking admin to approve |
| Approve button | Button | Confirms and triggers state transition |
| Cancel button | Button | Dismisses dialog; stays in Draft |

**Observed dialog screenshot:** `screenshots/59-approve-payroll-dialog.png`

### Step 4: State Transition to Approved
- API call: `POST /api/v1/payrollruns/{id}/approve` (inferred from behaviour)
- Status badge changes: "Draft" → "Approved"
- Variable inputs panel becomes read-only
- Page header actions change (kebab now shows "Reject Approval" instead of "Approve Payroll")
- URL: stays at `#/payruns/{id}/summary`
- "Approve Payroll" toast/success notification shown

## Skip Employee Dialog (blocker resolution path)

When admin chooses to skip an employee rather than completing their onboarding:

| Field | Type | Required | Content |
|-------|------|----------|---------|
| Dialog title | Heading | — | "Skip Employee?" |
| Reason | Textarea | Yes (mandatory) | Free text — captured and displayed in employee row |
| Confirm button | Button | — | "Skip" |
| Cancel button | Button | — | Dismisses dialog |

After skipping: employee row shows "Skipped" label + Reason text. Skip resolves the "Add Employees" pending task for that employee.

## Buttons & Actions

| Action | State | Trigger | Pre-condition | Post-behaviour |
|--------|-------|---------|---------------|----------------|
| Approve Payroll | Draft | Page kebab > Approve Payroll | All tasks complete | Opens confirmation dialog |
| Confirm Approval | Draft | Dialog > Approve button | — | State → Approved; inputs locked |
| Cancel Approval Dialog | Draft | Dialog > Cancel | — | Returns to Draft, no change |
| Skip Employee | Draft | Row kebab > Skip Employee | Reason entered | Employee marked Skipped; pending task resolved |
| Undo Skip | Draft | Row kebab > Undo Skip | Employee is skipped | Removes skip; employee re-enters run |

## State Machine — Draft to Approved

```
READY (initial)
   ↓  [period card clicked]
DRAFT
   ├─ Pending Tasks exist → [resolve tasks] → Pending Tasks cleared
   ↓  [Approve Payroll confirmed]
APPROVED
   ├─ [Reject Approval] → back to DRAFT
   ↓  [Record Payment confirmed]
PAID (terminal — reversible only via Delete Recorded Payment)
```

## Conditional Logic

- "Approve Payroll" in kebab is always visible in Draft state but triggers the hard-block toast if tasks remain.
- Once approved: "Approve Payroll" option disappears from kebab; "Reject Approval" appears instead.
- After approval, attempting to navigate back and access variable inputs → the split panel is read-only; no Save buttons visible.
- EMP003/004/005 were skipped with reason "Onboarding incomplete" — this resolved the "Add Employees" pending task and enabled approval.

## Cross-Module Links

- Approvals module (left sidebar) → may show payroll approval requests for approval workflow (multi-step approval). In this org, approval appeared to be single-step (creator = approver).
- Employee onboarding status → source of "Add Employees" pending task

## Key Observations for Our Build

1. **Hard block on pending tasks** — must implement. Our `ApprovePayrollRunCommand` handler should check all required preconditions and throw a domain exception if any fail. Return 409 Conflict with task details to the frontend.
2. **Pending task model** — tasks are dynamic and state-driven. Define a `PayrollRunTask` entity or value object with: type (AddEmployee, MissingBankDetails, etc.), status (Pending/Resolved), description.
3. **Approval is a single-step action** (in this org/plan) — no multi-step approval chain observed. The "Approvals" section in the sidebar may be for a premium feature. Our build: implement single-step approval first, design for multi-step later.
4. **Skip with mandatory reason** — reason is captured and displayed permanently in the employee row. Store in `PayrollRunEmployee.SkipReason`. Never allow skip without reason — mirrors Zoho's mandatory validation.
5. **Approval should be idempotent** — if admin clicks Approve twice (e.g., double-click), system should handle gracefully. Return 200 if already Approved.
6. **State immutability post-approval** — all variable inputs (LOP, earnings, TDS override) become immutable once approved. Enforce at API level (`if (run.Status != Draft) throw InvalidOperationException`).
7. **Rejection reversal** — Zoho allows rejecting an approved run back to Draft. Our `RejectApprovalCommand` must move status Draft and re-enable variable input editing.

## Screenshots

- `screenshots/59-approval-hard-block-toast.png` — Error toast "Please complete your pending tasks"
- `screenshots/59-approve-payroll-dialog.png` — Approve Payroll confirmation dialog
- `screenshots/59-post-approval-summary.png` — Summary page in Approved state

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
