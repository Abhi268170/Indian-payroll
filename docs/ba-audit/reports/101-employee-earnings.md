# Reports > Employee Earnings / Payroll Details Reports

This file covers three closely related per-employee reports under Payroll Overview:
- Salary Register - Monthly
- Employees' Salary Statement
- Employees' Pay Summary

---

## 101A: Salary Register - Monthly

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/employees-salary-register`

Full URL:
```
#/reports/employees-salary-register?entity_list=compensation%2Creimbursement%2Cdeduction%2Cbenefit%2Cdonation%2Cpayroll_tax%2Cpayroll_run
  &month=2026-05
  &page=1
  &per_page=50
  &response_option=1
  &selectedGroup=home
  &sort_column=
  &sort_order=
  &usestate=true
```

### Category
Payroll Overview

### Available Filters

| Filter | Type | Options |
|--------|------|---------|
| Month | Month picker | Single month selection (format: YYYY-MM in URL). Default: current month (May 2026 shown) |
| More Filters | Advanced criteria builder | Field + Operator + Value rows |

Note: This report uses **Month** filter (not Date Range like most others). This is appropriate since payroll is typically run per month.

### Output Columns (Hierarchical Headers)

The table uses a two-row header structure:

**Row 1 (group headers):**
| Employee Number | Employee Name | Earnings | | | Taxes | | | | | Gross Pay | Net Pay |

**Row 2 (sub-headers under each group):**

Under **Earnings:**
- Basic
- House Rent Allowance
- Fixed Allowance

Under **Taxes:**
- Tax on Income
- Surcharge
- Cess
- Total Income Tax
- Professional Tax

Standalone columns:
- Employee Number (sortable)
- Employee Name (sortable)
- Gross Pay
- Net Pay

### Sample Data (May 2026)
Report header: "lerno — Salary Register - Monthly — May, 2026"
No data rows visible for May 2026 (the completed pay run was May 2025, not May 2026).

### Pagination
- `page=1`, `per_page=50` — paginated. 50 records per page default.
- Sort by column supported (`sort_column` and `sort_order` URL params)

### Key Observations for Our Build
- **Hierarchical column headers** (group + sub-group) require careful table rendering. Standard HTML tables support `colspan` for this.
- **Separate Tax columns** (Tax on Income, Surcharge, Cess, Total Income Tax) are all visible at the register level — very useful for TDS reconciliation.
- **Professional Tax** appears as a column in the Salary Register — treated as a Tax sub-column, not a Deduction.
- Month-based filter (not date range) is appropriate for a monthly register.

---

## 101B: Employees' Salary Statement

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/employees-salary-statement`

Full URL:
```
#/reports/employees-salary-statement?entity_list=compensation%2Creimbursement%2Cdeduction%2Cbenefit%2Cdonation%2Cpayroll_tax%2Cpayroll_run
  &filter_by=
  &from_date=2026-04-01
  &page=1
  &per_page=50
  &response_option=1
  &selectedGroup=home
  &sort_column=
  &sort_order=
  &to_date=2026-04-30
  &usestate=true
  &view_history=false
```

### Available Filters
| Filter | Type | Default |
|--------|------|---------|
| Date Range | Preset + Custom | **Previous Month** (default) |
| More Filters | Criteria builder | — |

### Output Columns
Identical to Salary Register - Monthly:
- Employee Number, Employee Name
- Earnings group: Basic, House Rent Allowance, Fixed Allowance
- Taxes group: Tax on Income, Surcharge, Cess, Total Income Tax, Professional Tax
- Gross Pay, Net Pay

**Difference from Salary Register:** Uses Date Range filter instead of Month — allowing cross-month queries. Useful for generating statements for partial periods.

### Key Observations for Our Build
- Same column structure as Salary Register — our build can reuse a single report component with a filter type parameter.
- Default to "Previous Month" makes sense since current month may not be finalized.

---

## 101C: Employees' Pay Summary

### URL / Navigation Path
`https://payroll.zoho.in/#/reports/employee-salary`

Full URL:
```
#/reports/employee-salary?filter_by=
  &from_date=2026-04-01
  &page=1
  &per_page=50
  &response_option=1
  &selectedGroup=home
  &sort_column=
  &sort_order=
  &to_date=2027-03-31
  &view_history=false
```

Note: No `entity_list` parameter — this report uses a different API endpoint that aggregates all component types.

### Available Filters
| Filter | Type | Default |
|--------|------|---------|
| Date Range | Preset + Custom | **This Year** (full FY) |
| More Filters | Criteria builder | — |

### Output Columns
| Column | Description |
|--------|-------------|
| Employee Number | Employee ID |
| Employee Name | Full name |
| Gross Pay | Total earnings |
| Benefits | Pre-tax benefit contributions |
| Deductions | Post-tax deductions |
| Donations | Charitable contributions via payroll |
| Taxes | TDS + surcharge + cess |
| Reimbursements | Non-taxable reimbursements |
| Business Expense Reimbursements | Business expense claims paid |
| Net Pay | Take-home after all deductions |
| Total Amount | Net Pay + Reimbursements |

### Sample Data (FY 2026-27)

| Column | Total |
|--------|-------|
| Total Amount | ₹1,79,484.00 |
| Benefits | ₹0.00 |
| Deductions | ₹0.00 |
| Donations | ₹0.00 |
| Taxes | ₹0.00 |
| Reimbursements | ₹0.00 |
| Net Pay | ₹1,79,484.00 |
| Business Expense Reimbursements | ₹0.00 |

(Totals shown as footer row in the table)

### Key Observations for Our Build
- This is the broadest per-employee overview — shows ALL pay element categories in one row.
- "Total Amount" = Net Pay + Reimbursements (reimbursements add back since they're non-deductions).
- "Business Expense Reimbursements" is a separate column from regular "Reimbursements" — suggests two types of reimbursements in Zoho's model: regular (benefits-style) and business expense.
- Default to "This Year" (FY) means this is typically used for annual review, not monthly processing.

---

## Comparison: Three Payroll Overview Detail Reports

| Aspect | Salary Register | Salary Statement | Pay Summary |
|--------|----------------|-----------------|-------------|
| URL | `/employees-salary-register` | `/employees-salary-statement` | `/employee-salary` |
| Default Filter | Current Month | Previous Month | This Year |
| Filter Type | Month picker | Date Range | Date Range |
| Column Depth | Hierarchical (Earnings sub-cols, Tax sub-cols) | Same as Register | High-level categories only |
| Use Case | Monthly processing, payroll register | Statements for specific period | Annual review, headcount reporting |
| entity_list param | Yes | Yes | No |
| Paginated | Yes (50/page) | Yes (50/page) | Yes (50/page) |
| Sort | Yes | Yes | Yes |

## Export Formats (All Three Reports)
PDF, XLS (Excel 1997-2004), XLSX (Excel), Export to Zoho Sheet.

## Scheduling / Auto-email
Available (same mechanism as Payroll Summary — accessible within each report).
