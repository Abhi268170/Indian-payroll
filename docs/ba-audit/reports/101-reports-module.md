# Item 101: Reports Module — Full Audit

**URL:** `https://payroll.zoho.in/#/reports`  
**Module:** Reports  
**Navigation:** Left sidebar → "Reports"  
**Audit Date:** 2026-05-15

---

## Screenshots

- `screenshots/101-reports-centre-list.png` — Reports Centre full list (all 39 reports)
- `screenshots/101-payroll-summary-report.png` — Payroll Summary report view
- `screenshots/101-report-export-options.png` — Export format dropdown

---

## Reports Centre — Landing Page

**URL:** `#/reports`  
**Title:** "Reports Centre"

### Left Sidebar (Report Categories)

| Category | Reports Count |
|----------|--------------|
| Home | All reports (default view) |
| Favorites | Starred/bookmarked reports |
| Shared Reports | Reports shared between users |
| Scheduled Reports | Automated report delivery |

Report category filters:
- Payroll Overview
- Statutory Reports
- Employee/Contractor Reports
- Declarations & Investments
- Deduction Reports
- Taxes and Forms
- Loan Reports
- Payroll Journal
- Activity
- Advanced Financial *(Zoho Analytics integration — separate subscription)*

### Reports Table

**Table header:** "All Reports 39"

Columns:
- REPORT NAME (clickable — opens report)
- REPORT CATEGORY
- CREATED BY (all = "System Generated" for built-in reports)
- LAST VISITED (timestamp)

---

## Complete Report Inventory (39 Reports)

### Payroll Overview (8 reports)

| # | Report Name | URL Pattern | Description |
|---|-------------|-------------|-------------|
| 1 | Payroll Summary | `#/reports/payroll-summary` | Org-level aggregated pay summary: Earnings, Benefits, Deductions, Taxes, Net Pay |
| 2 | Salary Register - Monthly | `#/reports/employees-salary-register` | Employee-wise monthly salary breakdown with all components |
| 3 | Employees' Salary Statement | `#/reports/employees-salary-statement` | Detailed salary statement per employee |
| 4 | Employees' Pay Summary | `#/reports/employees-pay-summary` | Summarised net pay per employee |
| 5 | Payroll Liability Summary | `#/reports/payroll-liability-summary` | Outstanding payroll liabilities |
| 6 | Leave Encashment Summary | `#/reports/leave-encashment-summary` | Leave encashment amounts per employee |
| 7 | Loss Of Pay Summary | `#/reports/lop-summary` | LOP deductions per employee per pay run |
| 8 | Variable Pay Earnings Report | `#/reports/variable-pay-earnings` | Variable pay components |
| 9 | Scheduled Earning Summary | `#/reports/scheduled-earning-summary` | Scheduled/one-time earnings breakdown |

*Note: Listed 9 but Zoho categorises 8 under Payroll Overview — Scheduled Earning Summary may be sub-category.*

### Statutory Reports (7 reports)

| # | Report Name | Description |
|---|-------------|-------------|
| 10 | EPF Summary | Monthly EPF employee + employer contributions per employee |
| 11 | EPF ECR Report | Electronic Challan cum Return file (downloadable for EPFO portal upload) |
| 12 | ESI Summary | ESI contributions per employee |
| 13 | ESI Monthly summary | Month-wise ESI contribution summary |
| 14 | Professional Tax Summary | PT deductions summary |
| 15 | Employee-wise Professional Tax Report | PT per employee |
| 16 | Annual Professional Tax Report | Full FY PT summary |
| 17 | Labour Welfare Fund Summary | LWF employee + employer contributions |

### Employee/Contractor Reports (7 reports)

| # | Report Name | Description |
|---|-------------|-------------|
| 18 | Compensation Details | CTC breakdown per employee |
| 19 | Reimbursement Claim Summary | Claims submitted, approved, paid |
| 20 | Employee Perquisites Summary | Perquisite values (vehicle, loans, etc.) |
| 21 | Full and Final Settlement Report | F&F amounts for exited employees |
| 22 | Employees' Salary Revisions | History of salary revision changes |
| 23 | Salary Revision History | Revision timeline per employee |
| 24 | Salary Withhold Report | Withheld payments |

