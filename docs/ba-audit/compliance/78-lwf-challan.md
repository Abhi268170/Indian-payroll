# Compliance > LWF — Labour Welfare Fund Challan

## URL / Navigation Path
- Settings > Setup & Configurations > Statutory Components > Labour Welfare Fund tab
- URL: `#/settings/statutory-details/list/lwf`

## Purpose
Configure and manage Labour Welfare Fund (LWF) contributions per state. LWF is a state-level statutory deduction that provides social security and improves working conditions for employees. Contribution amounts and frequencies vary by state.

## Architecture: Per State (Work Location Derived)
Like PT, LWF is provisioned automatically based on the work location's state. The LWF configuration page shows a state-labelled card for each state where the organisation has work locations.

## Current State in Test Org
**Kerala LWF is auto-provisioned but Disabled.**

| Attribute | Value |
|-----------|-------|
| State | Kerala |
| Employee's Contribution | INR 50.00 |
| Employer's Contribution | INR 50.00 |
| Deduction Cycle | Monthly |
| Status | Disabled |

**Enable button** "(Enable)" is available to activate LWF deductions.

**Description text shown on page:**
> "Labour Welfare Fund act ensures social security and improves working conditions for employees."

## LWF Configuration Fields

| Field | Type | Notes |
|-------|------|-------|
| State label | Display only | Shows state name (e.g., "Kerala") |
| Employees' Contribution | Display (INR amount) | Pre-loaded from Zoho DB; state-mandated amount |
| Employer's Contribution | Display (INR amount) | Pre-loaded from Zoho DB; state-mandated amount |
| Deduction Cycle | Display | Monthly / Half-yearly / Annual (state-dependent) |
| Status | Display + Enable/Disable button | Toggle to activate/deactivate LWF for this state |

### Actions
| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| (Enable) | Button | LWF disabled | Activates LWF deductions in future pay runs for employees in this state |
| (Disable) | Button (when enabled) | LWF enabled | Deactivates LWF deductions |

## Kerala LWF Details

| Contributor | Amount | Basis |
|-------------|--------|-------|
| Employee | INR 50.00/month | Fixed amount (not percentage) |
| Employer | INR 50.00/month | Fixed amount (not percentage) |
| **Total** | **INR 100.00/month** | |

**Deduction Cycle:** Monthly
**Kerala LWF Act:** Kerala Labour Welfare Fund Act 1975

**Note on Kerala LWF frequency:** Kerala deducts LWF monthly, which differs from many other states that use annual or bi-annual schedules. This makes Kerala LWF one of the more frequent deductions.

## State-wise LWF Variation (for Our Build Reference)

States vary significantly in LWF amount, basis, and frequency:

| State | Employee | Employer | Cycle | Notes |
|-------|----------|----------|-------|-------|
| Kerala | INR 50 | INR 50 | Monthly | Fixed amount |
| Maharashtra | INR 6 (June), INR 12 (Dec) | INR 12 (June), INR 24 (Dec) | Half-yearly | Variable amounts per period |
| Karnataka | INR 20 | INR 40 | Annual | December deduction |
| Tamil Nadu | INR 10 | INR 20 | Annual | — |
| Gujarat | INR 6 | INR 12 | Half-yearly | — |
| Andhra Pradesh | INR 30 | INR 70 | Annual | — |
| West Bengal | INR 3 | INR 15 | Monthly | — |
| Madhya Pradesh | INR 10 (Jan-Jun), INR 20 (Jul-Dec) | INR 20 (Jan-Jun), INR 40 (Jul-Dec) | Half-yearly | — |

*Note: Amounts are approximate statutory rates; verify against current state notifications.*

## LWF Challan / Returns
Zoho does not have a dedicated "LWF Challan" UI section. LWF challan filing process:
1. Reports > Statutory Reports > **Labour Welfare Fund Summary** — available in Reports Centre
2. Employer remits LWF contributions to the state's Labour Welfare Board
3. No direct LWF board portal integration observed
4. Payment typically via state treasury challan or bank remittance

## Applicability
- LWF applies to specific categories of employees as defined per state act
- Not all states have LWF (e.g., Delhi, Bihar do not have LWF)
- Zoho shows only states where the org has work locations
- Employees may be exempt if they are in certain categories (managerial staff in some states)

## Government Portal Integration
- No direct state LWF board API integration observed
- Zoho generates LWF Summary report; employer files manually with state board

## Statutory Rules Referenced
- Kerala Labour Welfare Fund Act 1975
- Each state has its own act; Zoho maintains statutory amounts per state
- LWF deduction timing varies by state (monthly/half-yearly/annual)
- Maximum LWF amounts are statutory and cannot be overridden

## Cross-Module Dependencies
- Organisation Address → Work Location → State → LWF provisioning
- Reports > Statutory Reports > Labour Welfare Fund Summary
- Pay Run > per-employee calculations (LWF when enabled and applicable)
- Payslip: LWF line items (employee deduction + possibly employer share in CTC)

## Key Observations for Our Build
1. **LWF amounts are fixed per state, not percentage-based** — our DB config table must store fixed amounts, not rates. Kerala = INR 50 employee, INR 50 employer.
2. **Deduction cycle varies by state** — our engine needs a `LwfDeductionCycle` enum (Monthly, HalfYearly, Annual) and must know which months trigger the deduction.
3. **LWF is per state, provisioned from work location** — same architecture as PT. Our `LwfConfig` entity needs `WorkLocationId` or `StateCode`.
4. **Default is Disabled** — Zoho does not auto-enable LWF; the org admin must explicitly enable it. We should follow the same pattern.
5. **Kerala LWF = monthly INR 50 each** — for our test org, this is the reference deduction. Verified from Zoho's statutory DB.
6. **LWF Summary report** is in the 39-report inventory — we need to produce this report with the same columns.
7. **Not all states have LWF** — our config DB should only provision LWF entries for states that have the scheme, not all 37 states.

## Screenshots
- `screenshots/lwf-configuration-kerala.png` — Kerala LWF configuration (Disabled state, INR 50/50 monthly)
