# UF-29: Open POI (Proof of Investment) Window

**Module:** Settings > Setup & Configurations > Claims and Declarations > Proof Of Investments
**Tested:** 2026-05-16
**Mock Data Used:** lerno org, admin user abhijithss2255
**App State Before:** POI is in "Locked" state (default — never released)

## Steps Executed
1. Navigate to `#/settings/preferences/proof-of-investment`
2. Observed current state: "POI is Locked"
3. Documented all configuration fields

## Page Identity
- **URL:** `#/settings/preferences/proof-of-investment`
- **Page title:** "POI Preference | Preferences | Settings | Zoho Payroll"
- **Module path:** Settings > Setup & Configurations > Claims and Declarations > Proof Of Investments
- **Access:** Admin only

## Status Banner
The page shows a top-right counter: employees "yet to submit POI" with a "View Employees" link → `#/approvals/proof-of-investment/unsubmitted-list`

## Current State: POI is Locked

- **Icon:** lock illustration
- **Heading:** "POI is Locked"
- **Message:** "You are yet to enable submission of investment proofs for your employees through their respective portals. Release POI or submit it on their behalf under Employees > Employee profile > Investments > Proof of Investments."
- **Primary CTA:** "Release Proof Of Investments" button

## Fields & Validations

| Field | Type | Required | Default | Options/Rules |
|-------|------|----------|---------|---------------|
| POI state | Read-only status | N/A | Locked | Locked / Released |
| Process payroll with approved POI amount from | Month dropdown | No | March | Month selection for FY (governs when approved POI affects payroll TDS) |
| Allow employees to switch tax regimes | Checkbox | No | Checked | Same as IT Declaration setting |
| Allow TDS modification during Payroll | Checkbox | No | Unchecked | Admin can override TDS in pay run |
| Mandate investment proof attachments for POI submission | Checkbox | No | Unchecked | If checked, employees must attach document files when submitting POI |
| Mandate reviewer comments for partial investment amount approval | Checkbox | No | Unchecked | If checked, approver must enter a comment when approving less than declared amount |

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Release Proof Of Investments | Button click | Currently Locked | Opens POI submission window; employees can upload proofs |
| View Employees (yet to submit POI) | Link click | Any state | Navigates to `#/approvals/proof-of-investment/unsubmitted-list` |
| Save | Button click | Any config change | Persists all checkbox + month settings |

## Business Rules

### POI Processing Month
- The "Process payroll with approved POI amount from" dropdown (default: March) determines from which payroll month the system starts using approved POI amounts to recalculate TDS
- The explanatory text: "The approved POI amount will be considered for the payroll from **March** onwards to calculate and deduct income tax amount in subsequent payrolls"
- This means: even if POI is submitted and approved in January, the revised TDS only takes effect from the selected month (March by default in Indian payroll — aligned with Q4 recalculation)

### Mandate Attachment Rule
- If "Mandate investment proof attachments" is checked: employees cannot submit POI without uploading at least one document per declared investment

### Reviewer Comments for Partial Approval
- If "Mandate reviewer comments for partial amount approval" is checked: approver must justify in writing when approving an amount less than what the employee declared

## Key Difference from IT Declaration
- IT Declaration: the employee declares INTENT to invest (future projection)
- POI: the employee submits PROOF of actual investment (receipts, certificates)
- Both have separate locked/released states — they can be released independently

## Cross-Module Effects
- Released POI → employees see "Submit Proofs" section in employee portal under Investments
- Approved POI amounts feed into TDS calculation starting from the configured month
- "Unsubmitted POI" count shown on this settings page pulls from `#/approvals/proof-of-investment/unsubmitted-list`

## Informational Links
- "Learn how to manage investment proofs" → `https://www.zoho.com/in/payroll/help/employer/poi.html`
- "Download and share this POI ebook" → PDF employee guide

## Gaps / Observations
- No date-range picker for "POI submission window open from/to" — once released it stays open
- The "process from" month is org-wide, not employee-specific — cannot configure per-employee POI application month
- "Allow employees to switch tax regimes" appears on both IT Declaration and POI settings pages — they appear to be the same setting toggled from two places (potential UX duplication)
- No "auto-release on date X" or "auto-lock on date Y" scheduler
- No email notification configuration visible on this page
