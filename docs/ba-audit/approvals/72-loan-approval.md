# Approvals > Loans (Item 72)

## URL / Navigation Path

- Loans List: `#/loans` (top-level nav, NOT under Approvals)
- Create Loan: `#/loans/new`
- Employee Loans Tab: `#/people/employees/{employeeId}/loans`
- Loan Repayments: linked from "View Loan Repayments" button on list page
- Loan Settings (custom fields): `#/settings/loan/custom-field/list`
- Loan Settings (approvals): `#/settings/loan/...` (not found — loans may not have an approval workflow)

## Purpose

Allows payroll admin to create and manage employee loans, with automatic EMI deductions from monthly salary. Loans are a top-level module, separate from the Approvals section.

---

## Loans List Page

**URL:** `#/loans`
**Page Title:** "Loans | Zoho Payroll"
**Nav Position:** Top-level sidebar (NOT under Approvals)

### Layout

```
[Header]
  ["All Loans" view dropdown]    ["View Loan Repayments" link] [+ Add] [...more] [Filter] [Help]
[Content]
  [Empty state] OR [Loans table]
```

### View Toggle Dropdown Options

- All Loans
- Open Loans
- Paused Loans
- Closed Loans

### Toolbar Actions

| Action | Type | Notes |
|--------|------|-------|
| View Loan Repayments | Link button | Navigates to repayment schedule view |
| + Add | Primary button | Opens Create Loan form |
| "..." more | Dropdown | (content not captured — assumed: Import/Export) |
| Filter | Icon | Filter toggle |
| Instant Helper | "?" | In-app help |

### Empty State (Observed)

"You haven't provided any loans to your employees. You can provide loans to your employees and deduct the instalment amount from their salary every month."

No illustration — plain text.

---

## Create Loan Form

**URL:** `#/loans/new`
**Page Title:** "Loans | Zoho Payroll"

### Layout

```
[Page header: "Create Loan"]  [X close]
  Loan Name*          [Select Loan dropdown — pre-configured types]
  Perquisite Rate:    [Auto-populated: 0% by default]

  Employee Name*      [Search Employee autocomplete]
  Loan Amount* (i)    [₹ currency input]

  Disbursement Date*  [dd/MM/yyyy date picker]

  Reason*             [Textarea]

  [Checkbox] Exempt this loan from perquisite calculation
  [Statutory text about Rule 15(5)]

[Footer: Save | Cancel]    [* indicates mandatory fields]
```

### Fields (Complete)

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Loan Name | Dropdown (autocomplete) | Yes | Must select from pre-configured loan types | "SORRY! NO RESULTS FOUND" if no loan types configured |
| Perquisite Rate | Display (auto-populated) | No | Read-only | Shows perquisite tax rate for the selected loan type (default 0%) |
| Employee Name | Autocomplete search | Yes | Must select active employee | Free-text search; shows employee list |
| Loan Amount | Number (₹) | Yes | > 0 | Info icon (i) — purpose unclear (possibly shows max loan limit) |
| Disbursement Date | Date input | Yes | dd/MM/yyyy format | Date when loan amount is paid to employee |
| Reason | Textarea | Yes | Non-empty | Free text; no character limit observed |
| Exempt from perquisite calculation | Checkbox | No | — | Applies Rule 15(5) IT Rules exemption |

### Statutory Reference on Form

"According to Rule 15(5) of the Income Tax Rules, 2026, employees availing medical loans (for treatment of diseases specified in Rule 18) or any other loans below ₹2,00,000 in aggregate can be exempted from perquisite calculation."

This is a direct statutory reference to Income Tax Rules Rule 15(5) — employer loans are treated as perquisites (taxable benefit) unless exempt. The ₹2,00,000 threshold and medical loan categories are statutory exemptions.

### Missing Fields (Not in Create Loan Form)

Notably absent from the Create Loan form:
- Number of instalments / tenure (months)
- EMI amount
- Interest rate
- Repayment start month
- Disbursement month

**These are likely configured in the Loan Name/Template** — the loan type pre-defines tenure, EMI calculation, and interest rate. The admin selects a pre-configured template and provides employee-specific details.

