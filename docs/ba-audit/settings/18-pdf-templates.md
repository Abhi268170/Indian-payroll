# Settings > PDF Templates

## URL
`#/settings/templates/regular-payslip` (default sub-page)

## Sub-routes

### PAYSLIP section:
- `#/settings/templates/regular-payslip` — Regular Payslips
- `#/settings/templates/final-settlement-payslip` — Final Settlement Payslip

### LETTER TEMPLATES section:
- `#/settings/templates/letter-templates/salary-certificate` — Salary Certificate
- `#/settings/templates/letter-templates/salary-revision` — Salary Revision Letter
- `#/settings/templates/letter-templates/bonus-letter` — Bonus Letter

## Purpose
Manage PDF output templates for payslips and HR letters. Each template type has a default pre-built template; admins can select which variant is default and customise it with display preferences.

## Page Layout
Left secondary nav: PDF Templates > PAYSLIP (Regular Payslips, Final Settlement Payslip) + LETTER TEMPLATES (Salary Certificate, Salary Revision Letter, Bonus Letter).
Main content: Template cards with preview thumbnails.

---

## 1. Regular Payslips (`#/settings/templates/regular-payslip`)

### Available Templates (7 variants)

| # | Template Name | Default? | Actions |
|---|--------------|----------|---------|
| 1 | Elegant Template | Yes (DEFAULT) | Preview, Edit |
| 2 | Standard Template | No | Set as Default, Preview, Edit |
| 3 | Mini Template | No | Set as Default, Preview, Edit |
| 4 | Simple Template | No | Set as Default, Preview, Edit |
| 5 | Lite Template | No | Set as Default, Preview, Edit |
| 6 | Simple Spreadsheet Template | No | Set as Default, Preview, Edit |
| 7 | Professional Template | No | Set as Default, Preview, Edit |

### Actions Per Template
| Action | Behavior |
|--------|----------|
| Set as Default | Makes this template the default payslip PDF for all employees |
| Preview | Opens a preview of the rendered payslip PDF |
| Edit (pencil) | Opens the template editor with display preference checkboxes |

---

## Template Editor (Elegant Template example)
URL: `#/settings/templates/regular-payslip/{templateId}/edit`

### Left Panel: Customisation Options

#### Organisation Logo
| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Organisation Logo | File upload | Zoho-hosted | "Upload Logo" button. Shows current logo with "Choose File" |
| Show Organisation Address | Checkbox | Checked | Displays org address below logo on payslip |

#### Payslip Preferences
| Preference | Type | Default | Notes |
|-----------|------|---------|-------|
| Show PAN | Checkbox | Unchecked | Shows employee PAN on payslip |
| Show YTD Values | Checkbox | Checked | Shows Year-To-Date cumulative totals alongside monthly amounts |
| Show Bank Account Number | Checkbox | Checked | Shows bank account number on payslip |
| Show Work Location | Checkbox | Unchecked | Shows work location on payslip |
| Show Department | Checkbox | Unchecked | Shows department on payslip |
| Show Designation | Checkbox | Checked | Shows job designation on payslip |
| Show Benefits Summary | Checkbox | Unchecked | Shows benefits section (employer PF, ESI contributions) on payslip |

#### Buttons
| Button | Action |
|--------|--------|
| Save | Saves preference checkboxes |
| Preview | Opens a live preview of the payslip with current preferences |
| Close (×) | Returns to template list |

### Right Panel: Live Payslip Preview (Sample Data)

**Header:** Company logo + company name + company address | "Payslip For the Month [Month Year]"

**Employee Summary section:**
| Field | Sample Value |
|-------|-------------|
| Employee Name | Preet Setty |
| Designation | Software Engineer |
| Employee ID | emp012 |
| Date of Joining | 21-09-2014 |
| Pay Period | May 2026 |
| Pay Date | 31/05/2026 |
| Total Net Pay (large, highlighted) | ₹97,870.00 |
| Paid Days | 28 |
| LOP Days | 3 |

**Statutory Details row:**
| Field | Sample Value |
|-------|-------------|
| PF A/C Number | AA/AAA/0000000/000/0000000 |
| Bank Account No | 101010101010101 |
| UAN | 101010101010 |
| ESI Number | 1234567890 |

**Earnings table (with YTD columns):**
| Component | Amount | YTD |
|-----------|--------|-----|
| Basic | ₹60,000.00 | ₹1,20,000.00 |
| House Rent Allowance | ₹60,000.00 | ₹1,20,000.00 |
| Conveyance Allowance | ₹0.00 | ₹0.00 |
| Fixed Allowance | ₹0.00 | ₹0.00 |
| Bonus | ₹0.00 | ₹0.00 |
| Commission | ₹0.00 | ₹0.00 |
| Leave Encashment | ₹0.00 | ₹0.00 |

