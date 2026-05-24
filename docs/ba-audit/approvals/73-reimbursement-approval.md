# Approvals > Reimbursements (Item 73)

## URL / Navigation Path

- List: `#/approvals/reimbursements` (redirects to `?filter_by=Status.Submitted` after first visit)
- Create: `#/approvals/reimbursement-claims/new`
- Settings (FBP): `#/settings/preferences/fbp`
- Settings (Reimbursement): `#/settings/preferences/reimbursement`
- Settings (Salary Components Reimbursements): `#/settings/salary-components/reimbursements` (inferred from settings error message)

**Navigation:** Main Nav > Approvals > Reimbursements

## Purpose

Allows payroll admin to:
1. Review employee-submitted reimbursement claims
2. Approve or reject claims (with approved amount potentially differing from claimed amount)
3. Create reimbursement claims on behalf of employees

Reimbursements are salary components where employees claim tax exemption by submitting bills. The approved amount is included in the employee's payslip as a non-taxable reimbursement.

---

## Reimbursements List Page

**URL:** `#/approvals/reimbursements`
**Page Title:** "Approvals | Reimbursements | Zoho Payroll"

### Layout

```
[Header]
  ["All Claims" / "Pending Claims" view dropdown]    [+ Add] [...Import/Export] [Filter] [Help]
[Filter Band]
  [Claim Month: current M yyyy] [Employees: dropdown] [Clear Filter]
[Content]
  [Empty state] OR [Claims table]
```

### View Toggle Dropdown Options

- All Claims
- Pending Claims (default when accessed via `?filter_by=Status.Submitted`)
- Approved Claims
- Rejected Claims

### Filter Band Fields

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Claim Month | Month-year date picker | Current month (May 2026) | Defaults to current, not to "all time" |
| Employees | Autocomplete dropdown | "Select an Employee" | Filter to specific employee |
| Clear Filter | Button | — | Resets both filters |

### Toolbar Actions

| Action | Type | Notes |
|--------|------|-------|
| + Add | Primary button | Opens New Claim form at `#/approvals/reimbursement-claims/new` |
| "..." more menu | Dropdown | Import Reimbursements / Export Reimbursements |
| Filter icon | Toggle | Shows/hides filter band |
| Instant Helper | "?" | In-app help |

### More Menu Contents

- Import Reimbursements (bulk import via file)
- Export Reimbursements (download list)

### Empty State (Observed — Both Claim Month Filters)

- No claims found for May 2026 (current filter)
- Message: "No results found / Looks like you don't have any results for the filter applied"

### EMP003 Claim (Expected but Not Found)

The audit expected EMP003's ₹3,500 claim from May pay run to appear here. It was not found. This is because:
- EMP003 (Vikram Nair) has no reimbursement components configured in their salary structure
- The error "This Employee does not have any reimbursements opted and hence you cannot create claims" confirms no reimbursement types are assigned

---

## New Claim Form

**URL:** `#/approvals/reimbursement-claims/new`
**Page Title:** "Add Claim | Zoho Payroll"

### Layout

```
[Page Header: "New Claim"]  [X close]
[Body]
  Employee Name*   [autocomplete dropdown — all active employees]

  [Table]
    REIMBURSEMENT TYPE* | BILL DATE | BILL NUMBER | ATTACHMENTS | CLAIM AMOUNT* | APPROVED AMOUNT* | [comment] | [action]
    [empty rows — added via "+ Add another bill"]
  [+ Add another bill] link

[Footer]
  [Save & Approve]  [Cancel]    [* indicates mandatory fields]
```

### Employee Selector

- Autocomplete with all active employees
- Observed options: Arjun Mehta (EMP001), Priya Sharma (EMP002), Vikram Nair (EMP003), Aisha Khan (EMP004), Rahul Desai (EMP005)
- Selecting an employee triggers a prerequisite check

### Form Table Columns

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| Reimbursement Type | Dropdown | Yes | List of reimbursement components assigned to employee's salary structure |
| Bill Date | Date input | No | Date of the original bill/receipt |
| Bill Number | Text input | No | Bill/invoice reference number |
| Attachments | File upload | No | Receipt/invoice document |
| Claim Amount | Currency (₹) | Yes | Amount claimed by employee |
| Approved Amount | Currency (₹) | Yes | Amount admin approves — can differ from claimed |
| Comment | Text | No | Per-line-item comment |
| Action | Delete icon | — | Remove bill row |

### "+ Add another bill" Link

- Adds a new row to the table for multiple bill entries within a single claim
- A single claim submission can have multiple bills (e.g., different expense dates)

### Critical Business Rule: Employee Must Have Reimbursement Components

When an employee is selected and "+ Add another bill" is clicked:

**Error toast (red, top-center):** "This Employee does not have any reimbursements opted and hence you cannot create claims."

This means:
1. Reimbursement types must be created under Settings > Salary Components > Reimbursements
2. Each reimbursement type must be assigned to the employee's salary structure
3. Only then will the Reimbursement Type dropdown in the claim form show options
4. Without this configuration, no claims can be submitted for any employee

**This error was observed for BOTH EMP001 and EMP003 — confirming that NO employees in the test org have reimbursement components configured.**

### Save & Approve Button

- The admin creates the claim AND approves it in one step
- This design means the admin is the approver for admin-created claims
- No separate approval step for admin-created reimbursements
- Button was disabled until an employee is selected

### Cancel Link

Navigates back to `#/approvals/reimbursements?filter_by=Status.Submitted`

---

## Reimbursement Configuration Prerequisites (Settings)

### Path to Configure

