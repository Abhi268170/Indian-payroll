# Compliance > TDS — Form 24Q

## URL / Navigation Path
- Taxes & Forms > TDS Liabilities: `#/taxes-and-forms/tax-liabilities/pending`
- Taxes & Forms > Challans: `#/taxes-and-forms/tax-payments/unassociated`
- Taxes & Forms > Form 24Q: `#/taxes-and-forms/form24q`
- Form 24Q Detail: `#/taxes-and-forms/form24q/{id}`
- Form 24Q Preferences (read): `#/taxes-and-forms/form24q/{id}/preferences`
- Form 24Q Preferences (edit): `#/taxes-and-forms/form24q/{id}/preferences/edit`

## Purpose
Track TDS (Tax Deducted at Source) liabilities arising from each pay run, record challan payments made to the government, and generate Form 24Q (quarterly TDS return for salary income) as a text file compatible with the NSDL/TRACES FVU tool.

## Feature Flag: Must Be Explicitly Enabled

This entire module is hidden until explicitly activated. A persistent bottom banner in the sidebar reads:
> "Track TDS Liabilities and Generate Form 24Q"
> [View Details] button

Clicking "View Details" opens the "Record Challans and Generate Form 24Q" modal. The modal explains the feature and offers "Enable" / "Cancel". After enabling, the sidebar "Taxes & Forms" accordion expands with 4 items: TDS Liabilities, Challans, Form 24Q, Form 16.

**Critical constraint stated in modal:**
> "You can track TDS challan payments and Form 24Q from April 2026 onwards"

This means: pay runs prior to enabling the feature do NOT generate historical TDS liabilities retroactively. Liabilities only appear for pay runs approved after the feature is enabled.

---

## Section 1: TDS Liabilities

### URL
- Unpaid tab: `#/taxes-and-forms/tax-liabilities/pending`
- Paid tab: `#/taxes-and-forms/tax-liabilities/completed`

### Tabs
| Tab | URL suffix | Purpose |
|-----|------------|---------|
| Unpaid | `/pending` | TDS deducted but challan not yet paid |
| Paid | `/completed?fiscal_year=2026` | TDS where challan has been fully paid |

### Unpaid Tab — Empty State
> "You have no liabilities as of now. Your liabilities will be displayed here after you approve your next pay run. Once your liabilities are displayed here, you'll be able to view the TDS payables details for your liability period."

**Precondition:** TDS liabilities appear only after the next pay run is approved post-feature-enable. Since May 2025 run was completed before enabling, no liabilities show.

### Paid Tab — Fields
| Field | Type | Notes |
|-------|------|-------|
| Period | Dropdown | Select financial period to filter |

### Paid Tab — Empty State
> "The TDS paid details will be displayed here once you completely pay a challan for this period."

---

## Section 2: Challans

### URL
- Unassociated tab: `#/taxes-and-forms/tax-payments/unassociated`
- Associated tab: `#/taxes-and-forms/tax-payments/associated`

### Tabs
| Tab | URL suffix | Purpose |
|-----|------------|---------|
| Unassociated | `/unassociated` | Challan recorded but not yet linked to a TDS liability |
| Associated | `/associated` | Challan payment linked to specific TDS liabilities |

### "Record Challan" Modal (New button)

| Field | Type | Required | Validation / Notes |
|-------|------|----------|-------------------|
| Paid amount | Number (spinner with INR prefix) | Yes | Monetary; amount remitted to govt via bank |
| Add Penalty | Button | No | Expands to reveal Penalty Amount field (INR) |
| Add Interest | Button | No | Expands to reveal Interest Amount field (INR) |
| Challan Number | Text | Yes | BSR challan sequence number from bank |
| BSR Code | Text | Yes | Bank Branch Code (7-digit) from bank receipt |
| Paid Date | Date | Yes | Format: `dd/MM/yyyy`; date of payment at bank |

**Note:** "Add Penalty" and "Add Interest" are optional expandable fields for late payment penalties under Section 234E (penalty INR 200/day) and interest under Section 201(1A) (1.5% per month for non-deduction / 1% per month for late deduction).

### Actions
| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| New | Button | — | Opens "Record Challan" modal |
| Save | Button in modal | All required fields filled | Creates challan record; appears in Unassociated tab |
| Cancel | Button in modal | — | Closes modal; no changes |

### Unassociated Tab — Empty State
> "You have no unassociated Challans as of now. Your Challans will be displayed here if you have payment amount that hasn't been associated to TDS liabilities. You can record a new Challan and associate it's payment amount to TDS liabilities."

### Challan Association Flow
1. Record challan (Unassociated tab)
2. Navigate to TDS Liabilities
3. Link challan to specific liability period/employees
4. Challan moves from Unassociated → Associated

