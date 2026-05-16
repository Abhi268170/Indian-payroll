# UF-43: Variable Pay Inputs in Pay Run

**Module:** Pay Runs > [Month] Pay Run > Variable Inputs
**Tested:** 2026-05-16
**Mock Data Used:** N/A — DATE-GATED (requires June 2026 pay run, available from 01/06/2026)
**App State Before:** May 2026 pay run PAID; June 2026 pay run not yet auto-created

---

## DATE-GATED FLOW

This flow requires the June 2026 regular pay run to exist. Zoho Payroll auto-creates the monthly pay run at the start of each month. June 2026 pay run will be available from 01/06/2026 onward.

**Resume this flow on or after: 2026-06-01**

---

## Variable Pay Overview

Variable pay components in Zoho Payroll are salary components configured as "Variable" type — their amounts are not fixed in the salary structure but entered per pay run per employee.

### Variable Components Configured (from UF-86)
| Component | Type | Notes |
|-----------|------|-------|
| Gratuity | Variable | Flat Amount; EPF=No; ESI=No |
| Bonus | Variable | Flat Amount; EPF=No; ESI=No |
| Commission | Variable | Flat Amount; EPF=No; ESI=Yes |
| Leave Encashment | Variable | Flat Amount; EPF=No; ESI=No |
| Notice Pay | Variable | Flat Amount; EPF=No; ESI=No |
| Hold Salary | Variable | Flat Amount; EPF=No; ESI=No |
| Overtime Allowance | Variable; Inactive | Flat Amount; EPF=No; ESI=Yes |

---

## Expected Variable Inputs Flow (When Pay Run Active)

### Entry Point
Pay Runs > June 2026 Pay Run > [Employee Row] > Edit Variable Inputs
OR: Pay Runs > June 2026 Pay Run > "Variable Inputs" tab or button

### Steps
1. Open active pay run (June 2026)
2. Navigate to Variable Inputs section
3. Select employee (e.g., Arjun Mehta)
4. Enter amounts for applicable variable components:
   - Bonus: ₹X
   - Commission: ₹X
   - Leave Encashment: ₹X (if applicable)
5. Save
6. System recalculates gross, TDS, net pay

### Fields Expected
| Field | Type | Notes |
|-------|------|-------|
| Employee | Display (pre-selected) | |
| Component Name | Display | Variable component |
| Amount (₹) | Currency input | Per-component amount for this pay run |
| Remarks | Text | Optional note |

---

## Variable Pay and TDS

Variable pay is included in gross salary for TDS computation:
- Bonus, Commission, Leave Encashment → Taxable (added to salary income)
- Gratuity → Exempt up to ₹20,00,000 (see UF-35)
- Leave Encashment → Exempt up to ₹25,00,000 for government employees; taxable for private

**TDS recalculation after variable input:**
- If bonus ₹50,000 added to June 2026 pay run:
  - Annual bonus annualized or added to YTD income
  - Remaining TDS months recalculate to spread the additional liability

---

## Variable Pay and EPF/ESI

Refer to UF-86 for component-level EPF/ESI inclusion flags:
- Bonus: EPF=No, ESI=No → Does NOT affect EPF wage or ESI wage
- Commission: EPF=No, ESI=Yes → Included in ESI wage (if employee ESI-eligible)
- Overtime: EPF=No, ESI=Yes → Inactive in demo org

---

## Business Rules
1. Variable components must be manually entered each pay run — not auto-carried forward
2. If variable amount not entered, system assumes ₹0 for that component
3. TDS adjusts dynamically when variable pay is added
4. Variable components excluded from CTC calculation (they are over-and-above fixed structure)
5. Bonus under Payment of Bonus Act: Minimum 8.33% of annual wages (₹7,000/month minimum)

## Gaps / Observations
- Date-gated — not navigated
- Variable Inputs entry point in pay run (tab vs button vs row-level) not confirmed
- Bulk upload of variable inputs (Excel import) not confirmed — likely exists for large orgs

## Open Questions
- [ ] Is there a bulk import option for variable inputs (e.g., Excel upload for 100+ employees)?
- [ ] Can variable inputs be entered before the pay run is drafted (advance entry)?
- [ ] Is there a deadline within the month to enter variable inputs before payroll processing?
- [ ] Does the system allow negative variable inputs (e.g., notice pay deduction)?
