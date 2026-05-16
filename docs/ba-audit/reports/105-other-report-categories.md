# Reports > Additional Report Categories

This file documents all report categories not covered in the statutory reports file: Payroll Overview sub-reports, Employee/Contractor Reports, Declarations & Investments, Deduction Reports, Taxes and Forms, Loan Reports, Payroll Journal, and Activity Logs.

---

## Additional Payroll Overview Reports (beyond Payroll Summary)

### Payroll Liability Summary
**URL:** `#/reports/payroll-liability`
**Filter:** Date Range (default: Previous Month)
**Columns:** Liability Name | Employee Contribution | Employer Contribution | Total Contribution
**Total row:** ₹0.00 for all columns (no statutory liabilities in this org)
**Use case:** Shows all statutory liabilities (PF, ESI, PT, LWF) in a consolidated view. Useful for payment planning.
**Key observation:** Column header "Employee Contribution" appears twice in the raw DOM — this likely represents different breakdowns (e.g., employee-deducted vs. employer-borne). Needs clarification.

### Leave Encashment Summary
**URL:** `#/reports/leave-encashment-summary`
**Filter:** Date Range (default: Previous Month)
**Columns:** Employee Number | Employee Name | Employee Status | Days | Amount
**Use case:** Shows leave encashment payouts. Only populated when leave encashment is processed as part of payroll or FnF.

### Loss Of Pay Summary
**URL:** `#/reports/lop-summary`
**Filter:** Date Range (default: Previous Month)
**Columns:** Employee Number | Employee Name | Employee Status | LOP Days | Adjusted LOP Days | Actual LOP Days
**Key observations:**
- Three LOP day columns: Raw LOP Days, Adjusted (after any manual adjustment), Actual (final used for salary deduction)
- "Adjusted LOP Days" suggests HR can modify the automatically computed LOP before it affects payroll
- This maps to a LOP approval workflow upstream

### Variable Pay Earnings Report
**URL:** `#/reports/variable-pay-earnings-report`
**Filter:** Date Range (This Year) + Earnings Type dropdown (default: Overtime Allowance)
**Columns:** Employee ID | Employee Name | Total Amount Paid
**URL params:** `compensation_id=3848927000000032481` — the specific earning component ID
`period_type=pay_period` — filtered by pay periods not calendar dates
**Key observations:**
- One report per earning component — user selects which variable earning to view
- URL carries the `compensation_id` of the selected earning
- "Overtime Allowance" is the default (or only variable earning in this org)

### Scheduled Earning Summary
**URL:** `#/reports/scheduled-earnings-summary`
**Filter:** Date Range (This Year)
**Columns:** Employee Number | Employee Name | Employee Status | Earning Name | Total Scheduled Amount | Total Paid Amount | Amount Difference
**Key observations:**
- Tracks "scheduled" earnings (recurring one-time payments pre-scheduled in the system) vs what was actually paid
- "Amount Difference" = Scheduled - Paid, useful for identifying missed or partial payments
- "Earning Name" column means one row per employee per scheduled earning component

---

## Employee/Contractor Reports (Full Detail)

### Compensation Details
**URL:** `#/reports/employee-ctc-master`
**Filter:** Month picker (current month)
**Columns:** Employee Number | Employee Name | Date of Joining | Earnings | Basic | House Rent Allowance | Fixed Allowance
**URL params:** `entity_list=compensation,benefit` | `is_yearly_projection=false`
**Key observations:**
- "is_yearly_projection=false" suggests a toggle — this report can show monthly OR annual CTC projection
- Shows salary structure at a point in time (as of the selected month)
- "Date of Joining" included — useful for pro-ration queries
- Only Earnings components visible (not deductions) — this is the CTC breakdown, not the net pay breakdown

### Reimbursement Claim Summary
**URL:** `#/reports/reimbursement`
**Filter:** Date Range (This Year)
**Columns:** Employee Number | Employee Name | Employee Status | Reimbursement Type | Employee Eligible Amount | Employee Paid Amount | Employee Unclaimed Amount
**Key observations:**
- Three amount columns: Eligible (annual entitlement) / Paid (claimed and paid) / Unclaimed (balance remaining)
- "Reimbursement Type" column — one row per employee per reimbursement type
- Useful for tracking HRA claims, medical reimbursements, LTA, etc.

