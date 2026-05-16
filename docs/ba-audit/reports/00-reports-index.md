# Reports > Master Index

**URL:** `https://payroll.zoho.in/#/reports`
**Total Reports:** 39 (all System Generated)
**App Framework:** Ember.js (confirmed via `data-ember-action` DOM attributes)
**Session Note:** A persistent Ember router state bug was observed — clicking certain buttons (especially in rapid succession) redirects to `/loans/new`. This is an Ember route guard triggered by an incomplete loan creation action in the app's state machine. All report data below was captured via JS DOM scraping to work around this bug.

---

## Reports Centre Navigation Tabs

| Tab | URL Fragment | Description |
|-----|-------------|-------------|
| Home | `#/reports` | Shows all 39 reports in a table |
| Favorites | `#/reports?selectedGroup=favorites` | User-starred reports (empty by default) |
| Shared Reports | `#/reports?selectedGroup=shared` | Reports shared by other users |
| Scheduled Reports | `#/reports?selectedGroup=scheduled` | Reports with auto-email schedules (empty by default; message: "Currently, you don't have any scheduled reports. Go to a report and schedule it to view it here.") |

## Report Categories (Sidebar)

| Category | Count |
|----------|-------|
| Payroll Overview | 9 |
| Statutory Reports | 8 |
| Employee/Contractor Reports | 7 |
| Declarations & Investments | 3 |
| Deduction Reports | 4 |
| Taxes and Forms | 2 |
| Loan Reports | 4 |
| Payroll Journal | 1 |
| Activity | 1 |
| **Total** | **39** |

---

## Complete Report Inventory

