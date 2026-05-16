# Pay Runs > Create Pay Run — Period Selection & Initiation

## URL / Navigation Path

`https://payroll.zoho.in/#/payruns`
Tab: Run Payroll (default tab on Pay Runs landing page)

Navigation path: Left sidebar > Pay Runs > [Run Payroll tab is default]

## Purpose

Entry point for initiating a new regular pay run for the current/upcoming payroll period. Zoho auto-determines the next payable month based on the last completed run. There is no manual period selection UI — the period is displayed as a pre-computed card.

## Fields

| Field | Type | Required | Options | Behaviour |
|-------|------|----------|---------|-----------|
| Period label (e.g., "Process Pay Run for May 2026") | Read-only display | N/A | System-computed | Derived from last completed pay run + 1 month |
| Pay period date range | Read-only display | N/A | e.g., "01/05/2026 - 31/05/2026" | Shown on the card |
| Pay day | Read-only display | N/A | Configured in Settings > Pay Schedule | Shown on card (e.g., 29th) |

## Buttons & Actions

| Action | Trigger | Pre-condition | Post-behaviour |
|--------|---------|---------------|----------------|
| Process Pay Run (card click) | Click on the period card | At least one employee onboarded | Navigates to `#/payruns/{id}/summary` — Draft state with Pending Tasks |
| New > One Time Payout | Dropdown item | Always available | Opens "Create One Time Payrun" dialog |
| New > Off Cycle Pay Run | Dropdown item | Always available | Opens "Initiate Off Cycle Pay Run" dialog |

## Conditional Logic

- If no outstanding pay runs exist, the main area shows an empty state: heading "You deserve a break today!" + paragraph "You have no outstanding pay runs." and the New button remains available for off-cycle/one-time runs.
- The period card auto-advances: once May is Paid, the card will show June without any admin action.
- There is **no manual period override** — the period is locked to the next sequential month. This is a key architectural constraint.

## Cross-Module Links

- Pay Schedule configuration (pay day, base days) feeds the period card: Settings > Pay Schedule
- Employee onboarding status gates who appears in the run
- Payroll History tab (`#/payruns/payroll-history`) shows completed runs

## Key Observations for Our Build

1. **No period picker** — Zoho enforces sequential monthly runs. Our build should replicate this: compute next period from last finalised run. Admin cannot cherry-pick a period.
2. **Auto-creation behaviour** — Clicking the card immediately creates the payroll run record (assigns an ID like `3848927000000034159`) and navigates to the draft summary. There is no intermediate "Create" confirmation dialog for regular runs.
3. **Empty state UX** — The "You deserve a break today!" message is shown when no outstanding run exists. Our build needs an equivalent empty state on the Run Payroll tab.
4. **New button is always visible** — Even with no outstanding run, the New dropdown for off-cycle/one-time runs remains accessible. Important: these are separate payrun types, not period overrides.
5. **Tab persistence** — URL uses hash routing (`#/payruns` vs `#/payruns/payroll-history`). The active tab is determined by the hash fragment.

## Screenshots

- `screenshots/53-pay-runs-list.png` — Run Payroll tab with period card
- `screenshots/53-preview-draft-initial.png` — Draft summary immediately after creation

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
