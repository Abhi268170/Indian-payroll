# UF-16: Employee Statutory Settings — Per-Employee EPF/ESI/PT/LWF Toggles

**Module:** Employees → Employee Profile → Overview → Statutory Information
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta EMP001

## Steps Executed
1. Navigated to Arjun Mehta Overview tab
2. Observed Statutory Information section (read view)
3. Clicked pencil/edit icon on Statutory Information section
4. Observed edit modal/page: "Arjun Mehta's statutory information"
5. Captured screenshot of edit state

## Statutory Information — Read View

Located on Overview tab, below Basic Information section.

| Statutory Item | Status | Action |
|----------------|--------|--------|
| EPF | Disabled (red X) | (Enable) hyperlink |
| ESI | Disabled (red X) | (Enable) hyperlink |
| Professional Tax | Enabled (green checkmark) | (Disable) hyperlink |
| Labour Welfare Fund | Disabled (red X) | (Enable) hyperlink |

**Visual pattern:**
- Enabled items: green checkmark icon + "Enabled" label + "(Disable)" link
- Disabled items: red X icon + "Disabled" label + "(Enable)" link

## Statutory Information — Edit View

**Page title:** "Arjun Mehta's statutory information"
**Layout:** Simple form with 4 checkboxes

| Checkbox | Checked | Notes |
|----------|---------|-------|
| Employees' Provident Fund | No (unchecked) | EPF deduction toggle |
| Employees' State Insurance | No (unchecked) | ESI deduction toggle |
| Professional Tax | Yes (checked, blue) | PT deduction toggle |
| Labour Welfare Fund | No (unchecked) | LWF deduction toggle |

### Actions
| Action | Trigger | Post-behavior |
|--------|---------|---------------|
| Save | Button (primary) | Saves statutory flags; returns to Overview |
| Cancel | Button | Discards changes; returns to Overview |

## Business Rules

1. **Per-employee statutory control** — Each statutory deduction can be enabled/disabled per employee independently. This overrides org-level statutory configuration.

2. **EPF eligibility rule** — EPF can be disabled for employees whose PF wage consistently exceeds ₹15,000 (they are "excluded employees" per EPF Act). However, the system allows any employee to be excluded — no automatic enforcement based on salary threshold.

3. **ESI eligibility rule** — ESI is mandatory for employees earning gross ≤ ₹21,000/month. Employees earning above this threshold should have ESI disabled. System does NOT auto-disable ESI when salary exceeds threshold (potential compliance gap).

4. **PT is always applicable per work location** — PT is based on the employee's work location state. Since Head Office is Kerala, PT is applicable and pre-enabled.

5. **LWF applicability** — LWF is state-specific and optional. Kerala LWF (₹50/month) must be explicitly enabled per employee.

6. **No audit trail visible** — When a statutory flag is toggled, no "changed by / changed on" record is shown in the UI.

## Compliance Observations

- 🔴 **ESI non-auto-enforcement**: Arjun Mehta's gross = ₹70,000/month, which exceeds the ₹21,000 ESI ceiling. ESI is correctly disabled here, but the system does not prevent enabling ESI for high-salary employees — relies on admin judgment.

- 🟡 **EPF exclusion without UAN field**: Arjun Mehta's EPF is disabled. If an employee was previously contributing to EPF (has a UAN), disabling EPF must be accompanied by EPF member exit filing. No UAN capture or exit filing trigger visible in this flow.

- 🟢 **PT auto-enabled correctly**: PT is pre-enabled for Kerala work location — correct default behavior.

## Data Relationships
- Employee → Statutory Flags (1:1 per employee, 4 boolean attributes)
- Statutory Flags → Payroll Run deduction logic (flags read during pay run processing)
- EPF Flag → UAN, PF Member Number (separate fields not documented here)

## Navigation
- Entry: Employee Overview tab → Statutory Information section → pencil icon
- Edit URL: likely `#/employees/{id}/statutory` (exact route not captured)
- Post-save: Returns to Overview tab

## Screenshots
- [Statutory information edit view](../screenshots/UF-19-statutory-edit.png)
- [Arjun Mehta Overview showing statutory section](../screenshots/UF-14-arjun-mehta-overview.png)

## Gaps / Observations
- Edit form has no explanatory text per checkbox — new admin may not know EPF/ESI eligibility rules
- No ESI wage ceiling validation — system allows enabling ESI for high-salary employees
- No EPF UAN field visible in this edit form — UAN capture location not identified
- No "effective from" on statutory flag changes — retroactive impact unclear
- No bulk statutory update UI observed (for updating multiple employees at once)