| # | Report Name | Category | URL Pattern | Filter Type | Key Columns |
|---|-------------|----------|-------------|-------------|-------------|
| 1 | Payroll Summary | Payroll Overview | `/reports/payroll-summary` | Date Range | Pay Components, Amount |
| 2 | Salary Register - Monthly | Payroll Overview | `/reports/employees-salary-register` | Month | Emp No, Name, Basic, HRA, Fixed Allowance, Tax on Income, Surcharge, Cess, Total Income Tax, PT, Gross Pay, Net Pay |
| 3 | Employees' Salary Statement | Payroll Overview | `/reports/employees-salary-statement` | Date Range | Emp No, Name, Basic, HRA, Fixed Allowance, Tax on Income, Surcharge, Cess, Total Income Tax, PT, Gross Pay, Net Pay |
| 4 | Employees' Pay Summary | Payroll Overview | `/reports/employee-salary` | Date Range | Emp No, Name, Gross Pay, Benefits, Deductions, Donations, Taxes, Reimbursements, Net Pay, Business Expense Reimbursements, Total Amount |
| 5 | Payroll Liability Summary | Payroll Overview | `/reports/payroll-liability` | Date Range | Liability Name, Employee Contribution, Employer Contribution, Total Contribution |
| 6 | Leave Encashment Summary | Payroll Overview | `/reports/leave-encashment-summary` | Date Range | Emp No, Name, Employee Status, Days, Amount |
| 7 | Loss Of Pay Summary | Payroll Overview | `/reports/lop-summary` | Date Range | Emp No, Name, Employee Status, LOP Days, Adjusted LOP Days, Actual LOP Days |
| 8 | Variable Pay Earnings Report | Payroll Overview | `/reports/variable-pay-earnings-report` | Date Range + Earnings Type | Employee ID, Name, Total Amount Paid |
| 9 | Scheduled Earning Summary | Payroll Overview | `/reports/scheduled-earnings-summary` | Date Range | Emp No, Name, Employee Status, Earning Name, Total Scheduled Amount, Total Paid Amount, Amount Difference |
| 10 | EPF Summary | Statutory Reports | `/reports/epf-summary` | Date Range | ID, Name, PF Account Number, UAN, PF Wage, Employee Contribution, Employer Contribution, Total Contribution, PF Amount, VPF Amount, EPS Amount, EDLI, PF Admin Charges |
| 11 | EPF ECR Report | Statutory Reports | `/reports/epf-ecr-report` | Month | Emp No, Name, UAN Number, Gross Wage, EPF Wages, EPS Wages, EDLI Wages, EPF Contribution Remitted, EPS Contribution Remitted, EPF Employer Contribution, LOP Days, Refund of Advances |
| 12 | ESI Summary | Statutory Reports | `/reports/esi-summary` | Date Range | Emp No, Name, ESI Number, ESI Wages, Employee Contribution, Employer Contribution, Total Contribution |
| 13 | ESI Monthly summary | Statutory Reports | `/reports/esic-return` | Month | Insurance Person Number, Insurance Person Name, Paid Days, Total Monthly Wages, Reason Code For Zero Working Days, Last Working Day |
| 14 | Professional Tax Summary | Statutory Reports | `/reports/pt-summary` | Date Range | Emp No, Name, Work Location, Employee Status, PT Amount |
| 15 | Employee-wise Professional Tax Report | Statutory Reports | `/reports/pt-employees-summary` | Work Location + Month | Emp No, Name, Employee Status, PT Amount, Taxable Wages |
| 16 | Annual Professional Tax Report | Statutory Reports | `/reports/pt-annual-summary` | Work Location + Fiscal Year | Period Start, Period End, No. of Employees, PT Amount |
| 17 | Labour Welfare Fund Summary | Statutory Reports | `/reports/lwf-summary` | Date Range | Emp No, Name, Total Employee Contribution, Total Employer Contribution, Total Contribution |
| 18 | Compensation Details | Employee/Contractor Reports | `/reports/employee-ctc-master` | Month | Emp No, Name, Date of Joining, Earnings, Basic, HRA, Fixed Allowance |
| 19 | Reimbursement Claim Summary | Employee/Contractor Reports | `/reports/reimbursement` | Date Range | Emp No, Name, Employee Status, Reimbursement Type, Employee Eligible Amount, Employee Paid Amount, Employee Unclaimed Amount |
| 20 | Employee Perquisites Summary | Employee/Contractor Reports | `/reports/employee-perquisite-summary` | Fiscal Year | Emp No, Name, Employee Status, Perquisite Amount, Perquisite Amount Recovered, Perquisite Amount Chargeable |
| 21 | Full and Final Settlement Report | Employee/Contractor Reports | `/reports/employee-termination-report` | Date Range | Emp No, Name, Designation, Date of Joining, Last Working Day, Termination Type, Final Settlement Date, Payroll Status, Final Settlement Amount, Service Period |
| 22 | Employees' Salary Revisions | Employee/Contractor Reports | `/reports/employee-salary-revisions` | Date Range | Emp No, Name, Employee Status, Revised CTC, Previous CTC, Amount Difference, Amount Difference %, Change in %, Comments, Effective From, Payout Month, Status |
| 23 | Salary Revision History | Employee/Contractor Reports | `/reports/employee-salary-revision-history` | Employee (dropdown) | Emp No, Name, Revised CTC, Previous CTC, Amount Difference, Amount Difference %, Change in %, Comments, Effective From, Payout Month |
| 24 | Salary Withhold Report | Employee/Contractor Reports | `/reports/salary-hold-report` | Date Range | Employee ID, Name, Designation, Employee Status, Withheld Month, Amount to Pay, Withheld Reason, Release Status, Release Month |
| 25 | FBP declaration Report | Declarations & Investments | `/reports/fbp-declaration-report` | Submitted Date | (no data rows visible — empty state for this org) |
| 26 | Investment Declaration Report | Declarations & Investments | `/reports/investment-declaration-report` | Fiscal Year | Emp No, Name, Tax Regime, Total Chapter VI-A Amount, Total Allowance Amount, Total HRA Amount, Total Other Income Amount, Total Previous Employment Amount, Total Direct Tax Amount |
| 27 | Proof of Investment Report | Declarations & Investments | `/reports/proof-of-investment-report` | Fiscal Year | Emp No, Name, Tax Regime, Submitted On, Approver Name, Status, Actual Amount, Approved Amount, Document Count |
| 28 | Benefits & Deductions Summary | Deduction Reports | `/reports/deductions-summary` | Date Range | Type, Name, Total Employee Contribution, Total Employer Contribution, Total Contribution |
| 29 | Deductions Summary | Deduction Reports | `/reports/employee-post-tax-deductions-summary` | Date Range | Emp No, Name, Employee Status, Total Employee Contribution |
| 30 | Benefits Summary | Deduction Reports | `/reports/employee-pre-tax-deductions-summary` | Date Range | Emp No, Name, Employee Status, Total Employee Contribution, Total Employer Contribution, Total Contribution |
| 31 | Donations Summary | Deduction Reports | `/reports/employee-donation-summary` | Date Range | Emp No, Name, Employee Status, Total Employee Contribution |
| 32 | TDS Deduction summary | Taxes and Forms | `/reports/tds-summary` | Date Range | Emp No, Name, PAN Number, Taxable Amount (Year), Tax on Income, Surcharge Amount, Cess, Total TDS Amount |
| 33 | Form 24Q | Taxes and Forms | `/reports/form-24q` | Fiscal Year | (No table — shows "Form 24Q has not been generated for this Fiscal Year"; has "Annexure II" tab) |
| 34 | Loan Outstanding Summary | Loan Reports | `/reports/loan-outstanding-summary` | None visible | Emp No, Name, Loan Name, Perquisite Interest Rate, Loan Number, Loan Amount, Instalment Amount, Disbursement Date, Opening Balance, Repayment Start Date, Pending Instalments, Principal Paid, Paid In Period, Balance Amount, End Date, Paid Perquisite Amount |
| 35 | Loan Perquisite Summary | Loan Reports | `/reports/loan-perquisite-summary` | Fiscal Year | Emp No, Name, Loan Name, Loan Number, Loan Amount, Perquisite Amount |
| 36 | Loan Perquisite Projection | Loan Reports | `/reports/loan-perquisite-projection` | Fiscal Year | (No data for this org) |
| 37 | Loan Summary Report | Loan Reports | `/reports/loan-overall-summary` | Date Range | Emp No, Name, Employee Status, Loan Name, Perquisite Interest Rate, Loan Number, Loan Amount, Instalment Amount, Loan Reason, Disbursement Date, Opening Balance, Repayment Start Date, No Of Installments, Remaining Tenure, Principal Paid, End Date, Remaining Amount, Paid Perquisite Amount, Loan Status |
| 38 | Payroll Journal Summary | Payroll Journal | `/reports/journal` | Date Range | Pay Run Date, Debit, Credit (journal entry format) |
| 39 | Activity Logs | Activity | `/reports/activity-log` | Date Range | Date, Activity Details, Description, Audit Trail |

