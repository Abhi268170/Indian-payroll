# UF-34: Employee Exit / Full and Final Settlement

**Module:** Employees > Employee Profile > Actions (Exit/Terminate)
**Tested:** 2026-05-16
**Mock Data Used:** No exited employees in demo org
**App State Before:** All 5 employees active

## Steps Executed
1. Observed employee profile actions section (from prior session screenshots)
2. No exit flow triggered — no employees terminated
3. Documented expected flow from domain knowledge and UI patterns

---

## Employee Exit Flow — Overview

In Zoho Payroll, terminating an employee triggers a Full and Final (FnF) settlement process.

### Entry Points
- Employee profile → Actions → "Terminate Employee" (or similar)
- Employee profile may have a "Mark as Resigned" or "Initiate Exit" button
- URL pattern expected: `#/people/employees/{id}/exit` or similar

---

## Exit/Terminate Form (Expected)

### Fields
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Last Working Day | Date | Yes | Final day of employment |
| Separation Type | Dropdown | Yes | Resignation / Termination / Retirement / Death / Absconding |
| Notice Period Required | Number | Yes | As per employment contract (days) |
| Notice Period Served | Number | Yes | Actual days served |
| Reason | Text | Optional | Internal note |

### Post-Submission
- System creates a FnF settlement record
- FnF run is created (separate from regular pay run)

---

## FnF Settlement Computation

### Components of FnF

| Component | Calculation | Taxability |
|-----------|-------------|------------|
| Unpaid salary | Days worked in last month × daily rate | Fully taxable |
| Notice period pay | If employer releases notice period | Fully taxable |
| Notice period recovery | If employee short-serves notice | Deduction (offset) |
| Earned Leave Encashment | Accumulated EL × daily rate | Taxable (during service); exempt on retirement |
| Gratuity | Formula below | Partially exempt (see UF-35) |
| Bonus (accrued) | Pro-rated annual bonus | Fully taxable |
| Variable Pay (unpaid) | Any pending variable | As applicable |
| Outstanding Loan Recovery | Full outstanding loan balance | Not income — loan recovery |
| Income Tax (TDS on FnF) | TDS on total FnF taxable amount | Per applicable rate |

### Proration for Last Month
- If exit is mid-month: Salary = Full Salary × (Working Days / Calendar Days)
- Working Days = Last Working Day date within the month − 1st of month + 1
- LOP not applicable separately — the proration already accounts for partial month

---

## Notice Period Scenarios

### Scenario 1: Full Notice Served
- Employee gives notice, works full 60 days (or whatever contract specifies)
- No recovery, no additional pay
- FnF = Unpaid salary + Leave Encashment + Gratuity

### Scenario 2: Short Notice (Buy-Out)
- Employee leaves before completing notice period
- Employer deducts: Daily rate × Short Notice Days
- FnF = Unpaid salary − Notice Period Recovery + Leave Encashment + Gratuity

### Scenario 3: Employer Waives Notice
- Employer terminates employee or releases early
- Employee receives Notice Pay for unserved notice period
- FnF = Unpaid salary + Notice Pay for unserved period + Leave Encashment + Gratuity

---

## Outstanding Loan at Exit

**Business Rule (Critical):** When an employee exits with an outstanding loan balance:
- The full outstanding balance is deducted from the FnF settlement amount
- If FnF < outstanding loan: Employer may need to recover balance through other means
- System should automatically include loan recovery in FnF computation

**From current data:** Arjun has LOAN-00001 with ₹50,000 outstanding. Vikram has LOAN-00002 with ₹1,00,000 outstanding. If either exits, their loan must be fully recovered.

---

## FnF Pay Run

FnF is processed as a special pay run (not a regular monthly pay run):
- Type: "Full and Final Settlement"
- Pay period: From the last regular pay run end date to the Last Working Day
- Components: As per the FnF computation above

---

## State After Exit

- Employee status: Inactive / Resigned / Terminated
- Employee remains in the system (not deleted) — historical records preserved
- Payslips for previous months still accessible
- Form 16 for the last FY is still generated
- Statutory filings continue to include the employee for their employment period

---

## Business Rules
1. FnF must be processed before the employee is fully deactivated
2. TDS on FnF is computed based on FnF month's combined income (regular + FnF components)
3. Gratuity is exempt from tax up to ₹20,00,000 (Payment of Gratuity Act) — see UF-35
4. Loan recovery in FnF is NOT deducted from taxable income (it is not an income)
5. If FnF results in a net negative (loans > FnF amount), system should flag it for manual resolution
6. An exited employee's records must be retained for at least 7 years (statutory requirement)

## Gaps / Observations
- Exit/terminate flow not navigated in UI
- FnF pay run type not confirmed in "New" dropdown (only Off-Cycle and One-Time Payout observed)
- No exited employees to demonstrate FnF Reports content

## Open Questions
- [ ] Is there an "Exit Interview" or "Reason for Separation" structured dropdown or free text?
- [ ] Does FnF appear as a separate pay run type in Payroll History?
- [ ] Can admin process FnF before the Last Working Day (advance processing)?
- [ ] What happens to an employee's TDS if FnF is large and pushes them into higher slab?
- [ ] Is there a "Rehire" flow if a previously exited employee rejoins?
