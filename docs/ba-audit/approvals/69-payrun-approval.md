# Approvals > Pay Run Approval (Item 69)

## URL / Navigation Path

- Settings Config: `#/settings/payrun/custom-approval/list`
- Pay Run itself: `#/payruns` (approval happens inline in Pay Run list)
- Settings Path: Settings > Module Settings > General > Pay Runs > Approvals tab

## Purpose

Pay run approval is not a standalone approvals screen. It is a workflow configuration in Settings that determines how submitted pay runs are approved. The approval action is taken directly in the Pay Runs module, not the Approvals section.

## Pay Run Approval Architecture

### Zoho's Model

```
Pay Run States:
  Draft → [Admin submits for approval] → Pending Approval → [Approver approves] → Approved → [Admin finalizes] → Paid
                                                           → [Approver rejects] → Rejected (back to Draft for corrections)
```

### May 2025 Pay Run Status

The May 2025 pay run is in **Approved/Paid** state. During audit, the Approvals module showed no pending pay run approvals (consistent with the run being already paid).

## Approval Workflow Settings

**Location:** Settings > Module Settings > General > Pay Runs > Approvals tab
**Route:** `#/settings/payrun/custom-approval/list`

### Three Workflow Types

| Type | Description | When to Use |
|------|-------------|-------------|
| Simple Approval | Any approver with Pay Run approval permission can approve | Single approver scenario |
| Multi-Level Approval | Multiple levels; all approvers must approve | Hierarchical sign-off required |
| Custom Approval | Criteria-based routing (e.g., by pay schedule, amount threshold) | Complex org structures |

**Current State:** None selected (no approval workflow configured for lerno org)

### Tabs on Pay Runs Settings Page

| Tab | Purpose |
|-----|---------|
| Approvals | Approval workflow type selection |
| Custom Button | Add custom action buttons to pay runs |
| Record Locking | Lock pay run records after finalization |
| Related List | Configure related records shown in Pay Run detail |

## Pay Run Approval Detail (Inferred from Architecture)

Since the May 2025 run is already Paid, we observed the settings-level configuration rather than the live approval detail. Based on field inspection:

### What Approver Sees

- Pay run summary: total employees, total net pay, statutory deductions breakdown
- Individual employee payslip preview (navigable)
- Variance comparison vs. previous month (if configured)

### Actions Available in Approval Queue

| Action | Behaviour |
|--------|-----------|
| Approve | Moves pay run to Approved state; triggers email notification |
| Reject | Returns pay run to Draft/Rejected state; notification to submitter |
| Request Changes | Comments-based request without full rejection (if configured) |

### Comments Field

Not directly observed — the pay run approval detail is inaccessible since no pay run is in pending state. Assumed present based on Zoho's standard approval UI pattern.

## Audit Trail

Zoho Payroll maintains an immutable audit trail for all pay run state transitions. Not directly accessible via the UI audit — likely in the Reports module or accessible via activity logs.

## Role-Based Access

| Role | Can Submit for Approval | Can Approve | Can View |
|------|------------------------|-------------|----------|
| Payroll Admin | Yes | Yes (with permission) | Yes |
| Payroll Viewer | No | No | Yes (read-only) |
| Approver Role | No | Yes | Yes |

## Notification Behaviour (Expected based on Zoho standard)

- Email sent to designated approvers when pay run is submitted
- Email sent to submitter when approved/rejected
- Configurable via Settings > Email Templates

## Key Observations for Our Build

1. **No dedicated Approvals page for Pay Runs** — approval is inline within the Pay Runs module itself. This is a cleaner UX pattern vs. a separate queue.
2. **Three workflow types** mirror standard HRMS approval patterns: Simple, Multi-Level, Custom.
3. **Current org has no approval workflow configured** — all pay runs in lerno are effectively admin-approved immediately.
4. **State machine is clear**: Draft → Pending Approval → Approved/Rejected → Finalized/Paid.
5. **Immutability after finalization**: Pay run cannot be edited once finalized (Zoho enforces this via Record Locking settings).

## Screenshots

- `69-payrun-approval-settings.png` — Pay Run approval workflow selection (Settings)

## Open Questions

- [ ] When Simple Approval is enabled, does the same admin who submitted get auto-approval, or does it require a DIFFERENT user?
- [ ] Is there a pay run approval detail page separate from the pay run view itself?
- [ ] What does "pending approval" look like in the Pay Runs list — badge, status label, row highlight?
- [ ] Are audit trail entries accessible from the Pay Run detail page?
