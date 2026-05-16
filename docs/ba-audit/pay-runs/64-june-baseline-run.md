# Pay Runs > June Baseline Run & Payroll History

## URL / Navigation Path

**Run Payroll tab (next period card):**
`https://payroll.zoho.in/#/payruns`

**Payroll History tab:**
`https://payroll.zoho.in/#/payruns/payroll-history`

## Purpose

Documents the Run Payroll landing page state after May 2026 run is Paid (i.e., what the system shows as the "next" run), and the Payroll History table showing all completed runs.

## Run Payroll Tab — Post-May State

After May 2026 run was marked Paid, the Run Payroll tab shows:

**Empty state:**
- Heading: "You deserve a break today!"
- Paragraph: "You have no outstanding pay runs."
- No period card displayed

**Observation:** June 2026 period card does NOT auto-appear. As of audit date (15 May 2026), the system has not yet generated a June run card. This suggests Zoho generates the next period card based on some timing logic — either:
- The card appears only after the current month's end date has passed, OR
- The card appears on a configured date (e.g., first day of the month or a configured "run payroll from" date)

**Implication:** The "next run" is not pre-created until the system determines it's time. Admin cannot pre-initiate June run while in May.

## Payroll History Table

### URL

`https://payroll.zoho.in/#/payruns/payroll-history`

### Layout

- Filter control: "Payroll Type:" dropdown (combobox) with option "All" (default)
  - Filter options observed: All (and inferred: Regular Payroll, Off Cycle, One Time Payout, Resettlement Payroll)
- Table with 4 columns

### Table Columns

| Column | Type | Notes |
|--------|------|-------|
| Payment Date | Date (DD/MM/YYYY) | The payment date recorded when marking as Paid |
| Payroll Type | Text | e.g., "Regular Payroll" |
| Details | Date range button | e.g., "01/05/2026 - 31/05/2026" — clickable link to run summary |
| Payroll Status | Badge | e.g., "Paid" |

### Observed Data (2 runs)

| Payment Date | Payroll Type | Details | Status |
|-------------|-------------|---------|--------|
| 29/05/2026 | Regular Payroll | 01/05/2026 - 31/05/2026 | Paid |
| 30/04/2026 | Regular Payroll | 01/04/2026 - 30/04/2026 | Paid |

### Row Interaction

- Entire row is clickable
- Clicking navigates to the run summary: `#/payruns/{id}/summary`
- "Details" cell contains a button with the date range — clicking also navigates to summary

## Payroll Type Filter Options

The "Payroll Type" filter dropdown controls which runs appear in history. Observed option: "All". Inferred options (based on run types available via New dropdown):
- All
- Regular Payroll
- Off Cycle Payrun
- One Time Payout
- Resettlement Payroll (observed in filter list; not yet tested)

## Payroll History — Pagination & Limits

- In this test org with only 2 runs, no pagination was observed
- Expected: pagination controls appear for orgs with many historical runs
- No search/date-range filter beyond the Payroll Type dropdown

## Key Observations for Our Build

1. **Next run card timing logic** — investigate when Zoho generates the next period card. Our build should implement: after a run is marked Paid, create a "READY" payroll run record for the next month immediately. The Run Payroll tab shows this READY run as the period card. This avoids confusion about "when does the next run appear."
2. **Payroll History is simple** — 4 columns, Payroll Type filter, row click to navigate. Low complexity to implement. Build this early as a navigation hub for historical audit.
3. **Details column as link** — the date range in the Details cell is a clickable button. This is good discoverability. Our table should make the entire row clickable (as Zoho does) and the date range a labeled link for accessibility.
4. **Payroll Type filter** — must support filtering by: Regular, Off-Cycle, One Time Payout, F&F (our custom type). Implement as a server-side filter (`?type=regular`) for scalability.
5. **"Resettlement Payroll" type** — observed in filter but not tested. May be a Zoho-specific term for arrears payroll. Research and document in a future session.
6. **No date range filter** — Zoho History has no "from date / to date" filter beyond Payroll Type. For compliance audits covering a whole year, this is a gap. Our build should add date range filtering on Payroll History.
7. **Back navigation context** — when entering a run from Payroll History, the "Back" link in the run header points to `#/payruns/payroll-history`. When entering from the period card (Run Payroll tab), Back points to `#/payruns`. Track entry point for correct back navigation in our SPA.

## Screenshots

- `screenshots/64-payroll-history-two-runs.png` — Payroll History with May 2026 + April 2026 entries

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
