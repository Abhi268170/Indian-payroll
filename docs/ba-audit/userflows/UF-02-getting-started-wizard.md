# UF-02: Getting Started Wizard

**Module:** Dashboard
**Tested:** 2026-05-16
**Mock Data Used:** lerno org, 5 employees
**App State Before:** One completed pay run (May 2026, PAID)

## Steps Executed
1. Navigate to `#/home/dashboard`
2. Observed "Get started with Zoho Payroll" wizard section
3. Clicked Step 4 (Set up Statutory Components) — navigated to EPF settings
4. Returned to Dashboard, clicked "Complete Now" on Step 5 — navigated to Salary Components > Earnings

## Wizard State: 5/7 Completed

The wizard shows a progress indicator "5/7 Completed" at the top right of the wizard card.

| Step | Label | Status | Link Target |
|------|-------|--------|-------------|
| 1 | Add Organisation Details | Completed (green check) | `#/settings/orgprofile` |
| 2 | Provide your Tax Details | Completed | `#/settings/taxes` |
| 3 | Configure your Pay Schedule | Completed | `#/settings/pay-schedules` |
| 4 | Set up Statutory Components | **Incomplete** (expandable) | Expands sub-links in-page |
| 5 | Set up Salary Components | **Incomplete** (CTA button) | `#/settings/salary-components/earnings` |
| 6 | Add Employees | Completed | `#/people/employees` |
| 7 | Configure Prior Payroll | Completed | `#/prior-payroll` |

## Step 4 Detail — Set up Statutory Components (Incomplete)

When clicked, Step 4 expands in-place (accordion behavior) revealing 4 sub-links:
- **Employees' Provident Fund** → `#/settings/statutory-details/list`
- **Employees' State Insurance** → `#/settings/statutory-details/list/esi`
- **Labour Welfare Fund** → `#/settings/statutory-details/list/lwf`
- **Professional Tax (Configured based on Work Location)** → `#/settings/statutory-details/list/pt`

A "Mark as Completed" button is also visible within the step (manual override — admin can mark done without configuring).

## Step 5 Detail — Set up Salary Components (Incomplete)

Shows a "Complete Now" button. Clicking navigates to:
`#/settings/salary-components/earnings?filter_by=&page=1&per_page=50`

## Additional Notable Features Section

Below the 7 steps, a secondary section "Additional notable features" shows 4 optional setup items:
- **Direct Deposit** → `#/settings/direct-deposit`
- **Salary Templates** → `#/settings/salary-templates`
- **Auto Reminder for IT & POI Declaration** → `#/settings/preferences/it-declaration`
- **Employee Custom Field** → `#/settings/employee/custom-field/list`

These have no completion status indicator — purely informational links.

## Page Layout

- Top: Welcome heading "Welcome abhijithss2255!" + support phone number
- Middle: Live webinar banner (Tuesday/Thursday 3:30 PM IST, Register Now link)
- Main content: Getting Started wizard card (left side)
- Right panel: "Find help & resources" with video walkthrough, Help, FAQ, Forum
- Footer: Email support, toll-free number, employee portal app links (iOS/Android), Other Zoho Apps, Quick Links

## Fields & Validations

| Field | Type | Required | Default | Options/Rules |
|-------|------|----------|---------|---------------|
| Step completion status | Read-only badge | N/A | Auto-detected | "Completed" or "Complete Now" / "Mark as Completed" |
| Progress counter | Read-only | N/A | 5/7 | Auto-calculated from completed steps |

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Click step heading | Accordion expand | Step must be incomplete | Expands to show sub-links or CTA |
| Completed link | Navigate | Step is complete | Goes to the relevant settings page |
| Mark as Completed | Manual override | Step 4 only visible option | Marks step complete without fully configuring |
| Complete Now | Navigate | Step 5 only | Goes to Salary Components settings |
| Watch Complete Walkthrough Video | Opens video | None | External video player |
| Register Now (webinar) | External URL | None | Opens `meet.zoho.in` |

## Business Rules
- Steps 1-3 and 6-7 were completed via prior setup; their status is auto-detected from backend data
- Step 4 has a "Mark as Completed" manual override — allows bypassing statutory config if not applicable
- Step 5 links directly to Earnings settings tab — implies salary components are the criterion for completion
- Wizard persists across sessions (not a one-time modal)
- Wizard is dismissible — "Getting Started" appears as a sidebar nav link, collapsible

## Cross-Module Effects
- Step 4 sub-links go to Settings > Statutory Details (EPF, ESI, LWF, PT)
- Step 5 goes to Settings > Salary Components
- Step 6 goes to Employees list
- Step 7 goes to Prior Payroll (separate top-level route `#/prior-payroll`)

## UI Patterns Noted
- Wizard uses accordion/expand pattern for incomplete steps
- Completed steps show a green icon + "Completed" link (clickable to review)
- Each step separated by a horizontal rule (separator)
- No progress bar — only "X/7 Completed" text counter
- Wizard card is not dismissible from Dashboard — always visible until all complete

## Gaps / Observations
- Step 4 "Mark as Completed" is a manual override that could be misused — no enforcement that all 4 statutory components are actually configured
- Step 5 completion criterion is unclear — does saving any salary component mark it complete, or is there a minimum requirement?
- The wizard shows 5/7 but Steps 4 and 5 are incomplete — meaning 2 mandatory setup items are skipped yet payroll ran successfully (May 2026 PAID). This suggests the wizard is advisory, not a hard gate.
- No estimated time per step
- No "skip all" or "dismiss wizard" option visible

## Screenshots
- [Getting Started Wizard — Dashboard](../screenshots/uf02-getting-started.png)
