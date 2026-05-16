# UF-87: Settings — Additional Modules

**Module:** Settings > Salary Templates, User Management, FBP, Portal Web Tabs
**Tested:** 2026-05-16
**Mock Data Used:** Demo org configuration
**App State Before:** Multiple settings pages visited

## Steps Executed
1. Navigate to `#/settings/salary-templates` — observe templates
2. Navigate to `#/settings/users` — observe access restriction
3. Navigate to `#/settings/salary-components/benefits` — observe benefits list
4. Navigate to `#/settings/preferences/fbp` — observe FBP state
5. Navigate to `#/settings/portal/webtabs` — observe web tabs
6. Document Prior Payroll configuration

---

## Salary Templates (`#/settings/salary-templates`)

### Purpose
Pre-configured salary structure templates that can be applied to new employees. Reduces repetitive setup when multiple employees have the same salary structure pattern.

### List
| Template Name | Description | Status |
|---------------|-------------|--------|
| Standard-Exec | (blank) | Active |

### Actions
- "Create New" button — creates a new template
- Row overflow → Edit, Duplicate, Delete, Deactivate

### Relationship to Salary Structures
- Template = reusable salary structure blueprint
- When assigning salary to an employee, admin can select a template instead of building from scratch
- Template changes do NOT retroactively affect existing employee assignments

---

## User Management (`#/settings/users`)

**Access:** Returned `#/unauthorized` when navigated directly. The correct path may be through Settings > Users and Roles sub-navigation.

**Expected Features (from domain knowledge + Settings nav):**
- Add Users: Invite team members (HR, Finance, Payroll Admins) by email
- Roles: Assign roles (Super Admin, HR Manager, Payroll Officer, Read-Only)
- Access Permissions: Configure which modules each role can access

**Note:** `#/settings/loans` also returns unauthorized — Loan Settings may require Super Admin access or a specific role.

---

## Benefits Components (`#/settings/salary-components/benefits`)

### Table Columns
| Column | Description |
|--------|-------------|
| Name | Component name |
| Benefit Type | Category |
| Benefit Frequency | Recurring / One Time |
| Status | Active / Inactive |

### All Benefits Components
| Name | Benefit Type | Frequency | Status |
|------|-------------|-----------|--------|
| Voluntary Provident Fund | Voluntary Provident Fund | Recurring | Active |

**Only one benefit component configured.** VPF (Voluntary Provident Fund) allows employees to contribute MORE than the mandatory 12% of basic to their PF account.

### VPF Business Rules
- Statutory limit: Employer must contribute 12% of basic; employee mandatorily contributes 12% of basic
- VPF: Employee voluntarily contributes an additional amount (up to 100% of basic) to PF
- VPF gets the same interest rate as EPF (currently 8.25% p.a.)
- VPF contribution appears under Section 80C deduction (old regime)
- In new regime: VPF still earns interest but no 80C tax benefit
- Employer does NOT match VPF contributions (only the mandatory EPF contribution is matched)
- VPF is deducted from employee salary and remitted to EPFO UAN

---

## Deductions Components Summary (from previous navigation)

| Name | Type | Frequency | Status |
|------|------|-----------|--------|
| Meal Card | Other Deductions | Recurring | Active |
| Withheld Salary | Withheld Salary | One Time | Active |
| Notice Pay Deduction | Notice Pay Deduction | One Time | Active |

**Key observations:**
- "Withheld Salary" — allows admin to withhold salary in a pay run (pending payment)
- "Notice Pay Deduction" — used when employee leaves without serving notice period (salary recovery)
- "Meal Card" — recurring deduction for meal card charges (company-provided meal allowance)

---

## Reimbursements Components Summary (from previous navigation)

All 5 reimbursement components are INACTIVE with Maximum Amount = ₹0:
- Fuel Reimbursement
- Driver Reimbursement
- Vehicle Maintenance Reimbursement
- Telephone Reimbursement
- Leave Travel Allowance

**Implication:** No reimbursements are currently claimable by employees. All reimbursement components must be activated and given a maximum amount before employees can submit claims.

---

## FBP Settings (`#/settings/preferences/fbp`)

### Current State
"No Active FBP component" — message: "Your organisation does not have an active FBP component associated to an employee. Mark a reimbursement as FBP component under Settings > Salary Components > Reimbursements and associate it to the employee's salary."

### What This Means
- To enable FBP, admin must:
  1. Go to Settings > Salary Components > Reimbursements
  2. Edit one or more reimbursement components
  3. Mark them as "FBP component" (checkbox or toggle on the component)
  4. Assign the FBP component to employee salary structures
