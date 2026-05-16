# Reports > Statutory Compliance Reports

This file documents all 8 statutory reports in Zoho Payroll India.

---

## 102A: EPF Summary

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/epf-summary`

Full URL:
```
#/reports/epf-summary?filter_by=
  &from_date=2026-04-01
  &page=1
  &per_page=50
  &response_option=1
  &selectedGroup=home
  &to_date=2026-04-30
  &view_history=false
```

### Category
Statutory Reports

### Available Filters
| Filter | Type | Default |
|--------|------|---------|
| Date Range | Preset + Custom | Previous Month |
| More Filters | Criteria builder | — |

### Output Columns (14 columns)
| Column | Description | Statutory Reference |
|--------|-------------|---------------------|
| ID | Employee Number | — |
| Name | Employee Name | — |
| PF Account Number | Employee's PF account number | EPF & MP Act, 1952 |
| UAN | Universal Account Number | EPFO circular |
| PF Wage | Wages subject to PF (capped at ₹15,000 for statutory calculation) | EPF Act Section 6 |
| Employees' Contribution | Employee PF deduction (12% of PF Wage) | EPF Act Schedule III |
| Employer's Contribution | Total employer PF contribution | EPF Act Schedule III |
| Total Contribution | Employee + Employer | — |
| PF Amount | Employee contribution to PF account | — |
| VPF Amount | Voluntary Provident Fund (employee voluntary extra contribution) | — |
| PF Amount (employer) | Employer contribution to PF account | — |
| EPS Amount | Employer contribution to Employee Pension Scheme (8.33% of PF Wage, max ₹1,250) | EPS 1995 |
| EDLI | Employee's Deposit Linked Insurance levy (0.5% of PF Wage, max ₹75) | EDLI 1976 |
| PF Admin Charges | Admin charges (0.5% of PF Wage, min ₹75) | — |

### Features
- "Compare With" period comparison supported
- Pagination: 50 records per page

### Key Observations for Our Build
- **14 columns** — the most comprehensive PF report. Our build needs all of these.
- **PF Wage cap at ₹15,000** — if the employee's basic salary > ₹15,000, PF wage = ₹15,000 for statutory computation (employer and employee can opt to contribute on higher actual wages — VPF).
- **EDLI cap:** ₹75/month (0.5% of PF wage, max ₹75).
- **PF Admin Charges:** 0.5% of PF wages, minimum ₹75.
- VPF column is important — employees can contribute more than the mandatory 12%. This must be configurable in salary structure.
- **UAN** (Universal Account Number) is mandatory for EPFO filings. Must be stored per employee.

---

## 102B: EPF ECR Report

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/epf-ecr-report`

Full URL:
```
#/reports/epf-ecr-report?filter_by=
  &month=2026-05
  &page=1
  &per_page=50
  &response_option=1
  &selectedGroup=home
  &sort_column=
  &sort_order=
  &view_history=false
```

### Category
Statutory Reports

### Available Filters
| Filter | Type | Default |
|--------|------|---------|
| Month | Month picker | Current Month |
| More Filters | Criteria builder | — |

### Output Columns (12 columns — "Customize Report Columns: 12")
| Column | Description | EPFO ECR Field |
|--------|-------------|----------------|
| Employee Number | Internal employee ID | — |
| Employee Name | Full name | MEMBER_NAME |
| UAN Number | Universal Account Number | UAN |
| Gross Wage | Total salary paid | GROSS_WAGES_PAID |
| EPF Wages | Wages subject to EPF | EPF_WAGES |
| EPS Wages | Wages subject to EPS (same as EPF Wages, max ₹15,000) | EPS_WAGES |
| EDLI Wages | Wages subject to EDLI (same as EPF Wages) | EDLI_WAGES |
| EPF Contribution Remitted | Employee's EPF contribution amount | EPF_CONTRI_REMITTED |
| EPS Contribution Remitted | Employer's EPS contribution (8.33%) | EPS_CONTRI_REMITTED |
| EPF Employer Contribution | Employer's contribution to EPF (diff between 12% and EPS) | EPF_EPS_DIFF_REMITTED |
| LOP Days | Loss of Pay days in the month | NCP_DAYS |
| Refund of Advances | PF advance refund amount | REFUND_OF_ADVANCES |

### Features
- "Customize Report Columns" — user can show/hide columns (12 available)
- Column header: "EPF ECR Report" / "May, 2026"
- Pagination: 50 records per page
- Sort by column

