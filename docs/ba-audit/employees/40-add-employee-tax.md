# Employees > Investments & Tax Declaration (Investments Tab)

## URL / Navigation Path
- Route: `#/people/employees/{id}/investments-and-proofs`
- Full URL: `https://payroll.zoho.in/#/people/employees/3848927000000032948/investments-and-proofs`
- Entry: "Investments" tab in employee profile; also accessible after completing Add Employee wizard
- Page title: "Investments & Proofs | Employees | Zoho Payroll"

## Purpose
Per-employee tax investment declaration and proof submission management. Admin can view, lock/unlock, and submit IT Declaration and Proof of Investments (POI) on behalf of employees. Employees submit through the employee self-service portal.

## Sub-Tabs

### 1. IT Declaration
**URL suffix:** No query param (default tab)

#### Period Selector
- Button: "Period : 2026 - 27" — dropdown to select financial year. Allows historical period review.

#### Locked State (Default for New Employee)
- Icon + message: "IT Declaration submission is locked for this employee"
- Sub-message: "You can either allow the employee to submit IT Declaration through the portal or submit it on their behalf"
- Button: "Submit Declaration" → navigates to `#/people/employees/{id}/investment-declaration/new?tax_regime=with_exemptions`

**Critical observation:** The URL parameter `tax_regime=with_exemptions` indicates Zoho defaults IT Declaration to the OLD regime (with exemptions = old regime with 80C, HRA, etc. deductions). "Without exemptions" would be new regime. Our build is new regime ONLY — we must not implement old regime declaration flows.

#### Unlocked State (Not observed — would require admin action)
- Expected: Table of investment declaration categories (80C, 80D, HRA, etc.) with declared amounts and employee-submitted values
- "Approve/Reject" actions would appear per category

### 2. Proof Of Investments (POI)
**URL suffix:** `?resource_type=poi`

#### Locked State (Default)
- Icon + message: "POI submission is locked for this employee"
- Sub-message: "You can either allow the employee to submit POI through the portal or submit the investment proofs on their behalf"
- Button: "Submit Proofs" → navigates to `#/people/employees/{id}/proof-of-investment/new`

## Admin Actions Available

| Action | Behaviour | Notes |
|---|---|---|
| Submit Declaration (on behalf) | Navigates to declaration form with `tax_regime=with_exemptions` | Admin submits old-regime declaration for employee |
| Submit Proofs (on behalf) | Navigates to POI upload form | Admin uploads proof documents for employee |
| Period selector | Changes financial year for review | Historical data access |

## State Machine

| State | Trigger | Who Can Change |
|---|---|---|
| Locked (default) | New employee created | — |
| Unlocked (declaration open) | Admin unlocks; or org opens declaration window | Admin |
| Submitted | Employee submits via portal OR admin submits on behalf | Employee / Admin |
| Approved | Admin approves submitted declaration | Admin |

## Business Rules
1. IT Declaration submission is locked by default — admin must explicitly unlock for employee self-service OR submit on behalf.
2. Declaration is period-scoped (financial year). Each FY has its own declaration record.
3. Zoho's declaration URL uses `tax_regime=with_exemptions` = old regime; `without_exemptions` = new regime.
4. POI submission is separately locked from IT Declaration — they are independent workflows.
5. Admin can always submit on behalf regardless of lock state.
6. For new regime employees: IT Declaration has significantly fewer categories (no 80C, no HRA exemption, no LTA) — only NPS (80CCD(2)) and standard deduction apply.

## Cross-Module Impact
- IT Declaration inputs feed into TDS calculation engine (monthly TDS = (Projected Annual Taxable Income - Declarations) / remaining months).
- POI proofs are verified at year-end before Form 16 generation.
- Form 16 Part B reflects declared and approved investment amounts.
- If no declaration submitted: TDS computed on gross income with only standard deduction (₹75,000 for new regime FY26).

## Key Observations for Our Build
1. **New regime only** — our IT Declaration form must NOT include 80C, 80D, HRA exemption, LTA, etc. Only applicable deductions: NPS employer contribution (80CCD(2)), standard deduction (₹75,000). Under new regime, investment declarations have minimal impact on TDS.
2. **Lock/unlock workflow** — we need a `declaration_locked` boolean per employee per FY. Admin action to toggle.
3. **Admin submit on behalf** — must be a separate flow from employee self-service, with audit log (who submitted, when).
4. **POI and declaration are separate entities** — two different data models, two different workflows.
5. **Period-aware** — declaration records must be keyed by `(employee_id, financial_year)`.
6. **Zoho default is old regime** — their `with_exemptions` flag is a red flag for us. Our declaration form must enforce new regime structure from day one.

## Screenshots
- `screenshots/40-investments-poi-tab.png` — Proof Of Investments sub-tab in locked state
