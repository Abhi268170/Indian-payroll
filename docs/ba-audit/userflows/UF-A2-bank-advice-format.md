# UF-A2: Bank Advice File Format

**Module:** Pay Runs > Payroll History > Pay Run Summary
**Tested:** 2026-05-16
**Approach:** Clicked "Download Bank Advice" button on the May 2026 PAID pay run summary header. Explored the Download Bank Advice modal in full — opened Bank Statement Format dropdown to enumerate all bank templates. Downloaded the Standard Format XLS and parsed it using xlrd.

---

## Findings

### 1. Download Bank Advice Button Location

- **Location:** Pay run summary card header (top section), right side alongside Payroll Cost / Total Net Pay figures
- **Button label:** "Download Bank Advice" (with download icon)
- **Selector:** `button[data-test-selector="download-bank-advice"]`

---

### 2. Download Bank Advice Modal

**Modal title:** "Download Bank Advice"

**Informational banner (Zoho Payments upsell):**
> "Skip the bank advice next time. Use Payouts by Zoho Payments to disburse salaries directly from Zoho Payroll."
> [Setup Payout] button

**Form Fields:**

| Field | Type | Default | Editable | Notes |
|-------|------|---------|----------|-------|
| Generate Bank Advice for | Dropdown (disabled) | "Bank Transfer Employees (Paid & Unpaid)" | No (locked) | Pre-selected, cannot be changed |
| Filters | Button (expands filter options) | — | Yes | Likely filters by work location / designation |
| Bank Statement Format | Dropdown (searchable) | "Standard Format" | Yes | Bank-specific templates — full list below |
| Download as | Dropdown | "XLS (Microsoft Excel 1997-2004 Compatible)" | Yes | Output format options |
| Protect this file with a password | Checkbox | Unchecked | Yes | Optional password encryption |

**Buttons:** Download | Cancel

---

### 3. Bank Statement Format Options (Complete List)

The "Bank Statement Format" dropdown contains bank-specific templates, grouped by bank name:

| Group | Option |
|-------|--------|
| Axis Bank | Axis Bank |
| Axis Bank | Axis Bank Standard Format |
| Citi Bank | Citi Bank |
| Citi Bank | Citi Bank Standard Format |
| DMIT Bank | DMIT Bank |
| HDFC Bank | HDFC Bank(Updated) |
| ICICI Bank | ICICI Bank |
| ICICI Bank | ICICI Bank(Biz360) |
| IDFC Bank | IDFC Bank |
| Kotak Mahindra Bank | Kotak Mahindra Bank |
| Standard Chartered Bank | Standard Chartered Bank |
| Standard Format | **Standard Format** (default, selected) |

**Total:** 11 format options across 9 banks, plus a generic Standard Format.

**Key observation:** SBI (State Bank of India) does NOT have a specific bank format template despite being one of the employee's banks (Priya Sharma uses SBI). They would use "Standard Format".

---

### 4. Downloaded File Details

**Triggered by:** Selecting "Standard Format" (default), "XLS" format, no password, clicking Download.

| Property | Value |
|----------|-------|
| Filename | `Payroll_Bank_Statement.xls` |
| Format | XLS (Microsoft Excel 97-2004, .xls) |
| Sheets | 1 sheet named "BankStatement" |
| Rows | 3 (1 header + 2 data rows) |
| Columns | 7 |

---

### 5. Bank Statement File Structure (Standard Format)

**Sheet: BankStatement**

| Column | Header | EMP001 Sample | EMP002 Sample | Notes |
|--------|--------|---------------|---------------|-------|
| A | Employee No | EMP001 | EMP002 | Employee ID from master |
| B | Employee Name | Arjun Mehta | Priya Sharma | Full name |
| C | Amount | 65484.0 | 22000.0 | Net pay amount (numeric, no currency symbol) |
| D | Bank Name | HDFC Bank | State Bank of India | Bank name from employee bank details |
| E | Bank Account No | 50100123456789 | 31234567890 | Full account number (unmasked) |
| F | IFSC Code | HDFC0001234 | SBIN0001234 | IFSC from employee bank details |
| G | Beneficiary Name | Arjun Mehta | Priya Sharma | Employee name (same as column B) |

**No totals row.** No header row above the column headers. Single flat table structure.

**Field precision:** Amount is stored as floating-point number (65484.0), not formatted with commas or currency symbol. Bank-specific formats may differ.

---

### 6. Business Rules Observed

- Only employees with payment mode = "Bank Transfer" are included. Employees paid by cheque or cash would be excluded.
- "Paid & Unpaid" employees are included (pre-selected, locked) — meaning even employees whose salary has not yet been transferred can appear in the bank advice for external payment.
- The file is generated immediately (synchronous download) — unlike bulk payslip which is async.
- IFSC Code and Bank Account No are sourced from the Employee Master bank details section.
- Beneficiary Name = Employee Name (the system does not support a separate beneficiary name field distinct from employee name).

---

### 7. Zoho Payouts Integration Note

The modal prominently promotes "Payouts by Zoho Payments" as an alternative to manual bank transfer + bank advice file. This is a direct salary disbursement integration where salary is pushed from Zoho Payroll to employee bank accounts without exporting a file.

---

## Screenshots / Files

- `bank-advice-modal.png` — Download Bank Advice modal (format and options visible)
- `bank-advice-format-dropdown.png` — Bank Statement Format dropdown open showing all bank options
- `.playwright-mcp/Payroll-Bank-Statement.xls` — Downloaded Standard Format XLS file

---

## Gaps / Open Questions

- [ ] **Bank-specific format columns:** What columns appear in HDFC, ICICI, Axis formats vs Standard Format? These formats likely have bank-mandated column headers and ordering. Not tested.
- [ ] **"Download as" options:** Only "XLS" was observed as default. Are CSV, PDF, or .txt options available in the "Download as" dropdown? Not enumerated.
- [ ] **Filters button:** What filters are available when "Filters" is clicked (in addition to the "Generate Bank Advice for" field)? Not explored.
- [ ] **Password protection:** When "Protect this file with a password" is checked, what password is assigned to the XLS?
- [ ] **SBI format:** SBI is conspicuously absent from the bank-specific formats. Priya Sharma (EMP002) banks with SBI — she would be covered by Standard Format only.
- [ ] **Amount precision:** All amounts appear as floats (65484.0). Do bank-specific formats use integer or formatted currency?
- [ ] **NEFT/RTGS mode code:** The Standard Format does not include a transaction type field (NEFT/RTGS). Bank-specific formats may include this.
