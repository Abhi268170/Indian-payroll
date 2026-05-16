# UF-A14: Salary Revision Approval Flow

**Module:** Approvals > Salary Revision
**Tested:** 2026-05-16
**Route:** `#/approvals/salary-revision`
**Outcome:** No pending salary revisions in test org — empty state. Flow documented from prior session data (UF-12, UF-21, UF-22) and UI structure observed.

---

## Findings

### 1. Approvals > Salary Revision Page

**Route:** `#/approvals/salary-revision`
**Page title:** "Approvals | Salary Revision | Zoho Payroll"

**Empty state heading:** "No results found"

**Filter options:**
| Filter | Type | Default |
|--------|------|---------|
| View | Toggle | "All Revisions" |
| Payout Month | Month picker | (current) |
| Employees | Dropdown | "Select an Employee" |

**Empty state:** No pending salary revisions in this test org because no revision was submitted awaiting approval within the approval workflow.

---

### 2. Salary Revision Creation (from prior sessions UF-21, UF-22)

**Route to create:** Left sidebar → "Approvals" → "Salary Revision" → (or from Employee profile → Salary Details → "Revise Salary")

**Salary Revision form fields (from prior session):**
| Field | Type | Notes |
|-------|------|-------|
| Employee | Auto-set from employee profile | |
| Effective Date | Date | Month from which revised salary applies |
| Revision Type | Radio | Percentage increment / Flat amount / New CTC |
| New CTC / Increment % | Number | Depending on revision type |
| Component-level changes | Table | Shows each component's current vs new value |
| Include Arrears | Toggle | Whether to pay arrear for months since effective date |
| Arrears Pay Run | Month selector | Which pay run to include arrears in |
| Notes | Textarea | Internal notes on revision reason |

---

### 3. Approval Workflow Configuration

**Route:** `#/settings/salary-revision/custom-approval/list`

**Approval workflow for salary revisions:**
- Configurable whether revision requires approval or auto-applies
- Multi-level approval supported (e.g., HR → Finance → Director)
- Approvers can be assigned by role or specific user

**When approval is enabled:**
- Creating a salary revision submits it to approvers
- Appears in Approvals → Salary Revision for each approver
- Cannot be applied to payroll until approved

---

### 4. Approval Detail View (Expected — Not Tested)

When a pending revision exists in the Approvals list:
- Clicking the revision shows a detail panel/page
- Expected fields shown:
  - Employee name and ID
  - Old CTC and new CTC
  - Component-level from/to amounts (Basic: ₹X → ₹Y, HRA: ₹A → ₹B, etc.)
  - Effective date
  - Arrears: ₹amount for N months
  - Notes from submitter
- Action buttons: **Approve** | **Reject** (with reason)

---

### 5. Post-Approval Behaviour (Expected)

When an approver clicks **Approve**:
1. Salary revision status changes to "Approved"
2. System updates the employee's salary structure from the effective date
3. If arrears configured: system prompts to create an Arrears Pay Run (or includes in next regular run)
4. Employee receives notification (email) about salary revision

When **Rejected**:
1. Revision status changes to "Rejected"
2. Submitter notified with rejection reason
3. Can be resubmitted with changes

---

### 6. Arrears Pay Run Post-Approval

**Route (expected):** Creates a new pay run of type "Arrears"
From prior session (UF-22-salary-revision-with-arrears.md):
- Arrears calculated as: (new component amount − old component amount) × number of months since effective date
- Example: Basic raised from ₹40,000 to ₹45,000, effective March 2026, arrears for April and May = ₹10,000
- Arrears pay run is separate from regular pay run (or included in current month's pay run as a separate line)

---

### 7. Salary Revision Status Machine

```
Draft (submitted by HR)
    ↓
Pending Approval (in Approvals queue)
    ↓                    ↓
Approved            Rejected
    ↓                    ↓
Applied to         Back to submitter
salary structure
    ↓
Arrears pay run created (if applicable)
```

---

### 8. Settings — Custom Approval for Salary Revisions

**Route:** `#/settings/salary-revision/custom-approval/list`

Not fully explored in this session. Expected configuration:
- Enable/disable approval requirement
- Define approval levels (one-level or multi-level)
- Assign approvers by role or individual
- Define escalation rules if not approved within X days

---

## Screenshots / Files

No new screenshots — empty state page. Prior session screenshots in UF-12, UF-21, UF-22.

---

## Gaps / Open Questions

- [ ] **Approval detail view:** The actual approval form with component-level from/to changes was not seen (no pending revisions). Need a live test with pending revision.
- [ ] **Arrears prompt:** When approving a revision with arrears, is there an immediate prompt or does the system automatically create the arrears run?
- [ ] **Multi-level approval:** Does the system show all approvers in the chain and which level has acted?
- [ ] **Auto-apply without approval:** If approval workflow is disabled, does revision apply immediately on creation? Or still requires a separate "Apply" action?
- [ ] **Revision history:** Is there a full revision history per employee showing all past salary changes?
- [ ] **Bulk revision:** Can multiple employees be revised in one revision submission (e.g., all employees in Engineering department get 10% hike)?
