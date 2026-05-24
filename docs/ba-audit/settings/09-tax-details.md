# Settings > Tax Details

## URL
`#/settings/taxes`

## Purpose
Captures the organisation's tax registration details required for TDS (Tax Deducted at Source) compliance — PAN, TAN, AO Code, and the responsible Tax Deductor's identity. This data populates Form 24Q (quarterly TDS return) and Form 16 (annual TDS certificate) filings.

## Page Layout
Single form with two sections:
1. **Organisation Tax Details** — PAN, TAN, AO Code, Tax Payment Frequency
2. **Tax Deductor Details** — Type, Name (linked to employee or free text), Father's Name, Designation (for non-employees)

"Instant Helper" button (contextual help) appears in the page header.

## Fields

### Section 1: Organisation Tax Details

| Field | Type | Required | Current Value | Placeholder | Format | Help Text / Tooltip | Validation |
|-------|------|----------|---------------|-------------|--------|---------------------|------------|
| PAN | Text | Yes | ABCDE1234F | AAAAA0000A | `AAAAA0000A` — 5 letters, 4 digits, 1 letter | None | Must match PAN regex: 5 uppercase alpha, 4 digits, 1 uppercase alpha |
| TAN | Text | No | MUMR12345A | AAAA00000A | `AAAA00000A` — 4 letters, 5 digits, 1 letter | None | Standard TAN format validation expected |
| TDS circle / AO Code | Compound field (4 parts) | No | (empty) | AAA / AA / 000 / 00 | 4-part code separated by `/`: Area Code (3 alpha) / AO Type (2 alpha) / Range Code (3 digits) / AO Number (2 digits) | "This number can be obtained from Income tax office or you can login into your Income tax account and navigate to My Profile section to find this number." | Each part individually validated |
| Tax Payment Frequency | Text (disabled/read-only) | N/A | Monthly | — | Non-editable | "The Tax Deducted at Source (TDS) for each month should be paid to the Income Tax Department on or before the 7th of the following month. Only for the month of March, TDS should be deposited on or before the 30th of April." | Not editable — always Monthly |

**Notes on AO Code structure:**
The Assessing Officer (AO) Code is a 4-part identifier assigned by the Income Tax Department:
- Part 1: Area Code (3 uppercase alpha chars) — e.g., `MUM`
- Part 2: AO Type (2 uppercase alpha chars) — e.g., `W`
- Part 3: Range Code (3 digits) — e.g., `012`
- Part 4: AO Number (2 digits) — e.g., `01`
Full format example: `MUM / W / 012 / 01`

**Tax Payment Frequency is always Monthly** — hardcoded to reflect Indian TDS rules (Income Tax Act Sec 200(1)): TDS must be deposited by 7th of the following month (30th April for March quarter).

### Section 2: Tax Deductor Details

| Field | Type | Required | Default | Conditional | Notes |
|-------|------|----------|---------|-------------|-------|
| Deductor's Type | Radio group | No | Employee (selected) | Always visible | Two options: **Employee** / **Non-Employee** |
| Deductor's Name | Dropdown (autocomplete) | No | "Select a Tax Deductor" | When type = Employee: searches employee list. When type = Non-Employee: free-text input | The responsible person who signs/authorises TDS returns |
| Deductor's Father's Name | Text | No | (empty) | When type = Employee: auto-populated from employee record (disabled). When type = Non-Employee: free-text (enabled) | Required for Form 24Q signatory details |
| Deductor's Designation | Text | No | (empty) | Only shown when type = **Non-Employee** | Designation of the non-employee deductor |

**Deductor Type behaviour:**
- **Employee**: Deductor's Name is a searchable dropdown pulling from the employee master. Father's Name is auto-filled from the selected employee's record (read-only). Designation field hidden.
- **Non-Employee**: All three fields (Name, Father's Name, Designation) are free-text inputs. Used when the authorised signatory for TDS is not in the employee master (e.g., external CA or director not on payroll).

## Buttons & Actions

| Button | Label | State | Action |
|--------|-------|-------|--------|
| Instant Helper | Icon button | Always enabled | Opens contextual help for Tax Details page |
| Save | "Save" | Always enabled | Saves Organisation Tax Details and Deductor Details |

## Tabs (if any)
None. Single form page. However, the left nav "Taxes" section shows only "Tax Details" — the Statutory Components (EPF, ESI, PT, LWF) are under a separate "Setup & Configurations" section.

## Conditional Logic

1. **Deductor's Type = Employee**:
   - Deductor's Name: Searchable autocomplete from employee master
   - Deductor's Father's Name: Auto-populated from selected employee (read-only/disabled)
   - Deductor's Designation: Hidden

2. **Deductor's Type = Non-Employee**:
   - Deductor's Name: Free-text input
   - Deductor's Father's Name: Free-text input (enabled)
   - Deductor's Designation: Shown as free-text input

3. **Tax Payment Frequency**: Always disabled/read-only. Hardcoded to "Monthly" per Income Tax Act.

## Cross-Module Impact

| Setting | Impacts |
|---------|---------|
| PAN | Appears on Form 16, Form 24Q, all TDS filings. Required for e-TDS return submission |
| TAN | TDS deducted must be deposited under this TAN. Appears on TDS challans and Form 16 |
| AO Code | Used in e-TDS return (Form 24Q) XML. Required for TRACES filing |
| Tax Deductor Name + Father's Name | Printed on Form 16 Part A as the responsible person who deducted TDS |
| Tax Deductor Designation | Appears on Form 24Q as the authorised signatory details |
| Tax Payment Frequency | Informational only — determines TDS challan due date reminders |

## Statutory References
- **Section 200(1), Income Tax Act 1961** — Employer must deduct and deposit TDS monthly
- **Rule 31A** — Quarterly TDS return (Form 24Q) filing requirement
- **Rule 31** — Form 16 issuance to employees annually
- **TRACES** — IT Dept portal where Form 24Q is filed using TAN + AO Code

## Observations & Notes

1. **PAN field currently has ABCDE1234F** — this is clearly test/dummy data. Real PAN format is `AABCL1234F` (as per mock data). The first character of PAN's 4th group indicates entity type: C=Company, P=Person, H=HUF, etc.
2. **TAN field has MUMR12345A** — a Mumbai-based TAN (MUM prefix). Valid TAN format.
3. **AO Code tooltip** is well-written — directs user to the IT dept portal or their account to find the code. This is helpful because AO code is not widely known.
4. **Tax Payment Frequency locked to Monthly** — this is correct and non-negotiable per Indian TDS rules. No quarterly option (unlike some smaller-country payroll systems).
5. **Deductor as Employee vs Non-Employee** — critical distinction. Many companies have their CFO or HR Head (who is an employee) sign TDS returns. Some use an external CA. Zoho handles both cases well.
6. **Father's Name for deductor** — required on Form 24Q per IT Dept schema. This is a legacy Indian government form requirement.
7. **No Aadhaar field for deductor** — TAN-based identification is sufficient for TDS filings; Aadhaar not required at the company level.
8. For our build: PAN and TAN must be stored encrypted at rest. AO Code should be a structured 4-part value object, not a single string. Tax Deductor entity must reference either an Employee FK or a standalone contact record.

## Screenshots
`docs/ba-audit/settings/screenshots/09-tax-details.png`