### Declarations & Investments (3 reports)

| # | Report Name | Description |
|---|-------------|-------------|
| 25 | FBP declaration Report | Flexible Benefit Plan declarations |
| 26 | Investment Declaration Report | IT investment declarations |
| 27 | Proof of Investment Report | POI submission status |

### Deduction Reports (4 reports)

| # | Report Name | Description |
|---|-------------|-------------|
| 28 | Benefits & Deductions Summary | Combined benefits + deductions summary |
| 29 | Deductions Summary | All deductions (PF, PT, LWF, etc.) |
| 30 | Benefits Summary | Employer benefit contributions |
| 31 | Donations Summary | Employee giving/donation amounts |

### Taxes and Forms (2 reports)

| # | Report Name | Description |
|---|-------------|-------------|
| 32 | TDS Deduction summary | Monthly TDS deducted per employee |
| 33 | Form 24Q | Quarterly TDS return report |

### Loan Reports (4 reports)

| # | Report Name | Description |
|---|-------------|-------------|
| 34 | Loan Outstanding Summary | Remaining balances on active loans |
| 35 | Loan Perquisite Summary | Perquisite tax values per loan per period |
| 36 | Loan Perquisite Projection | Projected perquisite amounts for future periods |
| 37 | Loan Summary Report | All loans with status, amounts, instalment details |

### Payroll Journal (1 report)

| # | Report Name | Description |
|---|-------------|-------------|
| 38 | Payroll Journal Summary | Accounting journal entries for payroll (for Zoho Books integration) |

### Activity (1 report)

| # | Report Name | Description |
|---|-------------|-------------|
| 39 | Activity Logs | User action history / audit trail within the payroll module |

---

## Report UI — Common Pattern

All reports share a consistent UI:

### Filter Bar

| Filter | Type | Notes |
|--------|------|-------|
| Date Range / Month | Date filter chip | Presets: This Year, Custom, etc. |
| More Filters | Button | Opens criteria builder panel |
| Run Report | Button (primary) | Re-executes report with current filters |
| Compare With | Dropdown | "None" or another period for comparison |

