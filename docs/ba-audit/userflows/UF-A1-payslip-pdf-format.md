# UF-A1: Payslip PDF Format

**Module:** Pay Runs > Payroll History > Pay Run Summary > Employee Row
**Tested:** 2026-05-16
**Approach:** Navigated to May 2026 PAID pay run (payroll ID 3848927000000034159). Clicked "View" in Payslip column for Arjun Mehta (EMP001) and Priya Sharma (EMP002). Clicked "Download Payslip" in slide-in panel — triggered password-protection modal. Downloaded PDFs and extracted text via pdftotext. Also tested "Download all Payslips" from the pay run overflow dropdown.

---

## Findings

### 1. Payslip Slide-In Panel (Per-Employee)

**Trigger:** Clicking "View" in the Payslip column of the Employee Summary table.

**Panel layout (right-side drawer):**

| Section | Content |
|---------|---------|
| Header | Employee name (link to employee profile), "Net Pay" label, Emp. ID, Net Pay amount |
| Payment status banner | "Paid on DD/MM/YYYY through [Payment Mode]" with tick icon |
| Attendance summary table | Payable Days / LOP Days / Actual Payable Days |
| Leave summary table | (empty if no leaves configured) |
| Earnings table | Two-column: Component name | Amount |
| Deductions table | Grouped by category "Taxes" / sub-items: Income Tax, KL Professional Tax |
| Net Pay footer | "Net Pay" label + amount |
| Action buttons | "Download Payslip" | "Send Payslip" |

**Arjun Mehta (EMP001) Payslip Panel Data:**
- Emp. ID: EMP001
- Payable Days: 31, LOP Days: 2, Actual Payable Days: 29
- Earnings: Basic ₹37,417.00 | HRA ₹14,967.00 | Fixed Allowance ₹13,100.00
- Deductions: Income Tax ₹0.00 | KL Professional Tax ₹0.00
- Net Pay: ₹65,484.00
- Payment: Paid on 29/05/2026 through Manual Bank Transfer

**Priya Sharma (EMP002) Payslip Panel Data:**
- Emp. ID: EMP002
- Payable Days: 31, LOP Days: 0, Actual Payable Days: 31
- Earnings: Basic ₹11,000.00 | Fixed Allowance ₹11,000.00
- Deductions: Income Tax ₹0.00 | KL Professional Tax ₹0.00
- Net Pay: ₹22,000.00
- Payment: Paid on 29/05/2026 through Manual Bank Transfer

---

### 2. Password Protection Modal

When "Download Payslip" is clicked, a modal appears before download:

**Modal title:** "Download Payslip"

**Fields:**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| "Protect this file with a password" | Checkbox | Checked (enabled by default) | Enables PDF owner-password encryption |

**Buttons:** Download | Cancel

**Business Rule:** Password protection is ON by default. Admin can uncheck to download unprotected.

---

### 3. Payslip PDF Format (EMP001 — Arjun Mehta, May 2026)

**Filename:** `Payslip_EMP001_May_2026.pdf`
**Saved to:** `.playwright-mcp/Payslip-EMP001-May-2026.pdf`

**PDF Technical Properties:**
| Property | Value |
|----------|-------|
| PDF version | 1.5 |
| Producer | OpenPDF 1.3.26 |
| Page size | 612 x 792 pts (US Letter / 8.5 x 11 inches, portrait) |
| Pages | 1 |
| Encrypted | YES — RC4 algorithm |
| Permissions | Print: YES, Copy: YES, Change: NO, Add Notes: NO |
| User password | EMPTY (opens without password prompt) |
| Owner password | Set by Zoho (prevents editing/modification) |
| Creation date | Fri May 15 15:47:49 2026 IST (originally created) |
| Mod date | Sat May 16 18:36:56 2026 IST (download timestamp) |
| Font | Ubuntu (RSHNDR+Ubuntu / HPXHJU+Ubuntu-Bold) |

**Password Notes:**
- The PDF is encrypted with RC4 but the USER password is empty — meaning it opens freely in any PDF viewer.
- The OWNER password prevents modification. The password is NOT the PAN number.
- "Protect this file with a password" checkbox enables this encryption. When unchecked, an unencrypted PDF is generated.

**PDF Layout (text extraction via pdftotext):**

