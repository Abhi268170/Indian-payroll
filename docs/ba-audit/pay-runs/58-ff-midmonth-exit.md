# Pay Runs > Full & Final Settlement — Mid-Month Exit

## URL / Navigation Path

No dedicated F&F URL discovered. F&F processing is expected to occur within:
- Regular pay run split panel (for prorated last-month salary + one-time earnings)
- OR Off Cycle Pay Run (`#/payruns` > New > Off Cycle Pay Run)

## Purpose

Documents the Full & Final Settlement (F&F) capability — or its absence — for employees exiting mid-month. F&F typically covers: prorated last-month salary, leave encashment (statutory + excess), gratuity (if applicable), notice pay recovery or payment, and any outstanding advances/loans.

## Testing Constraint

**EMP005 (Rahul Desai) was skipped** in the May 2026 run because the employee's onboarding was incomplete in this test org. The planned test scenario was:
- Exit date: 20 May 2026
- Leave encashment: 8 days
- F&F settlement

The skip reason: "Onboarding incomplete." The system blocked inclusion of EMP005 in the run and required either completing onboarding or skipping. F&F flow was not observed.

## Inferred F&F Mechanisms in Zoho Payroll (from UI exploration)

### Option 1: Regular Pay Run with Manual Inputs

In the regular monthly run, an exiting employee would be included with:
1. **LOP Days** = days after exit date (e.g., exit 20 May → LOP = 11 days for May 21–31)
2. **Leave Encashment** added via "Add Earning" > Leave Encashment > amount
3. **Gratuity** (if applicable) — not observed as a dedicated earning type. Would need to be added as a custom one-time earning or configured as a component.
4. **Notice pay recovery** — would need to be an ad-hoc deduction
5. No dedicated "F&F worksheet" or "F&F summary" observed within the regular pay run

### Option 2: Off Cycle Pay Run

The Off Cycle Pay Run dialog (New > Off Cycle Pay Run) accepts a single date:
- "Select Date *" — single date dd/MM/yyyy

This may be intended for mid-month payment runs (paying someone on a non-standard date), but it does not appear to have F&F-specific fields. Requires further testing with a fully onboarded employee.

### Option 3: Dedicated F&F Module

Some Indian payroll systems have a dedicated F&F settlement module. Not observed in the current navigation structure. The left sidebar shows: Dashboard, Employees, Pay Runs, Approvals, Form 16, Loans, Giving, Documents, Reports, Settings. No explicit "F&F" or "Separation" module visible.

## Statutory Requirements for F&F (Reference)

| Component | Statutory Basis | Notes |
|-----------|-----------------|-------|
| Prorated salary for last month | Employment contract | (Base Days − exit_day) / Base Days × salary |
| Earned Leave encashment | Factories Act / state-specific | Encash unavailed earned leave at basic + DA rate |
| Gratuity | Payment of Gratuity Act 1972 | 15/26 × last basic × years of service (if ≥ 5 years) |
| Notice pay recovery | Employment contract | If employee leaves without notice |
| Bonus (if applicable) | Payment of Bonus Act | Pro-rated for the year |
| PF settlement | EPF Act | Employee's PF balance withdrawn or transferred |
| TDS on F&F | Income Tax Act | F&F components taxable; included in Form 16 |

## Key Observations for Our Build

1. **F&F is a critical gap to address explicitly** — Zoho does not appear to have a dedicated F&F module (based on current audit). Our build should implement a dedicated F&F settlement flow as it is a high-frequency operation for Indian employers.
2. **F&F settlement should be a separate Pay Run type** — alongside Regular, Off-Cycle, and One-Time. The F&F run should:
   - Pre-populate prorated salary based on exit date
   - Auto-compute leave encashment from leave balance
   - Optionally compute gratuity based on service duration
   - Generate a standalone F&F statement PDF (not just a payslip)
3. **Gratuity computation** — not visible in Zoho's current flow. Formula: `(15 / 26) × last_drawn_basic × completed_years`. Only applicable after 5 years of service. Store as a separate earning component with statutory basis documented.
4. **TDS impact** — F&F components must be included in annual TDS computation. Gratuity up to ₹20L is tax-exempt (FY2026). Leave encashment up to ₹25L is tax-exempt for non-govt employees.
5. **No dedicated F&F form** — absence of a structured F&F form means admins rely on manual calculation outside the system. This is a significant opportunity for our product.

## Open Questions

- [ ] Does Zoho have an F&F module hidden in a higher plan (Enterprise)?
- [ ] Is the Off Cycle Pay Run intended for F&F use? Test with fully onboarded employee exiting mid-month.
- [ ] Does Zoho compute gratuity automatically anywhere (employee profile → service years)?
- [ ] How does Zoho handle the PF settlement for exiting employees — is ECR generation for the final month automated?
- [ ] Is there a "Separation" or "Offboarding" flow in Zoho People (the HR module) that feeds into Zoho Payroll?

## Screenshots

No screenshots captured for this item (EMP005 was skipped due to onboarding incompleteness).

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
