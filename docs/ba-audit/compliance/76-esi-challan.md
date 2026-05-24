# Compliance > ESI — Challan Generation

## URL / Navigation Path
- Settings > Setup & Configurations > Statutory Components > ESI tab
- URL (list): `#/settings/statutory-details/list/esi`
- URL (enable/edit): `#/settings/statutory-details/edit-esi-details`

## Purpose
Configure the organisation's ESIC registration and contribution rates. Once configured, monthly pay runs deduct ESI from eligible employees (gross salary ≤ INR 21,000/month). ESI challan generation and return are handled via ESIC portal using data exported from Zoho.

## Current State in Test Org
ESI is **NOT configured**. The list page shows:
> "Are you registered for ESI? Organisations having 10 or more employees must register for Employee State Insurance (ESI). This scheme provides cash allowances and medical benefits for employees whose monthly salary is less than ₹21,000."
>
> CTA: "Enable ESI" → `#/settings/statutory-details/edit-esi-details`

## ESI Configuration Form Fields

The "Employees' State Insurance" form (`edit-esi-details`) captures:

| Field | Type | Required | Default / Options | Notes |
|-------|------|----------|-------------------|-------|
| ESI Number | Text | Yes | Placeholder: `00-00-000000-000-0000` | 17-digit format: Sub Region / Region / Employer Code / Check digits; format hint shown below field |
| Deduction Cycle | Text (disabled) | N/A | Monthly | Read-only; ESI deduction is always monthly |
| Employees' Contribution | Text (disabled) | N/A | 0.75% of Gross Pay | Read-only; statutory rate fixed by ESIC Act |
| Employer's Contribution | Text (disabled) | N/A | 3.25% of Gross Pay | Read-only; statutory rate fixed by ESIC Act |
| Include employer's contribution in employee's salary structure | Checkbox | No | Unchecked | When checked, employer ESI appears in CTC breakup |

### Important Note (shown in UI)
> "ESI deductions will be made only if the employee's monthly salary is less than or equal to ₹21,000. If the employee gets a salary revision which increases their monthly salary above ₹21,000, they would have to continue making ESI contributions till the end of the contribution period in which the salary was revised (April-September or October-March)."

This is a critical statutory rule: the ESI contribution period governs when eligibility changes take effect.

### Actions
| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Enable | Button | ESI Number entered | Saves ESI config; enables ESI deductions in future pay runs for eligible employees |
| Cancel | Link | — | Returns to `#/settings/statutory-details/list/esi` |

## ESI Contribution Rates (Fixed, Statutory)

| Contributor | Rate | Basis |
|-------------|------|-------|
| Employee | 0.75% | Gross Pay |
| Employer | 3.25% | Gross Pay |
| **Total** | **4.00%** | |

- Employees earning ≤ INR 21,000/month gross are eligible
- Employees earning > INR 21,000/month are exempt
- Once gross exceeds INR 21,000 during a contribution period, contributions continue until end of that period (April-September OR October-March)

## ESI Challan / Return
Zoho does not appear to have a dedicated ESI challan screen (unlike TDS which has a "Challans" section). ESI challan filing is via:
1. Reports > Statutory Reports > **ESI Summary** and **ESI Monthly Summary** exports
2. Manual upload to ESIC portal (esic.in)
3. No direct ESIC API integration observed

**No "ESI Challans" section found in nav** — only EPF ECR Report and ESI Summary reports exist.

## Employee-Level ESI Identification
- **ESI IP Number (Insurance Policy Number)**: Stored per employee in Statutory Information section
- This field appears only when ESI is enabled at org level
- ESIC assigns an IP number when employee is registered for the first time
- IP number is required for ESIC portal submissions

## Government Portal Integration
- No direct ESIC API integration observed
- Employer uses Zoho's ESI Summary reports to prepare ESIC portal submissions
- ESIC Employer portal: https://www.esic.in

## Statutory Rules Referenced
- ESI Act 1948
- ESIC notification on contribution rates (0.75% + 3.25%)
- Wage ceiling: INR 21,000/month gross
- Contribution periods: April-September (H1), October-March (H2)
- Mandatory for organisations with 10+ employees
- Applicable to employees earning ≤ INR 21,000/month

## Cross-Module Dependencies
- Employee Statutory Information (ESI IP Number field — appears only when ESI enabled)
- Reports > Statutory Reports > ESI Summary (monthly aggregate)
- Reports > Statutory Reports > ESI Monthly Summary (per-employee monthly detail)
- Pay Run > Statutory Summary (shows ESI deductions when configured)
- Employee eligibility: gross salary ≤ INR 21,000 (must be calculated post salary structure)

## Key Observations for Our Build
1. **ESI Number format**: `00-00-000000-000-0000` (17 characters with dashes) — validate this regex in FluentValidation.
2. **Both rates are fixed** — 0.75% employee, 3.25% employer — store in DB statutory config table but treat as read-only in UI.
3. **INR 21,000 wage ceiling is per-month gross** — our engine needs to check gross pay monthly, not annual. Ceiling applies to gross (includes all earnings, not just basic).
4. **Contribution period rule**: Once eligible, employee contributes for the entire half-year period even if salary revision pushes them above INR 21,000 mid-period. Our engine must implement period-based eligibility, not monthly recalculation.
5. **No ESI challan in Zoho** — they rely on reports for ESIC portal submission. We should similarly produce the ESI Summary in ESIC-compatible format.
6. **Test org note**: None of our 5 test employees are ESI-eligible — all have gross > INR 21,000. Need a sub-₹21k employee to test ESI deduction end-to-end.

## Screenshots
- `screenshots/esi-configuration-form.png` — ESI configuration form