### Key Observations for Our Build
- **ECR (Electronic Challan-cum-Return)** is the EPFO's standard format for monthly PF remittance. The column names above directly map to ECR file fields.
- **Critical gap in Zoho:** Export formats shown are PDF/XLS/XLSX. For EPFO portal upload, a `.txt` ECR file in EPFO's prescribed format is required. Our build should generate the ECR text file natively, not just an Excel sheet.
- **NCP Days** = Non-Contributing Period Days = LOP Days. This is mandatory in ECR filing.
- **Refund of Advances** = PF advance taken by employee and being repaid. Our build should track this separately.
- `EPF_EPS_DIFF_REMITTED` = 12% (EPF) - 8.33% (EPS) = 3.67% going to employee's EPF account. Some apps call this "EPF Difference".

---

## 102C: ESI Summary

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/esi-summary`

Full URL:
```
#/reports/esi-summary?filter_by=
  &from_date=2026-04-01
  &page=1
  &per_page=50
  &response_option=1
  &selectedGroup=home
  &sort_column=
  &sort_order=
  &to_date=2026-04-30
  &view_history=false
```

### Category
Statutory Reports

### Available Filters
| Filter | Type | Default |
|--------|------|---------|
| Date Range | Preset + Custom | Previous Month |
| More Filters | Criteria builder | — |

### Output Columns (7 columns — "Customize Report Columns: 7")
| Column | Description | Statutory Reference |
|--------|-------------|---------------------|
| Employee Number | Internal ID | — |
| Employee Name | Full name | — |
| ESI Number | Employee's ESI registration number | ESI Act 1948 |
| ESI Wages | Wages subject to ESI (gross salary up to ₹21,000/month) | ESI Act Section 2(22) |
| Employee Contribution | Employee ESI deduction (0.75% of ESI wages) | ESI (Amendment) 2019 |
| Employer Contribution | Employer ESI contribution (3.25% of ESI wages) | ESI (Amendment) 2019 |
| Total Contribution | Employee + Employer | — |

### Features
- "Compare With" period comparison
- "Customize Report Columns" (7 available)

### Key Observations for Our Build
- **ESI wage ceiling: ₹21,000/month** — employees earning above this are not covered.
- **Employee rate: 0.75%**, **Employer rate: 3.25%** (post-2019 revision).
- ESI Number must be stored per employee.
- Employees earning ≤ ₹21,000 must be enrolled in ESI. Above ₹21,000 = ESI not applicable.
- Note: ESI does NOT apply to establishments with fewer than 10 employees (or 20 in some states). Our build must have a configuration for the ESI applicability threshold.

---

## 102D: ESI Monthly Summary

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/esic-return`

Full URL:
```
#/reports/esic-return?filter_by=
  &month=2026-05
  &page=1
  &per_page=50
  &response_option=1
  &selectedGroup=home
  &sort_column=
  &sort_order=
  &view_history=false
```

### Category
Statutory Reports

### Available Filters
| Filter | Type | Default |
|--------|------|---------|
| Month | Month picker | Current Month |
| More Filters | Criteria builder | — |

### Output Columns (6 columns — "Customize Report Columns: 6")
| Column | Description | ESIC Portal Field |
|--------|-------------|-------------------|
| Insurance Person Number | Employee's ESI insurance number | IP_NUMBER |
| Insurance Person Name | Employee name as per ESI records | IP_NAME |
| Paid Days | Number of days paid in the month | PAID_DAYS |
| Total Monthly Wages | Total wages for ESI calculation | TOTAL_WAGES |
| Reason Code For Zero Working Days | Code if employee had zero paid days | REASON_CODE |
| Last Working Day | Last day of work (for exits) | LAST_WORKING_DAY |

