# Pay Runs > Payslip Preview Within Pay Run

## URL / Navigation Path

Payslip preview opens as a right-side split panel on:
`https://payroll.zoho.in/#/payruns/{id}/summary`

Accessed by:
- Clicking employee name row (in Approved/Paid state → read-only payslip view)
- Clicking "View" in the Payslip column
- Clicking "View Payslip" from the row kebab menu

TDS Sheet modal accessed by:
- Clicking "View" in the TDS Sheet column
- Clicking "View TDS Sheet" from the row kebab menu

## Purpose

In-run preview of the computed payslip for an individual employee. In Draft state, the split panel shows editable variable inputs. After approval (Approved/Paid states), the same split panel becomes read-only and displays the finalised payslip layout.

The TDS Sheet modal shows the full tax computation worksheet as a PDF.

## Payslip Split Panel — Fields (Post-Approval, Read-Only)

| Field | Type | Notes |
|-------|------|-------|
| Employee Name | Read-only | With employee ID |
| Designation | Read-only | From employee profile |
| Department | Read-only | From employee profile |
| Employee ID | Read-only | System-assigned |
| Pay Period | Read-only | e.g., "May 2026" |
| Basic Salary | Read-only ₹ | Prorated if LOP applied |
| HRA | Read-only ₹ | Prorated if LOP applied |
| Fixed Allowance | Read-only ₹ | Prorated if LOP applied |
| One-time Earnings (if any) | Read-only ₹ | Bonus, Commission, etc. |
| Gross Earnings | Read-only ₹ | Sum of all earnings |
| TDS | Read-only ₹ | System-computed or overridden |
| Professional Tax | Read-only ₹ | State-specific PT |
| PF Employee | Read-only ₹ | If PF configured |
| ESI Employee | Read-only ₹ | If ESI configured |
| Total Deductions | Read-only ₹ | Sum of all deductions |
| Net Pay | Read-only ₹ | Gross − Total Deductions |
| Paid Days | Read-only integer | Base Days − LOP Days |
| LOP Days | Read-only integer | As entered |
| Payment Mode | Read-only | Bank Transfer, etc. |
| Bank Account | Read-only | Masked account number |
| UAN | Read-only | PF Universal Account Number |
| PAN | Read-only | Masked (XXXXX1234X format) |

## TDS Sheet Modal — Fields

The TDS Sheet is rendered as a PDF within an `<iframe>` modal. Cannot inspect DOM — content observed visually from screenshot.

**API endpoint:** `/api/v1/employees/{employeeId}/taxworksheet?month={YYYY-MM}&accept=pdf&print=true`

**Content sections (observed):**
- Employee details (name, PAN, designation)
- Annual Projected Gross
- Standard Deduction (₹75,000 for new regime FY2026)
- Taxable Income after Standard Deduction
- New Tax Regime Slab Computation:
  - 0–4L: 0%
  - 4–8L: 5%
  - 8–12L: 10%
  - 12–16L: 15%
  - 16–20L: 20%
  - 20–24L: 25%
  - 24L+: 30%
- Rebate u/s 87A (if applicable — income ≤ ₹7L)
- Surcharge (if applicable)
- Health & Education Cess: 4% on tax
- Total Annual Tax Liability
- Tax per month (annual ÷ 12)
- Prior months' TDS (YTD)
- Remaining months in FY
- Monthly TDS for this and future months

## Buttons & Actions

| Action | Trigger | Pre-condition | Post-behaviour |
|--------|---------|---------------|----------------|
| View (Payslip column) | Click in table | Any state | Opens payslip split panel |
| View TDS Sheet | Click in table | Any state | Opens TDS Sheet PDF modal |
| Close split panel | X button | Panel open | Closes panel; no save action |
| Close TDS modal | X/close button | Modal open | Dismisses modal |
| Download Payslip (from split panel) | Button in panel header | Post-approval | Opens "Download Payslip" dialog |

### Download Payslip Dialog

Appears when clicking Download from within the payslip split panel or row kebab:

> "You can protect the payslip with a password to keep the data secure."

- Checkbox: "Protect this file with a password" (checked by default)
- Password field: appears when checkbox checked (text input)
- Button: **Download** | **Cancel**

When downloaded: PDF file, named with employee name + pay period.

## Conditional Logic

- In Draft state: clicking employee row opens the variable inputs panel (editable mode), NOT the read-only payslip. The read-only payslip view is only available post-approval.
- However: "View Payslip" column button and row kebab "View Payslip" option are available in Draft state too — these open read-only payslip preview even before approval.
- In Approved/Paid state: clicking the employee name row directly opens the read-only payslip panel (the variable inputs panel is no longer available).
- TDS Sheet is always available (Draft/Approved/Paid) via the View button in the TDS Sheet column.
- Password protection checkbox is checked by default — secure by default for salary data.

## Cross-Module Links

- Salary Structure → determines earnings section of payslip
- TDS declarations / regime configuration → feeds TDS Sheet computation
- PF/ESI configuration → determines deductions section of payslip
- PT configuration → determines Professional Tax amount
- Employee bank details → Payment Mode and Bank Account fields

## Key Observations for Our Build

1. **TDS Sheet is PDF-only** — Zoho renders the TDS worksheet as a server-generated PDF in an iframe. This means the tax computation logic is server-side only; it is not re-exposed in the UI as structured data. Our build should render TDS computation as structured HTML + offer PDF export.
2. **PDF iframe blocks DOM inspection** — cannot extract field values from TDS sheet via Playwright. Document the API endpoint: `GET /api/v1/employees/{id}/taxworksheet?month=YYYY-MM&accept=pdf&print=true`. Our API should expose equivalent: `GET /api/payroll-runs/{runId}/employees/{employeeId}/tds-worksheet`.
3. **Password-protected payslip** — default-on protection is a good security pattern. Our payslip PDF generation (Hangfire background job → MinIO storage) should support optional password protection at download time.
4. **Draft payslip preview** — Zoho provides a pre-approval preview of the payslip. Critical for accuracy checking. Our build must support this — compute and display the payslip in Draft state as a preview.
5. **Payslip split panel reuses the same URL** — state change (editable vs read-only) is handled client-side based on run state. Our React component should conditionally render edit vs display mode based on payroll run status.
6. **PAN masking in payslip** — PAN shown as XXXXX1234X on payslip. Full PAN stored encrypted; masked on all display surfaces.

## Screenshots

- `screenshots/56-payslip-view-post-approval.png` — Read-only payslip split panel (post-approval)
- `screenshots/56-tds-sheet-modal.png` — TDS Sheet PDF iframe modal
- `screenshots/62-download-payslip-dialog.png` — Download Payslip dialog with password protection option

---
*Audit session: May 2026 | Pay Run ID: 3848927000000034159*
