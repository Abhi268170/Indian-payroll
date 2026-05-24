# Settings > Claims and Declarations

## URL
`#/settings/preferences/fbp` (default tab)

Sub-routes (tabs):
- `#/settings/preferences/fbp` — Flexible Benefit Plan
- `#/settings/preferences/reimbursement` — Reimbursement Claims
- `#/settings/preferences/it-declaration` — IT Declaration
- `#/settings/preferences/proof-of-investment` — Proof of Investments

## Purpose
Configures employee self-service workflows for Flexible Benefit Plans, reimbursement claims, income tax declarations, and proof of investment submissions. Controls when employees can submit declarations and whether overrides are allowed.

## Page Layout
Four-tab page within Settings. Each tab is independently configurable. Right sidebar has Quick Links to FBP, Reimbursement Claims, Income Tax Declaration, Proof of Investments.

---

## Tab 1: Flexible Benefit Plan (FBP)

### URL
`#/settings/preferences/fbp`

### Current State
Empty state — no FBP components configured.

### Empty State Message
> "No Active FBP component. Your organisation does not have an active FBP component associated to an employee. Mark a reimbursement as FBP component under Settings > Salary Components > Reimbursements and associate it to the employee's salary."

### Business Rule
FBP tab becomes functional only after:
1. A Reimbursement component is marked as FBP-eligible in Salary Components > Reimbursements
2. That component is associated to at least one employee's salary structure

### Cross-Module Dependency
- Settings > Salary Components > Reimbursements (mark as FBP component)
- Employee salary structure assignment

---

## Tab 2: Reimbursement Claims

### URL
`#/settings/preferences/reimbursement`

### Current State
Empty state — no active reimbursement components configured.

### Empty State Message
> "No Active Reimbursement. Employees can get tax exemptions on producing necessary bills."

### Business Rule
Reimbursement Claims tab becomes functional only after active reimbursement components exist (configured in Salary Components > Reimbursements with max amount > 0 and associated to employees).

### Cross-Module Dependency
- Settings > Salary Components > Reimbursements (must have active components with configured max amounts)
- Employee salary structure assignment

---

## Tab 3: IT Declaration

### URL
`#/settings/preferences/it-declaration`

### Current State
IT Declaration is Locked.

### Lock/Unlock Control
| Element | Type | State | Action |
|---------|------|-------|--------|
| "IT Declaration is Locked" | Status banner | Locked state | Informational |
| Release IT Declaration | Button | Always visible | Unlocks IT declaration window — allows employees to submit/edit tax declarations via portal |

### Other Configurations Section

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Allow employees to switch tax regimes | Checkbox | No | (Unknown — not confirmed) | Enables employees to change between Old and New tax regime in portal |
| Allow TDS modification to exceed the current fiscal year's calculated tax amount | Checkbox | No | (Unknown — not confirmed) | Allows manual TDS override beyond system-calculated annual TDS |

### Buttons
| Button | Action |
|--------|--------|
| Release IT Declaration | Opens/unlocks the declaration submission window for all employees |
| Save | Saves the Other Configurations checkboxes |

### Business Rules
1. When IT Declaration is Locked, employees CANNOT submit or edit tax declarations via ESS portal.
2. "Release IT Declaration" toggles the declaration window open — typically done at start of financial year or when declaration period begins.
3. "Allow employees to switch tax regimes" — if unchecked, employer controls which regime applies (relevant for v1 new regime only build).
4. "Allow TDS modification to exceed calculated tax" — this is a compliance-risk override; allows payroll admin to input higher TDS than system computes (e.g., for employees with outside income).

### Statutory Notes
- 🔴 "Allow employees to switch tax regimes" is a critical field for New Regime Only builds — must be configurable at tenant level.
- TDS override beyond calculated amount is unusual — typically needed for employees with self-assessment tax obligations.

---

## Tab 4: Proof of Investments (POI)

### URL
`#/settings/preferences/proof-of-investment`

### Current State
POI is Locked.

### Content
> "Employees can submit the necessary supporting documents for their declared investments through the employee portal once you enable this option."

### Lock/Unlock Control
| Element | Type | State | Action |
|---------|------|-------|--------|
| "POI is Locked" | Status banner | Locked state | Informational |
| Release | Button | Always visible | Unlocks POI submission window — allows employees to upload proof documents |

### Business Rules
1. POI window is separate from IT Declaration window — can be opened/closed independently.
2. When POI is locked, employees cannot upload investment proof documents even if IT Declaration is open.
3. Typical workflow: Open IT Declaration → Employees submit declarations → Close IT Declaration → Open POI → Employees upload proof → Close POI → Finalize TDS.

### Statutory Note
POI submission supports Form 16 preparation — proof documents validate 80C/80D/HRA exemption claims declared by employees.

---

## Cross-Module Impact

| Setting | Impacts |
|---------|---------|
| IT Declaration Release | Employees can access IT declaration form in ESS portal |
| IT Declaration Locked | TDS computed based on last submitted declaration (or default) |
| POI Release | Employees can upload proof documents in ESS portal |
| Allow regime switch | Determines if employee can change Old→New or New→Old regime mid-year |
| TDS override permission | Allows payroll admin to set TDS higher than computed amount for individuals |
| FBP configuration | Employees can opt-in/out of FBP components in ESS portal |
| Reimbursement configuration | Employees can submit reimbursement claims in ESS portal |

## Observations & Notes

1. **Declaration lifecycle** — The lock/unlock pattern (IT Declaration + POI separately) maps to a real Indian payroll workflow: companies open declarations at year start, close after a window, then open POI for proof collection before finalizing TDS.
2. **No date-range picker visible** — Both IT Declaration and POI show only a Release button with no start/end date configuration visible. Either dates are set elsewhere or it's a simple on/off toggle.
3. **FBP and Reimbursement tabs are placeholder until salary components are configured** — empty state messages point admins to the right configuration path.
4. **"Allow TDS modification to exceed calculated amount"** — this flag is important for Form 16 accuracy and should be restricted to Admin role only.
5. For our build: Declaration window open/close must be a tenant-level flag per type (IT Declaration vs POI). The regime switch flag maps directly to our v1 new-regime-only constraint — we can default this to disabled.

## Screenshots
- `docs/ba-audit/settings/screenshots/14-claims-declarations-fbp.png`
- `docs/ba-audit/settings/screenshots/14-claims-declarations-it-declaration.png`
- `docs/ba-audit/settings/screenshots/14-claims-declarations-poi.png`
