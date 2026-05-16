# UF-A3: TDS Sheet (Tax Worksheet) Format

**Module:** Pay Runs > Payroll History > Pay Run Summary > Employee Row
**Tested:** 2026-05-16
**Approach:** Clicked "View" in the TDS Sheet column for Arjun Mehta (EMP001) and Priya Sharma (EMP002) from the May 2026 PAID pay run. TDS sheet opens in a modal dialog containing an iframe rendering a PDF. Discovered the direct API endpoint for the PDF (`/api/v1/employees/{id}/taxworksheet`) and downloaded both sheets. Extracted text using pdftotext.

---

## Findings

### 1. TDS Sheet Modal

**Trigger:** "View" button in the "TDS Sheet" column of the Employee Summary table.

**Modal characteristics:**
- **Title:** "TDS Sheet for May 2026"
- **Content:** An iframe embedding a PDF rendered directly by the browser PDF viewer
- **API endpoint:** `https://payroll.zoho.in/api/v1/employees/{employeeId}/taxworksheet?month={YYYY-MM}&accept=pdf&print=true&organization_id={orgId}`
- **Actions:** Print button | Close button
- **No download button** — only Print. PDF can be saved via browser Print > Save as PDF.
- **No "Download" option** — contrast with payslips which have a Download Payslip button.

---

### 2. TDS Worksheet — Arjun Mehta (EMP001), May 2026

**File:** `.playwright-mcp/TDS-Sheet-EMP001-May-2026.pdf`
**PDF size:** 39,238 bytes | 1 page | Letter size (612x792 pts) | Not encrypted

**Document Header:**
| Field | Value |
|-------|-------|
| Company name | lerno |
| Company address | lerno kazhakoottam thiruvananthapuram Kerala 695010 India |
| Document title | TDS WORK SHEET for the month of May 2026 |
| Employee Name | Arjun Mehta, EMP001 |
| PAN | ABCPM1234A |
| Designation | Senior Software Engineer |
| Tax Regime | New Regime |

**Income Computation Table (Particulars / Actual / Projection / Total):**

| Component | Actual (Apr–May YTD) | Projection (Jun–Mar) | Annual Total |
|-----------|---------------------|---------------------|--------------|
| Basic | ₹77,415.00 | ₹3,99,980.00 | ₹4,77,395.00 |
| House Rent Allowance | ₹30,966.00 | ₹1,59,990.00 | ₹1,90,956.00 |
| Fixed Allowance | ₹27,103.00 | ₹1,40,030.00 | ₹1,67,133.00 |

**Tax Computation Flow (New Regime, FY 2025-26):**

| Step | Particulars | Amount |
|------|-------------|--------|
| 1 | Gross Earnings (Total Annual Income) | ₹8,35,484.00 |
| 2 | Allowances exempt u/s 10 | ₹0.00 |
| 3 | Total After Exemption (1-2) | ₹8,35,484.00 |
| 4 | Taxable income under previous employment (i) Income after exemptions | ₹0.00 |
| 4 | (ii) Less: Professional Tax | ₹0.00 |
| 4 | Total taxable income under previous employment | ₹0.00 |
| 5 | Gross Total (3+4) | ₹8,35,484.00 |
| 6a | Entertainment allowance | ₹0.00 |
| 6b | Tax on employment (PT) | ₹0.00 |
| 6c | Standard Deduction (Section 16(ia)) | ₹75,000.00 |
| 6 | Total Under Section 16 | ₹75,000.00 |
| 7 | Income Chargeable Under Head Salaries (5-6) | ₹7,60,484.00 |
| 8 | Any other income reported by employee | ₹0.00 |
| 9 | Gross Total Income (7+8) | ₹7,60,484.00 |
| 10 | Deductions under Chapter VI-A | ₹0.00 |
| 11 | Total Income (rounded to nearest ₹10) | ₹7,60,480.00 |

**Tax Slab Calculation (New Regime FY 2025-26):**

| Taxable Income Range | Rate | Tax Amount |
|---------------------|------|-----------|
| ₹0 to ₹4,00,000 | 0% | ₹0.00 |
| ₹4,00,001 to ₹8,00,000 | 5% on ₹3,60,480 | ₹18,024.00 |
| Total Tax on Income | — | ₹18,024.00 |

**Rebates & Reliefs:**

| Item | Amount | Note |
|------|--------|------|
| Rebate u/s 87A | ₹18,024.00 | Full rebate since income < ₹12,00,000 |
| Relief u/s 89(1) | ₹0.00 | — |
| Total Tax on Income (after rebate) | ₹0.00 | Full rebate applicable |
| Surcharge | ₹0.00 | — |
| Education Cess (4%) | ₹0.00 | On ₹0.00 |
| Relief u/s 90/91 | ₹0.00 | — |
| Tax Payable incl. Cess | ₹0.00 | — |

**Statutory Note on worksheet:**
> "If taxable income is less than ₹12,00,000.00, tax rebate of a maximum of ₹60,000.00 is provided under Section 156."

