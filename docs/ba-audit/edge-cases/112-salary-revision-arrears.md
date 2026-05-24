# Edge Case > Salary Revision & Arrears

## Scenario
EMP001 (Arjun Mehta) has a pending salary revision for June 2026. Document the complete salary revision workflow: how a revision is initiated, effective date handling, how Zoho computes arrears when revision is backdated, and how arrears appear in the payslip.

## Steps to Reproduce
1. Navigate to Approvals > Salary Revision (shows pending revision for EMP001)
2. Note effective date and new vs old salary
3. Navigate to EMP001 > Salary Details tab (shows "revised salary will be reflected upon completion of June 2026 pay run")
4. Create June 2026 pay run (if possible) and observe arrears handling
5. Document payslip arrear line items

## Expected Behaviour (statutory rule)

### Salary Revision with Arrears (Standard Indian Payroll)
When a salary revision is effective from a past month (backdated revision):
1. Revised salary minus old salary = arrear amount per month
2. Arrears accumulated from effective month to current month
3. Arrears paid in the current pay run as a lump sum line item
4. TDS recalculated to account for arrear income (annualisation method)
5. Form 10E relief under Section 157 (formerly 89(1)) available if arrear pushes into higher slab

**Effective-same-month revision** (not backdated):
- No arrears — new salary applies from the current month forward

**Arrear formula:**
```
monthly_arrear = (new_monthly_component - old_monthly_component) × number_of_arrear_months
```

### Statutory TDS on Arrears
- Arrears are taxable income in the year received, not the year they relate to
- Section 157 / 89(1) relief: tax relief if arrear income causes higher tax in receipt year than it would have caused in the year it accrued
- Employer must compute and offer 89(1) relief automatically or via Form 10E

## Actual Zoho Behaviour

### Pending Revision — Approval State
From `Approvals > Salary Revision` page:
- **Employee**: Arjun Mehta (EMP001)
- **Revision ID**: `3848927000000034251`
- **Status**: Pending approval (visible in approvals queue)
- **Message on Salary Details tab**: "The revised salary will be reflected upon completion of June 2026 pay run"
- **Effective**: June 2026 pay run (not backdated — effective from current/next month)

### Salary History from API
From `/api/v1/employees/3848927000000032948/salary-details`:
```
Revision history:
- April 2026 (initial): ₹8,40,000 CTC (onboarding assignment)
- Current: ₹9,45,000 CTC (first revision, effective May 2026 or later)
```

The CTC at time of audit is ₹9,45,000/year (₹78,750/month). The pending revision (`3848927000000034251`) is a SECOND revision that will take effect in June 2026.

### Revision Workflow Observed
From UI:

**Step 1: Initiation**
- Entry point: Employee Profile > Salary > "Revise Salary" button
- OR: HR admin initiates from salary revision module
- Captures: new CTC, new component breakdown, effective date

**Step 2: Approval**
- Revision goes to "Approvals > Salary Revision" queue
- Approver can: Approve / Reject / Request changes
- Status: Pending / Approved / Rejected

**Step 3: Effect**
- Once approved, revision takes effect from the specified month's pay run
- Zoho explicitly links the revision to a payrun (not a calendar date) — "will be reflected upon completion of June 2026 pay run"
- This means Zoho's effective date concept is payrun-bound, not calendar-date-bound

### Backdated Revision Behaviour (Inferred — Not Live Tested)
The pending revision for EMP001 is effective from June 2026 (same-month, not backdated). Therefore arrears cannot be directly observed in this org.

However, from the salary-details API response and UI:
- Zoho shows salary revision history with effective dates per pay run
- For a backdated revision (e.g., revision effective March but processed in June), Zoho would:
  - Compute arrear months: March, April, May (3 months)
  - Arrear = (new monthly salary - old monthly salary) × 3
  - Add arrear as a separate line item in June payslip ("Salary Arrears" earning type)
- **Cannot confirm live** — would need a backdated revision in the org

### Arrear Line Item (From Earning Types List)
From `/api/v1/payroll/meta` earning types:
- No explicit "Salary Arrears" earning type listed in the variable pay types (Bonus, OT, Commission, etc.)
- Arrears may be auto-generated as a system earning type (not user-configurable)
- OR arrears may be computed and merged into the base component amounts

