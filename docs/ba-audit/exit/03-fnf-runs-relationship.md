# Zoho Payroll — How FnF runs relate to regular Pay Runs

**Audited:** 2026-05-24
**Visual evidence:** `zoho-payruns-list.png`

## 1. Three distinct payroll-run types

Pay Runs → Run Payroll tab exposes filter chips:

| Chip | Description |
| --- | --- |
| All Pending | Sum of below |
| **Final Settlement Payroll** | Single-employee FnF run, payment date = operator's chosen Final Settlement Date |
| **Bulk Termination Payroll** | Multi-employee FnF run, payment date = next regular pay run's pay date |

(There's also the regular monthly run, not pictured under "Pending" since it auto-aligns to the cycle.)

## 2. Mapping — Step 1 "Settlement timing" radio drives which run an exit lands in

| Step 1 radio | Resulting run type | Pay Date | Listing | Other exits go here? |
| --- | --- | --- | --- | --- |
| **Pay on a given date** | **Final Settlement Payroll** | = `Final Settlement Date` field | One card per (employee × date) | No — always single-employee |
| **Pay as per the regular pay schedule** | **Bulk Final Settlement Payroll** | = next regular run's pay day | Single card, grows in `No. of Employees` | **Yes** — all employees exiting with this option pool into one card per pay date |

## 3. What I saw in the tenant

**Card 1 — Final Settlement Payroll [DRAFT]** (Arjun Mehta, just initiated)
- Employees' Net Pay: ₹0.00 (no FnF amounts entered yet)
- Payment Date: 15/07/2026 (= our chosen Final Settlement Date)
- Employee: Arjun Mehta (EMP001)
- Approval deadline note: "Please approve this payroll on or before 15/07/2026"

**Card 2 — Bulk Final Settlement Payroll [DRAFT]** (Priya Sharma, pre-existing)
- Employees' Net Pay: ₹11,716.00
- Payment Date: 30/06/2026 (= next regular run)
- **No. of Employees: 1** ← key column unique to bulk type
- Approval deadline note: "Please approve this payroll on or before 30/06/2026"

The "No. of Employees" column is the visible difference between the two card layouts. Bulk shows count; single-shot Final Settlement shows the employee name.

## 4. Side-effect on regular payroll

Per business rule (confirmed by user, to verify via test):

> Once exit is initiated, the employee is **removed from the regular monthly payroll** for periods on/after their Last Working Day. Their pay for the LWD month (Pay Period) flows ONLY through the FnF run — never through both.

This implies:
- The regular monthly pay run for a period containing or after the LWD silently **excludes** any employee whose `last_working_day <= period_end`
- Conversely, the FnF run is the single source of truth for that month's earnings/deductions/statutory for the exiting employee

## 5. Other observations

- Both cards say **DRAFT** — initiating exit creates a Draft run; admin must explicitly Approve before pay date
- Both cards have `View Details` button → opens the FnF settlement screen (Step 2 we audited)
- No "Delete" affordance on the cards themselves — deletion happens via the FnF screen's trash icon
- The list is shared workspace — all admins see the same pending runs

## 6. Implementation mapping for our system

| Concept | Storage |
| --- | --- |
| Run type | Extend `PayrollRunType` enum: add `FinalSettlement` and `BulkFinalSettlement` (existing: `Regular`, plus possibly off-cycle types per Zoho audit done earlier) |
| Single FnF run | `payroll_runs.type = FinalSettlement`, `payroll_runs.pay_day = exit.settlement_date`, contains exactly 1 PayrunEmployee |
| Bulk FnF run | `payroll_runs.type = BulkFinalSettlement`, `payroll_runs.pay_day = nextRegularRun.pay_day`, contains N PayrunEmployees (one per opt-in exit) |
| Exclusion from regular run | InitiatePayrollRunHandler filter: `WHERE employee.last_working_day IS NULL OR employee.last_working_day > period_end`. Already partially handled if employee status switches to non-Active on exit; verify. |
| Idempotency for bulk run | When the same pay date already has a BulkFinalSettlement Draft, new opt-in exits should append a PayrunEmployee row instead of creating a second run. |

## 7. Open questions

- What happens if the Final Settlement Date falls inside the LWD's pay period (e.g. LWD 30/06, settlement 15/07)? Are statutory months recomputed for the partial period?
- If "Pay as per regular pay schedule" is chosen but LWD is AFTER the next regular pay date — is the exit deferred to the *following* regular run?
- Can an exiting employee on Bulk be moved to a custom date later (or vice versa) by editing LWD via the pencil?
- What if an admin cancels the exit (delete trash icon) — does the employee revert to regular payroll for the next run automatically?
