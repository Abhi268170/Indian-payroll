# Item 95: EPF Settings — Employees' Provident Fund Configuration

**URL:** `https://payroll.zoho.in/#/settings/statutory-details/edit-epf-details`  
**Entry Points:**
- Settings > Statutory Components > EPF tab → "Enable EPF" link (when not configured)
- Direct URL when re-editing after enabling

**Module:** Settings > Statutory Components  
**State in lerno org:** Not configured (onboarding state)

---

## Screenshots

- `screenshots/95-epf-not-configured.png` — EPF onboarding CTA state
- `screenshots/95-epf-config-form.png` — EPF configuration form (full)
- `screenshots/95-epf-employer-splitup.png` — Employer contribution splitup panel

---

## Onboarding State (Not Configured)

**URL:** `#/settings/statutory-details/list`

Displayed when EPF is not yet enabled:
- Heading: "Are you registered for EPF?"
- Body: "Any organisation with 20 or more employees must register for the Employee Provident Fund (EPF) scheme, a retirement benefit plan for all salaried employees."
- CTA link: "Enable EPF" → navigates to `#/settings/statutory-details/edit-epf-details`

**Business Rule:** EPF registration is mandatory for organisations with 20+ employees (Employees' Provident Funds and Miscellaneous Provisions Act, 1952).

---

## Configuration Form — "Employees' Provident Fund"

### Data Fields

| Field | Type | Required | Default | Editable | Validation | Notes |
|-------|------|----------|---------|----------|------------|-------|
| EPF Number | Text | Yes | Empty | Yes | Format: `AA/AAA/0000000/XXX` (shown as placeholder) | Format hint displayed below field |
| Deduction Cycle | Text (disabled) | N/A | Monthly | No | N/A | Always Monthly; not configurable |
| Employee Contribution Rate | Dropdown (disabled) | N/A | 12% of Actual PF Wage | No | N/A | Hardcoded per statute |
| Employer Contribution Rate | Dropdown | Yes | 12% of Actual PF Wage | Yes | 2 options | See options below |

**EPF Number Format:** `AA/AAA/0000000/XXX`
- Part 1: 2-letter state code
- Part 2: 3-letter establishment code
- Part 3: 7-digit establishment number
- Part 4: 3-character suffix (extension)

**Employer Contribution Rate Options:**
1. `12% of Actual PF Wage` — no cap, full 12% on actual wage
2. `Restrict Contribution to ₹15,000 of PF Wage` — employer contribution calculated on PF wage capped at ₹15,000

**"View Splitup" button** (next to Employer Contribution Rate label):
- Opens an inline panel showing the employer contribution breakdown:
  - EPS (Employees' Pension Scheme): 8.33% of PF Wage (max ₹15,000 wage ceiling → max ₹1,250)
  - EPF (Employers' Provident Fund): 12% of PF Wage − EPS amount = 3.67% of PF Wage
- Panel title: "CONTRIBUTION RATE — SUB COMPONENTS | EMPLOYER'S CONTRIBUTION"

### Checkboxes

| Checkbox | Default | Notes |
|----------|---------|-------|
| Include employer's contribution in employee's salary structure | Checked | Adds EPF employer share to CTC structure |
| Include employer's EDLI contribution in employee's salary structure | Unchecked | EDLI = Employees' Deposit Linked Insurance |
| Include admin charges in employee's salary structure | Unchecked | PF admin charges (currently 0.50% per EPFO circular) |
| Override PF contribution rate at employee level | Unchecked | Allows per-employee rate override |

### PF Configuration when LOP Applied

Two sub-options for handling LOP (Loss of Pay):

| Checkbox | Default | Notes |
|----------|---------|-------|
| Pro-rate Restricted PF Wage | Unchecked | PF contribution pro-rated based on number of days worked. Label: "Restricted PF Wage. PF contribution will be pro-rated based on the number of days worked by the employee." |
| Consider all applicable salary components if PF wage is less than ₹15,000 after Loss of Pay | Checked | "PF wage will be computed using the salary earned in that particular month (based on LOP) rather than the actual amount mentioned in the salary structure." |

---

## Sample EPF Calculation (Right Panel)

Inline sample shown during configuration (PF wage = ₹20,000):

**Employee's Contribution:**
| Component | Calculation | Amount |
|-----------|-------------|--------|
| EPF | 12% of ₹20,000 | ₹2,400 |

**Employer's Contribution:**
| Component | Calculation | Amount |
|-----------|-------------|--------|
| EPS | 8.33% of ₹20,000 (max ₹15,000) | ₹1,250 |
| EPF | 12% of ₹20,000 − EPS | ₹1,150 |
| **Total** | | **₹2,400** |

**Key observation:** Total employer contribution = ₹2,400 (same as employee). EPS max is ₹1,250/month (8.33% × ₹15,000).

**"Preview EPF Calculation" button:** Opens interactive calculator for testing configurations with multiple scenarios before enabling.

---

## Actions

| Action | Type | Behavior |
|--------|------|----------|
| Enable | Button (primary) | Saves EPF configuration and enables EPF for the organisation |
| Cancel | Link | Returns to `#/settings/statutory-details/list` without saving |
| View Splitup | Button (inline) | Opens employer contribution breakdown panel |
| Preview EPF Calculation | Button | Opens interactive multi-scenario EPF calculator |

---

## Business Rules

1. EPF mandatory for organisations with 20+ employees (EPF&MP Act, 1952).
2. Employee contribution = 12% of PF wage (hardcoded, per statute).
3. Employer contribution split: EPS = 8.33% of PF wage (capped at ₹15,000 wage); EPF = 12% − EPS.
4. EPS ceiling: 8.33% × ₹15,000 = ₹1,250/month maximum.
5. EDLI contribution: 0.5% of PF wage (capped at ₹15,000) — separate from EPF/EPS.
6. Admin charges: currently 0.50% of PF wage (EPFO circular — subject to change).
7. Deduction cycle is always Monthly — no flexibility.
8. LOP configuration affects how PF wage is computed when employee has loss of pay days.
9. Override at employee level (checkbox) enables per-employee PF rate customisation — useful for international workers or voluntary higher contributions.

---

## Statutory References

- Employees' Provident Funds and Miscellaneous Provisions Act, 1952
- EPS: Employees' Pension Scheme 1995 (8.33% employer share to pension fund)
- EDLI: Employees' Deposit Linked Insurance Scheme 1976
- EPF wage ceiling for EPS: ₹15,000/month (EPFO notification)

---

## Data Relationships

- EPF config → Org (one-to-one): one EPF configuration per organisation
- EPF config → Employee (one-to-many): all employees inherit org-level EPF settings unless override enabled
- EPF config → Salary Structure: employer contributions included/excluded from CTC based on checkboxes
- EPF config → Pay Run: EPF deductions computed per pay run using this config

---

## Post-Enable State (Expected)

After "Enable" is clicked:
- EPF tab on Statutory Components shows configured state (PT Number, Deduction Cycle, rates displayed)
- EPF deductions appear in salary structures for eligible employees
- ECR (Electronic Challan cum Return) generation available under Compliance/Reports
- UAN (Universal Account Number) field becomes visible on Employee Profile

---

## Open Questions

- [ ] What happens to employees added before EPF was enabled — are they retroactively enrolled?
- [ ] Can EPF be disabled after it's been enabled (once pay runs have processed EPF)?
- [ ] Is the EPF Number validated against EPFO registry (API check) or just format-validated?
- [ ] What does "Override PF contribution rate at employee level" reveal on the employee profile?
- [ ] Does "Include employer's contribution in CTC" affect the gross salary display or just the CTC figure?
- [ ] Is there a PF wage ceiling configuration (₹15,000) for the employee contribution side?
