# UF-09: Configure ESI

**Module:** Settings → Setup & Configurations → Statutory Components → ESI
**Tested:** 2026-05-16
**Mock Data Used:** ESI Number 52-00-123456-000-0001

## Steps Executed
1. Navigated to `#/settings/statutory-details/list/esi`
2. Observed empty state: "Are you registered for ESI?" with "Enable ESI" link
3. Observed descriptive text: "Organisations having 10 or more employees must register for Employee State Insurance (ESI). This scheme provides cash allowances and medical benefits for employees whose monthly salary is less than ₹21,000."
4. Clicked "Enable ESI" → navigated to `#/settings/statutory-details/edit-esi-details`
5. Documented all fields
6. Filled ESI Number: 52-00-123456-000-0001
7. Clicked "Enable" → redirected to ESI list view

## Fields & Validations

| Field | Type | Required | Default | Options/Rules |
|-------|------|----------|---------|---------------|
| ESI Number | Text | No | — | Format: 00-00-000000-000-0000 (shown as placeholder) |
| Deduction Cycle | Text (disabled) | — | Monthly | Read-only; always Monthly |
| Employees' Contribution | Number (disabled) | — | 0.75% | Fixed statutory rate; not editable |
| Employer's Contribution | Number (disabled) | — | 3.25% | Fixed statutory rate; not editable |
| Include employer's contribution in employee's salary structure | Checkbox | No | Unchecked | Controls ER-ESI in salary structure |

**Both contribution rates are disabled** — Zoho enforces statutory rates and does not allow overrides.

## Statutory Rule Note (displayed in form)
"ESI deductions will be made only if the employee's monthly salary is less than or equal to ₹21,000. If the employee gets a salary revision which increases their monthly salary above ₹21,000, they would have to continue making ESI contributions till the end of the contribution period in which the salary was revised (April-September or October-March)."

This encodes the statutory rule: ESI contribution periods are biannual (Apr–Sep and Oct–Mar). Mid-period salary revision above ₹21,000 does not immediately stop ESI deductions.

## Saved State (Read View)
- ESI Number: 52-00-123456-000-0001
- Deduction Cycle: Monthly
- Employees' Contribution: 0.75% of Gross Pay
- Employer's Contribution: 3.25% of Gross Pay

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Enable ESI | Button "Enable" | ESI not yet enabled | Saves config, redirects to ESI list page |
| Disable ESI | Button "Disable ESI" | ESI already enabled | Presumably disables ESI |
| Edit | Link | ESI enabled | Navigates to edit form |
| Cancel | Link | Any time | Returns to `#/settings/statutory-details/list/esi` |

## Cross-Module Effects
- ESI eligibility is determined per employee based on gross salary ≤ ₹21,000/month at time of each pay run
- Like EPF, enabling ESI org-wide does not auto-apply to employees — individual opt-in required

## Gaps / Observations
- No state-specific ESI options visible — ESI is central (ESIC), so this is expected
- Employer ESI contribution (3.25%) is not included in salary structure by default — checkbox unchecked
- The wage ceiling of ₹21,000 is hardcoded in UI text but the underlying enforcement presumably comes from configuration — no field to change it

## Screenshots
- [ESI configuration form](../screenshots/UF-09-ESI-form.png)