### Prerequisite: Loan Types Must Be Configured

When clicking "Loan Name" dropdown, the dropdown shows "SORRY! NO RESULTS FOUND" — because no loan types (templates) have been configured for the lerno org.

Loan types are configured under Settings (likely Settings > Salary Components > Deductions, or a dedicated Loans settings page). The exact configuration path was not fully traced.

---

## Employee Loans Tab

**URL:** `#/people/employees/{employeeId}/loans`

### Empty State

"This employee hasn't taken any loans yet."
With a "Create Loan" button.

Clicking "Create Loan" from this page likely pre-fills the Employee Name in the loan form, linking the loan to that employee.

---

## Loan Approval Architecture

### Key Finding: No Dedicated Loan Approval Queue

Loans do NOT appear in the `#/approvals` section. Based on the audit:

1. **Admin-created loans are auto-approved** — no approval workflow observed
2. **No loan approval Settings page found** — `#/settings/payrun/custom-approval/list` and `#/settings/salary-revision/custom-approval/list` exist, but no equivalent for loans
3. The Settings sidebar under Module Settings > General shows "Loans" leading to `#/settings/loan/custom-field/list` (only custom fields)

### Loan State Machine

```
Created → Open (EMI deductions begin from next pay run)
       → Paused (admin pauses; EMI deductions suspended)
       → Closed (loan fully repaid or manually closed)
```

### EMI Deduction Behaviour

- EMI is a deduction from salary in each pay run
- Disbursement Date determines when the loan starts (likely ties to a disbursement pay run or manual)
- Repayment starts from the configured start month (inferred from loan template)

---

## Business Rules

1. **Loan types must be pre-configured** — admin cannot create an ad-hoc loan without a matching loan type template.
2. **Perquisite tax applies to employer loans** — under Income Tax Act, loans from employers above ₹2,00,000 are perquisites and subject to tax.
3. **Medical loans exempt from perquisite** — per Rule 15(5) of IT Rules, Rule 18-specified disease medical loans are exempt.
4. **No approval workflow for loans** in Zoho's current implementation (at least for admin-created loans).
5. **Paused Loans** — admin can suspend EMI deductions (e.g., during unpaid leave).
6. **Closed Loans** — either fully repaid or manually closed by admin.

## Cross-Module Impact

- Loan EMI deduction appears as a deduction component in monthly pay run
- Loan balance updates after each pay run
- Employee-facing: visible in Employee Loans tab
- Disbursement may appear as an addition in the disbursement month's pay run

## Key Observations for Our Build

1. **Loan types/templates are a mandatory prerequisite** — design a LoanType entity with: name, tenure (months), interest rate, perquisite rate, calculation method.
2. **Perquisite rate is per loan type** — not per employee — shown on the create form as read-only once loan type is selected.
3. **No approval workflow required for v1** — admin-created loans are auto-approved in Zoho. We can follow the same pattern.
4. **Statutory compliance note**: Perquisite calculation on loans is an important TDS consideration (Rule 15(5) IT Rules). Need to implement this in the engine.
5. **Disbursement date is separate from repayment start** — clarify: does disbursement happen via payroll or external? Zoho appears to treat it as a record-keeping date.

## Screenshots

- `72-loans-empty.png` — Loans list empty state
- `72-loans-view-dropdown.png` — Status view dropdown (All/Open/Paused/Closed)
- `72-loans-create-form.png` — Create Loan form
- `72-loans-type-empty.png` — Loan Name dropdown with no configured types
- `72-loans-employee-page.png` — Employee Loans tab

## Open Questions

- [ ] Where exactly are loan types configured in Settings? (Not found under `#/settings/loan/`)
- [ ] Does the Loan Amount (i) info icon show a maximum loan limit tied to the employee's salary?
- [ ] How is the EMI amount determined — fixed from the template or entered per loan?
- [ ] Is there an interest rate and if so, how does it affect payslip/tax calculations?
- [ ] Does Disbursement Date trigger anything in the payroll engine or is it just metadata?
- [ ] Can employees request a loan via the employee portal, creating an approval request?
- [ ] What does "View Loan Repayments" show — a per-employee repayment schedule table?
