# Giving > Features — Campaign Creation & Management

## URL / Navigation Path

- **New Campaign form:** `#/donations/new` (routing bug — redirects to `#/loans/new`)
- **Form accessed via:** Ember `transitionTo('donations.new')` also broken; form accessible by clicking "New Campaign" button while on `#/donations`
- **Campaign detail:** `#/donations/{id}` (sub-routes: `pledged-employees`, `contributed-employees`)

## Purpose

Documents the complete feature set of the Giving module, including campaign creation, employee participation flow, exemption handling, and payroll integration.

---

## New Campaign Form

**Accessed via:** "New Campaign" button or "New" button on the Giving list page.

### Fields

| Field | Type | Required | Options / Validation | Notes |
|-------|------|----------|---------------------|-------|
| Campaign Name | Text | Yes | Free text | API field: `donation_name`. Validated server-side; returns `code: 4` if invalid |
| About the Campaign | Textarea | Yes | Free text, multi-line | API field: `description`. Campaign description shown to employees |
| Exemption Type | Dropdown/Combobox | Yes | See options below | API field: `exemption_type`. Drives IT calculation |
| Campaign Ends on | Date (Month/Year picker) | Yes | Placeholder: `M yyyy` (month-year format) | API field: `donation_end_date`. Exact API date format unconfirmed — standard `yyyy-mm-dd` rejected |
| Show in Employee Portal | Checkbox | No (default: unchecked assumed) | Toggle | API field: `show_in_portal` (boolean). Controls employee portal visibility |

### Exemption Type Options

Source: `GET /api/v1/donations/editpage`

| Display Label | API Value | Tax Treatment |
|--------------|-----------|--------------|
| Section 133 - Donation eligible for 100% exemption | `donation_100_percent_exemption` | Full 80G deduction applied |
| Section 133 - Donation eligible for 50% exemption | `donation_50_percent_exemption` | 50% 80G deduction applied |
| None - No Exemption Applicable | `none` | No IT exemption; deducted as standard contribution |

**Note:** Zoho labels these as "Section 133" internally but this maps to Income Tax Act **Section 80G** (charitable donations). The `editpage` API call (`GET /api/v1/donations/editpage`) returns these options, confirming they are configuration-driven.

### "Things to Note" Panel

A help panel on the form displays two key notes:
1. "All contributions made by your employees will be considered for exemptions based on the Exemption Type selected and will be applied in their Income Tax calculations and Form 16"
2. "Based on your employees' contribution, liability will be raised, and you can pay the amount deducted using their PAN."

**Implication for payroll build:** Donation deductions:
- Flow into TDS calculation (reduces taxable income)
- Appear on Form 16 under 80G deductions
- Create a liability entry (amount collected must be remitted to charity)
- Payment is tracked against the campaign administrator's PAN

### Form Actions

| Button | Action | Notes |
|--------|--------|-------|
| Save | POST `/api/v1/donations` | Creates the campaign |
| Cancel | Navigate to `#/donations` | Discards form |
| Close (X icon) | Navigate to `#/donations` | Discards form |

### Validation Errors Observed

| Scenario | Error Code | Message |
|----------|-----------|---------|
| Missing/invalid `donation_name` | 4 | "Invalid value passed for donation_name" |
| Invalid `end_date` format | 2 | "Invalid value passed for donation_end_date" |
| Wrong field name for end date | 6 | "Invalid data provided" |

---

## Campaign List View (Populated State)

When campaigns exist, the list likely shows:
- Campaign name
- End date
- Status (Active / Completed)
- Actions (Edit, Delete, View Details)

*(Not observable — no campaigns created. Inferred from route structure.)*

---

## Campaign Detail View

**Route:** `donations.details` with sub-routes:
- `donations.details.pledged-employees` — employees who opted in/pledged
- `donations.details.contributed-employees` — employees whose payroll deductions were processed

**Implication:** Two-stage employee participation tracked:
1. **Pledge:** Employee declares intent to donate (through Employee Portal)
2. **Contribution:** Actual payroll deduction processed during pay run

---

## Employee Portal Integration

- **"Show in Employee Portal" toggle** on campaign creation controls visibility in employee-facing portal.
- Employees can view active campaigns and opt in (pledge).
- Deduction amount is likely set by the employee at pledge time (not by admin).
- Employee portal has its own view of their donations — not audited in this session.

---

## Payroll Integration

1. Campaign deductions appear as a deduction component in the payroll run.
2. Exemption type drives IT computation: amount reduces taxable income per selected 80G clause.
3. Form 16 includes 80G deduction detail from campaign contributions.
4. A "liability" is raised on the organization for the collected amount — to be remitted to the charity.

---

## Giving Reports

| Report | Route | Content |
|--------|-------|---------|
| Employee Donation Summary | `reports.employee-donation-summary` | Per-employee donation totals |
| Employee Donation Details | `reports.employee-donation-details` | Transaction-level donation ledger |

---

## Buttons & Actions (Complete List)

| Action | Location | Trigger | Outcome |
|--------|----------|---------|---------|
| New Campaign | List empty state | Click | Opens New Campaign form |
| New | List header | Click | Opens New Campaign form (same) |
| Active Campaigns dropdown | List header | Click | Opens filter dropdown (All / Active / Completed) |
| Save | New Campaign form | Click | POST to `/api/v1/donations`, creates campaign |
| Cancel | New Campaign form | Click | Returns to list |
| Close (X) | New Campaign form | Click | Returns to list |

---

## Cross-Module Impact

| Module | Impact |
|--------|--------|
| Pay Runs | Donation deductions appear as line items in payroll |
| TDS / Income Tax | 80G deduction reduces employee taxable income |
| Form 16 | Section 80G donation breakdown appears |
| Employee Portal | Campaign visibility controlled by "Show in Employee Portal" toggle |
| Reports | Two dedicated donation reports available |

---

## Key Observations for Our Build

1. **80G compliance is baked in** — the exemption types map directly to statutory 80G categories. Our implementation must validate that the selected exemption type is applied correctly in the tax engine.

2. **Month-year granularity for campaign end date** — campaigns run for a month (aligned to payroll cycle), not arbitrary date ranges. Our date picker should use month-year only.

3. **API field naming convention** — Zoho uses `donation_name` (not just `name`), `donation_end_date` (not `end_date`). Follow this pattern for our own API design to avoid confusion when integrating.

4. **Liability tracking is explicit** — Zoho requires remittance of collected donations to charity via the organization's PAN. We need a payables ledger entry per campaign per payroll run.

5. **Two-stage participation model** — Pledge (intent) is separate from Contribution (actual deduction). Build both status states: `pledged` and `contributed`.

6. **No NGO verification or PAN validation** — Zoho does not validate the charity's PAN or 80G registration. Our build should flag this as a compliance gap — we may want to at minimum capture and store the charity PAN for Form 16 accuracy.

7. **Routing bug in Zoho** — `donations.new` route redirects to `loans/new`. This is a critical UX bug in their Ember app that we should not replicate. Our router implementation must ensure no cross-module route contamination.
