# Employees > EMP003 — Mid-Year Joiner with Prior Employer YTD (Vikram Nair)

## Employee Spec
- **Name:** Vikram Nair
- **Employee ID:** EMP003
- **Designation:** Engineering Manager
- **Department:** Engineering
- **Work Location:** Head Office (Mumbai HQ) — Bangalore not configured in org
- **Date of Joining:** 01/01/2025 (mid financial year — FY2025-26)
- **Gender:** Male
- **DOB:** 04/11/1985 (Age: 39)
- **PAN:** Not specified in spec (not entered)
- **Bank:** ICICI Bank, A/C 628305001234, IFSC ICIC0001234, Savings
- **Gross CTC:** ₹18,00,000/year (₹1,50,000/month)
- **Zoho Employee ID (internal):** 3848927000000034014

## Mid-Year Join: Key Observations

### Date of Joining = 01/01/2025
FY2025-26 starts 01/04/2024 and ends 31/03/2025. EMP003 joins on 01/01/2025:
- 9 months remain in FY (Jan–Mar 2025 = 3 months within this FY portion)
- Prior employer would have employed Apr–Dec 2024 (9 months)
- For TDS purposes: prior employer's YTD earnings, TDS deducted must be collected and fed into TDS engine
- Zoho handles this via the "Prior Employer Details" section in statutory/tax declaration

### Work Location Gap
Spec specified "Bangalore" as work location. Org only has "Head Office" (Mumbai) configured. No Bangalore location set up in Settings > Work Locations. EMP003 created with Head Office. Impact: PT slab applied = Maharashtra (Maharashtra PT), not Karnataka (₹200/month Karnataka PT). This is a configuration gap in the audit org — not a Zoho limitation.

### High Salary: PF Applicability
CTC = ₹1,50,000/month. PF is not configured at org level in lerno org, so PF deduction does not appear. If PF were enabled:
- PF wage = Basic salary. At ₹1,50,000 CTC, Basic (50%) = ₹75,000.
- PF statutory ceiling: employee deduction capped at 12% of ₹15,000 = ₹1,800/month (unless employee opts for higher PF on actual basic).
- Employer contribution: 12% of ₹15,000 = ₹1,800/month.

## Salary Structure (As Created)
- Annual CTC: ₹18,00,000
- Basic: 50% of CTC = ₹75,000/month (₹9,00,000/year)
- Fixed Allowance: ₹75,000/month residual (₹9,00,000/year)
- No HRA added (not added during creation — same as EMP002 pattern)
- Note: For high-CTC employees, HRA exemption is less tax-relevant under new regime (no HRA exemption under new regime)

## Prior Employer YTD — Zoho Behaviour

### Where to Enter Prior Employer YTD
In Zoho Payroll, prior employer YTD is entered in the IT Declaration form under a section called "Previous Employment Details" or via the "Prior Employer" tab in Investments. This was attempted during audit but the IT Declaration page hung on load for EMP003 (reason: DOB was not saved correctly due to calendar interaction issue during creation). Investigation deferred per user instruction.

### Expected Prior Employer Fields (from Zoho documentation pattern)
| Field | Type | Purpose |
|---|---|---|
| Employer Name | Text | Previous company name |
| Period From | Date | Start of prior employment in this FY |
| Period To | Date | End of prior employment in this FY |
| Gross Salary | Number | Total gross paid by prior employer this FY |
| Tax Exempt Allowances | Number | HRA and other exemptions claimed with prior employer |
| Standard Deduction | Number | ₹75,000 for new regime (or ₹50,000 old) — allocated |
| Professional Tax Paid | Number | PT deducted by prior employer |
| TDS Deducted | Number | Tax at source deducted by prior employer |
| Income from Other Sources | Number | Interest, capital gains, etc. declared by employee |

### TDS Calculation Impact
When prior employer YTD is entered:
- Engine projects annual income as: (Prior employer gross YTD) + (Current employer monthly × remaining months)
- TDS already deducted by prior employer is credited
- Remaining TDS for current employer = (Total projected tax) - (Prior employer TDS)
- Monthly TDS = Remaining TDS / remaining months in FY

### Key Invariant for Our Build
Prior employer YTD must be keyed by `(employee_id, financial_year)`. Multiple prior employer records possible if employee changed jobs twice in same FY (rare but must be handled). Data is employee-declared — not verified by employer. Form 16 Part A reflects only current employer TDS. Form 16 Part B can include prior employer income summary.

## IFSC Validation — ICICI
IFSC `ICIC0001234` — this is a mock IFSC. Zoho lookup likely failed (same as EMP002 SBI mock IFSC). Bank Name manually entered as "ICICI Bank". Pattern consistent with EMP002: mock IFSC codes not in Zoho's lookup DB.

## Business Rules Observed
1. **Mid-FY join does not block creation** — DOJ can be any date within the financial year.
2. **Work location defaults to configured locations** — if required location not set up in org, admin must use nearest alternative or set up the location first.
3. **Prior employer YTD is employee-declared** — not automatically imported; admin enters on behalf or employee enters via portal.
4. **TDS recomputed each month** — any new prior employer YTD entry triggers TDS recalculation for all remaining months.
5. **High CTC employees**: Fixed Allowance becomes large when only Basic is percentage-based with no HRA — residual absorbs all remaining CTC.

## Key Observations for Our Build
1. **Prior employer data model** — entity `PriorEmployerYtd` with fields: `employee_id`, `financial_year`, `employer_name`, `period_from`, `period_to`, `gross_salary`, `exempt_allowances`, `standard_deduction_claimed`, `professional_tax_paid`, `tds_deducted`. Multiple records per (employee, FY) must be supported.
2. **Work Location prerequisite** — onboarding flow must validate that employee's work location is configured. If not, graceful error: "Work Location 'Bangalore' not found. Please configure it in Settings > Work Locations before assigning."
3. **TDS recalculation trigger** — saving prior employer YTD must enqueue a TDS recomputation job for all future months in the FY.
4. **IT Declaration form must load reliably** — the hang experienced (likely due to incomplete employee data) is a UX risk. Our form must load with empty state if no declaration exists, not hang.
5. **Maharashtra PT applied by default** when no Bangalore location exists — location-PT mapping must be explicit, not defaulted silently.

## Salary Structure vs. Component Mapping
EMP003 illustrates the case where only Basic + Fixed Allowance exist (no HRA). Salary structure is minimal for high-CTC employees where HRA exemption is tax-irrelevant (new regime). Our engine must handle any combination of components as long as sum = CTC.

## Screenshots
- None captured for EMP003 due to page load issues with IT Declaration tab.

## Open Questions
- [ ] Can an employee be assigned to a work location that is not yet configured (create on-the-fly during onboarding)?
- [ ] What happens if prior employer YTD is entered after some months of payroll have already been processed — does the engine retroactively adjust past TDS or only future?
- [ ] Does Zoho allow multiple prior employer records per employee per FY?
