# UF-62: Approval History / Audit Trail

**Module:** Approvals
**Tested:** 2026-05-16
**Mock Data Used:** No completed approvals in system
**App State Before:** Approvals module — all sub-pages empty state

## Steps Executed
1. Scanned all Approvals sub-pages for "History" or "Audit" tab/link
2. Observed no dedicated history view visible in empty state
3. Checked Reports > Activity for audit trail coverage

---

## Audit Trail Coverage Observed

### Within Approvals Module
No dedicated "History" tab or approval log visible in the current UI state. The approvals pages (Reimbursements, Salary Revision, POI) show only Pending items filtered by date. Once an item is Approved or Rejected, it is unclear if it remains visible with a status filter or disappears.

**Hypothesis:** A status filter (All / Pending / Approved / Rejected) likely exists in the filter bar once items are present. Empty state does not render these filter options.

### Activity Logs Report
In Reports Centre (`#/reports`), under "Activity" category:
- **Activity Logs** report: Tracks all system actions with actor, timestamp, entity affected

This is likely the primary audit trail for approval actions. Expected columns:
| Column | Value |
|--------|-------|
| Date | Timestamp of action |
| Actor | Admin/user who performed the action |
| Action | "Approved Reimbursement" / "Rejected POI" / etc. |
| Entity | Employee name, claim ID, or document reference |
| Details | Amount, reason, old/new values |

---

## Expected Approval State Machine

For Reimbursements:
```
[Submitted by Employee] → Pending → [Admin: Approve] → Approved → [Pay Run] → Paid
                                   → [Admin: Reject]  → Rejected → [Employee resubmit] → Pending
```

For Salary Revision:
```
[Initiated by HR/Admin] → Pending → [Approver: Approve] → Approved → [Next Pay Run Effect]
                                  → [Approver: Reject]  → Rejected → [No salary change]
```

For Proof of Investments:
```
[Employee: Upload POI] → Pending → [Admin: Approve] → Verified → [TDS: exemption honored]
                                 → [Admin: Reject]  → Rejected → [TDS: exemption unverified]
```

---

## Business Rules
1. All approval actions should generate an audit log entry
2. Approved/Rejected status should be immutable — cannot be undone without a new action
3. Pay run finalization locks all related approval items (cannot un-approve after pay is sent)
4. Activity Logs report provides the cross-entity audit trail

## Gaps / Observations
- No dedicated approval audit/history view confirmed in current navigation
- 🟡 Activity Logs report not opened — content and column structure not verified
- No approval workflow configuration visible (who can approve, escalation rules, SLA)
- No email notification trail visible in UI

## Open Questions
- [ ] Is there a status filter (Pending/Approved/Rejected/All) on approval list pages?
- [ ] Does the Activity Logs report show granular approval actions?
- [ ] Is there an approval SLA or escalation if items stay Pending for too long?
- [ ] Can approval authority be delegated (e.g., manager approves reimbursements, HR approves salary revisions)?
