# Reports > Export Formats Deep-Dive

## Available Export Formats

The "Export as" button (dropdown-toggle, class: `dropdown-toggle btn btn-default reports-btn`) is present on every report view page.

### Format Options (confirmed via DOM inspection of Payroll Summary)
| Format | Label in UI | File Type | Notes |
|--------|------------|-----------|-------|
| PDF | PDF | .pdf | Includes org name header (branding) |
| XLS | XLS (Microsoft Excel 1997-2004 Compatible) | .xls | Legacy Excel format, max 65,535 rows |
| XLSX | XLSX (Microsoft Excel) | .xlsx | Current Excel format, higher row limits |
| Zoho Sheet | Export to Zoho Sheet | Opens in browser | Cloud spreadsheet in Zoho's ecosystem |

**No CSV export is available in Zoho Payroll.**

## Export Button Technical Details
- Button class: `dropdown-toggle btn btn-default reports-btn` with `aria-expanded="false"` (closed by default)
- `aria-controls` points to a dropdown container rendered as a portal in the DOM (not adjacent to the button in the DOM tree)
- Dropdown opens on button click; options rendered dynamically

## PDF Export Characteristics

From the report header metadata visible in the DOM:
- **Org name:** Shown as heading (e.g., "lerno")
- **Report name:** Shown as sub-heading
- **Date range:** Shown as descriptor (e.g., "From 01/04/2026 To 31/03/2027")
- **Currency symbol:** ₹ (Indian Rupee)
- **Number format:** Indian lakh-crore notation (e.g., ₹1,79,484.00)

The actual PDF output was not opened in this session, so exact page layout (letterhead, footer, page numbering) was not captured.

## XLS vs XLSX Differences
| Aspect | XLS (1997-2004) | XLSX |
|--------|----------------|------|
| Max rows | 65,535 | 1,048,576 |
| Max columns | 256 | 16,384 |
| Compatibility | Works in older Excel versions | Requires Excel 2007+ |
| File size | Larger (binary format) | Smaller (XML+ZIP) |
| Use case | Legacy systems, older ERPs | Modern use |

For payroll with large employee counts (1000+), XLS row limits could be hit. XLSX is the recommended format.

## Zoho Sheet Export
- Opens the report data in Zoho's cloud spreadsheet application (Zoho Sheet)
- Requires the user to be logged into their Zoho account (which they already are)
- Useful for collaborative editing, sharing with external accountants via Zoho's sharing features
- Data appears as a live sheet rather than a file download

## ECR / Government Portal Format Exports

### EPF ECR Text File
**Status: NOT AVAILABLE in Zoho Payroll.**
The EPFO Unified Portal requires ECR (Electronic Challan-cum-Return) data in a specific `.txt` pipe-delimited format. Zoho Payroll does not export EPF ECR data in this format — only in Excel/PDF.

**EPFO ECR format spec:**
```
Header: #~#ESTABLISHMENT_DETAILS~#
Row: UAN~MEMBER_NAME~GROSS_WAGES_PAID~EPF_WAGES~EPS_WAGES~EDLI_WAGES~EPF_CONTRI_REMITTED~EPS_CONTRI_REMITTED~EPF_EPS_DIFF_REMITTED~NCP_DAYS~REFUND_OF_ADVANCES
```

HR teams using Zoho Payroll must manually reformat the Excel export into ECR text format, which is error-prone and time-consuming.

### ESIC Portal Format
**Status: NOT AVAILABLE in Zoho Payroll.**
ESIC portal accepts a specific format for monthly contribution statements. Not natively exported.

### PT Challan
**Status: NOT AVAILABLE.**
PT challans vary by state authority and typically require a form-fill on the respective state's commercial tax portal. Zoho does not generate pre-filled PT challan documents.

### TDS / Form 24Q
**Status: PARTIAL.** Form 24Q is generated separately (not auto-generated with payroll run). The Form 24Q report shows "Annexure II" as a tab but shows "Form 24Q has not been generated for this Fiscal Year" — requires explicit generation action before export.

## Number Formatting in Exports
- Currency: ₹ (INR) prefix
- Indian comma notation: Lakhs with comma at 2 digits from right after hundreds, then every 3 digits
  - Example: ₹1,79,484.00 (not ₹179,484.00)
  - Another: ₹92,000.00
- Decimal: 2 decimal places (paise precision)
- All monetary values are `decimal` type (from domain rules)

## Date Formatting in Reports
- Report headers: "From DD/MM/YYYY To DD/MM/YYYY" (e.g., "From 01/04/2026 To 31/03/2027")
- Month-based reports: "Month, YYYY" (e.g., "May, 2026")
- Table date cells: DD/MM/YYYY (e.g., "01/04/2026")
- Last Visited column in report index: "DD/MM/YYYY hh:mm AM/PM" (e.g., "15/05/2026 04:40 PM")

## Key Observations for Our Build

1. **Add CSV as a primary export format.** CSV is the most universally supported format for downstream processing (bank uploads, custom scripts, other systems). Zoho's omission of CSV is a gap our build should address.

2. **ECR text file export is mandatory** for any payroll product targeting Indian companies with EPF obligations. Without this, the compliance workflow is broken — HR must manually reformat Excel to ECR text. This should be a first-class feature in our PF report.

3. **ESIC portal format export** should be on the roadmap.

4. **PDF branding:** Our PDF exports should include:
   - Organization logo (configured in org settings)
   - Org name and address
   - Report name
   - Date range or period
   - "Confidential" watermark (optional, configurable)
   - Page numbers
   - Generated on: timestamp

5. **XLSX over XLS:** Default export should be XLSX. XLS can be offered for legacy compatibility but should not be the primary format.

6. **Indian number formatting is mandatory:** All monetary amounts in all exports must use Indian lakh-crore comma notation. This must be handled at the export layer, not left to Excel's default number formatting.

7. **Zoho Analytics integration** (paid add-on) provides custom reporting capabilities. Our build should provide a native analytics/custom report builder to avoid this dependency.