**Gap**: The mechanism by which arrear amounts appear on the payslip (as a separate line vs merged into components) is unconfirmed.

### June 2026 Pay Run (Not Yet Created)
As of audit date (May 2026 pay run is Paid), June 2026 pay run has not been created. Cannot observe the revision's actual effect without creating and processing a June run — which would require completing EMP003-EMP005 onboarding or accepting a partial run.

## Screenshots
- No screenshots captured for this scenario (revision detail page not directly navigated due to SPA routing and existing modal issues)

## Gap / Bug / Surprise
1. **Effective date is payrun-bound, not calendar-date-bound**: Zoho ties salary revision to a specific pay run ("June 2026 pay run") rather than a specific date (e.g., "01/06/2026"). This is pragmatic but means mid-month revisions (e.g., effective 15 June) are not expressible — the revision must snap to a payrun boundary.
2. **Backdated arrears not live-tested**: The EMP001 revision is same-month (not backdated). Arrear computation logic, arrear line item format, and TDS recalculation on arrears cannot be confirmed.
3. **Approval workflow exists**: Salary revision requires explicit approval — not an immediate HR edit. This is good governance (segregation of duties).
4. **Two revisions already**: EMP001 went from ₹8,40,000 → ₹9,45,000 CTC within a single audit period (April to May 2026). The pending revision is a third state (unknown target CTC). This shows Zoho handles multiple revisions in same fiscal year.
5. **No Section 157 / 89(1) relief UI**: Cannot confirm if Zoho auto-computes Form 10E / Section 157 relief for backdated arrears. This is a statutory requirement when arrear income causes higher tax bracket in receipt year.
6. **Payrun-boundary snapping may cause issues**: If a revision effective date falls mid-month, snapping to the next payrun boundary could mean employee is underpaid for a partial month. No UI indication of how this edge is handled.

## How We Should Build This

### SalaryRevision Entity
```
SalaryRevision:
  - id
  - employee_id
  - effective_from (date — calendar date, NOT payrun-bound)
  - new_ctc (decimal)
  - new_components (SalaryComponentAssignment[])
  - initiated_by (user_id)
  - initiated_at (timestamp)
  - status (enum: Draft | PendingApproval | Approved | Rejected | Applied)
  - approved_by (user_id, nullable)
  - approved_at (timestamp, nullable)
  - applied_in_payrun_id (nullable — set when revision is processed)
  - revision_reason (string)
```

### Arrear Computation
```
// When processing a payrun where a revision was effective in a prior month:
arrear_months = months between revision.effective_from and current_payrun.period_start
for each arrear_month:
    for each salary_component:
        arrear_delta += (new_monthly_component - old_monthly_component) × proration_factor
// proration_factor = 1.0 for full months; partial factor for month of effective date

// Add as ArrearEarning line item in payslip
ArrearEarning:
  - description: "Salary Arrears (MMM YYYY – MMM YYYY)"
  - amount: total_arrear_delta
  - taxable: true (arrears are fully taxable in year of receipt)
```

### TDS on Arrears
- Arrear income added to annual taxable income for the FY
- Recompute annual tax → distribute over remaining months (same annualisation method)
- Section 157 / 89(1) relief: compute relief if arrear was for a prior FY
  - Required: Form 10E data (income of prior year, tax of prior year)
  - Zoho shows "Relief Under Section 157" in IT Declaration headers — ensure this maps to 89(1) computation

### Approval Workflow
- `SalaryRevisionApproval` with state machine: Draft → PendingApproval → Approved/Rejected
- Notification to approver on submission
- Notification to employee on approval
- Approver cannot be same as initiator (segregation of duties)

### Effective Date Handling
- Store effective date as calendar date (not payrun-bound)
- At payrun compute time: check all approved revisions with effective_from within or before the payrun period
- If revision.effective_from is mid-month: apply new salary from that date; prorate current month as split (old rate for days 1 to effective_day-1, new rate for days effective_day to month_end)
- Do NOT snap to payrun boundary — this causes underpayment

### Salary History
- Immutable ledger of salary states per employee
- Each payrun stores which salary assignment version was active when computed
- Enables: re-run same payrun with same salary → identical result (deterministic)