### Employee Perquisites Summary
**URL:** `#/reports/employee-perquisite-summary`
**Filter:** Fiscal Year (default: current FY = 2026-27)
**Columns:** Employee Number | Employee Name | Employee Status | Perquisite Amount | Perquisite Amount Recovered | Perquisite Amount Chargeable
**Key observations:**
- Perquisites = non-cash benefits provided by employer (car, club membership, accommodation, interest-free loans)
- "Perquisite Amount Recovered" = amount the employee paid back (reducing the taxable perquisite)
- "Perquisite Amount Chargeable" = net taxable perquisite (Amount - Recovered)
- Fiscal year filter (not monthly) — perquisite taxation is an annual calculation for Form 16/Form 12BA

### Full and Final Settlement Report
**URL:** `#/reports/employee-termination-report`
**Filter:** Date Range (This Year)
**Columns:** Employee Number | Employee Name | Designation | Date of Joining | Last Working Day | Termination Type | Final Settlement Date | Payroll Status | Final Settlement Amount | Service Period
**Key observations:**
- "Termination Type" — values would include Resignation, Retirement, Termination, Death, etc.
- "Payroll Status" — whether the final payroll was processed and paid
- "Final Settlement Amount" — total FnF payout
- "Service Period" — total tenure (years, months, days) — critical for gratuity calculation

### Employees' Salary Revisions
**URL:** `#/reports/employee-salary-revisions`
**Filter:** Date Range (This Year)
**Columns:** Employee Number | Employee Name | Employee Status | Revised CTC | Previous CTC | Amount Difference | Amount Difference In Percentage | Change In Percentage | Amount Difference In Comments | Effective From | Payout Month | Status
**Key observations:**
- Two percentage columns: "Amount Difference In Percentage" and "Change In Percentage" — likely one is absolute percentage and one is hike percentage
- "Comments" per revision — revision notes are stored and visible in reports
- "Effective From" vs "Payout Month" — salary revision can be effective from one date but paid from a later month (e.g., retroactive revision)
- "Status" — Approved/Pending/Rejected revision status

### Salary Revision History
**URL:** `#/reports/employee-salary-revision-history`
**Filter:** Employee picker dropdown (no date range)
**Columns:** Employee Number | Employee Name | Revised CTC | Previous CTC | Amount Difference | Amount Difference In Percentage | Change In Percentage | Amount Difference In Comments | Effective From | Payout Month
**Key observations:**
- Per-employee complete history (no date filter) — shows ALL revisions ever for the selected employee
- Same columns as Salary Revisions but without Status and Employee Status
- Employee picker = select one employee at a time (not multi-select based on snapshot)

### Salary Withhold Report
**URL:** `#/reports/salary-hold-report`
**Filter:** Date Range (Previous Month); URL param: `include_released_salary=false`
**Columns:** Employee ID | Name | Designation | Employee Status | Withheld Month | Amount to Pay | Withheld Reason | Release Status | Release Month
**Key observations:**
- "include_released_salary=false" — by default shows only currently held salaries. Toggle to true shows released ones too.
- "Withheld Reason" — text reason for holding salary (disciplinary action, document pending, etc.)
- "Release Status" — shows if the held salary was later released
- "Release Month" — when it was released (for auditing)

---

## Declarations & Investments Reports

### FBP Declaration Report
**URL:** `#/reports/fbp-declaration-report`
**Filter:** Submitted Date
**Columns:** (empty — no FBP configured in this org)
**Key observations:**
- FBP = Flexible Benefits Plan — allows employees to choose their own salary structure components
- Only relevant for orgs using the FBP module
- Filter is "Submitted Date" not "Date Range" — applies to when the declaration was submitted

