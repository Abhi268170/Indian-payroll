# UF-01: Complete Org Onboarding Checklist

**Module:** Dashboard / Getting Started
**Tested:** 2026-05-16
**Mock Data Used:** Org: lerno (Kerala/Thiruvananthapuram, Head Office)

## Steps Executed
1. Navigated to Dashboard at `#/home/dashboard`
2. Observed the "Get started with Zoho Payroll" checklist widget showing 5/7 Completed
3. Identified incomplete steps: Step 4 (Statutory Components) and Step 5 (Salary Components)
4. Completed Step 4 by enabling EPF, ESI, and LWF (PT was pre-configured)
5. Completed Step 5 by navigating to Salary Components and creating Special Allowance

## Checklist Structure

The onboarding checklist is a vertical accordion/stepper widget on the Dashboard main area. Each step shows:
- Step number badge (filled icon = done, outline = pending)
- Step label as a clickable button
- "Completed" link (when done) or "Complete Now" button (when pending)
- A horizontal separator between steps

| Step | Label | Status Observed | Route |
|------|-------|-----------------|-------|
| 1 | Add Organisation Details | Completed | `#/settings/orgprofile` |
| 2 | Provide your Tax Details | Completed | `#/settings/taxes` |
| 3 | Configure your Pay Schedule | Completed | `#/settings/pay-schedules` |
| 4 | Set up Statutory Components | Incomplete — expanded to show 4 sub-links | `#/settings/statutory-details/list` |
| 5 | Set up Salary Components | Incomplete — shows "Complete Now" button | `#/settings/salary-components/earnings` |
| 6 | Add Employees | Completed | `#/people/employees` |
| 7 | Configure Prior Payroll | Completed | `#/prior-payroll` |

**Step 4 Sub-links (when expanded):**
- Employees' Provident Fund → `#/settings/statutory-details/list`
- Employees' State Insurance → `#/settings/statutory-details/list/esi`
- Labour Welfare Fund → `#/settings/statutory-details/list/lwf`
- Professional Tax (Configured based on Work Location) → `#/settings/statutory-details/list/pt`

**Additional notable features section** (below main checklist):
- Direct Deposit → `#/settings/direct-deposit`
- Salary Templates → `#/settings/salary-templates`
- Auto Reminder for IT & POI Declaration → `#/settings/preferences/it-declaration`
- Employee Custom Field → `#/settings/employee/custom-field/list`

## UI Patterns Noted
- Progress indicator: "5/7 Completed" shown as a text badge top-right of checklist
- Completed steps show a checkmark icon + "Completed" green link
- Incomplete steps show a "Complete Now" primary button
- Live webinar banner above checklist: Tuesday or Thursday @3:30PM IST with "Register Now" link
- Top bar shows trial countdown: "Your trial expires in 13 day(s)"
- Notifications bell shows badge count "4"

## Business Rules
- Checklist is sequential display but steps can be completed in any order
- Steps 4 and 5 were still incomplete despite employees (Step 6) being added — no hard dependency enforcement
- Step 4 expands inline to show sub-component links when clicked (accordion behavior)

## Cross-Module Effects
- Completing Step 4 (statutory) enables statutory deductions in pay runs
- Completing Step 5 (salary components) enables salary structure assignment to employees

## Gaps / Observations
- The org is registered as "lerno" (Kerala/Thiruvananthapuram), not "Ierno" (Karnataka/Bangalore) as described in mock data. The actual state is Kerala, not Karnataka. This affects PT slabs (Kerala Half-Yearly) and LWF amounts.
- "Getting Started" is listed in the sidebar navigation as a link (not a module) pointing to `#/home/dashboard`
- Help section at bottom: toll-free 18005726671, Mon–Fri 9AM–7PM

## Screenshots
- [Dashboard onboarding checklist](../screenshots/UF-01-dashboard-onboarding.png)
