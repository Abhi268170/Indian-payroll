# UF-46: Pay Run Review and Approval Workflow

**Module:** Pay Runs > [Month] Pay Run > Review / Finalize
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 pay run observed (already PAID/FINALIZED)
**App State Before:** May 2026 pay run in PAID state

---

## Pay Run State Machine

From observations across sessions:

```
[Not Started] → [Draft] → [Under Review] → [Approved] → [Paid/Finalized]
                    ↑ (edit/add inputs)         ↓ (reject back to draft)
                    ←←←←←←←←←←←←←←←←←←←←←←←←←
```

### States Observed
| State | Description | Who Can Transition |
|-------|-------------|-------------------|
| Not Started | Pay run auto-created but not initiated | Admin initiates |
| Draft | Pay run in progress; inputs being entered | Admin edits |
| Under Review | Submitted for approval | Approver reviews |
| Approved | Approved; ready for payment recording | Admin records payment |
| Paid / Finalized | Payment recorded; payslips generated | Immutable (revision only) |

---

## Pay Run Initiation

### When Does Pay Run Start?
- Regular pay run: Auto-created at start of each month
- Off-cycle run: Manually created (UF-52)
- Bonus run: Manually created (UF-53)

### Initiation Steps (June 2026 — Expected)
1. Navigate to Pay Runs
2. June 2026 pay run visible in list (status: "Not Started" or "Draft")
3. Click "Process Pay Run" or "Start Pay Run"
4. System drafts pay run with all active employees' salaries
5. Skipped employees shown separately

---

## Review Process

### What Admin Reviews
1. **Employee-wise gross pay** — verify fixed + variable components
2. **LOP adjustments** — confirm attendance-based deductions
3. **Statutory deductions** — TDS, PF, PT per employee
4. **Reimbursements** — approved claims included
5. **New joiners** — proration for partial month
6. **Exits/FnF** — terminated employee final pay
7. **Total payroll cost** — gross vs net vs employer contributions

### Review Summary (Tabs from UF-50)
- **Summary tab**: Employee list with gross, deductions, net pay
- **Overall Insights tab**: Aggregate stats (Active, Paid, Skipped, Bank Transfer count)
- **Taxes & Deductions tab**: Statutory breakdown table

---

## Approval Workflow

### Single Approver (Default)
Most small orgs: Admin is both preparer and approver.
- Admin reviews → clicks "Approve" or "Finalize"
- No separate approval step

### Multi-Level Approval (If Configured)
If org has approval chain:
1. Payroll Preparer submits pay run for review
2. Payroll Approver reviews (different role)
3. Approver: Approve → moves to payment step
4. Approver: Reject → returns to Draft with comment

**Zoho Payroll RBAC Roles relevant:**
- Payroll Admin: Full access including finalization
- Payroll Manager: Can prepare and submit; cannot finalize
- Employee: No pay run access

---

## Finalization

### Pre-conditions for Finalization
- [ ] All employees' variable inputs entered
- [ ] LOP days entered
- [ ] Reimbursements approved and included
- [ ] TDS / statutory deductions verified
- [ ] Pay run reviewed by authorized role

### Finalization Action
"Finalize Pay Run" button (or "Mark as Paid" in Zoho's terminology)

Post-finalization:
1. Pay run becomes immutable (cannot edit)
2. Payslips generated for all paid employees
3. Bank transfer file available for download
4. Statutory deduction data frozen for reporting
5. TDS liability records updated
6. Pay run status → PAID

### Revision After Finalization
If error found after finalization:
- Cannot edit the finalized pay run
- Must create a "Revision Pay Run" (see UF-57)
- Revision run corrects specific employees/components
- Both runs visible in pay run history

---

## May 2026 Pay Run — Observed Actions (PAID State)

From UF-50, the header dropdown for a PAID pay run contains:
| Action | Description |
|--------|-------------|
| Download all Payslips | ZIP of all employee payslips |
| Download all TDS Worksheets | TDS computation for each employee |
| Show Downloads | Download history |
| Delete Recorded Payment | Revert payment recording (un-finalize?) |

**"Delete Recorded Payment"** — this is a significant action. It may allow reverting a PAID pay run to an Approved state, permitting re-recording of payment details. Does NOT revert to Draft (statutory data preserved).

---

## Business Rules
1. Pay run immutable once finalized — only revision run can correct
2. Finalization generates payslips — cannot regenerate without re-finalization
3. "Delete Recorded Payment" may un-finalize without deleting statutory data
4. TDS liability locks after finalization — frozen for Form 24Q
5. Skipped employees can be moved to a separate off-cycle run

## Gaps / Observations
- Approval workflow (multi-role) not tested — single admin in demo org
- June 2026 pay run initiation not observed (date-gated)
- "Delete Recorded Payment" exact behavior not confirmed

## Open Questions
- [ ] Does "Delete Recorded Payment" revert payslips to unpublished state?
- [ ] Can a pay run be deleted entirely (not just payment un-recorded)?
- [ ] Is there a configurable approval chain (multi-level) for pay runs?
- [ ] What is the maximum time limit to finalize a pay run before auto-lock?