### Key Observations for Our Build
- **ESIC Monthly Contribution Statement (MCS)** format — this maps directly to ESIC portal's expected upload format.
- "Insurance Person Number" (IP Number) is distinct from "ESI Number". ESIC uses "IP" terminology.
- "Reason Code For Zero Working Days" is required when an employee had no working days in a month (e.g., on leave without pay, on maternity leave, suspended).
- "Last Working Day" is critical for employees who resigned or were terminated during the month.
- Our build should generate the ESIC MCS in the format accepted by the ESIC portal (likely a specific CSV/text format per ESIC's spec).

---

## 102E: Professional Tax Summary

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/pt-summary`

Full URL:
```
#/reports/pt-summary?filter_by=
  &from_date=2026-04-01
  &page=1
  &per_page=50
  &response_option=1
  &selectedGroup=home
  &sort_column=
  &sort_order=
  &to_date=2026-04-30
  &view_history=false
```

### Category
Statutory Reports

### Available Filters
| Filter | Type | Default |
|--------|------|---------|
| Date Range | Preset + Custom | Previous Month |
| More Filters | Criteria builder | — |

### Output Columns (5 columns — "Customize Report Columns: 5")
| Column | Description |
|--------|-------------|
| Employee Number | Internal ID |
| Employee Name | Full name |
| Work Location | Office/branch location (drives which state's PT slab applies) |
| Employee Status | Active/On Leave/etc. |
| PT Amount | Professional Tax deducted for the period |

**Footer row:** Total Amount: ₹0.00 (no PT configured for this org — Head Office location has PT not set up)

### Features
- "Compare With" period comparison

### Key Observations for Our Build
- **Work Location** column is critical — PT is state-specific. Karnataka charges ₹200/month, Maharashtra charges up to ₹2,500/year, etc.
- Our build needs Work Location → State → PT Slab mapping in the statutory config tables.
- PT amount = ₹0.00 for this org because either PT is not configured or the employees' state has no PT.
- **PT is not shown separately in a tax sub-group in this report** — it appears as a deduction summary, unlike in the Salary Register where PT is under "Taxes".

---

## 102F: Employee-wise Professional Tax Report

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/pt-employees-summary`

Full URL:
```
#/reports/pt-employees-summary?filter_by=
  &month=2026-05
  &page=1
  &per_page=50
  &response_option=1
  &selectedGroup=home
  &sort_column=
  &sort_order=
  &view_history=false
  &work_location_id=3848927000000032281
```

### Category
Statutory Reports

### Available Filters
| Filter | Type | Default |
|--------|------|---------|
| Work Location | Dropdown | Head Office (ID: 3848927000000032281) |
| Month | Month picker | Current Month |
| More Filters | Criteria builder | — |

Note: Work Location is the PRIMARY filter — the report is per-location since PT is state/location specific.

### Output Columns (5 columns — "Customize Report Columns: 5")
| Column | Description |
|--------|-------------|
| Employee Number | Internal ID |
| Employee Name | Full name |
| Employee Status | Active/terminated/etc. |
| PT Amount | PT deducted |
| Taxable Wages | Salary on which PT is calculated |

### Key Observations for Our Build
- Filter shows `work_location_id` as URL param — PT report is location-scoped, not org-scoped.
- Our build needs a "Taxable Wages" column for PT — this is the gross salary for PT slab determination.
- Multiple work locations in one org will require switching the location filter per location for challan preparation.

---

## 102G: Annual Professional Tax Report

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/pt-annual-summary`

Full URL:
```
#/reports/pt-annual-summary?filter_by=
  &fiscal_year=2027
  &page=1
  &per_page=50
  &response_option=1
  &selectedGroup=home
  &sort_column=
  &sort_order=
  &view_history=false
  &work_location_id=3848927000000032281
```

### Available Filters
| Filter | Type | Default |
|--------|------|---------|
| Work Location | Dropdown | Head Office |
| Fiscal Year | Year picker | Current FY (2026-27 shown as `fiscal_year=2027` meaning ending year) |

### Output Columns (4 columns — "Customize Report Columns: 4")
| Column | Description |
|--------|-------------|
| Period Start | Start of PT challan period (e.g., 01/04/2026) |
| Period End | End of PT challan period |
| No. of Employees | Count of employees contributing PT in this period |
| PT Amount | Total PT for the period |

### Sample Data
Period: 01/04/2026 to 30/04/2026 — shown in table with ₹0.00 (no PT configured)

### Page Title
"PT Annual Return Statement" (actual page heading — differs from report name "Annual Professional Tax Report" shown in the index)

### Key Observations for Our Build
- **PT challan periods vary by state.** Some states require monthly challans, others half-yearly or annual. The "Period Start/End" approach in this report handles all scenarios.
- Fiscal year parameter uses end-year convention (`fiscal_year=2027` = FY 2026-27).
- Our build should use the same end-year convention for fiscal year parameters.

---

## 102H: Labour Welfare Fund Summary

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/lwf-summary`

Full URL:
```
#/reports/lwf-summary?filter_by=
  &from_date=2026-04-01
  &page=1
  &per_page=50
  &period_type=pay_period
  &response_option=1
  &selectedGroup=home
  &sort_column=
  &sort_order=
  &to_date=2026-04-30
  &view_history=false
```

### Category
Statutory Reports

### Available Filters
| Filter | Type | Default |
|--------|------|---------|
| Date Range | Preset + Custom | Previous Month |
| More Filters | Criteria builder | — |

Note: URL includes `period_type=pay_period` — this is different from other date-range reports and likely means filtering by pay run periods rather than calendar dates.

### Output Columns (5 columns — "Customize Report Columns: 5")
| Column | Description | Statutory Reference |
|--------|-------------|---------------------|
| Employee Number | Internal ID | — |
| Employee Name | Full name | — |
| Total Employee Contribution | LWF deducted from employee's salary | State LWF Act |
| Total Employer Contribution | Employer's LWF contribution (2x employee in most states) | State LWF Act |
| Total Contribution | Employee + Employer | — |

### Features
- "Compare With" period comparison

### Key Observations for Our Build
- **LWF is state-specific.** Not all states have LWF. States with LWF include Karnataka, Maharashtra, Andhra Pradesh, Tamil Nadu, Telangana, Gujarat, West Bengal, Punjab, Haryana, etc.
- LWF is typically deducted **twice a year** (June and December) or annually. The monthly `period_type=pay_period` suggests Zoho tracks it monthly but deducts at the correct interval.
- Employee contribution varies by state (e.g., Karnataka: ₹20 employee + ₹40 employer per year; Maharashtra: ₹12 employee + ₹36 employer biannually).
- Our build must have a LWF state-configuration table with deduction frequency, employee amount, and employer amount per state.
- The `period_type=pay_period` URL param suggests Zoho distinguishes between "calendar date" and "pay period" as the filtering basis — a useful distinction for our report engine.

---

## Statutory Reports — Comparative Table

| Report | URL Path | Filter | Columns | Govt Portal Format |
|--------|----------|--------|---------|-------------------|
| EPF Summary | `/epf-summary` | Date Range | 14 | No direct export |
| EPF ECR Report | `/epf-ecr-report` | Month | 12 | No `.txt` ECR export (gap) |
| ESI Summary | `/esi-summary` | Date Range | 7 | No direct export |
| ESI Monthly Summary | `/esic-return` | Month | 6 | No ESIC portal format export |
| PT Summary | `/pt-summary` | Date Range | 5 | No direct export |
| Employee-wise PT | `/pt-employees-summary` | Work Location + Month | 5 | No direct export |
| Annual PT | `/pt-annual-summary` | Work Location + Fiscal Year | 4 | No direct export |
| LWF Summary | `/lwf-summary` | Date Range | 5 | No direct export |

## Export Formats (All Statutory Reports)
PDF, XLS (Excel 1997-2004), XLSX (Excel), Export to Zoho Sheet.

**No government portal-compatible file formats** (ECR text, ESIC portal CSV, etc.) are explicitly offered. This is a significant gap for compliance teams.

## Premium / Free Tier
All statutory reports available on trial plan.

## Key Observations for Our Build (Statutory)

1. **ECR Format Gap (Critical):** Zoho does not export EPF ECR in EPFO portal text format. Our build should generate the `.txt` ECR file per EPFO's prescribed spec (pipe-delimited, header row with establishment details, one row per employee with UANs and contribution amounts).

2. **ESIC Portal Format Gap:** ESIC portal expects monthly contribution data in a specific format. Our build should generate this directly.

3. **Work Location as primary entity for PT and LWF:** Our data model needs Work Location → State → Statutory Config (PT slab, LWF amounts, LWF frequency). The Zoho URL shows `work_location_id` as a param in PT reports.

4. **PT Report Period flexibility:** The Annual PT Report shows "Period Start" and "Period End" columns — our build should support this period-based model for PT which varies by state.

5. **LWF deduction timing:** `period_type=pay_period` in the LWF URL suggests a distinction between deduction date and pay period. Our engine should support deduction scheduling (monthly, semi-annual, annual) per statutory type.

6. **ESI wage ceiling (₹21,000):** Must be stored in DB config, not hardcoded. When government revises the ceiling, only a DB update should be needed.

7. **PF wage ceiling (₹15,000 for statutory, but employers can choose higher):** Must be a configurable per-org setting.