- FBP declarations then appear in employee portal
- FBP claims are submitted monthly and approved by admin

---

## Employee Portal Web Tabs (`#/settings/portal/webtabs`)

### Current State
Empty: "You haven't created any web tab yet"
Message: "Create a web tab to help employees quickly access external sites like company policies, learning platforms, and other resources—right from their Employee Portal."

### Purpose
Web Tabs allow admin to embed external URLs (company intranet, learning portal, HR policy docs) as tabs within the employee portal. Employees see these tabs as additional navigation items in their portal.

---

## Prior Payroll (`#/prior-payroll`)

### Current State
Not enabled. Message: "You have not checked the option to include prior payroll during setup. In case you need to add prior payroll data for your employees, you can import the necessary details and continue processing payrolls."

### Onboarding Checklist Discrepancy
The Dashboard onboarding checklist shows Step 7 "Configure Prior Payroll" as COMPLETED. However, the actual page shows Prior Payroll is NOT enabled.

**Root cause:** The onboarding checklist marks a step as "complete" as soon as the admin visits the page — not based on actual configuration. This is misleading.

### Why Prior Payroll Matters
- If employees joined the company mid-year from another employer, their YTD salary and TDS from the prior employer must be captured
- Without prior payroll data: Zoho computes TDS only on salary paid through Zoho, ignoring prior employer income → TDS may be understated → employee owes tax at year-end
- Form 16 Part B will only cover Zoho-period salary
- Statutory requirement: Employer must consider prior employer income per Form 12B submitted by employee

### Enable Prior Payroll Action
Clicking "Enable Prior Payroll" allows admin to enter per-employee YTD data:
- Prior employer name
- Prior employer TAN
- Salary paid by prior employer
- TDS deducted by prior employer
- PF contributions by prior employer (if applicable)

---

## Settings Navigation — Complete Map

### Organisation Settings
| Section | URL Pattern |
|---------|------------|
| Organisation Profile | `#/settings/orgprofile` |
| Users and Roles | Unauthorized from direct URL |
| Taxes (Tax Deductor) | `#/settings/taxes` |
| Pay Schedule | `#/settings/pay-schedules` |
| Statutory Components | `#/settings/statutory-details/list` |
| Salary Components — Earnings | `#/settings/salary-components/earnings` |
| Salary Components — Deductions | `#/settings/salary-components/deductions` |
| Salary Components — Benefits | `#/settings/salary-components/benefits` |
| Salary Components — Reimbursements | `#/settings/salary-components/reimbursements` |
| Employee Portal — Preferences | `#/settings/portal/preferences` |
| Employee Portal — Web Tabs | `#/settings/portal/webtabs` |
| Claims & Declarations — FBP | `#/settings/preferences/fbp` |
| Claims & Declarations — IT Declaration | `#/settings/preferences/it-declaration` |

### Module Settings
| Section | URL Pattern |
|---------|------------|
| General (unknown sub-items) | — |
| Payments — Direct Deposit | `#/settings/direct-deposit` |

### Extensions & Developer Data
| Section | URL Pattern |
|---------|------------|
| Integrations | — |
| Developer Data | — |

### Additional Notable Features (from Dashboard)
| Feature | URL |
|---------|-----|
| Direct Deposit | `#/settings/direct-deposit` |
| Salary Templates | `#/settings/salary-templates` |
| Auto Reminder IT & POI | `#/settings/preferences/it-declaration` |
| Employee Custom Field | `#/settings/employee/custom-field/list` |

---

## Gaps / Observations
- Users and Roles returns unauthorized — role management not directly investigated
- Loan settings (`#/settings/loans`) returns unauthorized — loan type configuration not found
- Direct Deposit settings not explored
- Integrations section not explored (potential Zoho Books, HRMS integration)
- Developer Data not explored (may contain API keys, webhooks)
- Employee Custom Field not explored

## Open Questions
- [ ] Where are loan types configured? The Loans module showed Loan Name as a dropdown from a configured list.
- [ ] What user roles are available? (Super Admin, HR Manager, Payroll Officer, etc.)
- [ ] What does the Integrations section contain? (Zoho Books sync? Bank integration?)
- [ ] Can the Employee Custom Field be used to capture fields like employee category, grade band, cost center?
- [ ] Where is the "Auto Reminder for IT & POI Declaration" schedule configured — frequency and timing?
