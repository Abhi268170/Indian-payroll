# Reports > Full Inventory (Category View)

**URL:** `https://payroll.zoho.in/#/reports`
**Title:** Reports Centre
**Total:** 39 reports, all System Generated

---

## Reports Centre Layout

- **Page header:** "Reports Centre" (h2)
- **Search:** "Search reports" combobox at top
- **Left sidebar tabs:** Home, Favorites, Shared Reports, Scheduled Reports
- **Left sidebar categories:** Payroll Overview, Statutory Reports, Employee/Contractor Reports, Declarations & Investments, Deduction Reports, Taxes and Forms, Loan Reports, Payroll Journal, Activity
- **Right area:** Table with columns: Report Name | Report Category | Created by | Last Visited
- **Upsell panel (bottom of left sidebar):** "Advanced Financial Analytics for Zoho Payroll — Try Zoho Analytics" (links to integrations settings)

---

## Category 1: Payroll Overview (9 reports)

| Report Name | URL Pattern | Default Filter | Description |
|-------------|-------------|----------------|-------------|
| Payroll Summary | `/reports/payroll-summary` | This Year | Org-level aggregate of all pay components — Earnings, Benefits, Donations, Deductions, Taxes, Reimbursements, Net Pay. Two-column layout: Pay Component / Amount. Supports "Compare With" prior period. |
| Salary Register - Monthly | `/reports/employees-salary-register` | Current Month | Per-employee monthly salary register. Hierarchical column headers: Earnings sub-group, Taxes sub-group, Gross Pay, Net Pay. |
| Employees' Salary Statement | `/reports/employees-salary-statement` | Previous Month | Per-employee salary statement, same column structure as Salary Register. |
| Employees' Pay Summary | `/reports/employee-salary` | This Year | High-level per-employee summary: Gross Pay, Benefits, Deductions, Donations, Taxes, Reimbursements, Business Expense Reimbursements, Net Pay, Total Amount. Shows YTD totals. |
| Payroll Liability Summary | `/reports/payroll-liability` | Previous Month | Statutory liability summary by component type: Employee Contribution, Employer Contribution, Total Contribution. |
| Leave Encashment Summary | `/reports/leave-encashment-summary` | Previous Month | Employees who received leave encashment: Days, Amount. |
| Loss Of Pay Summary | `/reports/lop-summary` | Previous Month | LOP tracking: LOP Days, Adjusted LOP Days, Actual LOP Days per employee. |
| Variable Pay Earnings Report | `/reports/variable-pay-earnings-report` | This Year + specific earning | Per-earning-component report. Filter includes "Earnings" dropdown (defaulted to Overtime Allowance in test org). Shows Employee ID, Name, Total Amount Paid. |
| Scheduled Earning Summary | `/reports/scheduled-earnings-summary` | This Year | Tracks scheduled vs paid earnings: Earning Name, Total Scheduled Amount, Total Paid Amount, Amount Difference. |

---

## Category 2: Statutory Reports (8 reports)

| Report Name | URL Pattern | Default Filter | Description |
|-------------|-------------|----------------|-------------|
| EPF Summary | `/reports/epf-summary` | Previous Month | Comprehensive EPF: PF Account Number, UAN, PF Wage, Employee Contribution, Employer Contribution, Total Contribution, PF Amount, VPF Amount, EPS Amount, EDLI, PF Admin Charges. |
| EPF ECR Report | `/reports/epf-ecr-report` | Current Month | EPFO Electronic Challan-cum-Return data: UAN, Gross Wage, EPF Wages, EPS Wages, EDLI Wages, EPF Contribution Remitted, EPS Contribution Remitted, EPF Employer Contribution, LOP Days, Refund of Advances. Has "Customize Report Columns" feature (12 columns). |
| ESI Summary | `/reports/esi-summary` | Previous Month | ESI contribution summary: ESI Number, ESI Wages, Employee Contribution, Employer Contribution, Total Contribution. Supports "Customize Report Columns" (7 columns). |
| ESI Monthly summary | `/reports/esic-return` | Current Month | ESIC return format: Insurance Person Number, Insurance Person Name, Paid Days, Total Monthly Wages, Reason Code For Zero Working Days, Last Working Day. |
| Professional Tax Summary | `/reports/pt-summary` | Previous Month | PT by employee: Work Location, Employee Status, PT Amount. Supports "Compare With" and "Customize Report Columns" (5 columns). |
| Employee-wise Professional Tax Report | `/reports/pt-employees-summary` | Work Location (Head Office) + Current Month | PT detail: Employee Status, PT Amount, Taxable Wages. Filter: Work Location dropdown. |
| Annual Professional Tax Report | `/reports/pt-annual-summary` | Work Location + Fiscal Year | PT annual return statement: Period Start, Period End, No. of Employees, PT Amount. Useful for PT challans. |
| Labour Welfare Fund Summary | `/reports/lwf-summary` | Previous Month | LWF contributions: Total Employee Contribution, Total Employer Contribution, Total Contribution. Supports "Compare With". |

