# Compliance > EPF — ECR Challan Generation

## URL / Navigation Path
- Settings > Setup & Configurations > Statutory Components > EPF tab
- URL (list): `#/settings/statutory-details/list`
- URL (enable/edit): `#/settings/statutory-details/edit-epf-details`

## Purpose
Configure the organisation's EPF registration and contribution rules. Once configured, monthly pay runs generate EPF deductions per employee. The ECR (Electronic Challan cum Return) file is downloadable from Reports > Statutory Reports > EPF ECR Report.

## Current State in Test Org
EPF is **NOT configured**. The list page shows the prompt:
> "Are you registered for EPF? Any organisation with 20 or more employees must register for the Employee Provident Fund (EPF) scheme, a retirement benefit plan for all salaried employees."
>
> CTA: "Enable EPF" → `#/settings/statutory-details/edit-epf-details`

## EPF Configuration Form Fields

The "Employees' Provident Fund" form (`edit-epf-details`) captures:

| Field | Type | Required | Default / Options | Notes |
|-------|------|----------|-------------------|-------|
| EPF Number | Text | Yes | Placeholder: `AA/AAA/0000000/XXX` | Format hint shown below field. Regional PF Office code / establishment ID |
| Deduction Cycle | Text (disabled) | N/A | Monthly | Read-only; always Monthly for EPF |
| Employee Contribution Rate | Dropdown (disabled) | N/A | `12% of Actual PF Wage` | Read-only; statutory rate is fixed |
| Employer Contribution Rate | Dropdown | Yes | `12% of Actual PF Wage` | Editable; alternate options not visible (dropdown not expanded in test) |
| Include employer's contribution in employee's salary structure | Checkbox | No | Checked (default) | When checked, employer EPF is shown in CTC breakup |
| Include employer's EDLI contribution in employee's salary structure | Checkbox | No | Unchecked | EDLI = Employees' Deposit Linked Insurance |
| Include admin charges in employee's salary structure | Checkbox | No | Unchecked | EPF admin charges (~0.5% of PF wage) |
| Override PF contribution rate at employee level | Checkbox | No | Unchecked | When checked, allows per-employee rate override |
| PF Configuration when LOP Applied — Pro-rate Restricted PF Wage | Checkbox | No | Unchecked | Pro-rates PF wage based on days worked |
| PF Configuration when LOP Applied — Consider all applicable salary components if PF wage < ₹15,000 after LOP | Checkbox | No | Checked (default) | Uses actual earned amount instead of structure amount when LOP causes wage < ₹15,000 |

### Actions
| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Enable | Button | EPF Number entered | Saves EPF config; enables EPF deductions in future pay runs |
| Cancel | Link | — | Returns to `#/settings/statutory-details/list` |
| Preview EPF Calculation | Button | — | Opens a preview modal showing EPF calculation for configurable scenarios |

## Sample EPF Calculation (shown on form)
Zoho shows a live reference calculation on the form. Assuming PF Wage = INR 20,000:

**Employee's Contribution:**
- EPF: 12% of 20,000 = INR 2,400

**Employer's Contribution:**
- EPS: 8.33% of 20,000 (Max of INR 15,000) = INR 1,250 (capped at ₹15,000 wage ceiling)
- EPF: 12% of 20,000 − EPS = INR 1,150
- Total Employer = INR 2,400

**Key statutory rules embedded in UI:**
- Employee contribution: 12% of actual PF Wage (no ceiling mentioned in UI; ceiling is INR 15,000 statutory but employee can contribute on higher wage)
- Employer EPS: 8.33% capped at INR 15,000 wage base → max INR 1,250/month
- Employer EPF: Employer 12% minus EPS portion
- Admin charges: ~0.5% of PF wage (shown as checkbox option to include in CTC)
- EDLI: ~0.5% of PF wage (shown as checkbox option to include in CTC)

## ECR File (Electronic Challan cum Return)

Available via: Reports > Statutory Reports > **EPF ECR Report**

The ECR Report is listed as a system-generated report. Format details (column structure) are documented in the report audit. Zoho generates the file in EPFO's mandated text/CSV format for upload to the **Unified Portal (https://unifiedportal-emp.epfindia.gov.in)**.

**No direct EPFO portal integration observed** — Zoho generates the file for manual upload by the employer.

## Government Portal Integration
- No direct API integration with EPFO observed
- File generated locally; employer uploads to EPFO Unified Portal manually
- UAN seeding: UANs entered per-employee in Statutory Information section (enabled only when EPF is configured)

## Statutory Rules Referenced
- EPF & MP Act 1952
- EPS 1995 (8.33% employer contribution capped at ₹15,000 wage)
- EDLI 1976
- Mandatory for organisations with 20+ employees
- Threshold: once registered, EPF applies even if headcount drops below 20
- Voluntary registration possible below 20

## Cross-Module Dependencies
- Employee Statutory Information (UAN field) — appears only after EPF enabled
- Pay Run > Variable Inputs — LOP affects PF wage when "Consider all applicable…" option is set
- Reports > Statutory Reports > EPF Summary, EPF ECR Report
- Payslip shows EPF deduction + employer contribution lines when EPF is configured

## Key Observations for Our Build
1. **EPF Number format**: `AA/AAA/0000000/XXX` — region code / sub-code / establishment ID / extension. Must validate this format on input.
2. **Employer contribution rate dropdown**: Zoho shows options — investigate whether "10% of Actual PF Wage" (for lower headcount exemptions) is an option.
3. **Deduction Cycle is always Monthly** — no configurability needed in our EPF entity.
4. **"Include in salary structure" toggles**: These drive whether employer-side contributions appear in CTC display. Our salary structure calculation must support showing/hiding these components.
5. **LOP + PF interaction**: Two distinct behaviours — (a) pro-rate the restricted PF wage, (b) fall back to earned amount when wage drops below ₹15,000 threshold. Both must be implemented in the engine.
6. **EDLI and Admin charges**: Separate statutory amounts that Zoho optionally includes in CTC. Our engine must calculate these separately from EPF/EPS.
7. **ECR file**: EPFO mandated format — we need to replicate this exactly. Column order matters.

## Screenshots
- `screenshots/statutory-components-epf-not-configured.png` — EPF not-configured state
- `screenshots/epf-configuration-form.png` — Full EPF configuration form
