# UF-67: Loan Foreclosure

**Module:** Loans > Loan Detail
**Tested:** 2026-05-16
**Mock Data Used:** LOAN-00001 (Arjun, ₹50,000 outstanding)
**App State Before:** LOAN-00001 Open; no EMIs paid

## Steps Executed
1. Observed loan actions dropdown: Edit Loan, Pause Instalment Deduction, Delete Loan
2. No explicit "Foreclose" button observed in dropdown
3. Documented expected foreclosure flow from domain knowledge

---

## Foreclosure Options

### Option 1: Record Repayment (Full Outstanding Amount)
"Record Repayment" button on loan detail panel.
1. Admin clicks "Record Repayment"
2. Enters amount = full outstanding balance (₹50,000 for LOAN-00001)
3. Enters payment date and mode (Employee paid in cash/bank)
4. Saves — outstanding reduces to ₹0
5. Loan status auto-changes to "Closed"
6. No further EMI deductions in subsequent pay runs

### Option 2: FnF Recovery (When Employee Exits)
When employee is terminated/resigns:
1. Outstanding loan balance is deducted in FnF settlement
2. System reduces outstanding to ₹0
3. Loan status changes to "Closed"

### Option 3: Loan Write-Off (Employer Decision)
If employer decides to write off the loan (forgive the debt):
- The written-off amount becomes taxable income for the employee (Section 28(iv))
- Admin would need to add the forgiven amount to employee's income manually
- Zoho may not have a dedicated "write-off" feature — admin uses "Record Repayment" with a note

---

## Loan Actions Dropdown (Observed from Loan Detail)

| Action | Effect |
|--------|--------|
| Edit Loan | Modify loan details (amount, instalment, reason, perquisite) |
| Pause Instalment Deduction | Stops EMI from being deducted in next pay run without closing the loan |
| Delete Loan | Removes the loan record entirely (available before first EMI?) |

**Notable absence:** No "Foreclose Loan" or "Close Loan" button. Foreclosure is achieved via Record Repayment with full amount.

---

## Delete Loan vs Foreclose

| Action | When | Effect |
|--------|------|--------|
| Delete Loan | Before any EMI paid (no repayment history) | Removes loan record; no audit trail |
| Foreclose (via Record Repayment) | After any EMIs paid | Closes loan; repayment history preserved |

**Business Rule:** Deletion should only be allowed when no EMI has been paid (no repayment history). Once even one EMI is deducted in a pay run, the loan should not be deletable — only closeable via full repayment.

---

## Post-Foreclosure Behavior

After loan is closed:
1. Loan status = "Closed"
2. No further EMI deductions in any pay run
3. Loan remains in the system for reporting/audit
4. Loan Perquisite reports show ₹0 perquisite from closure month onward
5. Employee's payslip no longer shows EMI deduction
6. Loan Outstanding Summary report shows ₹0 outstanding

---

## Pause vs Foreclosure

| Dimension | Pause | Foreclosure |
|-----------|-------|-------------|
| EMI deduction | Stops temporarily | Stops permanently |
| Outstanding balance | Unchanged | ₹0 |
| Loan status | Open (Paused) | Closed |
| Resumable | Yes (un-pause) | No (would need new loan) |
| Perquisite | Continues on outstanding | ₹0 |

---

## Business Rules
1. Foreclosure = full outstanding repayment via "Record Repayment"
2. Loan auto-closes when outstanding = ₹0
3. Closed loans not deletable (audit trail must be preserved)
4. Loan write-off (forgiveness) requires manual income addition to employee — not a direct Zoho feature
5. For FnF: system deducts full outstanding; loan closes automatically after FnF pay run

## Gaps / Observations
- No "Foreclose" button seen — foreclosure achieved via Record Repayment
- Delete behavior (guard on first EMI) not confirmed
- 🟡 Mark for future session: record a partial repayment to observe outstanding balance reduction; then foreclose

## Open Questions
- [ ] Is there a "Delete Loan" guard that prevents deletion after first EMI is paid?
- [ ] Can admin "foreclose" a loan by entering a partial amount (not the full outstanding)?
- [ ] Is there a foreclosure discount/early repayment benefit feature?
- [ ] What is the Loan status display during "Paused" state in the loans list?