---

## Category 3: Employee/Contractor Reports (7 reports)

| Report Name | URL Pattern | Default Filter | Description |
|-------------|-------------|----------------|-------------|
| Compensation Details | `/reports/employee-ctc-master` | Current Month | CTC master view: Date of Joining, all Earnings components. `entity_list=compensation,benefit` in URL. Useful for HR. |
| Reimbursement Claim Summary | `/reports/reimbursement` | This Year | Reimbursement tracking: Reimbursement Type, Employee Eligible Amount, Paid Amount, Unclaimed Amount. |
| Employee Perquisites Summary | `/reports/employee-perquisite-summary` | Fiscal Year | Perquisite taxation: Perquisite Amount, Perquisite Amount Recovered, Perquisite Amount Chargeable. |
| Full and Final Settlement Report | `/reports/employee-termination-report` | This Year | Exits/FnF: Designation, Date of Joining, Last Working Day, Termination Type, Final Settlement Date, Payroll Status, Final Settlement Amount, Service Period. |
| Employees' Salary Revisions | `/reports/employee-salary-revisions` | This Year | Salary revision tracking: Revised CTC, Previous CTC, Amount Difference, %, Comments, Effective From, Payout Month, Status. |
| Salary Revision History | `/reports/employee-salary-revision-history` | Employee (dropdown) | Per-employee complete revision history. Filter: Employee picker. Shows all revision records with before/after CTC. |
| Salary Withhold Report | `/reports/salary-hold-report` | Previous Month | Salary hold management: Withheld Month, Amount to Pay, Withheld Reason, Release Status, Release Month. Toggle: "include_released_salary=false" in URL. |

---

## Category 4: Declarations & Investments (3 reports)

| Report Name | URL Pattern | Default Filter | Description |
|-------------|-------------|----------------|-------------|
| FBP declaration Report | `/reports/fbp-declaration-report` | Submitted Date | Flexible Benefits Plan declarations. Filter: Submitted Date. Empty for orgs not using FBP. |
| Investment Declaration Report | `/reports/investment-declaration-report` | Fiscal Year | Tax declarations summary: Tax Regime, Total Chapter VI-A Amount, Total Allowance Amount, Total HRA Amount, Total Other Income Amount, Total Previous Employment Amount, Total Direct Tax Amount. |
| Proof of Investment Report | `/reports/proof-of-investment-report` | Fiscal Year | POI submission status: Tax Regime, Submitted On, Approver Name, Status, Actual Amount, Approved Amount, Document Count. |

---

## Category 5: Deduction Reports (4 reports)

| Report Name | URL Pattern | Default Filter | Description |
|-------------|-------------|----------------|-------------|
| Benefits & Deductions Summary | `/reports/deductions-summary` | This Year | Org-level aggregate by type: Benefits and Deductions grouped by Type and Name, with Employee and Employer contributions. |
| Deductions Summary | `/reports/employee-post-tax-deductions-summary` | This Year | Post-tax deductions per employee: Total Employee Contribution. |
| Benefits Summary | `/reports/employee-pre-tax-deductions-summary` | This Year | Pre-tax benefits per employee: Total Employee Contribution, Total Employer Contribution, Total Contribution. |
| Donations Summary | `/reports/employee-donation-summary` | This Year | Employee giving/donations: Total Employee Contribution per employee. |

---

## Category 6: Taxes and Forms (2 reports)

| Report Name | URL Pattern | Default Filter | Description |
|-------------|-------------|----------------|-------------|
| TDS Deduction summary | `/reports/tds-summary` | This Year | TDS liability: PAN Number, Taxable Amount (Year), Tax on Income, Surcharge Amount, Cess, Total TDS Amount. |
| Form 24Q | `/reports/form-24q` | Fiscal Year | Quarterly TDS return form. Shows "Annexure II" tab. Empty state: "Form 24Q has not been generated for this Fiscal Year." — requires explicit generation. |

---

## Category 7: Loan Reports (4 reports)

