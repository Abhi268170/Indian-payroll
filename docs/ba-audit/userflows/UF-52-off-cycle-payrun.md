# UF-52: Off-Cycle Pay Run

**Module:** Pay Runs > New > Off Cycle Payrun
**Tested:** 2026-05-16
**Mock Data Used:** Initiated Off Cycle Pay Run dialog (cancelled without saving)
**App State Before:** No active pay runs; May 2026 = PAID

## Steps Executed
1. Navigate to `#/payruns`
2. Click "New" dropdown button
3. Click "Off Cycle Payrun"
4. Observe "Initiate Off Cycle Pay Run" modal
5. Click Cancel (no pay run created)

## Dialog: Initiate Off Cycle Pay Run

### Fields
| Field | Label | Type | Required | Validation | Notes |
|-------|-------|------|----------|------------|-------|
| Pay Date | When would you like to pay? | Date (dd/MM/yyyy) | Yes | Date picker | Custom pay date, not constrained to month end |

### Actions
| Button | Behavior |
|--------|----------|
| Save and Continue | Creates the off-cycle pay run and navigates to its configuration |
| Cancel | Closes modal, no pay run created |

## Business Rules

1. Off-cycle pay runs are NOT date-gated to the current month — they can be created any time
2. The only required input at creation is the pay date
3. Post-creation, the admin selects which employees to include and what components to pay
4. Off-cycle runs are independent of the regular payroll cycle
5. They appear in Payroll History under a different type label (likely "Off Cycle" vs "Regular Payroll")

## Differences vs Regular Pay Run

| Dimension | Regular Pay Run | Off Cycle Pay Run |
|-----------|----------------|------------------|
| Creation | Auto-created at month start | Admin-initiated via "New" button |
| Pay Period | Full calendar month | Custom date range |
| Employees | All eligible employees | Selectable subset |
| LOP | Applies | Configurable |
| Statutory deductions | As configured | As configured |
| Approvals | Per workflow config | Per workflow config |
| Payslip | Yes (standard template) | Yes |

## Use Cases for Off-Cycle Pay Run
- Rejoiner mid-month salary
- Supplemental pay (late bonus or arrears not captured in regular run)
- FnF settlement outside regular cycle
- Corrective pay after pay run error (before next regular run)

## Payroll History Type Labels
From `#/payruns/payroll-history` table: Column "Payroll Type" shows:
- "Regular Payroll" for monthly pay runs
- Off-cycle runs would appear with a different label (not yet observed)

## Gaps / Observations
- Only one field (pay date) captured at initiation — employee selection and component editing happens on the next screen (not explored)
- No minimum notice period enforcement for off-cycle pay date (can the admin set yesterday's date?)
- No indication of whether off-cycle runs go through the same approval workflow as regular runs
- No "Reason" field at creation — no mandatory justification for creating out-of-cycle payment