---

## Section 3: Form 24Q

### URL
- List: `#/taxes-and-forms/form24q?tax_year=2027`
- Detail: `#/taxes-and-forms/form24q/{form24q_id}`
- Generate Text File: `#/taxes-and-forms/form24q/{id}/preferences`
- Edit Preferences: `#/taxes-and-forms/form24q/{id}/preferences/edit`

### Form 24Q List Page

| Field | Type | Notes |
|-------|------|-------|
| Tax Year (heading) | Dropdown | Defaults to current FY (e.g., "2026 - 2027") |

**Pending Form 24Q List** — one card per quarter:

| Field | Display |
|-------|---------|
| Form 24Q | Label |
| Due Date | Statutory due date (e.g., 31/07/2026 for Q1) |
| Deposit Period | Quarter date range (e.g., 01/04/2026 - 30/06/2026) |
| View Details | Link → detail page |

**Q1 FY2026-27 observed:**
- Due Date: 31/07/2026
- Deposit Period: 01/04/2026 - 30/06/2026
- Status: Pending

### Form 24Q Detail Page

**Breadcrumb:** Back → Form 24Q → Pending

**Header section:**
| Field | Value |
|-------|-------|
| Deposit Period | 01/04/2026 - 30/06/2026 |
| Due Date | 31/07/2026 |
| Status badge | Pending |

**Warning banner (when pay runs still pending in period):**
> "There are pending Pay Runs for the filing period of 01/04/2026 - 30/06/2026. Complete the Pay Runs for this period to file this form."

**Per-month breakdown (for each month in the quarter):**

| Month | Employees | Total TDS |
|-------|-----------|-----------|
| April 2026 | 2 | INR 0.00 |
| May 2026 | 2 | INR 0.00 |

Empty state per month: "You have no tax liability for this period."

**Reason for INR 0 TDS in test org:** Test employees' annual CTC is below the new regime tax threshold of INR 12 lakh (FY2026 basic exemption). No TDS liability is generated.

**Action:**
- "Generate Text File" link → `#/taxes-and-forms/form24q/{id}/preferences`

### Form 24Q — Generate Text File (Preferences View)

Read-only pre-flight check before generating the FVU text file.

**Note displayed:** "Verify the employer details and responsible person details before generating the text file"

**Section 1: Employer Details**
| Field | Value in Test Org | Notes |
|-------|-------------------|-------|
| Name | lerno | From Org Profile |
| PAN | ABCDE1234F | From Settings > Tax Details |
| Email | - (blank) | Not entered yet |
| Phone Number | - | Not entered |
| STD Code | - | Not entered |
| State | KL | From org address |
| Address | lerno, kazhakoottam, thiruvananthapuram Kerala - 695010 | From org address |
| Has the address changed since the last return? | No | Checkbox (default) |
| GSTIN Number | - | Optional; for GST-registered entities |

**Section 2: Responsible Person Details** (all blank for test org)
| Field | Status |
|-------|--------|
| Name | - (not entered) |
| PAN | - |
| Designation | - |
| Email | - |
| Phone Number | - |
| STD Code | - |
| State | - |
| Address | - |
| Has address changed? | No |

**Actions:**
| Action | Notes |
|--------|-------|
| Generate (button) | Generates FVU-compatible text file for download |
| Edit (link on each section) | Opens edit form to fill missing details |

### Form 24Q — Edit Preferences Form

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Did you file Form 24Q in the previous quarter? | Checkbox | No | First-time filer indicator (no previous return) |
| **Employer Details** | | | |
| Employer Name | Text | Yes | Pre-filled from org |
| Employer PAN | Text | Yes | Placeholder `AAAAA0000A`; pre-filled from Tax Details |
| Employer Mail | Text | Yes | Email of employer |
| STD Code | Text | Yes | ISD/STD phone code |
| Telephone Number | Text | Yes | Employer phone |
| Employer Address (4 lines) | Text (4 lines) | Yes | Multi-line address |
| State | Dropdown | Yes | Indian state |
| City | Text | Yes | City name |
| PIN Code | Text | Yes | 6-digit PIN |
| GSTIN Number | Text | No | Placeholder `00AAAAA0000A0Z0` |
| Has the address changed since the last return? | Checkbox | No | — |
| **Responsible Person Details** | | | |
| Responsible Person Name | Text | Yes | Placeholder "First Name" |
| Responsible Person PAN | Text | Yes | Placeholder `AAAAA0000A` |
| Responsible Person Designation | Text | Yes | Job title |
| Responsible Person Mail | Text | Yes | Email |
| Mobile Number | Text | Yes | 10-digit |
| STD Code | Text | No | Optional phone code |
| Telephone Number | Text | No | Optional landline |
| Responsible Person Address (4 lines) | Text (4 lines) | Yes | |
| State | Dropdown | Yes | |
| City | Text | Yes | |
| PIN Code | Text | Yes | |
| Has address changed? | Checkbox | No | |