### Investment Declaration Report
**URL:** `#/reports/investment-declaration-report`
**Filter:** Fiscal Year (default: 2026-27)
**Columns:** Employee Number | Employee Name | Tax Regime | Total Chapter VI-A Amount | Total Allowance Amount | Total HRA Amount | Total Other Income Amount | Total Previous Employment Amount | Total Direct Tax Amount
**Key observations:**
- "Tax Regime" — New Regime or Old Regime (though v1 of our build is New Regime only)
- "Chapter VI-A Amount" — 80C, 80D, 80G, etc. (only relevant for Old Regime)
- "Total Allowance Amount" — allowances claimed (HRA, LTA, etc.)
- "Total Previous Employment Amount" — prior employer TDS and salary details for YTD calculation
- "Total Direct Tax Amount" — TDS to be deducted based on declaration

### Proof of Investment Report
**URL:** `#/reports/proof-of-investment-report`
**Filter:** Fiscal Year (default: 2026-27)
**Columns:** Employee Number | Employee Name | Tax Regime | Submitted On | Approver Name | Status | Actual Amount | Approved Amount | Document Count
**Key observations:**
- POI is submitted by employees with supporting documents for investment declarations
- "Actual Amount" (what employee claimed) vs "Approved Amount" (what HR approved after document verification)
- "Document Count" — number of documents attached
- "Approver Name" — HR/manager who approved the POI
- "Status" — Pending / Approved / Rejected

---

## Deduction Reports

### Benefits & Deductions Summary
**URL:** `#/reports/deductions-summary`
**Filter:** Date Range (This Year)
**Columns:** Type | Name | Total Employee Contribution | Total Employer Contribution | Total Contribution
**Key observations:**
- Org-level aggregate (not per-employee)
- "Type" = Benefits or Deductions category
- Shows both employee and employer contributions for each benefit/deduction component
- Useful for payroll reconciliation and statutory payment planning

### Deductions Summary
**URL:** `#/reports/employee-post-tax-deductions-summary`
**Filter:** Date Range (This Year)
**Columns:** Employee Number | Employee Name | Employee Status | Total Employee Contribution
**Key observations:**
- URL says "post-tax-deductions" — this is for deductions applied AFTER tax (e.g., VPF, NPS, salary advance, professional loan repayment)
- Only Employee Contribution column (no employer contribution for post-tax deductions)

### Benefits Summary
**URL:** `#/reports/employee-pre-tax-deductions-summary`
**Filter:** Date Range (This Year)
**Columns:** Employee Number | Employee Name | Employee Status | Total Employee Contribution | Total Employer Contribution | Total Contribution
**Key observations:**
- URL says "pre-tax-deductions" — maps to statutory benefits (PF, ESI, PT, LWF)
- Has both employee and employer contribution columns (unlike Deductions Summary)
- "Benefits" in Zoho = pre-tax statutory contributions; "Deductions" = post-tax voluntary deductions

