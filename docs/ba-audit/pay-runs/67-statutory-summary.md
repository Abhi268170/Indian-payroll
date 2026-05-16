# Pay Runs > Overall Insights & Statutory Summary

## URL / Navigation Path

`https://payroll.zoho.in/#/payruns/{id}/summary?selectedTab=insights`

Navigation: Pay Run Summary page > "Overall Insights" tab button

## Purpose

Provides an aggregate analytics view of the completed pay run across four sections:
1. Employee Breakdown — headcount by category
2. Statutory Summary — PF, ESI, PT totals
3. Payment Mode Summary — payment method distribution
4. Component Wise Breakdown — earnings by salary component with per-component drill-down

## Layout

- Heading: "Insights for May 2026 Payrun"
- Horizontal rule separator
- Four data sections (two in a two-column row, then full-width)

## Section 1: Employee Breakdown

### Employee Count Cards

| Metric | Value (May 2026) | Notes |
|--------|-----------------|-------|
| Active Employees | 2 | Total active in this run |
| Paid Employees | 2 | Successfully paid |
| New Joinee's Skipped | 0 | New employees excluded due to incomplete onboarding |
| Skipped Employees | 3 | EMP003, EMP004, EMP005 (onboarding incomplete) |
| Salary Withheld Employees | 0 | Employees in run but payment held |

### Employee Detail Table

| Row | Value (May 2026) |
|-----|-----------------|
| New Joinee's Arrear Released | 0 |
| Salary Released Employees | 0 |
| LOP Reversed Employees | 0 |

**Notes on each metric:**
- **New Joinee's Arrear Released** — count of employees for whom joining-date arrear was paid in this run (Zoho term for manual arrear entry for new joiners)
- **Salary Released Employees** — count of employees whose previously withheld salary was released in this run
- **LOP Reversed Employees** — count of employees for whom LOP was applied in prior run but reversed in this run

## Section 2: Statutory Summary

### Observed State

**"No data to display"** — the Statutory Summary table showed no rows.

**Reason:** PF, ESI, and Professional Tax are not configured in this test org (lerno). The Statutory Summary section only shows data when at least one statutory deduction is configured and non-zero.

### Expected Content (when configured)

| Row | Description |
|-----|-------------|
| EPF Employee Contribution | 12% of PF wage (employee side) |
| EPF Employer Contribution | 12% of PF wage (employer side, EPS + EPF split) |
| EPS Employer Contribution | 8.33% of PF wage (capped at ₹1,250/month) |
| EDLI Employer Contribution | 0.5% of PF wage (capped at ₹75) |
| ESI Employee Contribution | 0.75% of ESI wage |
| ESI Employer Contribution | 3.25% of ESI wage |
| Professional Tax | Per state slab |
| Labour Welfare Fund | Per state |
| TDS (Income Tax) | Aggregate TDS for all employees |

### Statutory Summary Interpretation

When PF is configured (employer has PF registration number in Settings):
- The Payroll Cost > Total Net Pay difference = employer PF + ESI + EDLI contributions
- These appear in Statutory Summary as liabilities to be remitted to authorities
- Statutory Summary drives the Compliance Calendar due dates

## Section 3: Payment Mode Summary

| Payment Mode | Count (May 2026) |
|-------------|-----------------|
| Direct Deposit | 0 |
| Bank Transfer | 2 |
| Cheque | 0 |
| Cash | 0 |

EMP001 and EMP002 were both paid via Manual Bank Transfer.

**Significance:** Payment mode distribution helps finance teams reconcile bank statements. "Direct Deposit" would be used if Zoho's automated payment integration were configured (linked to bank API/payment gateway).

## Section 4: Component Wise Breakdown

### Structure

Two-level hierarchy:
- **Category level**: "Base Earning" (collapsible accordion) | "Deductions" (would appear if deductions > 0)
- **Component level**: Individual salary components under each category

### Observed Data (May 2026)

