# Employees > Prior Employer YTD (Previous Employment Details)

## Overview
Prior employer YTD (Year-to-Date) data is required for mid-financial-year joiners to correctly compute TDS for the current employer. The current employer must account for income and TDS already deducted by the prior employer within the same financial year.

## Where It Lives in Zoho

### Primary Entry Point
IT Declaration form at:
`#/people/employees/{id}/investment-declaration/new?tax_regime=with_exemptions`

Under "Previous Employment Details" section of the IT Declaration. Accessible via:
- Employee Profile → Investments tab → IT Declaration → Submit Declaration (on behalf)
- Import Data modal → "Previous Employment Details" import type

### Import Entry Point
The Import Data modal on the Employee List page provides a "Previous Employment Details" import type (under "Investments" group). This allows bulk CSV upload of prior employer data for all mid-year joiners.

## Page Investigation Status
**IT Declaration page investigation was deferred** per user instruction during EMP003 audit. The page failed to load (hung at "Loading...") due to incomplete employee profile (DOB not saved correctly during EMP003 creation). Full investigation of the IT Declaration form is therefore deferred.

## Expected Data Fields (from Zoho Pattern + Indian TDS Requirements)

| Field | Type | Required | Notes |
|---|---|---|---|
| Employer Name | Text | Yes | Previous company name |
| Period From | Date | Yes | Start of prior employment in current FY (e.g., 01/04/2024) |
| Period To | Date | Yes | Last day at prior employer in current FY |
| Gross Salary | Decimal | Yes | Total gross salary paid by prior employer in this FY |
| Tax Exempt Allowances | Decimal | No | HRA and other exemptions claimed with prior employer (only for old regime) |
| Standard Deduction Claimed | Decimal | No | ₹75,000 (new regime) or ₹50,000 (old regime) — only one employer can claim this |
| Professional Tax Paid | Decimal | No | PT deducted by prior employer during the period |
| TDS Deducted (u/s 192) | Decimal | Yes | Tax deducted at source by prior employer on salary income |
| Other Income (Interest) | Decimal | No | Interest income declared by employee |

## TDS Computation with Prior Employer YTD

### Formula
```
Projected Annual Income = Prior Employer Gross YTD + (Current Employer Monthly × Remaining Months)
Projected Annual Tax = Apply new regime tax slabs to Projected Annual Income
TDS Already Deducted = Prior Employer TDS + Current Employer TDS deducted so far
Remaining TDS = Projected Annual Tax - TDS Already Deducted
Monthly TDS (remaining months) = Remaining TDS / Remaining Months
```

### Key Invariants
1. Standard Deduction (₹75,000 new regime) is claimed ONCE across both employers. If prior employer claimed it, current employer cannot claim it again. This is managed via the "Standard Deduction Claimed" field in prior employer YTD.
2. Multiple prior employers are possible in a single FY (employee changed jobs twice). Each is a separate record.
3. Prior employer TDS is self-declared by employee — not verified by current employer. Discrepancies reconcile via Form 26AS at year-end.
4. If no prior employer YTD entered: TDS computed as if employee earned full-year with current employer (overpayment scenario for mid-year joiners — they will get refund from IT Dept).

## Import File: "Previous Employment Details"

Selecting this option in the Import Data modal would provide a CSV template. Expected columns (based on standard Zoho import patterns):
- Employee ID or Work Email
- Employer Name
- Period From (dd/MM/yyyy)
- Period To (dd/MM/yyyy)
- Gross Salary
- Standard Deduction Claimed
- Professional Tax Paid
- TDS Deducted
- Income From Other Sources

## Business Rules

1. **Employee-declared data** — current employer does not verify; bears no liability if employee provides incorrect YTD.
2. **Period must be within current FY** — system should validate that Period From >= FY start and Period To <= FY end.
3. **Standard deduction claimed once per FY** — if prior employer claimed ₹75,000, set `standard_deduction_claimed_by_prior = true`. Current employer deduction = ₹0.
4. **TDS recalculation trigger** — saving prior employer YTD triggers TDS recomputation for all remaining months.
5. **Multiple records per employee per FY** — if employee had 2 prior employers, 2 records can be entered.
6. **Retroactive adjustment** — if months have already been processed (payslips generated) before prior employer data is entered, those months' TDS is not retroactively adjusted. Only future months are updated. Year-end Form 16 shows cumulative correct figure.

## Our Build Requirements

1. **Entity: `PriorEmployerYtd`**
   ```
   id: UUID
   employee_id: FK(Employee)
   financial_year: String (e.g., "2025-26")
   employer_name: String
   period_from: Date
   period_to: Date
   gross_salary: Decimal
   standard_deduction_claimed: Decimal (0 or 75000 for new regime)
   professional_tax_paid: Decimal
   tds_deducted: Decimal
   other_income: Decimal
   entered_by: FK(AdminUser)
   entered_at: Timestamp
   ```

2. **Engine input** — `PriorEmployerYtd` records for current FY passed into TDS engine as part of monthly payroll computation context.

3. **Audit trail** — who entered the data and when. Employee can view their own prior employer YTD in self-service portal (read-only).

4. **Validation** — period must not overlap with current employer's employment period. Period must be within current FY.

5. **Import support** — bulk CSV import for prior employer YTD is a V1 requirement (many mid-year joiners in onboarding cycle).

6. **New regime only** — `tax_exempt_allowances` field not needed for new regime. `standard_deduction_claimed` is either 0 or ₹75,000.

## Open Questions (Deferred Investigation)
- [ ] Full IT Declaration form UI — what exactly does it look like? (Deferred per user instruction due to page load failure)
- [ ] Does Zoho show a summary of prior employer data in the employee profile Overview tab?
- [ ] Can prior employer YTD be edited after entry? Is there a version history?
- [ ] Does Zoho validate period overlaps between multiple prior employer records?