```
HEADER:
  Company name: lerno
  Label: "Payslip For the Month"
  Month: May 2026
  Company address: lerno kazhakoottam thiruvananthapuram Kerala 695010 India

EMPLOYEE SUMMARY SECTION:
  Employee Name : Arjun Mehta
  Designation   : Senior Software Engineer
  Employee ID   : EMP001
  Date of Joining: 01/04/2025
  Paid Days      : 29
  Pay Period     : May 2026
  LOP Days       : 2
  Pay Date       : 29/05/2026
  Bank Account No: 50100123456789

NET PAY BANNER (right side):
  ₹65,484.00
  Total Net Pay

EARNINGS TABLE:
  Columns: Component | Amount | YTD
  Basic             ₹37,417.00   YTD: ₹77,415.00
  House Rent Allowance ₹14,967.00  YTD: ₹30,966.00
  Fixed Allowance   ₹13,100.00   YTD: ₹27,103.00
  Gross Earnings    ₹65,484.00

DEDUCTIONS TABLE:
  Columns: Component | Amount | YTD
  Total Deductions  ₹0.00        YTD: ₹0.00

TOTAL NET PAYABLE:
  Formula shown: "Gross Earnings - Total Deductions"
  Amount in Words: "Indian Rupee Sixty-Five Thousand Four Hundred Eighty-Four Only"

FOOTER:
  "-- This is a system-generated document. --"
```

**Key observations:**
- YTD column shows year-to-date cumulative figures alongside current month amounts
- Bank account number is SHOWN IN FULL (not masked) — 🔴 security concern
- No logo/image in PDF (company name text only)
- No signature block or digital signature
- Amount in words uses full Indian Rupee denomination text
- Statutory disclaimer: "This is a system-generated document"
- No Form 16 reference or TAN number on payslip itself

---

### 4. Payslip PDF Format (EMP002 — Priya Sharma, May 2026)

**Filename:** `Payslip_EMP002_May_2026.pdf`
**Saved to:** `.playwright-mcp/Payslip-EMP002-May-2026.pdf`

**Key differences from EMP001:**
- No HRA component (not configured in her salary structure)
- Paid Days: 31 (no LOP)
- Gross Earnings: ₹22,000.00
- Bank Account No: 31234567890
- Amount in Words: "Indian Rupee Twenty-Two Thousand Only"

---

### 5. Bulk Download — "Download all Payslips"

**Trigger:** Pay run header → "Show dropdown menu" (⋮) → "Download all Payslips"

**Dropdown options on pay run overflow menu:**
1. Download all Payslips
2. Download all TDS Worksheets
3. Show Downloads
4. Delete Recorded Payment

**"Download all Payslips" Modal:**

| Field | Type | Options |
|-------|------|---------|
| Portal Status | Dropdown (filter) | Both Enabled and Disabled (default) |
| Work Location | Multi-select listbox | All Locations (default) |
| Designation | Multi-select listbox | All Designations (default) |

**Buttons:** Download | Cancel

**Behavior after clicking Download:**
- Toast notification: "Downloading process has been initiated! Kindly wait, Within 15 minutes the link to download your documents will be ready."
- This is an ASYNC background job — not an instant download.
- The link to download the resulting file is delivered via Notifications (bell icon) after processing.
- Expected format: ZIP file containing individual PDFs (based on message text, not directly confirmed).
- No password-protection option shown in the bulk download modal.

---

### 6. Filename Naming Convention

| Scenario | Filename Pattern |
|----------|-----------------|
| Single employee payslip | `Payslip_{EmpID}_{MonthName}_{Year}.pdf` |
| Example | `Payslip_EMP001_May_2026.pdf` |
| Bulk download | Async job — link delivered via notification (format not directly observed) |

---

## Screenshots / Files

- `payslip-download-modal.png` — password protection modal
- `payslip-arjun-panel.png` — slide-in payslip panel for Arjun Mehta
- `payrun-more-options-dropdown.png` — overflow dropdown with 4 options
- `download-all-payslips-modal.png` — bulk download modal with filters
- `.playwright-mcp/Payslip-EMP001-May-2026.pdf` — Arjun Mehta payslip PDF
- `.playwright-mcp/Payslip-EMP002-May-2026.pdf` — Priya Sharma payslip PDF

---

## Gaps / Open Questions

- [ ] **Bank account masking:** Bank account number appears in FULL on the payslip PDF (e.g., "50100123456789"). Does Zoho offer an option to mask the account number on the payslip? This is a 🔴 data privacy concern.
- [ ] **Bulk download ZIP format:** Could not directly confirm the ZIP structure. The 15-minute async job delivery via notification link was not waited out.
- [ ] **Password when "Protect" is checked:** What is the owner password used? Is it derived from PAN, DOB, or a fixed admin-configured value?
- [ ] **Logo:** No logo appears in the PDF. Is there a setting to upload company logo for payslip?
- [ ] **"Send Payslip" button:** Sends by email to the employee's registered email — exact email template not captured in this session.
- [ ] **Payslip layout configuration:** Is there any customization of payslip layout/template available in settings?
