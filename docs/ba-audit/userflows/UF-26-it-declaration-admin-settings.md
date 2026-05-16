# UF-26: IT Declaration — Admin Settings (Open Declaration Window)

**Module:** Settings → Setup & Configurations → Claims and Declarations → Income Tax Declaration
**Tested:** 2026-05-16

## Steps Executed
1. Navigated to `#/settings/preferences/it-declaration`
2. Observed Claims and Declarations settings page
3. Identified IT Declaration tab (active) showing locked state
4. Observed "Other Configurations" section
5. Captured screenshot

## Page Layout

**Settings page title:** "Claims and Declarations"
**Sidebar location:** Settings → Setup & Configurations → Claims and Declarations

**Tab navigation within page:**
| Tab | Status |
|-----|--------|
| Flexible Benefit Plan | Not active |
| Reimbursement Claims | Not active |
| Income Tax Declaration | Active (underlined) |
| Proof Of Investments | Not active |

## IT Declaration — Locked State

### Status Display
- Illustration image (animated figure)
- Large text: **"IT Declaration is Locked"**
- Sub-text: "You are yet to enable the submission of IT Declaration for your employees through their respective portals. Release IT Declaration or submit it on their behalf under Employees > Employee profile > Investments > IT Declaration."

### CTA Button
| Button | Label | Action |
|--------|-------|--------|
| Primary action | "Release IT Declaration" | Opens IT declaration for all employees to submit via portal |

**Behavioral note:** "Release IT Declaration" enables the declaration window org-wide — all employees with portal access can then submit their IT declarations. This is a global toggle, not per-employee.

## Other Configurations Section

Two boolean settings below the locked state card:

| Setting | Type | Current Value | Notes |
|---------|------|---------------|-------|
| Allow employees to switch tax regimes | Checkbox | Checked (enabled) | If enabled, employees can switch between old and new regime on their declaration form |
| Allow TDS modification to exceed the current fiscal year's calculated tax amount | Checkbox | Unchecked (disabled) | Tooltip (?) present — advanced setting; allows manual TDS override beyond computed liability |

### Save Button
"Save" button at bottom — saves the "Other Configurations" checkboxes.

## Employee-Side View — Investments Tab (Locked State)

When IT Declaration is locked (not released), the employee's Investments tab shows:

- **Heading:** "IT Declaration submission is locked for this employee"
- **Sub-text:** "You can either allow the employee to submit IT Declaration through the portal or submit it on their behalf"
- **Button:** "Submit Declaration" (admin submits on employee's behalf, bypassing portal lock)

**Two paths when locked:**
1. **Release globally** via Settings → Claims and Declarations → Release IT Declaration
2. **Submit on behalf** via Employees → {Employee} → Investments → Submit Declaration

## IT Declaration Lifecycle (Admin View)

```
[Locked] 
    → "Release IT Declaration" → [Open for Employee Submission]
    → "Submit Declaration" (on behalf) → [Admin-submitted Declaration]
[Open for Employee Submission]
    → Employee submits via portal → [Submitted / Pending Admin Review]
    → Admin locks again (via "Lock IT Declaration") → [Locked]
[Submitted]
    → POI window opens → Employee uploads proofs → [POI Submitted]
    → Admin approves/rejects → [Approved / Rejected]
    → TDS finalized based on approved declaration
```

## Business Rules

1. **Global lock/release** — IT Declaration open/close is org-wide, not per-employee (unless admin submits on behalf individually)

2. **Tax regime switching** — If "Allow employees to switch tax regimes" is checked, employees see regime choice on their declaration form. Per CLAUDE.md, v1 is new regime only — this setting may conflict with v1 scope.

3. **TDS modification cap** — The unchecked "Allow TDS modification to exceed..." setting means TDS cannot be manually set above the system-computed liability. This is a compliance safeguard.

4. **Annual workflow** — IT Declaration is typically opened once per FY (April–June for declarations, Jan–March for POI). The system appears to support multiple open/close cycles per year.

5. **Fiscal year filter** — Employee's Investments tab shows "Period: 2026-27" dropdown — declarations are fiscal-year specific.

## Data Relationships
- IT Declaration settings → all employees in org (global)
- IT Declaration → Employee (M:1)
- IT Declaration → Fiscal Year (M:1)
- IT Declaration → Tax Regime selection (per employee)

## Navigation
- Entry: Settings sidebar → Setup & Configurations → Claims and Declarations → Income Tax Declaration tab
- URL: `#/settings/preferences/it-declaration`
- "Release IT Declaration" → changes state; button becomes "Lock IT Declaration"
- Employee path: `#/employees/{id}/investments` → IT Declaration sub-tab

## Screenshots
- [IT Declaration settings — locked state](../screenshots/UF-26-IT-declaration-settings.png)
- [Employee Investments tab — locked state](../screenshots/UF-26-investments-locked.png)

## Gaps / Observations
- 🔴 "Allow employees to switch tax regimes" is ENABLED — but v1 product constraint is new regime only. This setting should be disabled or hidden in v1 to prevent old-regime declarations.
- No per-employee lock/release — only global release or per-employee admin submission. Cannot release for subset of employees.
- No visible "last released on" / "last locked on" timestamps
- No email notification trigger visible — when released, do employees get notified? Not confirmed.
- Tooltip on "Allow TDS modification to exceed..." not captured — content unknown
- No visible audit trail for who released/locked the declaration window
