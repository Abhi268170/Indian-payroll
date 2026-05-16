# Compliance > EPF — UAN Management

## URL / Navigation Path
- Employee Profile > Overview > Statutory Information > Edit
- URL: `#/people/employees/{employee_id}/edit-statutory-details`

## Purpose
Capture and manage the Universal Account Number (UAN) assigned to each employee by EPFO, along with other statutory identifiers (ESI IP Number). These are required for ECR file generation and EPFO portal operations.

## Current State in Test Org
Since EPF is not configured at the organisation level, the employee statutory information edit page currently shows **only Professional Tax** as a checkbox field. UAN and ESI IP Number fields do **not appear** when EPF/ESI are not enabled.

**Observed behaviour**: Navigating to `#/people/employees/{id}/edit-statutory-details` shows:
- Heading: "Arjun Mehta's statutory information"
- Checkbox: "Professional Tax" (checked for Head Office / Kerala employees)
- Save / Cancel buttons

## Conditional Fields (when EPF is configured)
Based on Zoho's architecture (confirmed via Employee List column inventory from Session 5–6):

| Field | Appears When | Type | Format | Notes |
|-------|-------------|------|--------|-------|
| UAN (Universal Account Number) | EPF enabled at org level | Text | 12-digit numeric | EPFO-issued; unique per employee across employers |
| PF Account Number | EPF enabled | Text | Region/Sub-code/Est.ID format | Organisation-specific PF account for this employee |
| ESI Number (IP Number) | ESI enabled at org level | Text | Per ESIC format | Insurance Policy number from ESIC |

**Evidence from Employee List columns (Session 5–6 audit):**
- Column "UAN" is available in the 21-column customizable Employee List
- Column "PAN" is available (personal)
- Column "PF A/C" is available
- Column "ESI No" is available

## Employee Overview — Statutory Information Section

Displayed on read-only overview page:

| Field | Display | Edit Trigger |
|-------|---------|-------------|
| Professional Tax | Enabled / Disabled badge + (Disable)/(Enable) button | Inline toggle |
| (UAN, ESI — when configured) | Text value | Via edit form |

The "Edit" link opens `edit-statutory-details` form in context.

## UAN Entry Flow
1. Navigate to Employee Profile > Overview
2. Click "Edit" next to "Statutory Information" heading
3. URL: `#/people/employees/{id}/edit-statutory-details`
4. Enter UAN in the UAN field (only visible when EPF is enabled org-wide)
5. Save

## UAN Validation
- 12-digit numeric string
- EPFO issues UAN when employee registers for PF for the first time
- UAN is portable across employers (stays with employee throughout career)
- Must be unique per employee in the system

## Bulk UAN Entry
From prior audit (Session 5–6), the Bulk Import modal supports "Previous Employment Details" type which includes UAN. The Employee Import type also likely includes UAN as a column.

## Government Portal Integration
- UANs are required in the ECR file for EPFO challan and return submission
- EPFO Unified Portal uses UAN to link contributions across employers
- Zoho does not perform UAN verification against EPFO database (no API integration observed)

## Statutory Rules Referenced
- EPFO circular on UAN generation and seeding
- ECR 2.0 format requires UAN per employee row
- Employees without UAN: new joiners must be registered by employer on EPFO portal before first ECR

## Cross-Module Dependencies
- Settings > Statutory Components > EPF (must be enabled first)
- Reports > Statutory Reports > EPF ECR Report (UAN appears per row)
- Employee List view columns (UAN, PF A/C, ESI No)
- Pay Run > Statutory Summary section

## Key Observations for Our Build
1. **UAN field is gated on EPF org configuration** — our DB schema should have the UAN field on the Employee entity, but the UI should only show it once EPF is enabled in statutory config.
2. **UAN is 12 digits** — validate numeric, exactly 12 characters. Our domain has `string Uan` — ensure we add a regex validator.
3. **No EPFO API verification** in Zoho — we match this: store as entered, no live lookup.
4. **ESI IP Number follows same pattern** — field gated on ESI org configuration.
5. **PT is the only unconditionally visible statutory toggle** — because PT is automatically provisioned from work location state, unlike EPF/ESI which require explicit org-level registration.
6. **Employee statutory edit is a simple single-screen form** (not a multi-step wizard) — lightweight implementation is correct.

## Screenshots
- `screenshots/employee-statutory-info-edit.png` — Statutory info edit page (PT only, EPF/ESI not configured)
