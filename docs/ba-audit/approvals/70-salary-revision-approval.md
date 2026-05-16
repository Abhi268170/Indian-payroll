# Approvals > Salary Revision (Item 70)

## URL / Navigation Path

- Approvals List: `#/approvals/salary-revision`
- Revision Form (from employee): `#/people/employees/{employeeId}/salary-revision/new`
- Revision Detail: `#/people/employees/{employeeId}/salary-revision/{revisionId}/details`
- Settings Config: `#/settings/salary-revision/custom-approval/list`

**Navigation to initiate:** Employees > Employee > Salary Details > Revise button

## Purpose

Allows salary revisions to be created and (optionally) routed through an approval workflow before taking effect in the next pay run.

## Approvals List Page

**URL:** `#/approvals/salary-revision`

### Layout

```
[Header]
  ["All Revisions" dropdown]                 ["Send Revision Letters" button] [...Export] [Filter] [Help]
[Filter Band]
  [Payout Month: current month + year] [Employees: dropdown] [Clear Filter]
[Content]
  [Empty state OR revision table]
```

### Fields (Filter Band)

| Field | Type | Default | Options |
|-------|------|---------|---------|
| Payout Month | Month-year picker | Current month/year | Any past/future month |
| Employees | Autocomplete dropdown | "Select an Employee" | All active employees |
| Clear Filter | Button | — | Resets filters |

### View Toggle Dropdown Options

- All Revisions (default)
- Pending Revisions
- Approved Revisions
- Rejected Revisions

### Toolbar Actions

| Action | Trigger | Notes |
|--------|---------|-------|
| Send Revision Letters | Button | Sends revision letter to employees (likely email) |
| Export | "..." menu dropdown item | Exports revision list |
| Filter | Icon button | Toggle filter band visibility |
| Instant Helper | "?" button | In-app help |

### Empty State (Observed)

"No results found / Looks like you don't have any results for the filter applied"

### Critical Finding: Approvals List Was Empty After Admin Revision

After creating a salary revision for EMP001 (Arjun Mehta) as admin, the revision did NOT appear in the Approvals Salary Revision list. **This confirms that admin-initiated salary revisions are auto-approved and bypass the approval queue.**

The Salary Revision approvals list only shows revisions that have been routed through the custom approval workflow (configured in Settings > Salary Revisions > Approvals), and only when the revision requires sign-off by someone other than the creating admin.

---

## Salary Revision Form

**URL:** `#/people/employees/{employeeId}/salary-revision/new`
**Access Entry:** Employee > Salary Details > "Revise" button

### Page Structure

```
[Header: "Salary Revision for {Employee Name}"]  [X close]
  [Previous CTC: ₹X]  [Previous Monthly Salary: ₹Y]
[Section: Select the Salary Revision type *]
  [Radio: Revise CTC by percentage] [% input field]
  [Radio: Enter the new CTC amount below] (selected by default)
  [Revised Annual CTC *] [₹] [amount input] [per year]
[Section: Salary Components Table]
  Columns: SALARY COMPONENTS | CALCULATION TYPE | MONTHLY AMOUNT | ANNUAL AMOUNT
  Rows: Basic | HRA | Fixed Allowance | + Add Earning
  Footer row: Cost to Company | ₹X monthly | ₹Y annual
[Section: Payout Preferences *]
  [Revised Salary effective from] [M yyyy date picker]
  [Payout Month] [M yyyy date picker]
  [Note: auto-arrear calculation info box]
[Footer: Submit | Cancel]
```

### Fields (Complete)

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Revision Type | Radio button | Yes | One of two options | "Revise CTC by %" or "Enter new CTC amount" |
| Percentage | Number input | Conditional | Required if % type selected | Attached to % radio |
| Revised Annual CTC | Number (₹) | Yes (if amount type) | > 0 | Auto-calculates component breakdown in real-time |
| Basic % of CTC | Number | No (editable) | Decimal precision 2 | Pre-filled from current structure (57.14%) |
| Basic Monthly Amount | Number (₹) | No | Auto-calculated from % x CTC | Editable directly; overrides % |
| HRA % of Basic | Number | No (editable) | Decimal precision 2 | Pre-filled from current structure (40%) |
| HRA Monthly Amount | Number (₹) | No | Auto-calculated | Editable directly |
| Fixed Allowance | Display | No | Residual = CTC - all others | Not directly editable; auto-calculated |
| Revised Salary effective from | Month-year picker | Yes | Cannot be in past (before current month) | Validates on submit; text input rejected — must use datepicker |
| Payout Month | Month-year picker | Yes (implied) | Same or after effective date | When arrears will be processed |

### Calculation Logic