---

## Premium vs Free Tier

All 39 reports are marked "System Generated" and appear accessible on the current trial plan. No reports are locked or marked as premium-only within the Reports Centre UI itself.

**Advanced Analytics Upsell:** The sidebar shows a persistent panel:
> "Advanced Financial Analytics for Zoho Payroll — Try Zoho Analytics"
> Link: `#/settings/integrations/zoho/analytics`

This implies deeper analytics (custom dashboards, cross-module BI) requires a separate Zoho Analytics subscription, but all 39 pre-built reports are included in Zoho Payroll.

---

## Common UI Features Across All Reports

| Feature | Present | Details |
|---------|---------|---------|
| Favorite/Star | Yes | Star icon per report row on index page; "Mark as Favorite" aria label |
| Compare With | Yes | "Compare With: None" dropdown on report view — compares current period with prior period |
| Customize Report Columns | Yes (select reports) | Shows column count e.g. "12" for EPF ECR Report, "7" for ESI Summary |
| Export as | Yes | PDF, XLS (Excel 1997-2004), XLSX (Excel), Export to Zoho Sheet |
| Show History | Yes | Button on each report — shows prior runs |
| Schedule | Yes (accessible from report view) | "Scheduled Reports" tab shows empty state with note to schedule from within a report |
| Search Reports | Yes | Search combobox at top of Reports Centre |
| More Filters | Yes | Advanced filter builder with Add Criteria rows (field + operator + value) |

---

## Export Formats (All Reports)

| Format | Description |
|--------|-------------|
| PDF | Branded PDF with org name header |
| XLS | Microsoft Excel 1997-2004 Compatible (.xls) |
| XLSX | Microsoft Excel current format (.xlsx) |
| Export to Zoho Sheet | Opens in Zoho's cloud spreadsheet tool |

**Note:** No CSV export option is visible. ECR-format text file export for EPFO portal was not confirmed — the EPF ECR Report page shows standard export options.

---

## Filter Types Used Across Reports

| Filter | Reports Using It |
|--------|-----------------|
| Date Range (preset + custom dual calendar) | Payroll Summary, Employees' Salary Statement, Employees' Pay Summary, Payroll Liability Summary, Leave Encashment Summary, Loss Of Pay Summary, Scheduled Earning Summary, EPF Summary, ESI Summary, PT Summary, LWF Summary, Reimbursement Claim Summary, Full and Final Settlement Report, Employees' Salary Revisions, Benefits & Deductions Summary, Deductions Summary, Benefits Summary, Donations Summary, TDS Deduction Summary, Salary Withhold Report, Payroll Journal Summary |
| Month (single month picker) | Salary Register - Monthly, EPF ECR Report, ESI Monthly Summary, Employee-wise PT Report, Compensation Details |
| Fiscal Year | Employee Perquisites Summary, Investment Declaration Report, Proof of Investment Report, Annual PT Report, Loan Perquisite Summary, Loan Perquisite Projection, Form 24Q |
| Work Location | Employee-wise PT Report, Annual PT Report |
| Employee (dropdown) | Salary Revision History |
| Earnings Type | Variable Pay Earnings Report |
| Submitted Date | FBP Declaration Report |

**Date Range Presets:** This Month, This Quarter, This Year, Previous Month, Previous Quarter, Previous Year, Custom (dual calendar with month/year dropdowns — years from 1926 to 2126)
