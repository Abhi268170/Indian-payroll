# UF-11: Configure LWF (Labour Welfare Fund)

**Module:** Settings → Setup & Configurations → Statutory Components → Labour Welfare Fund
**Tested:** 2026-05-16
**Mock Data Used:** Kerala (org's actual state)

## Steps Executed
1. Navigated to `#/settings/statutory-details/list/lwf`
2. Observed LWF page showing Kerala pre-configured entry with Status: Disabled
3. Noted amounts and deduction cycle (pre-filled by Zoho for Kerala)
4. Clicked "(Enable)" button → confirmation dialog appeared
5. Dialog text: "You are about to enable LWF for your work location(s) at Kerala. Are you sure you want to proceed?"
6. Dialog buttons: "Yes, Proceed" (primary/active) and "Cancel"
7. Clicked "Yes, Proceed" → LWF enabled

## Fields (Read View)

| Field | Value Observed | Notes |
|-------|----------------|-------|
| State | Kerala | Grouped by work location state |
| Employees' Contribution | ₹50.00 | Fixed per Kerala statute |
| Employer's Contribution | ₹50.00 | Fixed per Kerala statute |
| Deduction Cycle | Monthly | Kerala: monthly deduction |
| Status | Disabled / Enabled | Toggle via "(Enable)" or "(Disable)" button |

## Key Statutory Facts (Kerala LWF)
- Employee contribution: ₹50/month
- Employer contribution: ₹50/month  
- Total LWF per employee per month: ₹100
- Deduction cycle: Monthly (some states like Karnataka deduct annually in December)

**Note:** Karnataka LWF differs significantly:
- Karnataka deduction is Annual (December)
- Employee: ₹20 per year
- Employer: ₹40 per year

Since this org is Kerala, the observed values are ₹50/₹50 Monthly.

## UI Pattern
- LWF page shows state-grouped cards (one per work location state)
- No free-text entry fields — all values are pre-loaded by Zoho based on state
- Page description: "Labour Welfare Fund act ensures social security and improves working conditions for employees."
- Confirmation dialog appears before enabling — guards against accidental activation

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| (Enable) | Button | LWF disabled for state | Shows confirmation dialog |
| Yes, Proceed | Button in dialog | Confirmation shown | Enables LWF; updates status to "Enabled" |
| Cancel | Button in dialog | Confirmation shown | Dismisses dialog; no change |
| (Disable) | Button | LWF enabled | Presumably shows disable confirmation |

## Cross-Module Effects
- Once enabled, LWF deduction appears automatically in pay runs for employees in the applicable state
- Both EE and ER contributions flow through payroll processing

## Gaps / Observations
- No separate "LWF Number" field visible (unlike EPF/ESI which have registration number fields)
- Cannot customize LWF amounts — they are state-mandated and read-only
- No per-employee LWF opt-out mechanism visible (contrast with EPF/ESI which have per-employee toggles)

## Screenshots
- [LWF page with Kerala disabled state](../screenshots/UF-11-LWF-disabled.png)
