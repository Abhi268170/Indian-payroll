# Employees > Employee Profile View (All Tabs)

## URL / Navigation Path
- Base Route: `#/people/employees/{id}`
- Full URL (EMP001): `https://payroll.zoho.in/#/people/employees/3848927000000032948`
- Entry: Clicking employee row in Employee List; or completing Add Employee wizard
- Page title: "Overview | Employees | Zoho Payroll"

## Purpose
Full employee profile view — read-only display of all employee data across 5 tabs, with edit access to each section. Also entry point for key employee lifecycle actions (exit, loan, salary revision).

## Profile Header (Persistent across all tabs)

### Employee Identity Strip
| Element | Value for EMP001 | Notes |
|---|---|---|
| Avatar | Initials "A" (auto-generated from first name) | No photo upload in this version |
| Employee ID | EMP001 | Displayed as "EMP001 -" before name |
| Employee Name | Arjun Mehta | Full name |
| Status Badge | Active | Badge; other values: Inactive, On Notice Period, Exited |
| Designation | Senior Software Engineer | Below name |

### Header Action Buttons
| Button | Type | Actions |
|---|---|---|
| Add | Split button (primary) | Dropdown: Scheduled Earning / Deduction / Benefit / Donation Contribution |
| Show dropdown menu | Caret on split button | Dropdown: Add / Update Vehicle Details (separator) / Delete Employee / Initiate Exit Process |
| Close | Icon button | Returns to Employee List |

**"Add" dropdown items:**
- Scheduled Earning → `#/people/employees/{id}/salary-details?add_scheduled_earning=true`
- Deduction → (navigates to add deduction form)
- Benefit → (navigates to add benefit form)
- Donation Contribution → (navigates to donation contribution form)

**"Show dropdown menu" items:**
- Add / Update Vehicle Details
- Delete Employee
- Initiate Exit Process

## Profile Tabs (5 Total)

| Tab | URL | Purpose |
|---|---|---|
| Overview | `#/people/employees/{id}` | All employee data read-only: Basic, Statutory, Personal, Payment info |
| Salary Details | `#/people/employees/{id}/salary-details` | Salary structure + Perquisites |
| Investments | `#/people/employees/{id}/investments-and-proofs` | IT Declaration + Proof of Investments |
| Payslips & Forms | `#/people/employees/{id}/payslips-and-forms` | Payslips by FY + Form 16 |
| Loans | `#/people/employees/{id}/loans` | Employee loans + Create Loan |

---

## Tab 1: Overview (`#/people/employees/{id}`)

Four sections, each independently editable.

### Section 1: Basic Information
Edit → opens inline edit (same page, not wizard)

| Field | EMP001 Value | Edit Route |
|---|---|---|
| Name | Arjun Mehta | Inline |
| Email Address | arjun.mehta@lerno.com | NOT editable (immutable post-save) |
| Mobile Number | 9876543210 | Inline |
| Date of Joining | 01/04/2025 | Inline |
| Gender | Male | Inline |
| Work Location | Head Office | Inline |
| Designation | Senior Software Engineer | Inline |
| Departments | Engineering | Inline |
| Portal Access | Disabled (Enable button) | Toggle via button |

### Section 2: Statutory Information
Edit → `#/people/employees/{id}/edit-statutory-details`

| Field | EMP001 Value | Notes |
|---|---|---|
| Professional Tax | Enabled (Disable button) | Toggle to enable/disable PT deduction for this employee |

Note: PF and ESI not shown because not configured at org level. PT shown because org has PT enabled (work location = Head Office, Maharashtra PT slab applies).

### Section 3: Personal Information
Edit → `#/people/employees/{id}/edit-personal-details`

| Field | EMP001 Value | Notes |
|---|---|---|
| Date of Birth | 15/03/1990 | — |
| Father's Name | Rajesh Mehta | Required for Form 16 |
| PAN | ABCPM1234A | Unmasked in admin view |
| Email Address (Personal) | - (not filled) | — |
| Residential Address | - (not filled) | — |
| Differently Abled Type | None | — |

### Section 4: Payment Information
Edit → inline edit (button trigger)

| Field | EMP001 Value | Notes |
|---|---|---|
| Payment Mode | Manual Bank Transfer | — |
| Account Number | XXXX6789 | Masked; "Show A/C No" button reveals full number |
| Account Holder Name | Arjun Mehta | — |
| Bank Name | HDFC Bank | — |
| IFSC | HDFC0001234 | — |
| Account Type | Savings | — |

---

## Tab 2: Salary Details (`#/people/employees/{id}/salary-details`)

### Salary Details Card
Header: "Salary Details" + Edit link + "Show dropdown menu" caret

**Caret dropdown options:**
- Print Salary Structure
- Send Salary Certificate
- Print Salary Certificate

| Field | EMP001 Value |
|---|---|
| Annual CTC | ₹8,40,000.00 per year |
| Monthly CTC | ₹70,000.00 per month |

