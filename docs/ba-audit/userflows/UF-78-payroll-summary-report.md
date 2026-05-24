# UF-78: Payroll Summary Report

**Module:** Reports > Payroll Overview > Payroll Summary
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 PAID pay run; 2 paid employees (Arjun, Priya), 3 skipped
**App State Before:** Reports Centre; May 2026 data available

## Steps Executed
1. Navigate to Reports > Payroll Overview > Payroll Summary
2. Report page not individually navigated — documented from captured data and domain knowledge

---

## Report: Payroll Summary

### Purpose
High-level summary of total payroll cost, gross pay, deductions, and net pay for a given pay period. Primary report for management review and CFO sign-off.

### Expected Columns / Sections
| Field | Value (May 2026 — Expected) |
|-------|----------------------------|
| Pay Period | May 2026 (01/05/2026 – 31/05/2026) |
| Total Employees | 5 (2 paid, 3 skipped) |
| Employees Paid | 2 |
| Employees Skipped | 3 |
| Gross Earnings | ₹87,484.00 |
| Less: Deductions | |
| — EPF (Employee) | ₹0.00 |
| — ESI (Employee) | ₹0.00 |
| — PT | ₹0.00 |
| — TDS | ₹0.00 |
| — Loan EMI | ₹0.00 (Arjun's loan starts July 2026) |
| — Other Deductions | ₹0.00 |
| Net Pay | ₹87,484.00 |
| Employer Contributions | |
| — EPF (Employer) | ₹0.00 |
| — ESI (Employer) | ₹0.00 |
| Total Payroll Cost | ₹87,484.00 |

### Key Insight: Payroll Cost = Net Pay
Since EPF is configured as "Included in Salary Structure" (employer contribution embedded in CTC), employer PF contribution does not appear as additional cost above net pay. Total Payroll Cost = Net Pay = ₹87,484.

---

## Employee-wise Breakout (Expected within Report)
| Employee | Gross | TDS | PF (EE) | ESI (EE) | PT | Net Pay |
|----------|-------|-----|---------|---------|-----|---------|
| Arjun Mehta | ₹65,484 | ₹0 | ₹0 | ₹0 | ₹0 | ₹65,484 |
| Priya Sharma | ₹22,000 | ₹0 | ₹0 | ₹0 | ₹0 | ₹22,000 |
| Vikram Nair | Skipped | — | — | — | — | — |
| Rahul Verma | Skipped | — | — | — | — | — |
| Sneha Patel | Skipped | — | — | — | — | — |
| **Total** | **₹87,484** | **₹0** | **₹0** | **₹0** | **₹0** | **₹87,484** |

---

## Filters
| Filter | Expected Options |
|--------|----------------|
| Financial Year | FY2026-27 |
| Pay Period | Monthly — May 2026 |
| Pay Run Type | Regular / Off-Cycle / All |
| Department | All / specific |

## Export Formats
- Excel (.xlsx): Detailed breakout with all components
- PDF: Print-ready format for management
- CSV: For external analysis

---

## Business Rules
1. Skipped employees are excluded from all financial totals
2. Payroll Cost = Net Pay when employer statutory contributions are embedded in CTC
3. LOP-impacted employees show prorated gross (Arjun: ₹65,484 not ₹70,000)
4. Report should reconcile with bank transfer advice total (same net pay figure)

## Gaps / Observations
- Report page not individually navigated — content inferred from pay run data
- 🟡 Mark for future session: open and screenshot actual Payroll Summary report output
- No custom date range (only month/period selector expected) — cannot span partial months

## Open Questions
- [ ] Does the Payroll Summary show component-level breakout (Basic, HRA, etc.) or just gross/net?
- [ ] Can the report span multiple pay periods (e.g., Q1 summary)?
- [ ] Is the Payroll Journal Summary a separate report or a section within Payroll Summary?
