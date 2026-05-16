# Reports > Payroll Summary

## URL / Navigation Path
`https://payroll.zoho.in/#/reports/payroll-summary`

Full URL with parameters:
```
#/reports/payroll-summary?entity_list=compensation%2Creimbursement%2Cdeduction%2Cbenefit%2Cdonation%2Cpayroll_tax%2Cpayroll_run
  &filter_by=
  &from_date=2026-04-01
  &response_option=1
  &selectedGroup=home
  &to_date=2027-03-31
  &usestate=true
  &view_history=false
```

**Navigation path:** Sidebar > Reports > Reports Centre > Payroll Overview > Payroll Summary

## Category
Payroll Overview

## Report Type
Org-level aggregate (not per-employee). Shows totals across all employees for the selected period.

## Available Filters

| Filter | Type | Options / Behavior |
|--------|------|--------------------|
| Date Range | Preset + Custom dual calendar | This Month, This Quarter, This Year, Previous Month, Previous Quarter, Previous Year, Custom |
| More Filters (criteria builder) | Advanced filter | Field selector + Operator dropdown + Value text box. "Add Criteria" button for additional rows. |
| Compare With | Dropdown | None (default), and other period options (prior month/quarter/year — not fully captured) |

### Date Range Preset Details
- Default on first load: **This Year** (current fiscal year: 01/04/2026 to 31/03/2027)
- Custom mode: Shows dual-panel calendar. Left panel = from date, Right panel = to date.
- Calendar dropdowns: Month (Jan–Dec) + Year (1926–2126, continuous numeric options)
- After selecting dates, "Apply" and "Cancel" buttons appear below calendar

### URL Parameters (documented)
| Parameter | Description | Example |
|-----------|-------------|---------|
| `entity_list` | Comma-separated entities to include in report | `compensation,reimbursement,deduction,benefit,donation,payroll_tax,payroll_run` |
| `filter_by` | Advanced filter criteria (JSON-encoded) | (empty = no filter) |
| `from_date` | Start date (YYYY-MM-DD) | `2025-05-01` |
| `to_date` | End date (YYYY-MM-DD) | `2025-05-31` |
| `response_option` | Format option (1 = aggregate) | `1` |
| `selectedGroup` | Which sidebar group navigated from | `home`, `payroll_overview` |
| `usestate` | Whether to restore prior state | `true` |
| `view_history` | Show historical runs | `false` |

## Output Columns

The Payroll Summary is NOT a columnar table — it is a two-column summary table:

| Column | Description |
|--------|-------------|
| Pay Components | Section header (Earnings / Benefits / Donations / Deductions / Taxes / Reimbursements) then individual component names |
| Amount (₹) | Total amount across all employees for the period |

### Sections and Rows

```
Earnings
  ├── Basic
  ├── House Rent Allowance
  ├── Fixed Allowance
  └── [any other earning components]
─────────────────────────────────
Total Gross Pay              [subtotal row]
─────────────────────────────────
Benefits
  └── (empty: "No statutories were included during this period")
─────────────────────────────────
Donations
  └── (empty: "No data to display")
─────────────────────────────────
Deductions
  └── (empty: "No deductions were applied in this period")
─────────────────────────────────
Taxes
  └── (empty: "No data to display")
─────────────────────────────────
Reimbursements
  └── (empty: "No data to display")
─────────────────────────────────
Net Pay                      [footer row]
```

### Empty State Messages (documented)
- Benefits with no data: "No statutories were included during this period"
- Donations with no data: "No data to display"
- Deductions with no data: "No deductions were applied in this period"
- Taxes with no data: "No data to display"
- Reimbursements with no data: "No data to display"

## Sample Data (from FY 2026-27 — the May 2025 run data)

Data shows April 2026 run data (since the current FY is 2026-27):

| Pay Component | Amount |
|---------------|--------|
| Basic | ₹99,415.00 |
| House Rent Allowance | ₹30,966.00 |
| Fixed Allowance | ₹49,103.00 |
| **Total Gross Pay** | **₹1,79,484.00** |
| Benefits | No statutories were included during this period |
| Donations | No data to display |
| Deductions | No deductions were applied in this period |
| Taxes | No data to display |
| Reimbursements | No data to display |
| **Net Pay** | **₹1,79,484.00** |

Note: ₹1,79,484.00 = Basic + HRA + Fixed Allowance (99415 + 30966 + 49103 = 179,484). Net Pay = Gross Pay since no deductions/taxes are showing for this period. The org appears to have no TDS, PT, PF, ESI configured or all employees are below applicable thresholds.

## Actions on Report Page

| Action | Description | Behavior |
|--------|-------------|---------|
| Export as | Dropdown button | Opens menu: PDF, XLS (Microsoft Excel 1997-2004 Compatible), XLSX (Microsoft Excel), Export to Zoho Sheet |
| Show History | Icon button | Shows prior report runs for this report |
| Close this report | X icon | Returns to reports list |
| Run Report | Button (primary) | Applies "More Filters" criteria and re-runs report. Disabled until a More Filter value is entered. |
| More Filters | Button | Opens advanced filter builder panel below the date range |
| Compare With: None | Dropdown | Appears in report body above the data table. Allows period comparison. |

## Export Formats

| Format | Label Shown | File Type |
|--------|------------|-----------|
| PDF | PDF | .pdf |
| XLS | XLS (Microsoft Excel 1997-2004 Compatible) | .xls |
| XLSX | XLSX (Microsoft Excel) | .xlsx |
| Zoho Sheet | Export to Zoho Sheet | Opens in Zoho Sheet (cloud) |

No CSV export available for this report.

## Scheduling / Auto-email
Report scheduling is available from within the report (accessible via the Reports Centre > "Scheduled Reports" tab when set up). Currently empty state in this org: "Currently, you don't have any scheduled reports. Go to a report and schedule it to view it here."

The scheduling UI itself was not fully captured due to navigation constraints in this session.

## Premium / Free Tier
Available on trial plan. No paywall.

## Key Observations for Our Build

1. **Two-column aggregate format** (not a per-employee detail table). Our build should have both formats: an org-level summary (like this) and a per-employee detail report.

2. **entity_list URL parameter** drives which pay component categories appear. This suggests the report engine is configurable at the category level. Our build should support similar filtering by compensation type.

3. **Empty state messaging is context-specific**: Different messages for Benefits vs Taxes vs Deductions. Our build should use similar contextual empty states rather than generic "No data" messages.

4. **Net Pay = Gross Pay** when no deductions/taxes apply. Our engine must handle this correctly — not assume deductions always exist.

5. **"Compare With" feature** is prominently placed in the report body (above the data table, not in filters). Useful for MoM and YoY comparisons. Our build should support this as a first-class feature.

6. **No CSV export** — Zoho only offers Excel formats and Zoho Sheet. Our build should add CSV as a standard format since most Indian accountants use CSV for bank transfer file generation.

7. **Decimal formatting:** ₹ symbol with Indian comma notation (e.g., ₹1,79,484.00 — uses Indian lakh-crore grouping: 1,79,484 not 179,484). Our build must use Indian number formatting.

## Screenshots
- `screenshots/100-payroll-summary-default.png`
- `screenshots/100-payroll-summary-may2025.png`
- `screenshots/100-payroll-summary-fy2627.png`
- `screenshots/100-payroll-summary-export-menu.png`
