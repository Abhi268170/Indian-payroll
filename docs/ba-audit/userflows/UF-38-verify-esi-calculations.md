# UF-38: Verify ESI Calculations (May 2026 Pay Run)

**Module:** Pay Runs > Summary > Taxes & Deductions / Settings > Statutory Components > ESI
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 Regular Pay Run, all 5 employees
**App State Before:** Pay Run status = PAID

## Steps Executed
1. Navigate to `#/settings/statutory-details/list/esi`
2. Capture ESI org-level configuration
3. Return to pay run summary — observe Benefits section = "No deductions present"
4. Check employee-level ESI status (from prior UF-19 investigation)

## ESI Configuration (Settings > Statutory Components > ESI)

| Setting | Value |
|---------|-------|
| ESI Number | 52-00-123456-000-0001 |
| Deduction Cycle | Monthly |
| Employee Contribution Rate | 0.75% of Gross Pay |
| Employer Contribution Rate | 3.25% of Gross Pay |

Note: ESI "Disable ESI" button is present at org level — ESI is currently ENABLED at org level. The employee-level toggle overrides this.

## ESI Eligibility Rule (Statutory)
Employees with Gross Pay ≤ ₹21,000/month are ESI-eligible (per ESIC notification, wage ceiling as of 2017).
Employees above ₹21,000/month gross are excluded from ESI by statute.

## ESI Calculations in May 2026 Pay Run

**Arjun Mehta (EMP001) — ₹65,484 gross (prorated) / ₹70,000 full month:**
- Statutory eligibility: Gross ₹70,000 > ₹21,000 → ESI NOT applicable
- ESI Status on profile: DISABLED
- EE ESI: ₹0.00
- ER ESI: ₹0.00
- Correct behavior ✓

**Priya Sharma (EMP002) — ₹22,000 gross:**
- Statutory eligibility: Gross ₹22,000 > ₹21,000 → ESI NOT applicable (just above ceiling)
- ESI Status on profile: DISABLED
- EE ESI: ₹0.00
- ER ESI: ₹0.00

Note: Priya's salary is ₹22,000/month which is slightly above the ₹21,000 ESI ceiling. The system correctly shows ₹0 ESI. However the toggle is also set to DISABLED on the employee profile — the system could be relying on the toggle rather than auto-computing eligibility.

**Vikram, Aisha, Rahul:** SKIPPED — not in pay run.

## Expected ESI If Applicable (Hypothetical Employee at ₹18,000)

For an employee with ₹18,000 gross:
- EE ESI = 0.75% × ₹18,000 = ₹135/month
- ER ESI = 3.25% × ₹18,000 = ₹585/month
- Total ESI cost = ₹720/month

## ESI Number Format Analysis
`52-00-123456-000-0001`:
- 52 = Region code (Kerala)
- Format aligns with ESIC employer code structure
- Correctly corresponds to Kerala location (unlike EPF number which has Karnataka prefix)

## Statutory Compliance Notes

- ESIC wage ceiling: ₹21,000/month since January 2017. No employees in this org fall below this ceiling currently.
- Once contribution period begins (joining month), ESI continues for full contribution period (April-September or October-March) even if salary exceeds ceiling mid-period.
- ESI is deducted on gross pay (Basic + HRA + all allowances), not just basic.
- Employer contribution (3.25%) is higher than employee (0.75%) — combined rate 4%.
- ESIC has a reduced rate period — 0% employee contribution for first 2 years of new ESIC implementation in a new area.

## Gaps / Observations
- 🔴 Same as EPF: no ESI exemption reason field when disabling
- The system does not appear to auto-enforce the ₹21,000 ceiling — it relies on the admin toggling ESI off for each employee. If an admin forgets to disable ESI for a salary-crossing employee, over-deduction would occur.
- No ESIC challan visible in this pay run (confirmed: all ₹0)
- No visible indicator in pay run summary of which employees are ESI-eligible vs ineligible vs disabled
