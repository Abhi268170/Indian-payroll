# UF-28: Lock IT Declaration

**Module:** Settings > Setup & Configurations > Claims and Declarations > Income Tax Declaration
**Tested:** 2026-05-16
**Mock Data Used:** lerno org, admin user abhijithss2255
**App State Before:** IT Declaration is in "Locked" state (default — never released)

## Steps Executed
1. Navigate to `#/settings/preferences/it-declaration`
2. Observed current state: "IT Declaration is Locked"
3. Documented all fields and actions on this page

## Page Identity
- **URL:** `#/settings/preferences/it-declaration`
- **Page title:** "IT Declaration Preference | Preferences | Settings | Zoho Payroll"
- **Module path:** Settings > Setup & Configurations > Claims and Declarations > Income Tax Declaration
- **Access:** Admin only

## Current State: IT Declaration is Locked

The page displays a locked-state panel with:
- **Icon:** lock illustration
- **Heading:** "IT Declaration is Locked"
- **Message:** "You are yet to enable the submission of IT Declaration for your employees through their respective portals. Release IT Declaration or submit it on their behalf under Employees > Employee profile > Investments > IT Declaration."
- **Primary CTA:** "Release IT Declaration" button

This means the system has two modes:
1. **Locked:** Employees CANNOT submit declarations via the portal. Admin submits on their behalf via employee profile.
2. **Released:** Employees CAN submit declarations via the employee portal.

## Fields & Validations

| Field | Type | Required | Default | Options/Rules |
|-------|------|----------|---------|---------------|
| IT Declaration state | Read-only status | N/A | Locked | Locked / Released (toggled by Release/Lock buttons) |
| Allow employees to switch tax regimes | Checkbox | No | Checked (enabled) | Allows employees to choose Old/New regime from portal |
| Allow TDS modification to exceed the current fiscal year's calculated tax amount | Checkbox | No | Unchecked | If enabled, admin can set TDS above computed annual tax |

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Release IT Declaration | Button click | Currently Locked | Opens portal submission window for employees; button changes to "Lock IT Declaration" |
| Save | Button click | Any config change | Persists checkbox settings |

## Business Rules
- When Locked: employees cannot access IT Declaration form in their portal; admin must enter declarations manually under each employee's profile (Employees > profile > Investments > IT Declaration)
- When Released: employees can access and edit their IT Declaration through the employee portal
- "Allow employees to switch tax regimes" being checked means: employee can elect Old Regime or New Regime from their portal (otherwise admin-controlled)
- "Allow TDS modification to exceed calculated tax" — dangerous flag: if enabled, TDS deducted can exceed the actual computed income tax liability for the FY

## Informational Links on Page
- "Learn how to manage investment declarations" → `https://www.zoho.com/in/payroll/help/employer/it-declaration.html`
- "Download and share this IT Declaration ebook" → PDF guide for employees

## Cross-Module Effects
- When Released: employee portal IT Declaration form becomes accessible
- Employees > Employee profile > Investments > IT Declaration: admin can always submit/edit regardless of lock state
- Regime switch setting directly affects whether employees can change their TDS calculation regime

## Gaps / Observations
- No "release window" date range configuration on this page — IT Declaration, once released, appears to remain open indefinitely until admin locks it again
- No automatic lock date (e.g., "auto-lock on 31-Jan") — admin must manually lock
- No employee notification triggered from this page (no "notify employees when released" toggle visible)
- The page title says "IT Declaration Preference" but the tab link in the nav is "Income Tax Declaration" — minor labelling inconsistency
