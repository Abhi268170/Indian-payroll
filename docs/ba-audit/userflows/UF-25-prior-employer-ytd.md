# UF-25: Prior Employer YTD — Previous Employment Details

**Module:** Employees → Import Data → Previous Employment Details / Prior Payroll
**Tested:** 2026-05-16

## Context

Prior employer YTD (Year-to-Date figures from a previous employer) is relevant when an employee joins mid-fiscal year after working elsewhere. The previous employer's YTD income, TDS deducted, and statutory contributions are needed to compute correct TDS for the rest of the fiscal year.

In Zoho Payroll, this is handled through two mechanisms:
1. **"Previous Employment Details" import** — bulk import of prior employer data via CSV
2. **IT Declaration form** — the "Previous Employment Details" section within an employee's IT declaration (accessible when declaration is submitted)

## Prior Payroll Page

**URL:** `#/prior-payroll`

### States Observed

**State: Disabled (prior payroll not enabled at org setup)**

- Message: "You have not checked the option to include prior payroll during setup. In case you need to add prior payroll data for your employees, you can import the necessary details and continue processing payrolls."
- CTA: "Enable Prior Payroll" button
- Error when attempting to enable (after pay runs exist): "Organisation prior payroll cannot be updated since payroll had been run already."

**Business rule:** Prior payroll data can only be enabled BEFORE the first pay run is processed. This is a one-time setup choice made during org onboarding. Once any pay run exists, the setting is permanently locked.

## Previous Employment Details — Import Type

Available in Employees → Import Data → type: "Previous Employment Details" (under Investments group).

Sample CSV columns not retrieved (URL pattern guessing failed). Expected columns based on Indian statutory requirements:

| Expected Field | Statutory Relevance |
|----------------|---------------------|
| Employee Number | Linking to employee record |
| Previous Employer Name | Form 12B requirement |
| PAN of Previous Employer | TDS cross-referencing |
| Period of Employment (From) | For pro-rated TDS |
| Period of Employment (To) | For pro-rated TDS |
| Gross Salary (Previous Employer) | TDS base computation |
| TDS Deducted (Previous Employer) | TDS credit |
| EPF Contribution (Previous Employer) | PF YTD |
| Professional Tax Paid (Previous Employer) | PT YTD |
| Allowances Exempt u/s 10 | HRA and other exemptions |

## IT Declaration — Prior Employer Section

The "Previous Employment Details" import type (under Investments group) feeds into the IT declaration computation. This is separate from the regular IT declaration investment sections.

The IT declaration form as observed has these sections:
1. House Property (rented house, home loan, rental income toggles)
2. Other Sources of Income (accordion, expands to 4 sub-fields)
3. Section 123 Investments (Earlier: 80C Investments) — limit ₹1,50,000
4. Section 126 Exemptions (Earlier: 80D Exemptions) — limit ₹1,00,000
5. Other Investments & Exemptions (NPS, education loan, medical)

**No visible "Previous Employment" section in the IT Declaration form UI** — prior employer YTD is entered separately via the import mechanism, not inline in the declaration form. This differs from products like GreytHR where prior employer details are entered directly in the employee's IT declaration.

## IT Declaration Form — Complete Section Inventory

### Other Sources of Income (expanded)
| Sub-field | Type | Default |
|-----------|------|---------|
| Income from other sources | Number (₹) | 0 |
| Interest Earned from Savings Deposit | Number (₹) | 0 |
| Interest Earned from Fixed Deposit | Number (₹) | 0 |
| Interest Earned from National Savings Certificates | Number (₹) | 0 |

### Section 123 Investments (Earlier: 80C Investments)
- Note: "List of investments including LIC schemes, mutual funds and PPF. Maximum limit ₹1,50,000.00"
- Input: Dropdown (Select an Investment type) + Amount (₹) — repeatable rows
- "Add an Investment" button to add more rows

### Section 126 Exemptions (Earlier: 80D Exemptions)
- Note: "Mediclaim policies for yourself, children, spouse and parents. Maximum limit ₹1,00,000.00"
- Input: Dropdown (Select an Investment type) + Amount (₹) — repeatable rows
- "Add an Investment" button

### Other Investments & Exemptions
- Note: "Declare other investments & exemptions such as Voluntary NPS, Interest Paid on Education Loan and Medical Expenditures"
- Input: Dropdown (Select an Investment type) + Amount (₹) — repeatable rows
- "Add an Investment" button

### Form Action
- "Submit and Compare" button — submits declaration and moves to regime comparison page
- "Cancel" link — returns to `#/people/employees/{id}/investments-and-proofs?resource_type=it`

## Business Rules

1. **Prior payroll is org-level, one-time setup** — cannot be enabled after first pay run. This means mid-year org migrations lose prior payroll data upload capability immediately after first pay run.

2. **Section 123 replaces 80C nomenclature** — Zoho has renamed 80C to "Section 123 Investments" for new regime alignment. But the ₹1,50,000 limit displayed is the OLD regime 80C limit — this limit does NOT apply under new tax regime (all such deductions are unavailable).

3. **Section 126 replaces 80D** — similar renaming. Under new regime, 80D deductions are NOT available.

4. **Statutory compliance gap** — displaying old-regime limits (₹1,50,000, ₹1,00,000) in a product that claims to be new-regime-only (v1 per CLAUDE.md) may mislead employees and admins.

5. **Prior employer TDS credit** — Previous Employment Details are fed into TDS computation to credit TDS already deducted by the prior employer. Without this data, the system would compute TDS on only the current employer's salary, potentially over-deducting.

## 🔴 Critical Compliance Concerns

- **Old regime limits displayed** for Section 123 and 126 investments — these are inapplicable under new tax regime but prominently shown with ₹ limits. Risk of employees entering declarations that have no TDS effect.
- **Prior payroll lock after first run** — orgs that run payroll before completing prior employer YTD setup will have incorrect TDS for joining employees for the full fiscal year.
- **`?tax_regime=with_exemptions` URL parameter** — the admin "Submit Declaration" path defaults to `with_exemptions` (old regime mode). This is a critical bug for new-regime-only orgs.

## Navigation
- Prior Payroll: `#/prior-payroll`
- Import Previous Employment: Employees list → Import Data → Previous Employment Details → Proceed
- IT Declaration: `#/people/employees/{id}/investments-and-proofs` → IT Declaration tab → Submit Declaration

## Screenshots
- [Prior payroll disabled state](../screenshots/UF-25-prior-payroll-disabled.png)
- [Prior payroll enable error](../screenshots/UF-25-prior-payroll-error.png)
- [IT Declaration form full sections](../screenshots/UF-27-IT-declaration-sections.png)

## Gaps / Observations
- Previous Employment Details import sample CSV not retrieved — column list not confirmed
- No UI path to enter prior employer details inline on employee profile (must use import or IT declaration)
- "Form 12B" submission mechanism (employee submitting prior employer details to current employer) not explicitly identified in UI
- YTD figures from prior employer display in payslip or not — not confirmed
