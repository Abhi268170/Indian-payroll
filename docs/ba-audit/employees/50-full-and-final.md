# Employees > Full and Final (F&F) Settlement

## Overview
Full and Final settlement is the payroll processing step that follows "Initiate Exit Process." It computes and pays the employee's final dues: prorated salary, earned leave encashment, gratuity (if eligible), notice pay adjustment (if applicable), loan deductions, and final statutory deductions.

## Page Investigation Status
**F&F settlement page was not directly navigated to** in this session (exit process was initiated on EMP001 to capture the form but not submitted, to avoid exiting a test employee). F&F page documentation is based on:
1. Industry standard F&F workflow knowledge
2. Zoho Payroll documented behavior patterns
3. Import Data types observed (no "F&F" import type found — F&F is processed via pay run, not import)

## Triggering F&F

### Via Exit Process
1. "Initiate Exit Process" from employee profile kebab → fill exit form → click "Proceed"
2. Employee transitions to "On Notice Period" status
3. F&F is processed in the appropriate pay run (regular or custom date, per exit form choice)
4. Pay run generates F&F payslip in addition to (or instead of) regular payslip for that period

### F&F Pay Run Type
Based on the exit form's "When do you want to settle the final pay?" radio:
- **Pay as per regular schedule**: F&F dues included in the next regular pay run for the period covering LWD
- **Pay on a given date**: A separate F&F pay run created for the specified date

## F&F Components (Indian Payroll Standard)

### Earnings in F&F
| Component | Computation | Statutory Basis |
|---|---|---|
| Prorated Salary | `Monthly Gross × (Days worked in final month / Total days in month)` | Proportional to service |
| Earned Leave Encashment | `(Leave balance days × Basic/26)` (26 working days per month) | As per leave policy |
| Gratuity | `(Last drawn Basic + DA) × 15/26 × Years of service` | Payment of Gratuity Act 1972 (applicable after 5 years) |
| Notice Pay (if waived) | `Monthly Gross × (Notice period days / 30)` — paid to employee if notice period waived by employer | Per employment contract |
| Arrears (if any) | Any unpaid salary from prior months | — |

### Deductions in F&F
| Component | Computation | Notes |
|---|---|---|
| Notice Pay Recovery | `Monthly Gross × (Shortfall days / 30)` — deducted if employee leaves without completing notice | Per employment contract |
| Loan Outstanding | Full outstanding loan balance | Outstanding EMIs accelerated to final settlement |
| TDS (Final Projection) | Remaining TDS for FY after accounting for all F&F income | u/s 192 |
| PF Deduction | For the final month's PF-eligible wage | — |
| PT | For the final month | Per state PT schedule |
| ESI | If still within ESI contribution period | — |

## F&F Payslip
- Similar format to regular payslip
- Clearly marked as "Full and Final Settlement"
- Shows all special components (gratuity, leave encashment, notice pay)
- Includes total income, deductions, and net pay to employee
- TDS computed on final income including all F&F components

## Post F&F Steps
1. F&F payslip generated and distributed (email to personal email address captured in exit form)
2. Employee status transitions to "Exited"
3. Form 16 generated at year-end (Part A from TRACES, Part B from Zoho records)
4. PF withdrawal initiated (employee files Form 19 with EPFO)
5. ESI benefit letter issued if applicable
6. Employee no longer appears in regular pay runs

## Gratuity Eligibility Rules (Indian Law)
- Minimum 5 years continuous service (4 years 240 days sufficient for some interpretations)
- Reason: "Resigned By Employee" — eligible if 5+ years
- Reason: "Terminated By Employer" — eligible regardless of tenure (employer liability)
- Reason: "Termination By Death/Disability" — eligible; paid to legal heirs or employee
- Calculation cap: Maximum gratuity = ₹20,00,000 (₹20 lakhs) tax-free; excess taxable

## Leave Encashment
- As per organization's leave policy (configurable)
- Typically: Earned Leave (EL) balance. Casual Leave (CL) and Sick Leave (SL) typically not encashed at exit
- Calculation: `EL balance × (Basic + DA) / 26` (26 working days per month standard)
- Tax treatment: Fully exempt up to ₹25,00,000 (FY2024-25 limit for private sector employees, raised from ₹3L)

## Key Observations for Our Build

1. **`FullAndFinalSettlement` entity:**
   ```
   id: UUID
   employee_exit_id: FK(EmployeeExit)
   payroll_run_id: FK(PayrollRun)
   prorated_salary: Decimal
   earned_leave_days: Decimal
   leave_encashment_amount: Decimal
   gratuity_amount: Decimal
   notice_pay_paid: Decimal (positive if employer waives notice)
   notice_pay_recovered: Decimal (positive if employee short-served notice)
   loan_deduction: Decimal
   tds_final: Decimal
   net_payable: Decimal
   status: Enum (Computed, Approved, Disbursed)
   ```

2. **Payroll run type extension** — `PayrollRun.run_type` enum: `Regular`, `FullAndFinal`. F&F runs processed separately from regular monthly payroll.

3. **Gratuity computation** — our engine must implement Payment of Gratuity Act 1972 formula. Gratuity is not a salary component — it is a lump sum computed at exit based on tenure and last Basic. Store as separate line item in F&F, not in salary structure.

4. **Leave balance integration** — requires a Leave module (or at minimum, `leave_balance` field on Employee) to compute leave encashment. Leave management is likely a future module.

5. **Loan acceleration** — all outstanding EMIs from Loans module automatically deducted in F&F. Engine must query outstanding loan balance per employee at exit date.

6. **TDS on F&F** — entire F&F income (prorated salary + leave encashment + notice pay + gratuity above ₹20L) is aggregated for TDS computation. Gratuity up to ₹20L is tax-free.

7. **Form 16 at year-end** — even for exited employees, Form 16 must be generated for the period employed. This means exited employee records must be retained in the system.

8. **Personal email for F&F communication** — payslip and Form 16 emailed to personal email (captured during exit initiation), not work email.

## State Machine: F&F
```
EmployeeExit created (status: OnNoticePeriod)
  ↓ [Admin/System initiates F&F payroll run]
F&F PayrollRun created (status: Draft)
  ↓ [Engine computes F&F amounts]
F&F PayrollRun (status: Computed)
  ↓ [Admin reviews and approves]
F&F PayrollRun (status: Approved)
  ↓ [Disbursement]
F&F PayrollRun (status: Finalized)
  + Employee status → Exited
```

## Open Questions
- [ ] Does Zoho show a dedicated F&F summary page before processing, or is it just a tagged regular payslip?
- [ ] How does Zoho handle gratuity if the leave encashment and gratuity are paid in different months?
- [ ] Is there an approval workflow for F&F (separate from regular payroll approval)?
- [ ] Can F&F be reopened after finalization if an error is found?
- [ ] How does Zoho compute gratuity for fractional service years (e.g., 6 years 3 months)?