**Actions:**
| Action | Notes |
|--------|-------|
| Save and Continue | Saves preferences; navigates back to preferences read view |
| Cancel | Returns to preferences read view without saving |

---

## Form 24Q Filing Workflow (End-to-End)

1. Complete all pay runs for the quarter
2. Record TDS challan payments (Challans > New)
3. Associate challans with TDS liabilities
4. Navigate to Form 24Q > View Details
5. Verify/Edit preferences (Employer + Responsible Person details)
6. Click "Generate Text File"
7. Download the generated `.txt` file (FVU-compatible format)
8. Upload to TRACES portal via FVU tool
9. TRACES generates Form 27A (control sheet) for physical submission or digital acceptance

## File Format
- Output: Text file compatible with NSDL FVU (File Validation Utility)
- Format: Pipe-delimited or fixed-width (NSDL mandated)
- Contains: Annexure I (challan details) + Annexure II (deductee-wise TDS details)
- Annexure II key columns: Employee PAN, Employee Name, TDS amount, Surcharge, Cess, Total Tax

## Government Portal Integration
- **No direct TRACES integration** — file is generated for manual upload
- TRACES portal: https://www.tdscpc.gov.in
- FVU tool: NSDL e-Governance TDS Filing Utility
- Form 27A must be filed physically or digitally alongside the FVU file

## Quarterly Due Dates (Section 200 IT Act)
| Quarter | Period | Due Date |
|---------|--------|---------|
| Q1 | April - June | 31 July |
| Q2 | July - September | 31 October |
| Q3 | October - December | 31 January |
| Q4 | January - March | 31 May (salary TDS) |

## Statutory Rules Referenced
- Section 192 (TDS on salary)
- Section 200 (quarterly return — Form 24Q)
- Section 234E (penalty for late filing: INR 200/day)
- Section 201(1A) (interest on late deduction/payment)
- New tax regime slabs FY2026-27: INR 0-4L (0%), 4-8L (5%), 8-12L (10%), 12-16L (15%), 16-20L (20%), 20-24L (25%), above 24L (30%)
- Basic exemption under new regime: INR 12 lakh (FY2026 Budget — zero tax up to ₹12L after standard deduction ₹75,000)

## Cross-Module Dependencies
- Settings > Tax Details (Employer PAN, TAN, Deductor details)
- Pay Runs (must be approved for TDS liabilities to appear)
- Employees > PAN (deductee PAN in Form 24Q Annexure II)
- Reports > Taxes and Forms > TDS Deduction Summary, Form 24Q report

## Key Observations for Our Build
1. **TDS Liabilities are generated at pay run approval** — our architecture needs to compute TDS per employee per month and store as a TdsLiability entity.
2. **Challan entity**: `challan_number`, `bsr_code`, `paid_date`, `paid_amount`, `penalty_amount`, `interest_amount`, `association_status` (Unassociated/Associated).
3. **Form 24Q is a quarterly aggregate** — our system must aggregate monthly TDS liabilities per employee into quarterly returns.
4. **FVU text file generation** is a non-trivial output — requires exact NSDL format compliance. Build as a dedicated report generation service.
5. **Responsible Person details** are stored separately from org profile — a `TdsDeductorPreferences` entity per quarterly return makes sense.
6. **"Did you file in the previous quarter?" flag** — needed for NSDL's first-time filer indicator in the file header.
7. **No retroactive liabilities** — Zoho's design is: feature enable date determines from when liabilities are tracked. Our build should follow this.
8. **GSTIN on Form 24Q** — optional but should be captured in employer details.
9. **Penalty and Interest fields on Challan** — Section 234E penalty and Section 201(1A) interest must be separately capturable on challan.

## Screenshots
- `screenshots/tds-form24q-enable-modal.png` — Enable modal for TDS/Form 24Q feature
- `screenshots/tds-liabilities-unpaid-empty.png` — TDS Liabilities Unpaid tab (empty state)
- `screenshots/challans-empty-state.png` — Challans list (empty state)
- `screenshots/record-challan-modal.png` — Record Challan modal (all fields visible)
- `screenshots/form24q-list.png` — Form 24Q list (Q1 FY2026-27 visible)
- `screenshots/form24q-detail.png` — Form 24Q detail (April + May breakdown)
- `screenshots/form24q-generate-text-file.png` — Generate Text File preferences view
- `screenshots/form24q-edit-preferences.png` — Edit Preferences form (both sections)