**Deductions table (with YTD columns):**
| Component | Amount | YTD |
|-----------|--------|-----|
| Income Tax | ₹22,130.00 | ₹2,65,554.00 |

**Summary row:**
- Gross Earnings: ₹1,20,000.00
- Total Deductions: ₹22,130.00
- **Total Net Payable:** ₹97,870.00

**Amount in Words:** "Indian Rupee Ninety-Seven Thousand Eight Hundred Seventy Only"

**Footer:** "-- This is a system-generated document. --"

---

## 2. Final Settlement Payslip (`#/settings/templates/final-settlement-payslip`)

### Available Templates (1 variant)
| Template Name | Default? | Actions |
|--------------|----------|---------|
| Final Settlement Template | Yes (DEFAULT) | Preview, Edit |

---

## 3. Salary Certificate (`#/settings/templates/letter-templates/salary-certificate`)

### Enable Feature Banner
> "Configure Salary Certificate Template. By configuring the Salary Certificate template, you can ensure that the Salary Certificate you send to your employees meet your organisational requirements."
> **Enable Now** button

### Templates
| Template Name | Default? |
|--------------|----------|
| Standard Template | Yes (DEFAULT) |

**Note:** Feature requires explicit enabling via "Enable Now" button. Locked/gated behavior.

---

## 4. Salary Revision Letter (`#/settings/templates/letter-templates/salary-revision`)

### Enable Feature Banner
> "Configure Salary Revision Letter Template. By configuring the Salary Revision Letter template, you can ensure that the Salary Revision Letter you send to your employees meet your organisational requirements."
> **Enable Now** button

### Templates
| Template Name | Default? |
|--------------|----------|
| Standard Template | Yes (DEFAULT) |

---

## 5. Bonus Letter (`#/settings/templates/letter-templates/bonus-letter`)

### Enable Feature Banner
> "Configure Bonus Letter Template. By configuring the Bonus Letter template, you can ensure that the Bonus Letter you send to your employees meet your organisational requirements."
> **Enable Now** button

### Templates
| Template Name | Default? |
|--------------|----------|
| Standard Template | Yes (DEFAULT) |

---

## Business Rules

1. **One default template per payslip type** — only one template can be set as Default; "Set as Default" on another removes the current default.
2. **Letter Templates require "Enable Now"** — Salary Certificate, Salary Revision Letter, and Bonus Letter are not active by default; admin must explicitly enable them.
3. **Regular Payslip templates are edit-only** — no add/delete; 7 fixed variants, customisable via preference checkboxes.
4. **Show PAN is opt-in** — PAN not shown by default on payslips (privacy).
5. **Show YTD is default-on** — cumulative YTD figures shown alongside monthly amounts.
6. **Bank Account displayed by default** — shown on payslip; admin can disable.
7. **Amount in Words** — auto-generated in Indian number system (lakh/thousand notation).

## Indian Statutory Notes
- 🔴 **PAN on payslip** — PAN visibility is opt-in, which is good for privacy. However for Form 16 correlation purposes, PAN should ideally be shown or at minimum included in the PDF metadata.
- **YTD values** — essential for employees to track annual tax liability against deductions.
- **Indian number format** — ₹1,20,000 uses Indian number system (lakh = 1,00,000). Amount in words uses Indian denomination.

## Cross-Module Impact
| Template | Triggered By |
|----------|-------------|
| Regular Payslip | Payroll Run → Pay → Payslip generation |
| Final Settlement Payslip | Full & Final Settlement processing |
| Salary Certificate | HR → Generate Salary Certificate for employee |
| Salary Revision Letter | Salary Revision → Generate revision letter |
| Bonus Letter | Bonus payment → Generate bonus letter |

## Observations & Notes
1. **7 payslip template variants** — large selection; typical products offer 2-3. All are built-in; no custom template upload.
2. **Live preview with sample data** — the editor renders a fully populated payslip with dummy employee data (Preet Setty). Useful for admins to visually validate before enabling.
3. **Letter Templates are gated** — "Enable Now" pattern suggests these may be subscription-tier features.
4. **Mini Template** — likely a single-page compact format useful for bulk printing.
5. For our build: PDF generation for payslips requires a template engine (Razor/RDLC/HTML-to-PDF). Must support: YTD columns, Indian number formatting, amount in words (Indian), logo upload, configurable field visibility. Minimum viable: 1 standard template + 1 mini template.

## Screenshots
- `docs/ba-audit/settings/screenshots/18-pdf-templates-regular-payslip.png`
- `docs/ba-audit/settings/screenshots/18-pdf-template-editor.png`