**Base Earning — ₹87,484.00 (total)**

| Component | Employees Involved | Total Amount |
|-----------|-------------------|--------------|
| Basic | 2 | ₹48,417.00 |
| House Rent Allowance | 1 | ₹14,967.00 |
| Fixed Allowance | 2 | ₹24,100.00 |
| **Total Earnings** | — | **₹87,484.00** |

**Notes:**
- HRA shows 1 employee: only EMP001 has HRA in salary structure; EMP002's structure has no HRA component
- Basic: ₹48,417 = EMP001 prorated Basic ₹37,417 + EMP002 full Basic ₹11,000 (EMP002's total ₹22,000 contains Basic ₹11,000 + Fixed ₹11,000 — inferred)

### Component Links

Each component name in the breakdown table is a **clickable link** navigating to a component-level insights drill-down:

```
/url: "#/payruns/insights/{runId}/earnings/{componentId}?override_type="
```

Example:
- Basic: `#/payruns/insights/3848927000000034159/earnings/3848927000000032463?override_type=`
- HRA: `#/payruns/insights/3848927000000034159/earnings/3848927000000032465?override_type=`
- Fixed Allowance: `#/payruns/insights/3848927000000034159/earnings/3848927000000032469?override_type=`

These drill-down pages show per-employee breakdown for that component. Not explored in this session.

## Key Observations for Our Build

1. **Statutory Summary empty state is misleading** — "No data to display" with no explanation that it requires PF/ESI configuration. Our build should show: "No statutory deductions configured. Configure PF, ESI, and PT in Settings to see statutory summary." This is better UX.
2. **Statutory Summary is the compliance dashboard** — this section, when populated, tells admins exactly how much PF/ESI/PT to remit and by when. Our Compliance Calendar module should pull from these computed totals.
3. **Component drill-down is high-value** — clicking a component name to see per-employee breakdown is powerful for auditing (e.g., "why is total Basic lower than expected?"). Implement this drill-down in our build. URL pattern: `GET /api/payroll-runs/{runId}/insights/component/{componentId}`.
4. **HRA shows 1 employee** — different salary structures for different employees result in different component cardinality in this view. Our aggregation query must group by component across all employees in the run, counting only those where the component is present and non-zero.
5. **LOP Reversed Employees metric** — indicates Zoho tracks LOP reversals (admin corrects prior-month LOP). Our build needs a `LopReversal` table or `PayrollRunEmployee.LopReversed: bool` flag to support this metric.
6. **New Joinee's Arrear metric** — tracks employees for whom arrear was manually entered for their first month. Our build: when a mid-month joiner receives first payslip in a subsequent run with arrear for the joining month, flag `IsNewJoineeArrear = true` on that `PayrollRunEmployee` record.
7. **Payment mode distribution** — this metric feeds into bank reconciliation. Our Reports module should expose a "Payment Mode Summary" report downloadable as CSV. Link to the bank's transaction portal where possible.
8. **Payroll Cost = Total Net Pay in this org** — only true because employer PF/ESI = ₹0. When PF is configured: Payroll Cost = Total Net Pay + Total Employer Contributions. Store `EmployerPfContribution`, `EmployerEsiContribution`, `EdliContribution` as separate fields on `PayrollRun` entity.

## Open Questions

- [ ] What does the Statutory Summary show exactly when PF/ESI are configured? (Test with a fully configured org)
- [ ] Does the component drill-down (`#/payruns/insights/...`) page show override/manual override flags?
- [ ] What is "Resettlement Payroll" type — is it for arrears? Does it appear in Statutory Summary differently?
- [ ] Does "New Joinee's Arrear Released" auto-compute or does admin manually mark it?

## Screenshots

- `screenshots/67-overall-insights-statutory.png` — Full Overall Insights tab (employee breakdown, statutory summary empty, payment mode, component breakdown)
- `screenshots/55-overall-insights-tab.png` — Overall Insights tab (Draft state snapshot)

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
