# UF-37: Verify PF Calculations (May 2026 Pay Run)

**Module:** Pay Runs > Summary > Taxes & Deductions / Settings > Statutory Components > EPF
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 Regular Pay Run (ID: 3848927000000034159), Arjun Mehta (EMP001), Priya Sharma (EMP002)
**App State Before:** Pay Run status = PAID

## Steps Executed
1. Navigate to `#/payruns/3848927000000034159/summary`
2. Open Taxes & Deductions tab — observe ₹0 Benefits row
3. Open each employee detail panel — observe ₹0 PF line items
4. Navigate to `#/settings/statutory-details/list` (EPF tab) to verify configuration
5. Cross-reference employee statutory settings (EPF Disabled per UF-19 findings)

## EPF Configuration (Settings > Statutory Components > EPF)

| Setting | Value |
|---------|-------|
| EPF Number | KA/KAR/1234567/001 |
| Deduction Cycle | Monthly |
| Employee Contribution Rate | 12% of Actual PF Wage |
| Employer Contribution Rate | 12% of Actual PF Wage |
| Allow Employee Level Override | No |
| Pro-rate Restricted PF Wage | No |
| Consider applicable salary components based on LOP | Yes (when PF wage < ₹15,000) |
| Eligible for ABRY Scheme | No |

## Employer Contribution Preferences (Included in Salary Structure)
- Employer's PF contribution: Included in CTC (checked)
- EDLI contribution: Included in CTC (checked)
- Admin charges: Included in CTC (checked)

## EPF Sample Calculation (Shown in Settings)
Assumes PF Wage = ₹20,000:

**Employee Contribution:**
| Line | Amount |
|------|--------|
| EPF (12% of 20,000) | ₹2,400 |

**Employer Contribution:**
| Line | Calculation | Amount |
|------|-------------|--------|
| EPS (8.33% of 20,000, max ₹15,000 wage) | 8.33% × ₹15,000 | ₹1,250 |
| EPF (12% of 20,000 − EPS) | ₹2,400 − ₹1,250 | ₹1,150 |
| Total Employer | | ₹2,400 |

## PF Calculations in May 2026 Pay Run

**Arjun Mehta (EMP001):**
- EPF Status: DISABLED (toggled off on employee statutory settings)
- EE PF Deduction: ₹0.00
- ER PF Contribution: ₹0.00
- EPS: ₹0.00
- EDLI: ₹0.00 (per Taxes & Deductions tab → Benefits: "No deductions present")

**Priya Sharma (EMP002):**
- EPF Status: DISABLED
- EE PF Deduction: ₹0.00
- ER PF Contribution: ₹0.00

**Vikram Nair, Aisha Khan, Rahul Desai:** SKIPPED — excluded from pay run entirely.

## Expected PF If Enabled (Arjun, Pre-Revision ₹70,000/month)

If EPF were enabled:
- PF Wage = Basic = ₹39,998 (57.14% of ₹70,000). PF wage > ₹15,000 statutory cap.
- Actual PF Wage used = ₹39,998 (config says "Actual PF Wage", not restricted to ₹15,000 cap)
- EE EPF = 12% × ₹39,998 = ₹4,799.76 ≈ ₹4,800
- ER EPS = 8.33% × ₹15,000 = ₹1,250 (EPS always capped at ₹15,000 wage)
- ER EPF = 12% × ₹39,998 − ₹1,250 = ₹4,800 − ₹1,250 = ₹3,550
- Total ER PF = ₹3,550 + ₹1,250 = ₹4,800
- EDLI = 0.5% × min(₹39,998, ₹15,000) = 0.5% × ₹15,000 = ₹75
- Admin charges = 0.5% × ₹39,998 = ₹200 (or ₹75 minimum — EPFO norms)
- Monthly cost impact: ₹4,800 (EE deduction from net) + ₹4,800 (ER contribution in CTC)

## Why EPF Is Disabled

From UF-19: All 5 employees have EPF toggled off via the employee-level statutory information toggle at:
`#/people/employees/{id}/edit-statutory-details` → EPF section → Enable toggle = OFF

The EPF toggle appears to be a binary enabled/disabled with no reason field required — a statutory audit risk noted in UF-19.

## Statutory Compliance Notes

- EPF Number format: `KA/KAR/1234567/001` — this is a Karnataka establishment code, even though the work location is Kerala (kazhakoottam, thiruvananthapuram). This may indicate the company is registered in Karnataka for EPF even though employees work in Kerala.
- "Pro-rate Restricted PF Wage" = No: This means if an employee's PF wage is restricted to ₹15,000 ceiling, it is NOT pro-rated for partial months.
- LOP consideration: "Yes (when PF wage < ₹15,000)" — for wage-ceiling employees only, LOP reduces PF wage proportionally.
- ABRY Scheme not enabled (relevant only for new employees post-2020 earning < ₹15,000).

## Gaps / Observations
- 🔴 No EPF reason field when disabling — cannot distinguish genuine exemption (e.g., voluntarily covered employee with salary > ₹15,000 who opted out) from oversight
- EPF Number shows Karnataka code (KA/KAR) for a Kerala-based workforce — potential registration mismatch
- EDLI contribution is included in CTC (employee bears cost) — not typical; usually EDLI is a pure employer cost outside CTC
- "Contribution Preferences: Included in Salary Structure" suggests all PF costs (EE + ER + EDLI + Admin) are embedded in CTC — confirming Payroll Cost = Net Pay in pay run
- No ECR file generated for May run (all ₹0 contributions)