Settings > Setup & Configurations > Salary Components > Reimbursements (inferred)

Then: Associate reimbursement component to employee's salary structure

Then: Optionally: Mark as FBP component (for Flexible Benefit Plan) via Settings > Claims and Declarations > FBP

### Claims and Declarations Settings

| Tab | URL | Current State |
|-----|-----|---------------|
| Flexible Benefit Plan | `#/settings/preferences/fbp` | "No Active FBP component" |
| Reimbursement Claims | `#/settings/preferences/reimbursement` | "No Active Reimbursement" |
| Income Tax Declaration | `#/settings/preferences/it-declaration` | IT Declaration is Locked |
| Proof Of Investments | `#/settings/preferences/proof-of-investment` | POI is Locked |

**Reimbursement Claims Settings error message:**
"No Active Reimbursement — Employees can get tax exemptions on producing necessary bills. You can enable a reimbursement component under **Settings > Salary Components > Reimbursements** and associate it to the employee's salary."

---

## Reimbursement Architecture (Conceptual)

```
Admin creates Reimbursement Component (type: LTA, Medical, etc.)
  ↓
Admin assigns component to employee's salary structure
  ↓
Employee portal: Employee submits claim + bill + attachments
OR
Admin portal: Admin creates claim on behalf of employee (Save & Approve — immediate)
  ↓
[If employee-submitted] Admin reviews in Approvals > Reimbursements > Pending Claims
  ↓
Admin approves (with approved amount ≤ claimed amount) or rejects
  ↓
Approved amount included in employee's payslip for that month as non-taxable reimbursement
```

---

## Business Rules

1. **Reimbursement types must be configured** before any claim can be created.
2. **Admin-created claims are self-approved** (Save & Approve in one step).
3. **Employee-submitted claims** (via portal) appear in Pending Claims and require admin approval.
4. **Approved Amount can be less than Claimed Amount** — partial approval is supported.
5. **Multiple bills per claim** — one claim can have multiple bill line items (different dates/invoices).
6. **Claim Month filter defaults to current month** — claims from previous months require changing the filter.
7. **Import/Export available** — bulk claim management supported.
8. **FBP vs Regular Reimbursement**: FBP (Flexible Benefit Plan) components allow employee to choose their allocation; regular reimbursements have fixed limits set in the salary structure.

## Indian Statutory Context

Reimbursements are a common Indian payroll component:
- **LTA (Leave Travel Allowance)**: Exempt from tax per Section 10(5) of IT Act; exempt up to actual travel cost
- **Medical Reimbursement**: Up to ₹15,000 per year (Section 17(2)) — though largely replaced by standard deduction
- **Telephone/Internet**: Actual amount reimbursed against bills; tax-exempt
- **Food/Meal vouchers**: Up to ₹50/meal (Swiggy, Sodexo, etc.)
- **Fuel/Car maintenance**: If company-owned car with driver, different treatment

Under New Regime, most of these reimbursements are still processed but their tax treatment differs:
- In New Regime, most allowances/reimbursements are taxable (no exemptions like HRA, LTA)
- However, actual expense reimbursements (not allowances) may still qualify
- This is a critical area that needs clarification for our v1 build

## Cross-Module Impact

- Approved reimbursements appear as "Reimbursement" or specific component name in payslip
- The approved amount is shown separately from gross salary (as a non-taxable component if within limits)
- TDS calculation must exclude approved reimbursement amounts (within statutory limits)

## Key Observations for Our Build

1. **Dual claim entry**: Both admin-created (immediate approval) and employee-submitted (requires admin approval). Our build needs to support both flows.
2. **Approved Amount field is a first-class citizen** — the form has BOTH Claim Amount and Approved Amount, meaning partial approval is built into the core data model.
3. **Reimbursement type is separate from salary component** — the dropdown in claims shows only assigned types, not all types in the system.
4. **Claim-to-Payslip link** — approved amounts need to be passed to the payroll engine for the relevant month.
5. **Bill Date vs Claim Month** — a bill from a different month can be claimed in the current month (e.g., LTA from December claimed in March). Our model needs to support this.
6. **Under New Regime v1**: We should initially support reimbursement processing but clearly document that tax exemption treatment will need statutory review.

## Screenshots

- `73-reimbursements-page.png` — Reimbursements list (empty, "Pending Claims" view)
- `73-reimbursements-view-dropdown.png` — View dropdown (All/Pending/Approved/Rejected Claims)
- `73-reimbursements-more-menu.png` — More menu (Import/Export)
- `73-reimbursements-add-form.png` — New Claim form (before employee selection)
- `73-reimbursements-employee-dropdown.png` — Employee dropdown in New Claim form
- `73-reimbursements-arjun-add-bill.png` — Error when adding bill for employee without reimbursements
- `73-claims-declarations-settings.png` — Claims and Declarations Settings (FBP tab)
- `73-reimbursement-claims-settings.png` — Reimbursement Claims settings tab

## Open Questions

- [ ] Where exactly are reimbursement component types created in Settings? (Message says "Settings > Salary Components > Reimbursements" — needs audit of that page)
- [ ] Is there a per-employee, per-type annual limit configuration?
- [ ] Does the employee portal show a "remaining balance" for each reimbursement type?
- [ ] When is the claim linked to a specific pay run? Is it the claim month or the approval month?
- [ ] Under new regime, which reimbursements are still tax-exempt? (LTA, medical, etc.)
- [ ] Is there a carryforward mechanism for unused reimbursement limits?
- [ ] What columns appear in the Claims table when claims exist (employee name, claim amount, approved amount, status, date)?
