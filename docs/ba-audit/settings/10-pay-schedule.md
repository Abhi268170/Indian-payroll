# Settings > Pay Schedule

## URL
`#/settings/pay-schedules` (redirects to `#/settings/pay-schedules/new` on first setup)

## Purpose
Defines the organisation's working week, salary proration method, and employee pay date. These settings underpin all payroll calculations — determining payable days, loss-of-pay deductions, and when salary is credited.

## Page Layout
Single form with three sections:
1. **Work Week** — Segmented day-selector (toggleable checkboxes for each day of the week)
2. **Salary Calculation Method** — Radio group (Actual days / Fixed working days)
3. **Pay Date** — Radio group (Last day of month / Specific day 1–30)

Footer: Note about non-working day fallback + Save button.

## Fields

### Work Week
| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Work Week Days | Segmented toggle buttons (7 days: SUN–SAT) | Yes | MON, TUE, WED, THU, FRI selected | Click to toggle each day on/off. Currently: Mon–Fri = working days; Sat, Sun = non-working |

Help text below heading: *"These days will be considered when calculating payable days and loss of pay."*

**Current selection:** MON, TUE, WED, THU, FRI (standard 5-day week). SUN and SAT are unselected (weekend).

Each day is a `segmented-checkbox` component — clicking toggles `selected` class. Multiple days can be selected simultaneously.

### Salary Calculation Method
| Field | Type | Required | Default | Options | Tooltip |
|-------|------|----------|---------|---------|---------|
| Salary Calculation Method | Radio group | Yes | Actual days in a month | 1. Actual days in a month, 2. Based on fixed working days per month | Option 2 tooltip: "Salary is prorated based on a fixed number of working days each month." |

**Option 1 — Actual days in a month:**
- Daily rate = Monthly CTC / Actual calendar days in the month (28/29/30/31)
- Example: Feb → divide by 28; March → divide by 31
- Standard method used by most Indian companies

**Option 2 — Based on fixed working days per month:**
- Daily rate = Monthly CTC / Fixed working days (e.g., 26 days)
- Proration uses the configured fixed number, regardless of actual calendar days
- Tooltip: "Salary is prorated based on a fixed number of working days each month."
- Note: When this option is selected, a "Fixed working days per month" input field presumably appears (not explored — would require selecting this option)

### Pay Date
| Field | Type | Required | Default | Options | Notes |
|-------|------|----------|---------|---------|-------|
| Pay Date | Radio group + conditional dropdown | Yes | On the last day of every month | 1. On the last day of every month, 2. On Day [1–30] of every month | — |

**Option 1 — Last day of every month:**
- Salary credited on the last calendar day of each month
- Non-working day fallback applies (see Note below)

**Option 2 — On Day N of every month:**
- Dropdown: Days 1 through 30 (NOT 31 — avoids February/short-month issues)
- Default value shown: 1
- When selected, the day dropdown becomes enabled (it is disabled when Option 1 is active)
- Non-working day fallback applies

**Note (displayed below the form):** *"If the selected pay date falls on a non-working day or holiday, payment will be processed on the previous working day."*

## Buttons & Actions

| Button | Label | State | Action |
|--------|-------|-------|--------|
| Save | "Save" | Always enabled | Saves all pay schedule settings |

## Tabs (if any)
None.

## Conditional Logic

1. **Work Week selection** — toggling days on/off directly affects payable-day calculations and loss-of-pay deductions in every pay run.
2. **Salary Calculation Method — "Based on fixed working days"** — selecting this option likely reveals an additional numeric input for the fixed working days count (e.g., 26). Not confirmed from current UI state.
3. **Pay Date — "On Day N"** — when this radio is selected, the day-of-month dropdown becomes enabled (was disabled when "Last day" was selected).
4. **Pay date non-working day fallback** — automatic: if the configured date falls on a weekend or holiday, the system pays on the previous working day. This is enforced by the engine, not user-configurable.

## Cross-Module Impact

| Setting | Impacts |
|---------|---------|
| Work Week | Determines "payable days" for each employee in a pay run. Also drives Loss-of-Pay (LOP) calculation when employee is absent on working days |
| Work Week | Affects mid-month joiner/leaver proration — only working days count |
| Work Week | Feeds into the non-working day fallback for pay date |
| Salary Calculation Method | Determines the daily rate divisor used in proration and LOP calculations across ALL pay runs |
| Salary Calculation Method | "Actual days" changes the denominator every month; "Fixed" uses a constant denominator |
| Pay Date | Determines the payslip date and bank transfer initiation date for every pay run |
| Pay Date | Shown on payslips as the payment date |
| Pay Date | Drives the payroll run calendar — when the system schedules pay run reminders |

## Statutory / Business Rules

- **Salary Calculation Method change mid-year** — would require retroactive recalculation or is locked after first pay run (not confirmed from UI). Zoho likely prevents changing this after payroll has been run.
- **Pay Date day 29/30** — February would always fall back to the last working day in February (28th or 29th). This is handled by the non-working day fallback rule noted in the UI.
- **Indian practice**: Most companies use "Actual days" method and pay on the last working day of the month. The 26-day fixed method is more common in older HR systems.

## Observations & Notes

1. **No "pay period" start date** — Zoho Payroll only supports monthly payroll (calendar month). No fortnightly or weekly pay periods visible. This is typical for Indian payroll.
2. **Pay Date capped at 30** — correctly avoids day 31 to handle short months. Well-designed.
3. **Work Week as segmented checkboxes** — visually intuitive toggle-button pattern. Each day lights up when selected. Good UX.
4. **"All fields are mandatory"** footer note — unusual phrasing (normally individual fields are marked *). On this page all three settings are required before Save.
5. **Page redirects to `/new`** even for editing — suggests this is a one-time setup wizard rather than a recurring CRUD form. Once configured, it probably shows the existing configuration with an Edit flow.
6. For our build: Pay schedule is an immutable-after-first-payrun entity. Work week, salary calculation method, and pay date should be stored as tenant-level configuration in a `PaySchedule` entity. Changing the salary calculation method post-payrun must be blocked or require admin override with audit trail.

## Screenshots
`docs/ba-audit/settings/screenshots/10-pay-schedule.png`
