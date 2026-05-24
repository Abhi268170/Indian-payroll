# Pay Runs > Payment Advice & Export — Bank Advice, Export Data, Downloads

## URL / Navigation Path

All actions available from:
`https://payroll.zoho.in/#/payruns/{id}/summary` (Approved or Paid state)

Downloads panel (if accessed via Show Downloads):
`https://payroll.zoho.in/#/payruns/{id}/summary?canShowDownloadHistory=true`

## Purpose

Covers all file download and export capabilities within a pay run:
1. Bank Advice — file for manual bank transfers
2. Export Data — payroll data CSV/XLSX export
3. Comparison Report — month-over-month variance
4. Download all Payslips — bulk payslip PDF download
5. Download all TDS Worksheets — bulk TDS PDF download
6. Downloads panel — download history (limited utility observed)

## Bank Advice Download

### Trigger

Button visible in the info card strip: "Download Bank Advice" (icon + text)
Available in: Approved state, Paid state

### Behaviour

- Clicking initiates an **immediate file download** directly in the browser
- No confirmation dialog
- No password protection option (unlike individual payslip download)
- File format: Observed as a file download (CSV or XLSX format, bank-specific layout)
- Content expected: Employee Name, Account Number, IFSC Code, Amount, Reference

### Important Finding

Download Bank Advice does **NOT** create a download history entry in the "Show Downloads" panel. The panel showed "no files to download" even after Bank Advice was downloaded. This is because Bank Advice is a direct browser download, not an async server-generated artifact stored in the system.

**Implication for our build:** Store Bank Advice as a generated file in MinIO at generation time. Log the download event in `PayrollRunDownload` table. Enable re-download without regeneration.

## Export Data

### Trigger

"Export Data" button (with icon) in the Employee Summary tab toolbar
Available in: Draft, Approved, Paid states

### Options

| Option | Format | Content |
|--------|--------|---------|
| Payroll Data | CSV/XLSX | All employee earnings + deductions for this run |
| Comparison Report | CSV/XLSX | Current month vs previous month delta |

### Payroll Data Export Columns (expected, based on domain knowledge)

- Employee ID, Employee Name, Department, Designation
- Basic, HRA, Fixed Allowance, (other earnings)
- One-time Earnings (Bonus, Commission, etc.) if any
- Gross Earnings
- TDS, PT, PF Employee, ESI Employee, (other deductions)
- Total Deductions
- Net Pay
- Paid Days, LOP Days
- Payment Mode, Bank Account (masked)

### Comparison Report

- Shows delta between current month and previous month for each component per employee
- Useful for auditing salary changes, LOP impact, bonus additions
- Format: likely two columns per component (current / previous) + delta

## Downloads Panel

### Access

Page kebab > "Show Downloads" (in both Approved and Paid states)
URL appends: `?canShowDownloadHistory=true`

### Observed State

"No files to download" — panel was empty even after Bank Advice download. This confirms that direct browser downloads (Bank Advice) are not tracked here. The panel may be intended for async background jobs (bulk payslip generation, ECR files, etc.) that are queued and ready for download.

**Panel layout:**
- Header: "Downloads"
- Content area: "No files to download" (empty state)
- Close button

## Download All Payslips

### Trigger

Page kebab > "Download all Payslips" (in Paid state)
Also: icon button in page header (left of Send Payslip button)

### Behaviour

- Opens "Download Payslip" dialog (same as individual payslip download)
- Dialog: "You can protect the payslip with a password to keep the data secure."
  - Checkbox: "Protect this file with a password" (default: checked)
  - Password field if checkbox checked
  - Buttons: Download | Cancel
- Downloads all non-skipped employee payslips
- Format: Individual PDFs or ZIP archive (format not confirmed)

## Download All TDS Worksheets

### Trigger

Page kebab > "Download all TDS Worksheets" (in Paid state)

### Behaviour

- No dialog observed — likely direct download
- Downloads all TDS computation worksheets as PDFs
- One PDF per employee

## Fields Summary

| Download Type | Trigger | Format | Protected | History |
|---------------|---------|--------|-----------|---------|
| Bank Advice | Button in info card | CSV/XLSX | No | Not tracked |
| Export Payroll Data | Export Data button | CSV/XLSX | No | Not tracked |
| Export Comparison Report | Export Data > option | CSV/XLSX | No | Not tracked |
| Download all Payslips | Kebab / header button | PDF (per employee) | Optional (password) | Not confirmed |
| Download all TDS Worksheets | Kebab | PDF (per employee) | No | Not confirmed |
| Individual Payslip | Row kebab / Payslip column | PDF | Optional (password) | Not tracked |
| Individual TDS Sheet | Row kebab / TDS column | PDF (iframe) | No | Not tracked |

## Key Observations for Our Build

1. **Bank Advice format** — the file must match the bank's expected format (HDFC, ICICI, SBI each have different column layouts). Build a pluggable Bank Advice formatter: `IBankAdviceFormatter` with implementations per bank. Initially support generic tab/CSV format + HDFC format.
2. **Downloads panel is largely empty in Zoho** — this suggests Zoho does not background-generate payslip ZIPs or ECR files. Our build should use Hangfire for async background generation of large files (bulk payslips ZIP, ECR file) stored in MinIO, with download links in the panel.
3. **Comparison Report is high-value** — month-over-month delta export helps HR quickly spot errors (e.g., unexpected salary change, missing LOP, extra bonus). Implement from day one.
4. **Password protection for payslips** — default on is a strong security default. Implement in PDF generation layer. Password = employee date of birth in DDMMYYYY format is a common Indian convention. Check if Zoho uses this convention.
5. **Export availability by state** — Export Data is available in Draft state too (observed toolbar button in Draft). Useful for mid-run data validation. Our build should match this.
6. **No per-employee export** — all exports are bulk (all employees in the run). No observed option to export a single employee's payroll data as a row in the CSV.
7. **ECR file (PF) not observed** — the Employee Contribution Report for PF is expected to be in this area. Not visible in this org (PF not configured). Will appear when PF is set up. Our build: generate ECR file here, store in MinIO, show in Downloads panel.

## Screenshots

- `screenshots/61-downloads-panel-empty.png` — Downloads panel showing "no files to download"
- `screenshots/61-export-data-options.png` — Export Data dropdown: Payroll Data | Comparison Report

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
