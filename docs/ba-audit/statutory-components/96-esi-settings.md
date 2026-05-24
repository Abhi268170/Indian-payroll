# Item 96: ESI Settings — Employees' State Insurance Configuration

**URL:** `https://payroll.zoho.in/#/settings/statutory-details/edit-esi-details`  
**Entry Points:**
- Settings > Statutory Components > ESI tab → "Enable ESI" link (when not configured)
- Direct URL when re-editing after enabling

**Module:** Settings > Statutory Components  
**State in lerno org:** Not configured (onboarding state)

---

## Screenshots

- `screenshots/96-esi-config-form.png` — ESI configuration form

---

## Onboarding State (Not Configured)

**URL:** `#/settings/statutory-details/list/esi`

Displayed when ESI is not yet enabled:
- Heading: "Are you registered for ESI?"
- Body: "Organisations having 10 or more employees must register for Employee State Insurance (ESI). This scheme provides cash allowances and medical benefits for employees whose monthly salary is less than ₹21,000."
- CTA link: "Enable ESI" → navigates to `#/settings/statutory-details/edit-esi-details`

**Business Rule:** ESI registration is mandatory for organisations with 10+ employees (ESI Act, 1948).

---

## Configuration Form — "Employees' State Insurance"

### Data Fields

| Field | Type | Required | Default | Editable | Validation | Notes |
|-------|------|----------|---------|----------|------------|-------|
| ESI Number | Text | Yes | Empty | Yes | Format: `00-00-000000-000-0000` | 17-character format with hyphens |
| Deduction Cycle | Text (disabled) | N/A | Monthly | No | N/A | Always Monthly; not configurable |
| Employees' Contribution | Text (disabled) | N/A | 0.75% | No | N/A | Hardcoded per statute — of Gross Pay |
| Employer's Contribution | Text (disabled) | N/A | 3.25% | No | N/A | Hardcoded per statute — of Gross Pay |

**ESI Number Format:** `00-00-000000-000-0000`
- 17-character code issued by ESIC regional office upon registration

**Key observation:** Both contribution rates (0.75% employee, 3.25% employer) are read-only. Unlike EPF where employer rate has a dropdown, ESI rates are statutory constants — not configurable.

### Checkbox

| Checkbox | Default | Notes |
|----------|---------|-------|
| Include employer's contribution in employee's salary structure | Unchecked | Adds ESI employer share (3.25%) to CTC structure |

### Statutory Note (displayed in form)

> "ESI deductions will be made only if the employee's monthly salary is less than or equal to ₹21,000. If the employee gets a salary revision which increases their monthly salary above ₹21,000, they would have to continue making ESI contributions till the end of the contribution period in which the salary was revised (April-September or October-March)."

This note encodes two important statutory rules:
1. **Eligibility ceiling:** ₹21,000 gross salary/month. Above this, ESI not applicable.
2. **Contribution period rule:** Employees who cross ₹21,000 mid-period must continue contributing until the end of that contribution period (Apr–Sep or Oct–Mar).

---

## Actions

| Action | Type | Behavior |
|--------|------|----------|
| Enable | Button (primary) | Saves ESI configuration and enables ESI for the organisation |
| Cancel | Link | Returns to `#/settings/statutory-details/list/esi` without saving |

---

## Business Rules

1. ESI mandatory for organisations with 10+ employees (ESI Act, 1948).
2. Employee contribution = 0.75% of gross pay (hardcoded, per statute).
3. Employer contribution = 3.25% of gross pay (hardcoded, per statute).
4. Total ESI contribution = 4.00% of gross pay.
5. Eligibility ceiling = ₹21,000 gross salary/month. Employees above this threshold are exempt.
6. Contribution period: April–September and October–March (semi-annual).
7. Mid-period salary revision above ₹21,000: employee continues ESI until end of current contribution period.
8. ESI deductions are on Gross Pay, NOT on PF wage (unlike PF which uses a configurable PF wage base).
9. Deduction cycle is always Monthly.

---

## ESI vs EPF Configuration Differences

| Aspect | EPF | ESI |
|--------|-----|-----|
| Contribution rates | EPF: fixed; EPS cap configurable via employer rate dropdown | Both rates fully hardcoded, no UI configuration |
| Wage ceiling | PF wage ceiling configurable (restrict to ₹15,000) | Eligibility ceiling (₹21,000) is statutory, no config |
| Wage base | Configurable PF wage | Gross pay only |
| Contribution periods | Monthly | Monthly deduction, but semi-annual contribution period for eligibility |
| Override at employee level | Checkbox available | No per-employee override |

---

## Statutory References

- Employees' State Insurance Act, 1948
- ESI (Central) Rules, 1950
- ESIC notification on contribution rates (0.75%/3.25% effective from July 2019)
- Wage ceiling ₹21,000: ESIC notification 2016

---

## Data Relationships

- ESI config → Org (one-to-one): one ESI config per org
- ESI config → Employee (one-to-many): applied to all employees with salary ≤ ₹21,000
- ESI config → Pay Run: ESI deductions computed per run for eligible employees
- ESI config → Salary Structure: employer contribution included/excluded from CTC based on checkbox

---

## Open Questions

- [ ] How is "gross pay" defined for ESI wage base — does it include all allowances or only specific components?
- [ ] Is the ₹21,000 ceiling checked against monthly gross or the configured ESI wage?
- [ ] What happens to ESI for an employee who crosses ₹21,000 mid-pay-run (e.g., one-time payout pushes gross over limit)?
- [ ] Can ESI be enabled after payroll has already been processed (retroactive enrollment)?
- [ ] Is the ESI Number validated (format check only, or API check with ESIC)?