### Salary Structure Table
Columns: Salary Components | Monthly Amount | Annual Amount

| Component | Formula | Monthly | Annual |
|---|---|---|---|
| Earnings (section header) | — | — | — |
| Basic | 57.14% of CTC | ₹39,998.00 | ₹4,79,976.00 |
| House Rent Allowance | 40.00% of Basic Amount | ₹15,999.00 | ₹1,91,988.00 |
| Fixed Allowance | Residual | ₹14,003.00 | ₹1,68,036.00 |
| **Cost to Company** | Sum | **₹70,000.00** | **₹8,40,000.00** |

### Perquisites Section
- Heading: "Perquisites"
- Additional Benefits: ₹0.00
- "View Details" link → `#/people/employees/{id}/perquisites`
- Perquisites = non-cash benefits subject to TDS (e.g., company car, rent-free accommodation, ESOPs)

---

## Tab 3: Investments (`#/people/employees/{id}/investments-and-proofs`)
See item 40 (40-add-employee-tax.md) for full detail.

Sub-tabs: IT Declaration | Proof Of Investments
- Both locked by default for new employee
- Period selector: FY 2026-27
- Admin can submit on behalf

---

## Tab 4: Payslips & Forms (`#/people/employees/{id}/payslips-and-forms`)

### Payslips and TDS Sheets Section
- Financial Year selector: "Financial Year : 2026 - 27" (dropdown — can switch years)
- Empty state: "There are no payslips for this financial year."
- When payslips exist: table rows with Month | Pay Period | Net Pay | Download/View actions

### Form 16 Section
- Heading: "Form 16"
- Empty state: "Form 16 hasn't been generated for this employee yet!"
- When generated: shows Part A and Part B download links per FY

---

## Tab 5: Loans (`#/people/employees/{id}/loans`)

### Empty State
- Illustration + message: "This employee hasn't taken any loans yet."
- Button: "Create Loan" — opens loan creation flow

### When Loans Exist (not observed for EMP001)
Expected: Table with Loan Amount | Disbursed Date | EMI | Outstanding Balance | Status

---

## State Machine: Employee Status
| Status | Meaning | Trigger |
|---|---|---|
| Active | Normal working employee | Default on creation |
| On Notice Period | Exit initiated, serving notice | "Initiate Exit Process" action |
| Exited | Full & Final settlement complete | F&F completion |
| Inactive | Administratively disabled | Admin action (not same as exit) |

## Business Rules
1. Work Email is immutable — cannot be edited post-creation (used as portal login identifier).
2. PT can be toggled per-employee independently of org-level PT config.
3. Account number always masked in display (`XXXX` + last 4). Full reveal = authorised roles only.
4. Perquisites are tracked separately from salary components — they affect TDS but not gross salary.
5. Salary revision creates a new salary record (not overwrite) — history preserved.
6. Delete Employee is available from header dropdown — likely soft-delete with confirmation.
7. "Initiate Exit Process" starts the offboarding workflow (Notice Period → F&F).

## Cross-Module Impact
- Overview edit routes (`edit-statutory-details`, `edit-personal-details`) are separate from wizard routes.
- Salary Details tab links to `edit-salary-details` and `perquisites` sub-pages.
- Payslips & Forms tab is populated by Payroll Run module — no data until first pay run.
- Loans tab feeds into EMI deductions in payroll run.
- Investments tab feeds into TDS calculation.

## Key Observations for Our Build
1. **5 profile tabs** — our employee profile must implement all 5: Overview, Salary Details, Investments, Payslips & Forms, Loans.
2. **Split-button header actions** — "Add" dropdown (Scheduled Earning / Deduction / Benefit / Donation) and kebab dropdown (Vehicle Details / Delete / Initiate Exit) must be implemented.
3. **Perquisites as separate entity** — perquisites need their own data model and TDS impact calculation, separate from salary components.
4. **Print/Send salary certificate** — requires PDF generation for salary structure. Our Reports module must cover this.
5. **Form 16 per employee per FY** — after payroll finalization and year-end, Form 16 generation populates this tab.
6. **FY-aware payslip listing** — payslips organized by financial year with year selector.
7. **PT per-employee toggle** — even if org has PT enabled, individual employees can be exempted. Implement `pt_enabled` boolean on employee entity.
8. **Account number masking** — `XXXX` + last 4 pattern must be consistent across all views.

## Screenshots
- `screenshots/41-emp001-summary.png` — Wizard summary/confirmation step
- `screenshots/41-employee-profile.png` — Overview tab
- `screenshots/41-salary-details-tab.png` — Salary Details tab with structure table
- `screenshots/41-payslips-forms-tab.png` — Payslips & Forms tab (empty state)
- `screenshots/41-loans-tab.png` — Loans tab (empty state)
- `screenshots/41-profile-dropdown-menu.png` — Header kebab dropdown: Delete / Initiate Exit / Vehicle Details
- `screenshots/41-profile-add-button.png` — Header Add button dropdown