| Report Name | URL Pattern (Actual) | Default Filter | Description |
|-------------|---------------------|----------------|-------------|
| Loan Outstanding Summary | `/reports/loan-outstanding-summary` | None (all time) | Outstanding loan ledger: Loan Name, Perquisite Interest Rate, Loan Number, Loan Amount, Instalment Amount, Disbursement Date, Opening Balance, Repayment Start Date, Pending Instalments, Principal Paid, Paid In Period, Balance Amount, End Date, Paid Perquisite Amount. Most detailed loan report (16 columns). |
| Loan Perquisite Summary | `/reports/loan-perquisite-summary` | Fiscal Year | Perquisite tax on loans: Loan Name, Loan Number, Loan Amount, Perquisite Amount. |
| Loan Perquisite Projection | `/reports/loan-perquisite-projection` | Fiscal Year | Future perquisite projection. Empty for this org. |
| Loan Summary Report | `/reports/loan-overall-summary` | This Year | Comprehensive loan view: all fields including Loan Status, Remaining Tenure, No Of Installments, Remaining Amount. 19 columns total. Actual page title: "Loan Overall Summary". |

---

## Category 8: Payroll Journal (1 report)

| Report Name | URL Pattern | Default Filter | Description |
|-------------|-------------|----------------|-------------|
| Payroll Journal Summary | `/reports/journal` | Previous Month | Double-entry accounting journal. Shows pay run date as section header, then Debit/Credit pairs. Example: "30/04/2026 - Payroll Journal" and "30/04/2026 - Wage Payment" each with Debit and Credit totals. Amount: ₹92,000.00 for April 2026 pay run. |

---

## Category 9: Activity (1 report)

| Report Name | URL Pattern | Default Filter | Description |
|-------------|-------------|----------------|-------------|
| Activity Logs | `/reports/activity-log` | Current Month (Custom Date) | Audit trail: Date, Activity Details, Description, Audit Trail. Filter: `filter_by=CreatedDate.CustomDate`. |

---

## Observations & Flags

### Critical Gaps (for our build)

- **No CSV export.** Only PDF, XLS, XLSX, Zoho Sheet. CSV is expected by most payroll teams for bank transfers and EPFO/ESIC portal uploads. Our build should include CSV as a standard format.
- **EPF ECR Report does not explicitly offer `.txt` or ECR format download.** The EPFO unified portal requires a specific ECR text file. Zoho's export appears to be in spreadsheet format only, which would require manual conversion by the accountant.
- **ESI Monthly Summary column "Reason Code For Zero Working Days"** aligns with ESIC monthly contribution statement requirements — important to implement correctly.
- **Form 24Q requires manual generation** — it is not auto-generated when payroll is finalized. Our build should clarify this workflow.
- **Activity Logs** are placed inside the Reports module (not a separate Audit Logs module). This is architecturally notable.

### Ambiguities

- The "Customize Report Columns" feature shows a column count badge (e.g., "12") but the selection UI was not fully captured due to navigation issues. Unclear if columns can be reordered or just shown/hidden.
- "Compare With" feature on Payroll Summary — unclear what comparison options are available beyond "None" (likely prior month, prior year, prior quarter).
- Form 24Q "Annexure II" tab — unclear what it contains without a generated Form 24Q.
- Benefits & Deductions Summary is categorized under "Deduction Reports" but its URL uses `/deductions-summary` — the actual mapping of pre-tax vs post-tax to "Benefits" vs "Deductions" naming is worth documenting in our data model.

### Well-Implemented

- The Reports Centre UI with tabs (Home/Favorites/Shared/Scheduled) is clean and professional.
- All 39 reports are accessible without any premium paywall on the trial plan.
- The "Show History" feature per report is excellent for audit purposes.
- The "Payroll Journal Summary" with proper double-entry journal format (Debit/Credit) is sophisticated — useful for accounting integration.
- PT Annual Report with Period Start/End columns is exactly what PT challan filing requires.
- The dual-calendar custom date picker spanning 1926–2126 allows for very long-horizon historical reporting.

---

## Screenshots

- `screenshots/00-reports-index.png` — Reports Centre list view (initial load)
- `screenshots/00-reports-list-full.png` — Full reports list (all 39)
- `screenshots/100-payroll-summary-default.png` — Payroll Summary default (This Year)
- `screenshots/100-payroll-summary-may2025.png` — Payroll Summary with May 2025 date range
- `screenshots/100-payroll-summary-fy2627.png` — Payroll Summary FY 2026-27 with data
- `screenshots/100-payroll-summary-export-menu.png` — Export dropdown (PDF/XLS/XLSX/Zoho Sheet)