**Important:** The section reference used is "Section 156" — this appears to be Zoho's internal reference to Section 87A of the Income Tax Act, which provides rebate for income below ₹7,00,000 (old limit) / ₹12,00,000 (new regime FY2025-26 Budget announcement). The wording "Section 156" is unusual and may be a bug or internal codename. 🔴

**TDS Summary:**

| Item | Amount |
|------|--------|
| TDS till last month | ₹0.00 |
| TDS for May | ₹0.00 |
| TDS by Previous Employer | ₹0.00 |
| Total TDS Deducted | ₹0.00 |
| Tax Payable / Refundable | ₹0.00 |
| TDS per month for next 10 months | ₹0.00 |

**Effective TDS:** ₹0 (income falls within ₹12L rebate threshold under new regime).

---

### 3. TDS Worksheet — Priya Sharma (EMP002), May 2026

**File:** `.playwright-mcp/TDS-Sheet-EMP002-May-2026.pdf`
**PDF size:** 38,813 bytes

**Document Header:**
| Field | Value |
|-------|-------|
| Employee Name | Priya Sharma, EMP002 |
| PAN | *(BLANK — not shown)* |
| Designation | Junior Developer |
| Tax Regime | New Regime |

**Observation:** Priya Sharma's TDS worksheet does NOT show a PAN number — the PAN field is present in the header but appears empty. This is because PAN was not entered in her employee master. 🔴

**Income Computation:**

| Component | Actual | Projection | Annual Total |
|-----------|--------|-----------|--------------|
| Basic | ₹22,000.00 | ₹1,10,000.00 | ₹1,32,000.00 |
| Fixed Allowance | ₹22,000.00 | ₹1,10,000.00 | ₹1,32,000.00 |
| Gross Earnings | — | — | ₹2,64,000.00 |

**Tax Computation Summary:**
- Standard Deduction: ₹75,000
- Income chargeable under head salaries: ₹1,89,000
- Total taxable income: ₹1,89,000
- Tax on income: ₹0 (entirely within 0% slab, ₹0–₹4,00,000)
- TDS: ₹0

---

### 4. Structural Observations

**Column 3 (Projection) logic:**
- The worksheet uses the current month's salary to extrapolate the remaining months' income.
- "Actual" = YTD from April to current month
- "Projection" = current month salary × remaining months (FY runs Apr–Mar)
- For May 2026 (month 2 of FY 2026-27): Actual = 2 months, Projection = 10 months

**Standard Deduction:**
- ₹75,000 applied automatically for new regime (Budget 2024 announcement, effective FY 2024-25 onwards)
- No employee action required — system applies it automatically

**Section 87A Rebate:**
- Applied automatically when total income < ₹12,00,000 under new regime
- Max rebate capped at ₹60,000 (but worksheet shows full tax amount as rebate: ₹18,024)

**Chapter VI-A Deductions:**
- Section labelled as "Deductions under Chapter VIII" on the worksheet — this is a typo/nomenclature error. Correct reference is Chapter VI-A. 🟡
- Shows qualifying and deductible amounts — both ₹0 since no IT declaration submitted

**Previous Employer Income:**
- Dedicated row for "Taxable income under previous employment"
- Supports prior-employer YTD entry (salary + PT paid to previous employer)

---

### 5. Download / Print Flow

| Action | Behavior |
|--------|---------|
| View TDS Sheet (in-app) | Opens modal with iframe rendering PDF |
| Print button | Opens browser print dialog for the iframe PDF |
| No Download button | Employee/admin must use browser print-to-PDF to save |
| "Download all TDS Worksheets" | Async job via overflow dropdown (same as bulk payslips — 15-minute async delivery) |

---

## Screenshots / Files

- `tds-sheet-arjun-modal.png` — TDS sheet modal (iframe view)
- `tds-sheet-arjun-pdf.png` — Full-page screenshot of TDS PDF in browser
- `.playwright-mcp/TDS-Sheet-EMP001-May-2026.pdf` — Arjun Mehta TDS PDF
- `.playwright-mcp/TDS-Sheet-EMP002-May-2026.pdf` — Priya Sharma TDS PDF

---

## Gaps / Open Questions

- [ ] **"Section 156" reference:** The rebate is labelled "Section 156" instead of "Section 87A". This appears to be an incorrect statutory reference. 🔴 Needs verification with Zoho support.
- [ ] **"Chapter VIII" label:** Deductions chapter is labelled "Chapter VIII" instead of "Chapter VI-A". 🟡
- [ ] **No download button on TDS modal:** Why is there no direct "Download" button like on payslips? Intentional design choice?
- [ ] **PAN missing for EMP002:** TDS worksheet is generated even without PAN. For TDS filing/Form 16, PAN is mandatory. 🔴
- [ ] **Previous employer income input:** How does admin/employee enter prior employer salary? Is there a UI for this (IT Declaration section)?
- [ ] **HRA exemption calculation:** For Arjun (who has HRA component), the exemption u/s 10(13A) shows ₹0. Is HRA exemption computation supported in new regime? (It should NOT be — new regime does not allow HRA exemption, only standard deduction.) Correct behavior confirmed.
