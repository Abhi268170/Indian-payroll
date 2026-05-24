# UF-94: Settings — Loans Module Configuration

**Module:** Settings > Module Settings > General > Loans
**Tested:** 2026-05-16
**URL:** `#/settings/loan/custom-field/list`

---

## Navigation

Settings > Module Settings > General (expand) > Loans

---

## Loans Settings — Sub-Navigation (5 tabs)

| Tab | URL | Description |
|-----|-----|-------------|
| Custom Field | `/custom-field/list` | Define custom fields for loan records |
| Custom Button | `/custom-button/list` | Add custom action buttons to loan UI |
| Validation Rules | `/field-validations` | Custom validation rules for loan fields |
| Record Locking | `/record-locking` | Configure when loan records become locked |
| Related List | `/related-list` | Define related entity lists in loan detail panel |

**This is the "Module Customization" for Loans — NOT the loan type configuration (loan types are configured directly in the Loans module, not here).**

---

## Custom Fields (Current State)

"You haven't created any custom fields yet."

**Custom Fields Usage:** 0 / 59 (59 total allowed custom fields)

**"Create Custom Field" button** → Creates a new custom field for the Loan entity

### Expected Custom Field Types
| Type | Example |
|------|---------|
| Single Line Text | Loan Reference Number |
| Multi Line Text | Loan Approval Notes |
| Number | Loan Account Number |
| Decimal | Custom interest rate (if not using standard) |
| Date | Custom review date |
| Dropdown | Loan purpose category |
| Checkbox | Collateral provided? |
| User | Loan approver (user reference) |
| URL | Loan agreement document link |
| File Upload | Loan agreement upload |

### Custom Field Usage
- Custom fields appear on the Create Loan form and Loan Detail panel
- Can be marked required or optional
- Can be included in Loan reports

---

## Validation Rules

`/field-validations`

Allows admin to define business rules that prevent certain loan configurations:
- e.g., "Loan amount cannot exceed ₹5,00,000"
- e.g., "Number of instalments cannot exceed 36"
- e.g., "Loan disbursement date cannot be in the past"

---

## Record Locking

`/record-locking`

Configures when a loan record becomes read-only (locked):
- e.g., "Lock loan record after first EMI is paid"
- e.g., "Lock loan record when status = Closed"

**Purpose:** Prevents accidental modification of loan records that have active deductions.

---

## Loan Type Configuration (NOT in Settings)

**Important distinction:** Loan Types (Personal Loan, Emergency Loan, Vehicle Loan, etc.) are configured directly in the Loans module, NOT in Settings.

**Observed loan types in demo org (from UF-63):**
- Personal Loan (used for LOAN-00001 — Arjun)
- Emergency Loan (used for LOAN-00002 — Vikram)

These types are created/managed at: `#/loans` → Loan Types section (if exists) or within the Create Loan modal.

---

## Module Settings — General (Full Sub-Navigation)

From the Settings sidebar under "Module Settings > General":
| Sub-item | URL | Description |
|----------|-----|-------------|
| Employees & Contractors | `#/settings/employee/contractor` | Employee type settings |
| Pay Runs | `#/settings/payrun/custom-approval/list` | Pay run approval chain |
| Salary Revisions | `#/settings/salary-revision/custom-approval/list` | Salary revision approval |
| Leave & Attendance | `#/settings/holiday-leave/enable-module` | Leave module configuration |
| Loans | `#/settings/loan/custom-field/list` | Loans customization |

---

## Business Rules
1. Loans settings in this page = customization (custom fields, buttons, validations)
2. Loan types (Personal, Emergency, etc.) configured in Loans module directly
3. Custom fields allow orgs to capture additional loan data not in default fields
4. Validation rules prevent misconfigured loans (admin-defined business constraints)
5. Record locking prevents edits after EMI cycle begins

## Gaps / Observations
- Loan type configuration location in Loans module (not Settings) not directly navigated
- Custom fields creation not tested
- Validation rules and Record Locking pages not navigated

## Open Questions
- [ ] Where in the Loans module are Loan Types created/managed?
- [ ] Can custom fields be made mandatory for loan creation?
- [ ] Are validation rules evaluated client-side or server-side?
- [ ] What is the maximum custom field limit for Loans (59 confirmed from UI)?
