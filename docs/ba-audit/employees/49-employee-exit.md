# Employees > Employee Exit Process

## URL / Navigation Path
- Route: `#/people/employees/{id}/terminate`
- Full URL: `https://payroll.zoho.in/#/people/employees/3848927000000032948/terminate`
- Entry: Employee Profile header → kebab dropdown → "Initiate Exit Process"
- Page title: "Exit Process | Zoho Payroll"

## Purpose
Initiates the offboarding / exit workflow for an employee. Captures last working day, exit reason, F&F pay schedule, and notification details. Transitions employee status from Active → On Notice Period.

## Page Layout
- Left panel: Exit details form
- Right panel: Employee summary card

## Employee Summary Card (Right Panel)
Read-only summary of the employee being exited:
- Avatar with initials
- Employee name (large heading)
- "ID: EMP001" (employee ID)
- Designation
- Department
- Date of Joining

## Exit Details Form (Left Panel)

### Fields

| Field | Type | Required | Validation | Notes |
|---|---|---|---|---|
| Last Working Day | Date textbox | Yes | dd/MM/yyyy format; placeholder shown | Must be >= today; should be >= DOJ |
| Reason for Exit | Combobox | Yes | Must select from predefined list | Has search/filter |
| When to settle final pay | Radio group | Yes | Default: "Pay as per the regular pay schedule" | Controls F&F timing |
| Personal Email Address | Text | No | Has info icon (tooltip not captured) | Used for exit notification or post-employment Form 16 delivery |
| Notes | Textarea | No | Free text | Internal notes visible to admin only |

### Reason for Exit — Options
1. Terminated By Employer
2. Termination By Death
3. Termination by Disability
4. Resigned By Employee

Note: These 4 options map to standard Indian HR reasons. The reasons are important for:
- Gratuity eligibility (Resignation after 5+ years = eligible; Terminated by employer before 5 years = complex)
- Form 16 issue timing
- PF withdrawal eligibility
- ESI last wage computation

### Final Pay Settlement — Radio Options
| Option | Behavior |
|---|---|
| Pay as per the regular pay schedule | F&F included in the next regular pay run. Employee processed alongside all other employees. |
| Pay on a given date | Opens additional date picker — admin specifies a custom F&F disbursement date |

### Notes Section
- Displayed below: "Note: Portal is not enabled for this employee. Kindly collect the proof of investments before processing the payroll."
- This is a contextual warning — if the employee's self-service portal is not enabled, the admin must manually collect investment proofs (for Form 16 Part B accuracy) before running F&F.

## Buttons
| Button | Behavior |
|---|---|
| Proceed | Validates form; saves exit details; transitions employee status to "On Notice Period"; navigates to F&F page |
| Cancel | Link → `#/people/employees/{id}`; returns to employee overview without saving |

## State Transition: Active → On Notice Period
When "Proceed" is clicked:
- Employee status badge changes from "Active" to "On Notice Period"
- Employee remains in payroll for regular pay runs until Last Working Day
- F&F payroll item created for processing in the appropriate pay run (per the "when to settle" choice)
- Employee visible in "Exited Employees" list after F&F is complete

## Business Rules

1. **Last Working Day is mandatory** — cannot initiate exit without specifying when the employee's last day is.
2. **Reason for exit drives statutory logic** — "Resigned By Employee" affects gratuity eligibility; "Termination By Death" triggers different nominee-related workflows.
3. **Portal access governs proof collection** — if portal disabled, admin must collect investment proof manually. This is critical for accurate Form 16 Part B.
4. **F&F timing is configurable** — admin can choose regular pay cycle or custom date. Our engine must handle a "final settlement pay run" as a separate run type, or as a flagged employee within the regular pay run.
5. **Employee remains on payroll through LWD** — partial month salary (if LWD is mid-month) must be prorated.
6. **Salary for notice period** — if employee is serving notice, salary continues at current rate through LWD.
7. **Personal email** — important post-exit for Form 16 delivery since work email may be deactivated after exit.

## Cross-Module Impact
- Payroll Run: employee flagged as "exiting" in the run covering their LWD; payroll engine computes final salary (prorated if needed) + gratuity + leave encashment + notice pay adjustments.
- Form 16: generated for the FY even if employee exits mid-year.
- Loans: any outstanding loan balance at exit must be deducted from F&F.
- PF: final PF settlement instructions generated in ECR file.
- ESI: final ESI return includes exit employee.

## Key Observations for Our Build

1. **`EmployeeExit` entity required:**
   ```
   id: UUID
   employee_id: FK(Employee)
   last_working_day: Date
   reason: Enum (TerminatedByEmployer, TerminationByDeath, TerminationByDisability, ResignedByEmployee)
   final_pay_mode: Enum (RegularSchedule, CustomDate)
   final_pay_date: Date? (required if final_pay_mode = CustomDate)
   personal_email: String?
   notes: String?
   initiated_by: FK(AdminUser)
   initiated_at: Timestamp
   ```

2. **Employee status enum must include:** `Active`, `OnNoticePeriod`, `Exited`, `Inactive`

3. **F&F payroll run type** — our payroll engine needs a `PayrollRunType` enum: `Regular`, `FullAndFinal`. F&F runs may have a custom pay date.

4. **Proration on LWD** — if LWD is not the last day of the month, engine must prorate final month salary: `Monthly × (LWD day / Total days in month)`.

5. **Route naming** — Zoho uses `/terminate` as the URL; our route should be `/exit-process` or `/offboarding` for clearer semantics.

6. **Note about investment proofs** — our UI must also check portal status and display a warning if proofs not submitted before running F&F.

## State Machine: Employee Exit

```
Active
  ↓ [Initiate Exit Process — fill Last Working Day, Reason, F&F timing]
On Notice Period
  ↓ [Pay Run processes F&F — includes prorated salary, gratuity, leave encashment, notice pay adj.]
Exited (Full & Final Complete)
```

## Open Questions
- [ ] What happens if Last Working Day is set to a past date? Does Zoho allow backdated exit initiation?
- [ ] Are there additional exit reason options not shown (e.g., contract end, retirement)?
- [ ] Does clicking Proceed immediately create a pay run, or just flag the employee?
- [ ] Is there a "Revoke Exit" option if the employee retracts resignation?
