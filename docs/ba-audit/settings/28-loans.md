# Settings > Module Settings > Loans

## URL
`#/settings/loan/custom-field/list` (default sub-tab)

## Sub-tabs

| Tab | URL |
|-----|-----|
| Custom Field | `#/settings/loan/custom-field/list` |
| Custom Button | `#/settings/loan/custom-button/list` |
| Validation Rules | `#/settings/loan/field-validations` |
| Record Locking | `#/settings/loan/record-locking` |
| Related List | `#/settings/loan/related-list` |

## Purpose
Configuration for the Loan module. Supports custom fields, custom buttons, validation rules, record locking, and related list views — same extensibility pattern as Employees & Contractors.

---

## Tab 1: Custom Field

### Custom Fields Usage Indicator
`Custom Fields Usage: 0/59` — up to 59 custom fields per Loan entity.

### Empty State
"You haven't created any custom fields yet."

**Button:** Create New / Create Custom Field

**Data Types available:** Same 18 types as Employee Custom Fields (Text, Number, Date, Dropdown, Formula, etc.)

---

## Business Rules
1. **Same custom field limit as employees** — 59 custom fields per Loan entity.
2. **Loan approval** is a separate module from payroll approval — configured separately but both use the same 3-tier approval workflow (Simple/Multi-Level/Custom) visible under Module Settings > Loans approvals (not explicitly a sub-tab here, but likely exists at `#/settings/loan/custom-approval/list`).
3. **Loan deduction in payroll** — approved loan EMIs are deducted from employee net pay each payroll run; requires Loan module to be configured.

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| Loan custom fields | Appear on Loan application form |
| Loan approval workflow | Gates loan disbursement (separate from payroll approval) |
| Loan EMI | Deducted from employee's net pay each pay run |

## Observations & Notes
1. **Loan module is active** — sub-tab visible in nav under Module Settings > Loans (unlike Contractors which is disabled).
2. **No Approvals sub-tab visible here** — Loan approval likely at a different URL (`#/settings/loan/custom-approval/list` — to verify).
3. For our build: Loan entity with: employee_id, principal, interest_rate, tenure_months, emi_amount, disbursement_date, status (Pending/Approved/Active/Closed). EMI deduction auto-appears in payroll as a deduction line.

## Screenshots
`docs/ba-audit/settings/screenshots/28-loans.png`
