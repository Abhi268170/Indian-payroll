# Settings > Module Settings > Leave & Attendance

## URL
`#/settings/holiday-leave/enable-module`

## Purpose
Configure the Leave and Attendance module, which integrates attendance cycle and leave management with payroll (for LOP/Loss-of-Pay calculations).

## Current State
**Blocked — Pay Schedule prerequisite not met.**

### Prerequisite Notice
> "Configure Your Pay Schedule. To access the Leave and Attendance module, go to Settings > Pay Schedule and configure your Pay Schedule now. Once configured, you can define the Attendance Cycle, and other similar configurations."

**Button:** "Configure Pay Schedule" → navigates to `#/settings/pay-schedule`

---

## Expected Configuration (once Pay Schedule is set)

Based on the prerequisite message and known Zoho Payroll structure, the Leave & Attendance settings would include:
- **Attendance Cycle** — defines the start/end dates of the attendance period mapped to payroll
- **Payroll Report Generation Day** — when attendance data is frozen for payroll processing
- Leave types, holiday lists, and LOP rules

---

## Business Rule
Leave & Attendance module **requires a configured Pay Schedule** (work week + pay date) before it can be accessed. This prevents mis-configured attendance cycles that don't align with payroll periods.

## Cross-Module Impact
| Setting | Impacts |
|---------|---------|
| Attendance Cycle | Determines which attendance days count for each payroll period |
| LOP calculation | Absent days reduce salary based on Salary Calculation Method (Actual Days / Fixed Days) |
| Leave types | Define paid/unpaid leaves; unpaid = LOP deduction |

## Observations & Notes
1. **Dependency chain:** Pay Schedule → Attendance Cycle → Leave & Attendance → LOP in Payroll.
2. **Leave module not enabled** for "lerno" — no employees have leave policies configured.
3. For our build: Attendance cycle is a tenant-level config: start_day, end_day per month. LOP deduction formula: (Monthly Salary / Payable Days) × LOP Days.

## Screenshots
`docs/ba-audit/settings/screenshots/27-leave-attendance.png`