```
Monthly CTC = Annual CTC / 12

Basic = Monthly CTC * (Basic % / 100)
HRA = Basic * (HRA % / 100)
Fixed Allowance = Monthly CTC - Basic - HRA - [all other components]
Annual amounts = Monthly * 12
Percentage increase shown = (New CTC - Old CTC) / Old CTC * 100
```

### Test Case: EMP001 Arjun Mehta

| Field | Before | After |
|-------|--------|-------|
| Annual CTC | ₹8,40,000 | ₹9,45,000 (+13%) |
| Monthly CTC | ₹70,000 | ₹78,750 |
| Basic | ₹39,998 (57.14%) | ₹44,998 (57.14%) |
| HRA | ₹15,999 (40% of Basic) | ₹17,999 (40% of Basic) |
| Fixed Allowance | ₹14,003 | ₹15,753 |
| Effective From | — | June 2026 |
| Payout Month | — | June 2026 |

### Effective Date Constraints (Business Rules)

- **Cannot backdate past current month**: The month picker disables all months before the current month (Jan-May 2026 disabled in May 2026)
- **Historical fiscal year months disabled**: Even when browsing year 2025, all months are in `disabled` class
- **Text input rejected**: Typing "Jun 2025" in the date field is accepted visually but rejected at form submit with error: "Please enter the revised salary effective month."
- **Must use the datepicker UI**: Text entry does not work; datepicker selection is required

### Auto-Arrear Note

A blue info box states: "Zoho Payroll will automatically calculate any arrears in the salary and process them in the payout month, eliminating the need for manually adding arrear components."

### Submit Behaviour

- Successful submit → navigates to `#/people/employees/{id}/salary-details`
- Salary details shows informational notice: "The revised salary amount will be reflected in salary details upon the completion of June, 2026 pay run. **View Details**"
- The "View Details" link goes to the revision detail page

### Validation Error Message

When effective date is not set via datepicker:
```
"Oops! Looks like you missed something...
• Please enter the revised salary effective month."
```
Shown as a pink/red inline banner at the top of the form body.

---

## Salary Revision Detail View

**URL:** `#/people/employees/{employeeId}/salary-revision/{revisionId}/details`

### Layout

```
[Header: "Salary Revision for {Name}"]   [Delete] [Edit] [X]
  [Previous CTC: ₹X]  [New CTC: ₹Y (N% arrow)]  [Effective From: Month Year]  [Payout Month: Month Year]
[Section: Salary Structure]
  Table: SALARY COMPONENTS | MONTHLY AMOUNT | ANNUAL AMOUNT
  [Earnings: Basic | HRA | Fixed Allowance]
  [Cost to Company: ₹X | ₹Y]
```

### Actions on Detail View

| Action | Effect |
|--------|--------|
| Delete | Deletes pending salary revision |
| Edit | Returns to revision form with pre-filled values |
| X (close) | Returns to salary details |

---

## Salary Revision Approval Settings

**URL:** `#/settings/salary-revision/custom-approval/list`

Same three workflow types as Pay Runs:
- Simple Approval — any approver with Salary Revision approval permission
- Multi-Level Approval — all approvers must approve
- Custom Approval — criteria-based routing

**Current state:** None configured for lerno org.

---

## Business Rules

1. Admin-initiated salary revisions bypass the approval queue unless a workflow is configured in Settings.
2. Effective date cannot be set to a past month (only current month or future).
3. The revision does not take effect until the payout month's pay run is completed.
4. Arrears are auto-calculated between effective date and payout month.
5. A revision can be Edited or Deleted before the payout month pay run is processed.
6. The "Revise" button remains available on the salary details page even with a pending revision (multiple pending revisions may be possible — needs verification).

## Cross-Module Impact

- Salary Details page (`#/people/employees/{id}/salary-details`) shows pending revision notice
- Pay Run (June 2026) will automatically apply the revised salary
- Payslip for June 2026 will show revised amounts + any arrears

## Screenshots

- `70-salary-revision-list.png` — Salary Revision approvals list (empty)
- `70-salary-revision-form.png` — Revision form top half
- `70-salary-revision-payout-section.png` — Payout Preferences section with datepicker open
- `70-salary-revision-submit-result.png` — Validation error on text-entered date
- `70-salary-revision-submitted-salary-details.png` — Post-submission salary details
- `70-salary-revision-detail-view.png` — Revision detail with comparison display
- `70-salary-revision-approval-settings.png` — Settings approval workflow selection
- `70-salary-revision-more-menu.png` — Export option in more menu

## Open Questions

- [ ] When a custom approval workflow IS configured, does the revision appear in the Approvals > Salary Revision queue?
- [ ] Can an employee initiate their own salary revision request via the employee portal? (Not observed)
- [ ] If effective date differs from payout month, is the arrear calculation shown to admin before submission?
- [ ] What happens if you try to create a second revision while one is already pending?