**More Filters criteria builder:**
- Row: [Row#] [Field Select] [Condition Select] [Value] [Delete row]
- Fields available (Payroll Summary example): Payroll Status, Department, Designation, Work Location
- "Add Criteria" button: appends new filter row
- "Run Report" / "Cancel" buttons

### Toolbar Actions

| Action | Description |
|--------|-------------|
| Export as | PDF \| XLS (Microsoft Excel 1997-2004 Compatible) \| XLSX (Microsoft Excel) \| Export to Zoho Sheet |
| Show History | Toggle history view of previous report runs |
| Customize Report Columns | (on tabular reports) — lets user reorder/hide columns |
| Close | Returns to reports list |

### Export Formats

1. **PDF** — formatted PDF document
2. **XLS** — Excel 97-2004 format (.xls)
3. **XLSX** — Modern Excel format (.xlsx)
4. **Export to Zoho Sheet** — direct push to Zoho Sheets (cloud spreadsheet)

---

## Report Details: Payroll Summary

**URL:** `#/reports/payroll-summary?entity_list=compensation,reimbursement,deduction,benefit,donation,payroll_tax,payroll_run&filter_by=&from_date=YYYY-MM-DD&to_date=YYYY-MM-DD&response_option=1&selectedGroup=home&usestate=true&view_history=false`

**Default date range:** This Year (01/04/YYYY to 31/03/YYYY+1)

**Table structure:**
| Section | Components |
|---------|-----------|
| Earnings | Basic, HRA, Fixed Allowance, other components |
| Total Gross Pay | Sum of all earnings |
| Benefits | Employer statutory contributions |
| Donations | Employee giving deductions |
| Deductions | PF, PT, LWF, other deductions |
| Taxes | Income tax, surcharge, cess |
| Reimbursements | Approved reimbursements |
| **Net Pay** | Final amount paid |

**Data observed (April + May 2026):**
- Basic: ₹99,415
- HRA: ₹30,966
- Fixed Allowance: ₹49,103
- Total Gross Pay: ₹1,79,484
- Net Pay: ₹1,79,484 (no deductions/taxes in test org)

---

## Report Details: Salary Register - Monthly

**URL:** `#/reports/employees-salary-register?entity_list=compensation,reimbursement,deduction,benefit,donation,payroll_tax,payroll_run&month=YYYY-MM&page=1&per_page=50&response_option=1&selectedGroup=home&sort_column=&sort_order=&usestate=true`

**Filter:** Month (default = current month)

**Columns observed:**
- EMPLOYEE NUMBER
- EMPLOYEE NAME
- EARNINGS: BASIC | HOUSE RENT ALLOWANCE | FIXED ALLOWANCE
- TAXES: TAX ON INCOME | SURCHARGE | CESS | TOTAL INCOME TAX | PROFESSIONAL TAX
- GROSS PAY
- NET PAY

**Pagination:** 50 per page (`per_page=50` in URL)  
**Sorting:** `sort_column`, `sort_order` URL params  
**Customize Report Columns:** button present — allows hiding/reordering columns

**Data observed (May 2026):**
- EMP001: Basic ₹37,417 | HRA ₹14,967 | Fixed ₹13,100 | Gross ₹65,484 | Net ₹65,484
- EMP002: Basic ₹11,000 | HRA ₹0 | Fixed ₹11,000 | Gross ₹22,000 | Net ₹22,000

---

## Analytics for Zoho Payroll

Displayed at the bottom of category list:
- Label: "Analytics for Zoho Payroll"
- CTA: "Try Zoho Analytics" → `#/settings/integrations/zoho/analytics`
- Separate Zoho Analytics subscription required
- Advanced financial analysis capabilities not in base product

---

## Business Rules

1. **All 39 reports are "System Generated"** — no custom report builder in base product.
2. **Report persistence:** "Last Visited" timestamp tracked per report — shows most recent access date.
3. **Favorites/Shared/Scheduled** are sub-views — reports can be bookmarked, shared between users, or scheduled for automatic delivery (email).
4. **Export formats:** PDF, XLS, XLSX, Zoho Sheet — consistent across all reports.
5. **Criteria builder:** Multi-row filter with AND logic (add criteria appends row).
6. **EPF ECR Report** is the statutory file required for EPFO portal submission — downloadable directly from reports.
7. **Form 24Q** report (in Taxes and Forms) is separate from Form 24Q in Taxes & Forms module — the report here is for data review; the TDS module handles actual quarterly return.
8. **Activity Logs report** provides audit trail visibility from the reports module.
9. **Payroll Journal Summary** feeds accounting integrations (Zoho Books).
10. **Loan Perquisite Projection** is forward-looking — projects future perquisite tax liability for active loans.

---

## Cross-Module Dependencies

| Report | Depends On |
|--------|-----------|
| EPF ECR Report | EPF enabled in Statutory Components |
| ESI Summary | ESI enabled in Statutory Components |
| PT Summary | PT configured per work location |
| LWF Summary | LWF enabled per state |
| Form 24Q | Tax Details (PAN, TAN, Tax Deductor) configured |
| Loan reports | Loans module — active loans with repayment history |
| FBP Declaration Report | Claims & Declarations configured; employee declarations submitted |
| Activity Logs | All modules — captures user actions across the system |

---

## Open Questions

- [ ] Can custom reports be created (custom SQL/filter builder)? Not visible in trial — may be paid feature.
- [ ] What is the "Scheduled Reports" workflow — what delivery options (email, FTP)?
- [ ] Can reports be shared with specific users or all users in org?
- [ ] Is there a "Bank Advice" report distinct from the pay run Bank Advice download?
- [ ] What are the specific column options in "Customize Report Columns"?
- [ ] Is there a report for employee bank details / payment modes?
- [ ] Does Activity Logs show login history or only payroll actions?
- [ ] Can Payroll Journal Summary be exported directly to Zoho Books (auto-post journal entries)?