### Donations Summary
**URL:** `#/reports/employee-donation-summary`
**Filter:** Date Range (This Year); URL param: `period_type=pay_period`
**Columns:** Employee Number | Employee Name | Employee Status | Total Employee Contribution
**Key observations:**
- Tracks charitable donations made via payroll (Zoho's "Giving" feature)
- `period_type=pay_period` — filtered by pay period, same as LWF report
- Only employee contribution (employers don't typically match charitable donations through payroll)

---

## Taxes and Forms Reports

### TDS Deduction Summary
**URL:** `#/reports/tds-summary`
**Filter:** Date Range (This Year)
**Columns:** Employee Number | Employee Name | PAN Number | Taxable Amount (Year) | Tax on Income | Surcharge Amount | Cess | Total TDS Amount
**Footer totals:** ₹0.00 across all columns for this org
**Key observations:**
- **PAN Number** prominently shown — essential for TDS/Form 16 compliance
- "Taxable Amount (Year)" = annual taxable income YTD — this accumulates across months
- Tax components broken out: Tax on Income + Surcharge + Cess = Total TDS Amount
- All zeros suggest no TDS is configured/applicable for this org's employees (all below taxable income threshold or new regime with zero tax)

### Form 24Q
**URL:** `#/reports/form-24q`
**Filter:** Fiscal Year (default: 2026-27)
**Empty state message:** "Form 24Q has not been generated for this Fiscal Year."
**Tab structure:** "Annexure II" tab visible (Annexure I would be the quarterly TDS detail; Annexure II is the Form 16 data for Q4)
**Key observations:**
- Form 24Q must be explicitly generated — it does NOT auto-generate with payroll runs
- The report in Zoho is a view of the generated Form 24Q, not the generation trigger itself
- "Annexure II" (Form 16 details) is visible as a tab — suggests the Form 24Q in Zoho covers both Annexure I (quarterly TDS) and Annexure II (employee-wise tax computation for Form 16)
- The generation action is presumably in Taxes & Forms > Form 24Q module (not in Reports)

---

## Loan Reports

### Loan Outstanding Summary
**URL:** `#/reports/loan-outstanding-summary`
**Filter:** None visible (shows all-time outstanding loans)
**Columns (16):** Employee Number | Employee Name | Loan Name | Perquisite Interest Rate | Loan Number | Loan Amount | Instalment Amount | Disbursement Date | Opening Balance | Repayment Start Date | Pending Instalments | Principal Paid | Paid In Period | Balance Amount | End Date | Paid Perquisite Amount
**Key observations:**
- "Paid In Period" — amount paid toward the loan in the selected/current period
- "Balance Amount" — remaining principal outstanding
- "Paid Perquisite Amount" — perquisite tax paid on the loan benefit
- Most comprehensive loan ledger view — 16 columns

### Loan Perquisite Summary
**URL:** `#/reports/loan-perquisite-summary`
**Filter:** Fiscal Year (default: 2026-27)
**Columns:** Employee Number | Employee Name | Loan Name | Loan Number | Loan Amount | Perquisite Amount
**Key observations:**
- Annual summary of loan perquisites for Form 16/Form 12BA
- Perquisite amount = imputed interest (notional interest at SBI rate vs actual rate charged)

### Loan Perquisite Projection
**URL:** `#/reports/loan-perquisite-projection`
**Filter:** Fiscal Year (default: 2026-27)
**Columns:** (empty — no data for this org)
**Key observations:**
- Shows projected future perquisite amounts for the remaining fiscal year
- Useful for TDS planning (projecting future tax liability from loan perquisites)

### Loan Summary Report (actual title: Loan Overall Summary)
**URL:** `#/reports/loan-overall-summary`
**Filter:** Date Range (This Year)
**Columns (19):** Employee Number | Employee Name | Employee Status | Loan Name | Perquisite Interest Rate | Loan Number | Loan Amount | Instalment Amount | Loan Reason | Disbursement Date | Opening Balance | Repayment Start Date | No Of Installments | Remaining Tenure | Principal Paid | End Date | Remaining Amount | Paid Perquisite Amount | Loan Status
**Key observations:**
- Most comprehensive loan view — includes "Loan Status" (Active/Closed/etc.), "Remaining Tenure", "Loan Reason"
- "No Of Installments" = total instalment count
- "Remaining Tenure" = instalments remaining
- "Loan Reason" = text reason given when creating the loan

---

## Payroll Journal

### Payroll Journal Summary
**URL:** `#/reports/journal`
**Filter:** Date Range (default: Previous Month)
**URL params:** `show_tags=false`
**Structure:** NOT a standard columnar table — shows journal entry groups

**Sample structure from DOM:**
```
30/04/2026 - Payroll Journal
  | Debit | Credit |
  | 92,000.00 | 92,000.00 |

30/04/2026 - Wage Payment
  | Debit | Credit |
  | 92,000.00 | 92,000.00 |
```

**Key observations:**
- Double-entry accounting format (Debit/Credit)
- Two journal types per pay run: "Payroll Journal" and "Wage Payment"
- "Payroll Journal" = accrual entry (recording salary expense)
- "Wage Payment" = payment entry (recording bank disbursement)
- `show_tags=false` URL param suggests there may be account tags/labels that can be shown for accounting categorization
- Total: ₹92,000.00 for April 2026 pay run (this matches the 5-employee org with average salary ~₹18,400)
- This report is essential for integration with accounting software (Zoho Books, Tally, etc.)

---

## Activity Logs

### Activity Logs
**URL:** `#/reports/activity-log`
**Filter:** Date Range — but with a different URL pattern: `filter_by=CreatedDate.CustomDate` with `usestate=false`
**Columns:** Date | Activity Details | Description | Audit Trail
**Default period:** Current month (May 2026 shown)
**Key observations:**
- This is the application audit trail — every significant action taken by users
- "Activity Details" — likely the action type (e.g., "Pay Run Created", "Employee Updated")
- "Description" — details of the action
- "Audit Trail" — may be a link to detailed log
- Placed in Reports module (not a separate "Audit Logs" module) — architectural decision
- `filter_by=CreatedDate.CustomDate` is a different filter_by pattern from other reports which use plain `filter_by=`

---

## Leave Reports
**Status: NOT FOUND.** Zoho Payroll does not include leave management reports in the Reports module. Leave is a separate module (typically in Zoho People) that integrates with Zoho Payroll for LOP calculation. There are no standalone "leave balance" or "leave request" reports in the Payroll Reports Centre.

The only leave-related data visible is:
- "Leave Encashment Summary" (encashment amounts, not leave balances)
- "Loss Of Pay Summary" (LOP days per employee)

---

## Headcount / HR Analytics Reports
**Status: NOT FOUND.** No headcount report, employee count by department/location, or hire/exit trend reports are present in the 39 reports. These would typically live in an HRMS module (Zoho People) rather than Zoho Payroll.

---

## Bank Advice / Payment Reports
**Status: NOT DIRECTLY FOUND.** No "Bank Transfer Advice" or "Bank Payment File" report appears in the 39 reports. Bank payment files are likely generated within the Pay Runs module (not Reports). The Payroll Journal Summary shows the payment entry but not a bank transfer format.

**Key gap for our build:** Most Indian payroll systems generate bank advice files (NEFT/RTGS format files for SBI, HDFC, ICICI, etc.) for salary disbursement. This is a separate feature from reports.

---

## CTC Reports
**Status: PARTIAL.** "Compensation Details" (`/reports/employee-ctc-master`) serves as the CTC report, showing the salary structure components per employee for a given month. It does not show total CTC as a single sum — it shows each component. The "Employees' Salary Revisions" and "Salary Revision History" reports show Revised CTC and Previous CTC as aggregate amounts, implying Zoho does store a total CTC figure per employee.

---

## Variance Reports (Month-on-Month Comparison)
**Status: PARTIAL.** The "Compare With" feature on several reports (Payroll Summary, PT Summary, ESI Summary, LWF Summary) provides MoM or YoY comparison. There is no dedicated "Variance Report" or "Month-on-Month Comparison Report" as a standalone report.

---

## Key Observations for Our Build (All Other Categories)

1. **Loan reports are extensive (4 reports, 19 columns in the most detailed).** This reflects the complexity of the perquisite taxation on concessional loans (Rule 3 of Income Tax Rules). Our loan module must support all the fields shown: loan reason, perquisite rate, instalment plan, opening/closing balance, perquisite amounts.

2. **Payroll Journal is essential for accounting integration.** The two-entry format (Payroll Journal accrual + Wage Payment disbursement) is standard double-entry bookkeeping. Our build should generate these entries automatically when payroll is finalized.

3. **Activity Logs in Reports (not a separate module)** is a design choice worth replicating. Centralizing all logs in Reports makes them accessible to anyone with report access.

4. **No bank payment file report** is a gap. Our build should generate NEFT/RTGS bank transfer files in the formats required by major Indian banks (SBI, HDFC, ICICI, Axis, etc.).

5. **Leave reports absent** — our build should consider whether to include basic leave analytics in the Payroll Reports module (at minimum: LOP summary, leave encashment) even if full leave management is in a separate module.

6. **Salary Revision report** has "Effective From" vs "Payout Month" distinction — this is critical for supporting retroactive salary revisions (arrear payments). Our revision model must store both dates.

7. **FBP Declaration Report** requires the FBP (Flexible Benefits Plan) module. Our v1 may not include FBP, so this report can be deferred.

8. **POI (Proof of Investment) approval workflow** — the POI report shows Approver Name and Status, confirming there's an approval workflow for investment proofs. Our TDS module should include this.
